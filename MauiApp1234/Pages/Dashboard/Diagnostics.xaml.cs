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

        // Store the currently selected time period
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
            Task incomeTask = LoadCustomerIncomeAsync();

            // Load expenses based on the _selectedTimePeriod
            Task expenseTask = LoadExpenseDataAsync();

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
        private async Task LoadCustomerIncomeAsync()
        {
            if (_customerId == 0)
            {
                TotalIncomeLabel.Text = "Login Required";
                _totalIncome = 0;
                return;
            }

            // *** Limitation Note ***: Fetches stored 'monthly_income', not filtered by date.
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

                            switch (_selectedTimePeriod)
                            {
                                case "Week":
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        TotalIncomeLabel.Text = _totalIncome.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));
                                    });
                                    break;

                                case "Year":
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                       _totalIncome = _totalIncome * 12;
                                        TotalIncomeLabel.Text = _totalIncome.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));
                                    });
                                    break;

                                case "Month":
                                default:
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        TotalIncomeLabel.Text = _totalIncome.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));
                                    });
                                    break;
                            }
                           
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
                        // Check if the page is still visible before displaying the alert
                        if (this.Window != null)
                        {
                            DisplayAlert("Database Error", $"Failed to load income data: {ex.Message}", "OK");
                        }
                    });
                }
            }
        }

        // --- MODIFIED: Load expense data with FIXED date filtering for Dec 2024 / Year 2024 ---
        private async Task LoadExpenseDataAsync()
        {
            if (_customerId == 0)
            {
                MainThread.BeginInvokeOnMainThread(() => { // Ensure UI updates are on Main Thread
                    TotalExpensesLabel.Text = "Login Required";
                    _totalExpenses = 0;
                    if (ExpensePieSeries != null) ExpensePieSeries.ItemsSource = null; // Null check
                    if (ExpenseBreakdownLayout != null) ExpenseBreakdownLayout.Children.Clear(); // Null check
                });
                return;
            }

            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm"; // Replace with your actual password
            var expenseData = new List<ExpenseItem>();
            _totalExpenses = 0; // Reset total expenses

            // --- Calculate FIXED Date Range based on Dec 2024 / Year 2024 ---
            DateTime startDate;
            DateTime endDate;
            int targetYear = 2024;
            int targetMonth = 12; // December

            switch (_selectedTimePeriod)
            {
                case "Week":
                    // First week of December 2024 (Dec 1st to Dec 7th inclusive)
                    startDate = new DateTime(targetYear, targetMonth, 1); // Dec 1st, 2024 00:00:00
                    endDate = startDate.AddDays(7);                      // Dec 8th, 2024 00:00:00 (exclusive)
                    break;

                case "Year":
                    // Entire year 2024 (Jan 1st 2024 to Dec 31st 2024 inclusive)
                    startDate = new DateTime(targetYear, 1, 1);     // Jan 1st, 2024 00:00:00
                    endDate = startDate.AddYears(1);                // Jan 1st, 2025 00:00:00 (exclusive)
                    break;

                case "Month":
                default: // Default to Month (December 2024)
                    // Entire month of December 2024 (Dec 1st to Dec 31st inclusive)
                    startDate = new DateTime(targetYear, targetMonth, 1); // Dec 1st, 2024 00:00:00
                    endDate = startDate.AddMonths(1);                 // Jan 1st, 2025 00:00:00 (exclusive)
                    break;
            }
            Debug.WriteLine($"Filtering expenses for period '{_selectedTimePeriod}' from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd} (exclusive)");


            using (var conn = new MySqlConnection(connString))
            {
                try
                {
                    await conn.OpenAsync();

                    // Build SQL Query with Date Filters
                    var sqlBuilder = new StringBuilder(@"
                        SELECT
                            t.`transaction-category` AS Category,
                            SUM(ABS(t.`transaction-amount`)) AS TotalAmount
                        FROM `transaction` t
                        JOIN `account` a ON t.`account-id` = a.`account-id`
                        WHERE a.`customer-id` = @customerId
                    ");

                    // Append date filtering clause
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
                        // Add null checks for UI elements before accessing them
                        if (ExpensePieSeries != null)
                        {
                            ExpensePieSeries.ItemsSource = expenseData;
                            ExpensePieSeries.XBindingPath = "Category";
                            ExpensePieSeries.YBindingPath = "Amount";
                            ExpensePieSeries.ExplodeIndex = expenseData.Count > 1 ? 0 : -1;
                        }

                        if (ExpenseBreakdownLayout != null)
                        {
                            PopulateExpenseBreakdown(expenseData);
                        }

                        if (TotalExpensesLabel != null)
                        {
                            TotalExpensesLabel.Text = _totalExpenses.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));
                        }
                        UpdateNetCashFlow(); // Recalculate net flow after expenses are updated
                    });
                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Add null checks for UI elements
                        if (TotalExpensesLabel != null) TotalExpensesLabel.Text = "Error";
                        if (ExpensePieSeries != null) ExpensePieSeries.ItemsSource = null;
                        if (ExpenseBreakdownLayout != null)
                        {
                            ExpenseBreakdownLayout.Children.Clear();
                            ExpenseBreakdownLayout.Children.Add(new Label { Text = "Error loading expense data.", TextColor = Colors.Red });
                        }
                        _totalExpenses = 0; // Reset on error
                        UpdateNetCashFlow(); // Update net flow even on error

                        // Check if the page is still visible before displaying the alert
                        if (this.Window != null)
                        {
                            DisplayAlert("Database Error", $"Failed to load expense data for {_selectedTimePeriod}: {ex.Message}", "OK");
                        }
                    });
                    Console.WriteLine($"Error loading expenses: {ex.Message}");
                }
            }
        }

        // Helper method to populate expense breakdown UI (No changes needed here)
        private void PopulateExpenseBreakdown(List<ExpenseItem> expenses)
        {
            // Add null check for the layout
            if (ExpenseBreakdownLayout == null) return;

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
                horizontalLayout.Children.Add(new Label { Text = item.Category, FontSize = 14, FontAttributes = FontAttributes.Bold, WidthRequest = 100, VerticalOptions = LayoutOptions.Center });
                // Progress Bar
                horizontalLayout.Children.Add(new ProgressBar { Progress = progress, HeightRequest = 10, WidthRequest = 150, ProgressColor = Color.FromArgb("#6f61ef"), VerticalOptions = LayoutOptions.Center });
                // Amount Label
                horizontalLayout.Children.Add(new Label { Text = ((decimal)item.Amount).ToString("C0", CultureInfo.GetCultureInfo("en-GB")), FontSize = 14, TextColor = Colors.Black, HorizontalOptions = LayoutOptions.EndAndExpand, VerticalOptions = LayoutOptions.Center });

                ExpenseBreakdownLayout.Children.Add(horizontalLayout);
            }
        }


        // Method to update Net Cash Flow (Added null check)
        private void UpdateNetCashFlow()
        {
            MainThread.BeginInvokeOnMainThread(() => // Ensure UI update is on main thread
            {
                // Add null check for the label
                if (NetCashFlowLabel == null) return;

                decimal netFlow = _totalIncome - _totalExpenses;
                NetCashFlowLabel.Text = netFlow.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));
                NetCashFlowLabel.TextColor = netFlow >= 0 ? Colors.Green : Colors.Red;
            });
        }


        // --- Event Handlers ---

        // MODIFIED: Update selected period and reload data
        private async void OnTimePeriodChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value && sender is RadioButton selectedRadioButton) // Act only when checked
            {
                string newlySelectedPeriod = selectedRadioButton.ContentAsString();

                if (!string.IsNullOrEmpty(newlySelectedPeriod) && newlySelectedPeriod != _selectedTimePeriod)
                {
                    _selectedTimePeriod = newlySelectedPeriod;
                    Debug.WriteLine($"Time Period Changed. New Period: {_selectedTimePeriod}");

                    // Reload all financial data based on the new period
                    await LoadFinancialData();
                }
            }
        }

        // Other event handlers (Added null checks for safety)
        private void OnAddNewAccountClicked(object sender, EventArgs e)
        {
            if (this.Window != null) DisplayAlert("Add Account", "Functionality to add a new account is not implemented.", "OK");
        }

        private async void OnCategoryRemoved(object sender, TappedEventArgs e)
        {
            string category = e.Parameter as string;
            if (this.Window != null) await DisplayAlert("Remove Category", $"Functionality to remove category '{category}' is not fully implemented.", "OK");
        }

        private void OnAddCategoryTapped(object sender, TappedEventArgs e)
        {
            // Assuming CategoryPickerPopup is defined in XAML
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = true;
            }
        }

        private void OnCancelCategorySelection(object sender, EventArgs e)
        {
            // Assuming CategoryPickerPopup is defined in XAML
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = false;
            }
        }

        private async void OnAddSelectedCategories(object sender, EventArgs e)
        {
            // Assuming CategoryPickerPopup is defined in XAML
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = false;
            }
            if (this.Window != null) await DisplayAlert("Add Categories", "Functionality to add selected categories is not fully implemented.", "OK");
        }

        private void OnViewDetailedReportTapped(object sender, TappedEventArgs e)
        {
            if (this.Window != null) DisplayAlert("Detailed Report", "Functionality to show detailed report is not implemented.", "OK");
        }

        private void ReturnButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        // Add this line if CategoryPickerPopup is defined in XAML code-behind
        // If it's purely in XAML with x:Name, this isn't strictly needed here
        // but ensure it's accessible (e.g., public or internal field in the partial class)
        // Example assuming it's a ContentView named CategoryPickerPopup in XAML:
        // public ContentView CategoryPickerPopup { get; set; } // Adjust type if needed
    }
}


