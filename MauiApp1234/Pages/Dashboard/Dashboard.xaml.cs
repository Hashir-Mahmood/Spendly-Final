using System;
using System.Globalization;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using MySqlConnector;
using Syncfusion.Maui.Gauges;
using Microsoft.Maui.Storage; // For Preferences
using MauiApp1234.Pages.AI;
using MauiApp1234.Pages.Dashboard;
using MauiApp1234.Pages.Settings;

namespace MauiApp1234
{
    public partial class Dashboard : ContentPage
    {
        private long _customerId = 0;
        private decimal _totalIncome = 0;
        private decimal _totalExpenses = 0;
        private decimal _totalBalance = 0;

        public Dashboard()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("Dashboard OnAppearing called");
            await LoadFinancialDataAsync();
        }

        private async Task LoadFinancialDataAsync()
        {
            Debug.WriteLine("Starting to load financial data");

            // Get customer ID first, abort if not available
            if (!await GetCustomerIdAsync())
            {
                Debug.WriteLine("Customer ID not available, aborting data load");
                UpdateHealthMeter(); // Still update meter to show 0/no data
                return;
            }

            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";

            try
            {
                using (var conn = new MySqlConnection(connString))
                {
                    await conn.OpenAsync();
                    Debug.WriteLine("Database connection successful");

                    // Set fixed date for December 2024
                    DateTime startDate = new DateTime(2024, 12, 1);
                    DateTime endDate = new DateTime(2024, 12, 31);

                    Debug.WriteLine($"Using fixed date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                    // First, get total balance across all accounts
                    await LoadTotalBalanceAsync(conn);

                    // Calculate total income (including salary and other positive transactions)
                    string incomeSql = @"
                        SELECT COALESCE(c.monthly_income, 0)  AS TotalIncome
                        FROM customer c
                        LEFT JOIN (
                            SELECT a.`customer-id`, SUM(t.`transaction-amount`) AS total_income
                            FROM `transaction` t
                            JOIN `account` a ON t.`account-id` = a.`account-id`
                            WHERE a.`customer-id` = @customerId
                            AND t.`transaction-date` BETWEEN @startDate AND @endDate
                            AND t.`transaction-amount` > 0
                            GROUP BY a.`customer-id`
                        ) t ON c.customer_id = t.`customer-id`
                        WHERE c.customer_id = @customerId";

                    using (MySqlCommand incomeCmd = new MySqlCommand(incomeSql, conn))
                    {
                        incomeCmd.Parameters.AddWithValue("@customerId", _customerId);
                        incomeCmd.Parameters.AddWithValue("@startDate", startDate);
                        incomeCmd.Parameters.AddWithValue("@endDate", endDate);

                        object incomeResult = await incomeCmd.ExecuteScalarAsync();

                        if (incomeResult != DBNull.Value && incomeResult != null)
                        {
                            _totalIncome = Convert.ToDecimal(incomeResult);
                            Debug.WriteLine($"Total Income: {_totalIncome}");
                        }
                        else
                        {
                            _totalIncome = 0;
                            Debug.WriteLine("Could not load income data or income is zero.");
                        }
                    }

                    // Get total expenses (negative transactions)
                    string expensesSql = @"
                        SELECT COALESCE(SUM(ABS(t.`transaction-amount`)), 0) AS TotalExpenses
                        FROM `transaction` t
                        JOIN `account` a ON t.`account-id` = a.`account-id`
                        WHERE a.`customer-id` = @customerId
                        AND t.`transaction-date` BETWEEN @startDate AND @endDate
                        AND t.`transaction-amount` < 0";

                    using (MySqlCommand expensesCmd = new MySqlCommand(expensesSql, conn))
                    {
                        expensesCmd.Parameters.AddWithValue("@customerId", _customerId);
                        expensesCmd.Parameters.AddWithValue("@startDate", startDate);
                        expensesCmd.Parameters.AddWithValue("@endDate", endDate);

                        object expensesResult = await expensesCmd.ExecuteScalarAsync();

                        if (expensesResult != DBNull.Value && expensesResult != null)
                        {
                            _totalExpenses = Convert.ToDecimal(expensesResult);
                            Debug.WriteLine($"Total Expenses: {_totalExpenses}");
                        }
                        else
                        {
                            _totalExpenses = 0;
                            Debug.WriteLine("No expenses found for the current month.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading financial data: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                if (this.Window != null)
                {
                    await DisplayAlert("Database Error", $"Failed to load financial data: {ex.Message}", "OK");
                }
            }

            // Always update the meter, even if there was an error
            UpdateHealthMeter();
            UpdateTotalBalanceDisplay();
        }

        private async Task LoadTotalBalanceAsync(MySqlConnection conn)
        {
            try
            {
                string balanceSql = @"
                    SELECT COALESCE(SUM(a.`account-balance`), 0) AS TotalBalance
                    FROM `account` a
                    WHERE a.`customer-id` = @customerId";

                using (MySqlCommand balanceCmd = new MySqlCommand(balanceSql, conn))
                {
                    balanceCmd.Parameters.AddWithValue("@customerId", _customerId);

                    object balanceResult = await balanceCmd.ExecuteScalarAsync();

                    if (balanceResult != DBNull.Value && balanceResult != null)
                    {
                        _totalBalance = Convert.ToDecimal(balanceResult);
                        Debug.WriteLine($"Total Balance: {_totalBalance}");
                    }
                    else
                    {
                        _totalBalance = 0;
                        Debug.WriteLine("Could not load balance data or balance is zero.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading total balance: {ex.Message}");
                _totalBalance = 0;
            }
        }

        private void UpdateTotalBalanceDisplay()
        {
            MainThread.BeginInvokeOnMainThread(() => {
                if (TotalBalanceLabel != null)
                {
                    TotalBalanceLabel.Text = $"£{_totalBalance:N2}";
                }
            });
        }

        // Get customer ID from preferences
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

        private void UpdateHealthMeter()
        {
            Debug.WriteLine($"Updating health meter with Income: {_totalIncome}, Expenses: {_totalExpenses}");

            MainThread.BeginInvokeOnMainThread(() => {
                if (_totalIncome > 0)
                {
                    // Calculate percentage of income spent
                    decimal spentPercentage = (_totalExpenses / _totalIncome) * 100;

                    // Calculate health score (100 - spent percentage, with minimum of 0)
                    // Lower spending = higher health score
                    int healthScore = Math.Max(0, 100 - (int)Math.Round(spentPercentage));

                    Debug.WriteLine($"Spent: {spentPercentage}%, Health Score: {healthScore}%");

                    // Update needle position
                    if (HealthMeterNeedle != null)
                    {
                        HealthMeterNeedle.Value = healthScore;
                    }

                    // Update text and color
                    if (HealthMeterPercentageLabel != null)
                    {
                        HealthMeterPercentageLabel.Text = $"{healthScore}% financial health";

                        // Set color based on health score
                        if (healthScore < 33)
                        {
                            HealthMeterPercentageLabel.TextColor = Colors.Red; // Critical
                        }
                        else if (healthScore < 67)
                        {
                            HealthMeterPercentageLabel.TextColor = Colors.Orange; // Warning
                        }
                        else
                        {
                            HealthMeterPercentageLabel.TextColor = Colors.Green; // Good
                        }
                    }

                   
                }
                else
                {
                    // No income data
                    if (HealthMeterNeedle != null)
                    {
                        HealthMeterNeedle.Value = 0;
                    }

                    if (HealthMeterPercentageLabel != null)
                    {
                        HealthMeterPercentageLabel.Text = "No income data";
                        HealthMeterPercentageLabel.TextColor = Colors.Gray;
                    }

                    
                }
            });
        }

        #region Event Handlers

        private void ChatbotIcon_Tapped(object sender, TappedEventArgs e)
        {
            Navigation.PushAsync(new Ai());
        }

        private void OnNotificationsIconTapped(object sender, TappedEventArgs e)
        {
            Navigation.PushAsync(new notifications());
        }

        private void SettingsIcon_Tapped(object sender, TappedEventArgs e)
        {
            Navigation.PushAsync(new Settings());
        }

        private void OnDiagnosticsLabelTapped(object sender, TappedEventArgs e)
        {
            // Navigate to financial diagnostics page
            Navigation.PushAsync(new Diagnostics());
        }

        private void OnCategorizedSpendingTapped(object sender, TappedEventArgs e)
        {
            // Navigate to categorized spending page (if implemented)
            DisplayAlert("Coming Soon", "Detailed spending categories will be available soon.", "OK");
        }

        private void OnAddCategoryClicked(object sender, EventArgs e)
        {
            DisplayAlert("Add Category", "Feature to add custom spending categories coming soon.", "OK");
        }

        private void OnAddGoalClicked(object sender, EventArgs e)
        {
            DisplayAlert("Add Goal", "Feature to add new financial goals coming soon.", "OK");
        }

        private void OnEditGoalTapped(object sender, TappedEventArgs e)
        {
            DisplayAlert("Edit Goal", "Feature to edit financial goals coming soon.", "OK");
        }

        private void OnViewAllGoalsTapped(object sender, TappedEventArgs e)
        {
            DisplayAlert("View Goals", "Feature to view all financial goals coming soon.", "OK");
        }

        #endregion
    }
}