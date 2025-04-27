using Microsoft.Maui.Controls;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;
using MySqlConnector;
using Microsoft.Maui.Storage; // For Preferences
using System.Threading.Tasks;
using System;
using System.Linq; // Required for Sum()
using System.Globalization; // Required for CultureInfo
using System.Text; // Required for StringBuilder
using System.Diagnostics; // For Debug.WriteLine

namespace MauiApp1234.Pages.Dashboard
{
    // Model class for expense items (no changes needed here)
    public class ExpenseItem
    {
        public string Category { get; set; }
        public double Amount { get; set; } // Using double for consistency, consider decimal for currency

        public ExpenseItem(string category, double amount)
        {
            Category = category;
            Amount = amount;
        }
    }

    // Model class for income source (no changes needed here)
    public class IncomeSource
    {
        public string AccountName { get; set; }
        public double Amount { get; set; }
        public double Percentage { get; set; }
    }

    public partial class Diagnostics : ContentPage
    {
        // Store fetched income and expenses for calculations
        private decimal _totalIncome = 0;
        private decimal _totalExpenses = 0;
        private long _customerId = 0; // Store customer ID for reuse

        // --- NEW: Store the currently selected time period ---
        private string _selectedTimePeriod = "Month"; // Default to Month

        public Diagnostics()
        {
            InitializeComponent();
            // Use OnAppearing for async data loading
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Load data using the default or current time period
            await LoadFinancialData();
        }

        // Combined method to load all financial data based on the selected period
        private async Task LoadFinancialData()
        {
            if (!await GetCustomerIdAsync())
            {
                return; // Stop if no valid customer ID
            }

            // Show loading indicators (optional)
            // ActivityIndicator.IsRunning = true;

            // Create tasks to load income and expenses concurrently
            // NOTE: LoadCustomerIncomeAsync currently fetches a static monthly income.
            // It will NOT be filtered by the selected TimePeriod unless you modify it
            // to sum actual income transactions within the date range.
            Task incomeTask = LoadCustomerIncomeAsync();

            // Load expenses based on the _selectedTimePeriod
            Task expenseTask = LoadExpenseDataAsync(); // Pass the period or let it read the field

            // Wait for both tasks to complete
            await Task.WhenAll(incomeTask, expenseTask);

            // Calculate and display Net Cash Flow
            UpdateNetCashFlow();

            // Hide loading indicators (optional)
            // ActivityIndicator.IsRunning = false;
        }

        // Method to get and validate Customer ID (No changes needed here)
        private async Task<bool> GetCustomerIdAsync()
        {
            if (Preferences.Default.ContainsKey("customer_id"))
            {
                string customerIdStr = Preferences.Default.Get("customer_id", string.Empty);
                if (string.IsNullOrWhiteSpace(customerIdStr) || !long.TryParse(customerIdStr, out _customerId))
                {
                    await DisplayAlert("Error", "Invalid Customer ID found. Please Log In again.", "OK");
                    Preferences.Default.Remove("customer_id");
                    _customerId = 0;
                    return false;
                }
                return true;
            }
            else
            {
                await DisplayAlert("Error", "Please Log In before proceeding.", "OK");
                _customerId = 0;
                return false;
            }
        }

