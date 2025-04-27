using Microsoft.Maui.Controls;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;
using MySqlConnector;
using Microsoft.Maui.Storage; // For Preferences
using System.Threading.Tasks;
using System;
using System.Linq; // Required for Sum()
using System.Globalization; // Required for CultureInfo

namespace MauiApp1234.Pages.Dashboard
{
    // Model class for expense items (no changes needed here)
    public class ExpenseItem
    {
        public string Category { get; set; }
        public double Amount { get; set; } // Using double for consistency with original code, though decimal might be better for currency

        public ExpenseItem(string category, double amount)
        {
            Category = category;
            Amount = amount;
        }
    }

    // Model class for income breakdown (Optional but good practice)
    // You might want to fetch this from the DB too later
    public class IncomeSource
    {
        public string AccountName { get; set; }
        public double Amount { get; set; }
        public double Percentage { get; set; } // Calculated based on total income
    }


    public partial class Diagnostics : ContentPage
    {
        // Store fetched income and expenses for calculations
        private decimal _totalIncome = 0;
        private decimal _totalExpenses = 0;
        private long _customerId = 0; // Store customer ID for reuse

        public Diagnostics()
        {
            InitializeComponent();
            // Don't load data directly in the constructor if it involves async operations
            // Use OnAppearing instead
        }

        // Load data when the page appears
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadFinancialData();
        }

        // Combined method to load all financial data
        private async Task LoadFinancialData()
        {
            if (!await GetCustomerIdAsync())
            {
                // Stop if we can't get a valid customer ID
                return;
            }

            // Show loading indicators (optional)
            // ActivityIndicator.IsRunning = true;

            // Create tasks to load income and expenses concurrently
            Task incomeTask = LoadCustomerIncomeAsync();
            Task expenseTask = LoadExpenseDataAsync();

            // Wait for both tasks to complete
            await Task.WhenAll(incomeTask, expenseTask);

            // Calculate and display Net Cash Flow after both income and expenses are loaded
            UpdateNetCashFlow();

            // Hide loading indicators (optional)
            // ActivityIndicator.IsRunning = false;
        }

        // Method to get and validate Customer ID
        private async Task<bool> GetCustomerIdAsync()
        {
            if (Preferences.Default.ContainsKey("customer_id"))
            {
                // Schema confirmation: customer_id in Preferences matches customer.customer_id (bigint)
                string customerIdStr = Preferences.Default.Get("customer_id", string.Empty);

                if (string.IsNullOrWhiteSpace(customerIdStr) || !long.TryParse(customerIdStr, out _customerId))
                {
                    await DisplayAlert("Error", "Invalid Customer ID found. Please Log In again.", "OK");
                    // Clear potentially invalid preference
                    Preferences.Default.Remove("customer_id");
                    _customerId = 0;
                    return false; // Indicate failure
                }
                // _customerId is now set
                return true; // Indicate success
            }
            else
            {
                await DisplayAlert("Error", "Please Log In before proceeding.", "OK");
                _customerId = 0;
                return false; // Indicate failure
            }
        }


        // Updated method to load income
        private async Task LoadCustomerIncomeAsync()
        {
            // Ensure customerId is valid (already checked in LoadFinancialData)
            if (_customerId == 0)
            {
                TotalIncomeLabel.Text = "Login Required";
                _totalIncome = 0;
                return;
            }

            // Database connection string - Consider moving to a configuration file/service
            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm"; // Replace with your actual connection string

            using (var conn = new MySqlConnection(connString))
            {
                try
                {
                    await conn.OpenAsync();
                    // Query to get monthly_income for the specific customer
                    // Schema confirmation: Uses customer.monthly_income (decimal(10,2)) and customer.customer_id (bigint)
                    string sql = "SELECT `monthly_income` FROM `customer` WHERE `customer_id` = @customerId";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@customerId", _customerId);
                        object result = await cmd.ExecuteScalarAsync();

                        if (result != DBNull.Value && result != null && decimal.TryParse(result.ToString(), out _totalIncome))
                        {
                            // Format as currency (e.g., £1,234) - Use CultureInfo for correct formatting
                            TotalIncomeLabel.Text = _totalIncome.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));
                        }
                        else
                        {
                            TotalIncomeLabel.Text = "£0";
                            _totalIncome = 0;
                        }
                    }
                    // TODO: Load Income Breakdown (similar to expenses, but filter for income transactions)
                    // You would query transactions, group by account, and populate IncomeBreakdownLayout
                    // Clear existing hardcoded income breakdown for now
                    IncomeBreakdownLayout.Children.Clear();
                    // Add a placeholder message or load real data
                    IncomeBreakdownLayout.Children.Add(new Label { Text = "Income breakdown data loading not implemented.", TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center });

                }
                catch (Exception ex)
                {
                    TotalIncomeLabel.Text = "Error";
                    _totalIncome = 0;
                    Console.WriteLine($"Error loading income: {ex.Message}");
                    await DisplayAlert("Database Error", $"Failed to load income data: {ex.Message}", "OK");
                }
            }
        }

        // New method to load expense data
        private async Task LoadExpenseDataAsync()
        {
            // Ensure customerId is valid (already checked in LoadFinancialData)
            if (_customerId == 0)
            {
                TotalExpensesLabel.Text = "Login Required";
                _totalExpenses = 0;
                ExpensePieSeries.ItemsSource = null; // Clear chart
                ExpenseBreakdownLayout.Children.Clear(); // Clear list
                return;
            }

            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm"; // Replace with your actual connection string
            var expenseData = new List<ExpenseItem>();
            _totalExpenses = 0; // Reset total expenses before loading

            using (var conn = new MySqlConnection(connString))
            {
                try
                {
                    await conn.OpenAsync();

                    // SQL Query to get expenses grouped by category for the specific customer
                    // Schema Confirmation & Assumptions:
                    // 1. Joins `transaction` (t) and `account` (a) using `account-id` (int) from both tables.
                    // 2. Filters `account` table using `customer-id` (bigint) which links to the logged-in user.
                    // 3. Assumes expenses are identified by `transaction-category` (varchar(14)) being in the specified list.
                    //    Modify the IN (...) clause if other categories represent expenses or if identification is different (e.g., negative amounts).
                    // 4. Uses `ABS()` on `transaction-amount` (decimal(7,2)). This assumes expenses might be stored as negative numbers,
                    //    and we want the positive magnitude for summing and display. If expenses are stored as positive numbers, remove ABS().
                    // 5. Groups results by `transaction-category` to sum up amounts for the chart/list.
                    // 6. Uses backticks (`) for hyphenated column names, which is correct MySQL syntax.
                    string sql = @"
                        SELECT
                            t.`transaction-category` AS Category,
                            SUM(ABS(t.`transaction-amount`)) AS TotalAmount
                        FROM `transaction` t
                        JOIN `account` a ON t.`account-id` = a.`account-id`
                        WHERE a.`customer-id` = @customerId
                          -- TODO: Add date filters based on TimePeriod selection (Week, Month, Year) using t.`transaction-date` (date)
                          -- Example for 'Month' (assuming current month):
                          -- AND YEAR(t.`transaction-date`) = YEAR(CURDATE())
                          -- AND MONTH(t.`transaction-date`) = MONTH(CURDATE())

                          -- Filter for specific expense categories (Modify this list as needed based on your app's logic)
                          AND t.`transaction-category` IN ('Mortgage', 'Utility', 'Food', 'Shopping', 'Leisure', 'Health', 'Transfer', 'Gambling', 'Life Event', 'Monthly fees', 'Withdrawal')
                        GROUP BY t.`transaction-category`
                        HAVING TotalAmount > 0; -- Only include categories with expenses in the selected period
                    ";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@customerId", _customerId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string category = reader["Category"] as string ?? "Unknown";
                                // Use GetDecimal for precision from DB (matches decimal(7,2))
                                decimal amountDecimal = reader.GetDecimal("TotalAmount");
                                // Cast to double for ExpenseItem model (consider changing ExpenseItem.Amount to decimal)
                                double amountDouble = (double)amountDecimal;

                                expenseData.Add(new ExpenseItem(category, amountDouble));
                                // Accumulate total expenses using decimal for accuracy
                                _totalExpenses += amountDecimal;
                            }
                        }
                    }

                    // Update UI on the main thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Update Pie Chart
                        if (ExpensePieSeries != null)
                        {
                            ExpensePieSeries.ItemsSource = expenseData;
                            // Ensure binding paths are set (can also be set in XAML)
                            ExpensePieSeries.XBindingPath = "Category";
                            ExpensePieSeries.YBindingPath = "Amount";
                            // Consider disabling explosion if data is empty or has only one item
                            ExpensePieSeries.ExplodeIndex = expenseData.Count > 1 ? 0 : -1;

                        }

                        // Update Expense Breakdown List
                        PopulateExpenseBreakdown(expenseData);

                        // Update Total Expenses Label
                        TotalExpensesLabel.Text = _totalExpenses.ToString("C0", CultureInfo.GetCultureInfo("en-GB")); // Format as currency
                    });

                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TotalExpensesLabel.Text = "Error";
                        ExpensePieSeries.ItemsSource = null; // Clear chart on error
                        ExpenseBreakdownLayout.Children.Clear(); // Clear list
                        ExpenseBreakdownLayout.Children.Add(new Label { Text = "Error loading expense data.", TextColor = Colors.Red });
                        DisplayAlert("Database Error", $"Failed to load expense data: {ex.Message}", "OK");
                    });
                    _totalExpenses = 0; // Reset on error
                    Console.WriteLine($"Error loading expenses: {ex.Message}");
                }
            }
        }

        // Helper method to dynamically create the expense breakdown UI
        private void PopulateExpenseBreakdown(List<ExpenseItem> expenses)
        {
            ExpenseBreakdownLayout.Children.Clear(); // Clear previous items

            if (expenses == null || !expenses.Any())
            {
                ExpenseBreakdownLayout.Children.Add(new Label { Text = "No expense data for selected period.", HorizontalOptions = LayoutOptions.Center, TextColor = Colors.Gray });
                return;
            }

            // Calculate total for percentage calculation (use double for consistency with ExpenseItem)
            // If ExpenseItem.Amount becomes decimal, use decimal here too.
            double totalAmount = expenses.Sum(e => e.Amount);

            foreach (var item in expenses.OrderByDescending(e => e.Amount)) // Order by amount
            {
                double progress = (totalAmount > 0) ? (item.Amount / totalAmount) : 0;

                var horizontalLayout = new HorizontalStackLayout { Spacing = 10, Padding = new Thickness(0, 5) }; // Add padding

                // Category Label
                horizontalLayout.Children.Add(new Label
                {
                    Text = item.Category,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    WidthRequest = 120, // Match XAML width
                    VerticalOptions = LayoutOptions.Center // Align vertically
                });

                // Progress Bar
                horizontalLayout.Children.Add(new ProgressBar
                {
                    Progress = progress,
                    HeightRequest = 10,
                    WidthRequest = 180, // Adjust width slightly if needed
                    ProgressColor = Color.FromArgb("#6f61ef"), // Match XAML color
                    VerticalOptions = LayoutOptions.Center // Align vertically
                });

                // Amount Label
                horizontalLayout.Children.Add(new Label
                {
                    // Format item amount as currency
                    Text = ((decimal)item.Amount).ToString("C0", CultureInfo.GetCultureInfo("en-GB")),
                    FontSize = 14,
                    TextColor = Colors.Red, // Match XAML color
                    HorizontalOptions = LayoutOptions.EndAndExpand, // Push to the right
                    VerticalOptions = LayoutOptions.Center // Align vertically
                });

                // Trend Indicator (Static for now, could be dynamic later)
                // horizontalLayout.Children.Add(new Label
                // {
                //     Text = "⬇", // Or "⬆" or "➡" based on comparison with previous period
                //     FontSize = 16,
                //     TextColor = Colors.Red, // Or Green or Gray
                //     VerticalOptions = LayoutOptions.Center // Align vertically
                // });

                ExpenseBreakdownLayout.Children.Add(horizontalLayout);
            }
        }


        // Method to update Net Cash Flow
        private void UpdateNetCashFlow()
        {
            MainThread.BeginInvokeOnMainThread(() => // Ensure UI update is on main thread
            {
                decimal netFlow = _totalIncome - _totalExpenses;
                NetCashFlowLabel.Text = netFlow.ToString("C0", CultureInfo.GetCultureInfo("en-GB")); // Format as currency
                NetCashFlowLabel.TextColor = netFlow >= 0 ? Colors.Green : Colors.Red;
            });
        }


        // --- Event Handlers (Keep existing ones, ensure they don't conflict) ---

        // Modify this to reload data based on the selected period
        private async void OnTimePeriodChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value) // Only act when a radio button is checked
            {
                // Identify which button was checked (e.g., by Content or AutomationId)
                var selectedRadioButton = sender as RadioButton;
                string period = selectedRadioButton?.ContentAsString();

                // TODO: Implement filtering logic based on 'period'
                // You'll need to modify the SQL query in LoadExpenseDataAsync
                // (and potentially LoadCustomerIncomeAsync) to include date filters
                // based on t.`transaction-date`.
                // For now, just reload all data as an example.
                await LoadFinancialData();

                // Example: Display selected period (remove later)
                // await DisplayAlert("Period Changed", $"Selected: {period}", "OK");
            }
        }

        private void OnAddNewAccountClicked(object sender, EventArgs e)
        {
            DisplayAlert("Add Account", "Functionality to add a new account is not implemented.", "OK");
        }

        private async void OnCategoryRemoved(object sender, TappedEventArgs e)
        {
            string category = e.Parameter as string;
            // TODO: Implement logic to actually remove/hide the category
            // This might involve:
            // 1. Updating a list of active categories.
            // 2. Re-filtering the displayed data (call LoadExpenseDataAsync or a filtering method).
            // 3. Visually removing the chip (requires managing chips dynamically).
            await DisplayAlert("Remove Category", $"Functionality to remove category '{category}' is not fully implemented.", "OK");
        }

        private void OnAddCategoryTapped(object sender, TappedEventArgs e)
        {
            // Show category picker popup
            if (CategoryPickerPopup != null)
            {
                // TODO: Populate checkboxes based on available vs already selected categories
                CategoryPickerPopup.IsVisible = true;
            }
        }

        private void OnCancelCategorySelection(object sender, EventArgs e)
        {
            // Hide category picker popup
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = false;
            }
        }

        private async void OnAddSelectedCategories(object sender, EventArgs e)
        {
            // TODO: Implement logic to add selected categories
            // 1. Get checked categories from the popup (e.g., SavingCheckbox.IsChecked).
            // 2. Update the list of active categories.
            // 3. Re-filter data (call LoadExpenseDataAsync or a filtering method).
            // 4. Dynamically add chips for the new categories.
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = false;
            }
            await DisplayAlert("Add Categories", "Functionality to add selected categories is not fully implemented.", "OK");
        }

        // Keep this handler
        private void OnViewDetailedReportTapped(object sender, TappedEventArgs e)
        {
            DisplayAlert("Detailed Report", "Functionality to show detailed report is not implemented.", "OK");
        }
    }
}