        // Updated method to load income (Still loads static monthly income)
        // Consider modifying this later to sum income transactions for the selected period if needed.
        private async Task LoadCustomerIncomeAsync()
        {
            if (_customerId == 0)
            {
                TotalIncomeLabel.Text = "Login Required";
                _totalIncome = 0;
                return;
            }

            // *** Limitation Note ***: This method currently fetches the stored 'monthly_income'
            // It does not filter actual income transactions by the selected Week/Month/Year.
            // To filter income like expenses, you would need to query the 'transaction' table
            // for income categories within the selected date range.

            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm"; // Replace with your actual password

            using (var conn = new MySqlConnection(connString))
            {
                try
                {
                    await conn.OpenAsync();
                    string sql = "SELECT `monthly_income` FROM `customer` WHERE `customer_id` = @customerId";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@customerId", _customerId);
                        object result = await cmd.ExecuteScalarAsync();

                        if (result != DBNull.Value && result != null && decimal.TryParse(result.ToString(), out _totalIncome))
                        {
                            // Using MainThread for UI updates from background task
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                TotalIncomeLabel.Text = _totalIncome.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));
                            });
                        }
                        else
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                TotalIncomeLabel.Text = "£0";
                                _totalIncome = 0;
                            });
                        }
                    }
                    // Clear or update income breakdown (implementation depends on whether you fetch transactions)
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IncomeBreakdownLayout.Children.Clear();
                        IncomeBreakdownLayout.Children.Add(new Label { Text = "Static monthly income shown. Breakdown not implemented.", TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center });
                    });
                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TotalIncomeLabel.Text = "Error";
                        _totalIncome = 0;
                        Console.WriteLine($"Error loading income: {ex.Message}");
                        DisplayAlert("Database Error", $"Failed to load income data: {ex.Message}", "OK");
                    });
                }
            }
        }

        // --- MODIFIED: Load expense data with fixed date for December 2024 ---
        private async Task LoadExpenseDataAsync()
        {
            if (_customerId == 0)
            {
                MainThread.BeginInvokeOnMainThread(() => { // Ensure UI updates are on Main Thread
                    TotalExpensesLabel.Text = "Login Required";
                    _totalExpenses = 0;
                    ExpensePieSeries.ItemsSource = null;
                    ExpenseBreakdownLayout.Children.Clear();
                });
                return;
            }

            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm"; // Replace with your actual password
            var expenseData = new List<ExpenseItem>();
            _totalExpenses = 0; // Reset total expenses

            // --- FIXED DATE RANGE FOR DECEMBER 2024 ---
            DateTime startDate = new DateTime(2024, 12, 1); // December 1st, 2024
            DateTime endDate = new DateTime(2025, 1, 1);  // January 1st, 2025 (exclusive)
            Debug.WriteLine($"Filtering expenses from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd} (exclusive) for December 2024");


            using (var conn = new MySqlConnection(connString))
            {
                try
                {
                    await conn.OpenAsync();

                    // --- Build SQL Query with Date Filters ---
                    var sqlBuilder = new StringBuilder(@"
                        SELECT
                            t.`transaction-category` AS Category,
                            SUM(ABS(t.`transaction-amount`)) AS TotalAmount
                        FROM `transaction` t
                        JOIN `account` a ON t.`account-id` = a.`account-id`
                        WHERE a.`customer-id` = @customerId
                    ");

                    // Append date filtering clause for December 2024
                    sqlBuilder.Append(" AND t.`transaction-date` >= @startDate AND t.`transaction-date` < @endDate ");

                    // Append category filtering clause
                    sqlBuilder.Append(@"
                        AND t.`transaction-category` IN ('Mortgage', 'Utility', 'Food', 'Shopping', 'Leisure', 'Health', 'Transfer', 'Gambling', 'Life Event', 'Monthly fees', 'Withdrawal')
                        GROUP BY t.`transaction-category`
                        HAVING TotalAmount > 0;
                    ");

                    string sql = sqlBuilder.ToString();
                    Debug.WriteLine($"Executing SQL: {sql}"); // For debugging

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        // Add parameters securely
                        cmd.Parameters.AddWithValue("@customerId", _customerId);
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string category = reader["Category"] as string ?? "Unknown";
                                decimal amountDecimal = reader.GetDecimal("TotalAmount");
                                double amountDouble = (double)amountDecimal;

                                expenseData.Add(new ExpenseItem(category, amountDouble));
                                _totalExpenses += amountDecimal;
                            }
                        }
                    }

                    // Update UI on the main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (ExpensePieSeries != null)
                        {
                            ExpensePieSeries.ItemsSource = expenseData;
                            ExpensePieSeries.XBindingPath = "Category";
                            ExpensePieSeries.YBindingPath = "Amount";
                            ExpensePieSeries.ExplodeIndex = expenseData.Count > 1 ? 0 : -1;
                            // Optional: Force chart refresh if needed, though ItemsSource change should handle it
                            // ExpensePieChart.InvalidateChart();
                        }

                        PopulateExpenseBreakdown(expenseData);
                        TotalExpensesLabel.Text = _totalExpenses.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));
                        UpdateNetCashFlow(); // Recalculate net flow after expenses are updated
                    });
                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TotalExpensesLabel.Text = "Error";
                        ExpensePieSeries.ItemsSource = null;
                        ExpenseBreakdownLayout.Children.Clear();
                        ExpenseBreakdownLayout.Children.Add(new Label { Text = "Error loading expense data.", TextColor = Colors.Red });
                        DisplayAlert("Database Error", $"Failed to load expense data for December 2024: {ex.Message}", "OK");
                        _totalExpenses = 0; // Reset on error
                        UpdateNetCashFlow(); // Update net flow even on error
                    });
                    Console.WriteLine($"Error loading expenses: {ex.Message}");
                }
            }
        }

        // Helper method to populate expense breakdown UI (No changes needed here)
        private void PopulateExpenseBreakdown(List<ExpenseItem> expenses)
        {
            ExpenseBreakdownLayout.Children.Clear(); // Clear previous items

            if (expenses == null || !expenses.Any())
            {
                ExpenseBreakdownLayout.Children.Add(new Label { Text = $"No expense data for selected period ({_selectedTimePeriod}).", HorizontalOptions = LayoutOptions.Center, TextColor = Colors.Gray });
                return;
            }

            double totalAmount = expenses.Sum(e => e.Amount);

            foreach (var item in expenses.OrderByDescending(e => e.Amount)) // Order by amount
            {
                double progress = (totalAmount > 0) ? (item.Amount / totalAmount) : 0;
                var horizontalLayout = new HorizontalStackLayout { Spacing = 10, Padding = new Thickness(0, 5) };

                // Category Label
                horizontalLayout.Children.Add(new Label { Text = item.Category, FontSize = 14, FontAttributes = FontAttributes.Bold, WidthRequest = 120, VerticalOptions = LayoutOptions.Center });
                // Progress Bar
                horizontalLayout.Children.Add(new ProgressBar { Progress = progress, HeightRequest = 10, WidthRequest = 180, ProgressColor = Color.FromArgb("#6f61ef"), VerticalOptions = LayoutOptions.Center });
                // Amount Label
                horizontalLayout.Children.Add(new Label { Text = ((decimal)item.Amount).ToString("C0", CultureInfo.GetCultureInfo("en-GB")), FontSize = 14, TextColor = Colors.Red, HorizontalOptions = LayoutOptions.EndAndExpand, VerticalOptions = LayoutOptions.Center });
                // Trend Indicator (Static placeholder)
                // horizontalLayout.Children.Add(new Label { Text = "⬇", FontSize = 16, TextColor = Colors.Red, VerticalOptions = LayoutOptions.Center });

                ExpenseBreakdownLayout.Children.Add(horizontalLayout);
            }
        }


        // Method to update Net Cash Flow (No changes needed here)
        private void UpdateNetCashFlow()
        {
            // This method is now called after both income and expenses are potentially updated
            // and also within the BeginInvokeOnMainThread blocks of the loading methods.
            MainThread.BeginInvokeOnMainThread(() => // Ensure UI update is on main thread
            {
                decimal netFlow = _totalIncome - _totalExpenses;
                NetCashFlowLabel.Text = netFlow.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));
                NetCashFlowLabel.TextColor = netFlow >= 0 ? Colors.Green : Colors.Red;
            });
        }


        // --- Event Handlers ---

        // --- MODIFIED: Update selected period and reload data ---
        private async void OnTimePeriodChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value && sender is RadioButton selectedRadioButton) // Act only when checked
            {
                // Use Content or assign AutomationId/StyleId in XAML for reliability
                string newlySelectedPeriod = selectedRadioButton.ContentAsString(); // Example: "Week", "Month", "Year"

                // Only reload if the period actually changed
                if (!string.IsNullOrEmpty(newlySelectedPeriod) && newlySelectedPeriod != _selectedTimePeriod)
                {
                    _selectedTimePeriod = newlySelectedPeriod;
                    Debug.WriteLine($"Time Period Changed. New Period: {_selectedTimePeriod}");

                    // Reload all financial data based on the new period
                    // LoadFinancialData handles fetching both income (static) and expenses (filtered)
                    await LoadFinancialData();
                }
            }
        }

        // Other event handlers (no changes needed for date filtering logic)
        private void OnAddNewAccountClicked(object sender, EventArgs e)
        {
            DisplayAlert("Add Account", "Functionality to add a new account is not implemented.", "OK");
        }

        private async void OnCategoryRemoved(object sender, TappedEventArgs e)
        {
            string category = e.Parameter as string;
            await DisplayAlert("Remove Category", $"Functionality to remove category '{category}' is not fully implemented.", "OK");
        }

        private void OnAddCategoryTapped(object sender, TappedEventArgs e)
        {
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = true;
            }
        }

        private void OnCancelCategorySelection(object sender, EventArgs e)
        {
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = false;
            }
        }

        private async void OnAddSelectedCategories(object sender, EventArgs e)
        {
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = false;
            }
            await DisplayAlert("Add Categories", "Functionality to add selected categories is not fully implemented.", "OK");
        }

        private void OnViewDetailedReportTapped(object sender, TappedEventArgs e)
        {
            DisplayAlert("Detailed Report", "Functionality to show detailed report is not implemented.", "OK");
        }
    }
}