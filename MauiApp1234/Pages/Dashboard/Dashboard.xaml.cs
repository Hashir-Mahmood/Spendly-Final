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
using System.Collections.ObjectModel;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;

namespace MauiApp1234
{
    public partial class Dashboard : ContentPage
    {
        private void home_Tapped(object sender, TappedEventArgs e)
        {
            Navigation.PushAsync(new Dashboard());
        }

        private void budget_Tapped(object sender, TappedEventArgs e)
        {
            Navigation.PushAsync(new Budgeting());
        }

        private void investment_Tapped(object sender, TappedEventArgs e)
        {
            Navigation.PushAsync(new Investing());
        }

        private void infohub_Tapped(object sender, TappedEventArgs e)
        {
            Navigation.PushAsync(new InfoHub1());
        }

        private long _customerId = 0;
        private decimal _totalIncome = 0;
        private decimal _totalExpenses = 0;
        private decimal _totalBalance = 10361;
        private List<SpendingCategory> _spendingCategories = new List<SpendingCategory>();

        // Define available categories
        private readonly string[] _availableCategories = {
            "Mortgage", "Utility", "Food", "Shopping", "Leisure",
            "Health", "Transfer", "Gambling", "Life Event",
            "Monthly fees", "Withdrawal"
        };

        // Class to hold spending category data
        public class SpendingCategory
        {
            public string Name { get; set; }
            public decimal CurrentAmount { get; set; }
            public decimal BudgetAmount { get; set; }
            public double Progress => BudgetAmount > 0 ? Math.Min(1.0, (double)(CurrentAmount / BudgetAmount)) : 0;
            public Color ProgressColor => Progress < 0.5 ? Colors.Green : (Progress < 0.8 ? Colors.Orange : Color.FromHex("#6f61ef"));
        }

        public Dashboard()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("Dashboard OnAppearing called");
            await LoadFinancialDataAsync();
            await LoadCategorizedSpendingAsync();
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

        private async Task LoadCategorizedSpendingAsync()
        {
            Debug.WriteLine("Loading categorized spending data");

            // Clear existing UI elements
            CategorizedSpendingContainer.Clear();

            // If no customer ID, abort
            if (_customerId == 0)
            {
                Debug.WriteLine("No customer ID available for categorized spending");
                return;
            }

            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
            DateTime startDate = new DateTime(2024, 12, 1);
            DateTime endDate = new DateTime(2024, 12, 31);

            try
            {
                using (var conn = new MySqlConnection(connString))
                {
                    await conn.OpenAsync();

                    // Get all categories from transactions and their spending totals
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
                    Debug.WriteLine($"Executing SQL: {sql}");

                    Dictionary<string, decimal> categorySpending = new Dictionary<string, decimal>();

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
                                string category = reader.GetString("Category");
                                decimal amount = reader.GetDecimal("TotalAmount");
                                categorySpending[category] = amount;
                            }
                        }
                    }

                    // Now get custom budgets from the spending-budget table (if available)
                    var categoryBudgets = new Dictionary<string, decimal>();

                    sql = @"
                SELECT
                    `category-name` AS Category,
                    `budget-amount` AS BudgetAmount
                FROM `spending-budget`
                WHERE `customer-id` = @customerId
            ";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        // Add parameter securely
                        cmd.Parameters.AddWithValue("@customerId", _customerId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string category = reader.GetString("Category");
                                decimal budget = reader.GetDecimal("BudgetAmount");
                                categoryBudgets[category] = budget;
                            }
                        }
                    }

                    // Create spending categories list
                    _spendingCategories.Clear();

                    // Add categories that have spending
                    foreach (var kvp in categorySpending)
                    {
                        // First check if a custom budget exists for this category
                        decimal budget = 0;
                        if (categoryBudgets.ContainsKey(kvp.Key))
                        {
                            // Use custom budget if available
                            budget = categoryBudgets[kvp.Key];
                        }
                        else
                        {
                            // Default to 120% of the current spending if no custom budget exists
                            budget = kvp.Value * 1.2m;
                        }

                        _spendingCategories.Add(new SpendingCategory
                        {
                            Name = kvp.Key,
                            CurrentAmount = kvp.Value,
                            BudgetAmount = budget
                        });
                    }

                    // Sort by most spending to least
                    _spendingCategories = _spendingCategories.OrderByDescending(c => c.CurrentAmount).ToList();
                }

                // Now populate the UI with the categories
                if (_spendingCategories.Count == 0)
                {
                    // Add demo categories if none found
                    _spendingCategories.Add(new SpendingCategory { Name = "Mortgage", CurrentAmount = 1200, BudgetAmount = 1500 });
                    _spendingCategories.Add(new SpendingCategory { Name = "Food", CurrentAmount = 350, BudgetAmount = 400 });
                    _spendingCategories.Add(new SpendingCategory { Name = "Utility", CurrentAmount = 180, BudgetAmount = 200 });
                    _spendingCategories.Add(new SpendingCategory { Name = "Leisure", CurrentAmount = 120, BudgetAmount = 100 });
                }

                // Add categories to the UI
                foreach (var category in _spendingCategories)
                {
                    // Create horizontal layout for category name and amount
                    var categoryLayout = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        Spacing = 10,
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    // Add category name
                    categoryLayout.Add(new Label
                    {
                        Text = category.Name,
                        FontAttributes = FontAttributes.Bold
                    });

                    // Add amount text (current/budget)
                    categoryLayout.Add(new Label
                    {
                        Text = $"£{category.CurrentAmount:N0} / £{category.BudgetAmount:N0}",
                        HorizontalOptions = LayoutOptions.EndAndExpand,
                        TextColor = category.CurrentAmount > category.BudgetAmount ? Colors.Red : Colors.Black
                    });

                    // Add to container
                    CategorizedSpendingContainer.Add(categoryLayout);

                    // Create progress bar
                    var progressBar = new ProgressBar
                    {
                        Progress = category.Progress,
                        ProgressColor = category.ProgressColor,
                        Margin = new Thickness(0, 0, 0, 20)
                    };
                    CategorizedSpendingContainer.Add(progressBar);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading categorized spending: {ex.Message}");

                // Add a message to the UI about the error
                CategorizedSpendingContainer.Add(new Label
                {
                    Text = "Unable to load spending categories.",
                    TextColor = Colors.Red,
                    HorizontalOptions = LayoutOptions.Center
                });
            }
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
                    TotalBalanceLabel.Text = "10,361";
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
            Navigation.PushAsync(new Diagnostics());
        }

        private async void OnAddCategoryClicked(object sender, EventArgs e)
        {
            // Create a list of categories that aren't already being tracked
            var existingCategories = _spendingCategories.Select(c => c.Name).ToList();
            var availableNewCategories = _availableCategories.Where(c => !existingCategories.Contains(c)).ToList();

            if (availableNewCategories.Count == 0)
            {
                await DisplayAlert("Add Category", "All available categories are already being tracked.", "OK");
                return;
            }

            // Show picker with available categories
            string categoryName = await DisplayActionSheet("Select Category", "Cancel", null, availableNewCategories.ToArray());

            if (string.IsNullOrWhiteSpace(categoryName) || categoryName == "Cancel")
                return;

            string budgetAmountStr = await DisplayPromptAsync("Budget Amount", $"Enter monthly budget for {categoryName}:", "OK", "Cancel", null, -1, Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(budgetAmountStr) || !decimal.TryParse(budgetAmountStr, NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal budgetAmount))
                return;

            // Add to database
            await AddCategoryToDatabase(categoryName, budgetAmount);

            // Reload categories
            await LoadCategorizedSpendingAsync();
        }

        private async Task AddCategoryToDatabase(string categoryName, decimal budgetAmount)
        {
            if (_customerId == 0)
                return;

            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";

            try
            {
                using (var conn = new MySqlConnection(connString))
                {
                    await conn.OpenAsync();

                    string insertSql = @"
                        INSERT INTO `spending-budget` 
                        (`customer-id`, `category-name`, `budget-amount`) 
                        VALUES (@customerId, @categoryName, @budgetAmount)
                        ON DUPLICATE KEY UPDATE `budget-amount` = @budgetAmount";

                    using (var cmd = new MySqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@customerId", _customerId);
                        cmd.Parameters.AddWithValue("@categoryName", categoryName);
                        cmd.Parameters.AddWithValue("@budgetAmount", budgetAmount);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding category: {ex.Message}");
                await DisplayAlert("Error", $"Failed to add category: {ex.Message}", "OK");
            }
        }

        private void OnAddGoalClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new AddGoal()); 
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

    public class GoalViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<GoalItem> _goals;
        public ObservableCollection<GoalItem> Goals
        {
            get => _goals;
            set
            {
                _goals = value;
                OnPropertyChanged(nameof(Goals));
            }
        }

        public ICommand DeleteGoalCommand { get; }
        public ICommand AddGoalCommand { get; }
        public ICommand RefreshGoalsCommand { get; }

        // Use a constant for the table name to ensure consistency
        private const string GOALS_TABLE = "financial_goals";

        // Consider moving the connection string to a secure storage or config file
        private readonly string _connectionString;

        public ObservableCollection<GoalProgressDataModel> GoalProgressData { get; private set; }

        // Add loading state properties
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public GoalViewModel()
        {
            Goals = new ObservableCollection<GoalItem>();
            GoalProgressData = new ObservableCollection<GoalProgressDataModel>();

            // Retrieve connection string from secure storage or config
            _connectionString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";

            // Initialize commands
            DeleteGoalCommand = new Command<int>(async (goalId) => await DeleteGoalAsync(goalId));
            AddGoalCommand = new Command(async () => await AddGoalAsync());
            RefreshGoalsCommand = new Command(async () => await LoadGoalsAsync());

            // Load goals when the ViewModel is created
            LoadGoalsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads goals from the database for the current customer
        /// </summary>
        public async Task LoadGoalsAsync()
        {
            if (IsLoading) return;

            IsLoading = true;

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    await conn.OpenAsync();

                    // Get customer ID from preferences
                    long customerId = GetCurrentCustomerId();
                    if (customerId <= 0)
                    {
                        // Handle case where customer ID is invalid
                        await ShowAlertAsync("Error", "Customer ID not found. Please log in again.", "OK");
                        IsLoading = false;
                        return;
                    }

                    string sql = $"SELECT * FROM `{GOALS_TABLE}` WHERE `customer-id` = @customerId";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@customerId", customerId);
                        using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            ObservableCollection<GoalItem> goalList = new ObservableCollection<GoalItem>();
                            while (await reader.ReadAsync())
                            {
                                GoalItem goal = new GoalItem
                                {
                                    GoalId = reader.GetInt32(0),
                                    CustomerId = reader.GetInt64(reader.GetOrdinal("customer-id")),
                                    Name = reader.GetString(reader.GetOrdinal("GoalName")),
                                    Description = reader.GetString(reader.GetOrdinal("Description")),
                                    Progress = reader.GetDouble(reader.GetOrdinal("Progress")),
                                    RemainingAmount = reader.GetDouble(reader.GetOrdinal("RemainingAmount")),
                                    TargetDate = reader.GetDateTime(reader.GetOrdinal("TargetDate")),
                                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
                                };
                                goalList.Add(goal);
                            }
                            Goals = goalList;
                        }
                    }

                    // Update progress data after loading goals
                    UpdateGoalProgressData();
                }
                catch (MySqlException ex)
                {
                    await HandleDatabaseExceptionAsync("Error loading goals", ex);
                }
                catch (Exception ex)
                {
                    await HandleGeneralExceptionAsync("Error loading goals", ex);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// Updates the goal progress data for visualization
        /// </summary>
        private void UpdateGoalProgressData()
        {
            GoalProgressData.Clear();
            foreach (var goal in Goals)
            {
                GoalProgressData.Add(new GoalProgressDataModel
                {
                    GoalName = goal.Name,
                    Progress = goal.Progress
                });
            }
            OnPropertyChanged(nameof(GoalProgressData));
        }

        /// <summary>
        /// Deletes a goal from the database
        /// </summary>
        private async Task DeleteGoalAsync(int goalId)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Delete",
                "Are you sure you want to delete this goal?",
                "Yes", "No");

            if (!confirm) return;

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    await conn.OpenAsync();

                    // Use the correct table name and column name
                    string sql = $"DELETE FROM `{GOALS_TABLE}` WHERE `goalId` = @goalId";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@goalId", goalId);
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            // Remove goal from local collection
                            var goalToRemove = Goals.FirstOrDefault(g => g.GoalId == goalId);
                            if (goalToRemove != null)
                            {
                                Goals.Remove(goalToRemove);
                                UpdateGoalProgressData();
                            }

                            await ShowAlertAsync("Success", "Goal deleted successfully.", "OK");
                        }
                        else
                        {
                            await ShowAlertAsync("Error", "Goal not found or could not be deleted.", "OK");
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    await HandleDatabaseExceptionAsync("Error deleting goal", ex);
                }
                catch (Exception ex)
                {
                    await HandleGeneralExceptionAsync("Error deleting goal", ex);
                }
            }
        }

        /// <summary>
        /// Navigates to the add goal page
        /// </summary>
        private async Task AddGoalAsync()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new AddGoal());
        }

        #region Helper Methods

        /// <summary>
        /// Gets the current customer ID from preferences
        /// </summary>
        private long GetCurrentCustomerId()
        {
            long customerId = 0;
            if (Preferences.Default.ContainsKey("customer_id"))
            {
                string customerIdString = Preferences.Default.Get("customer_id", "");
                if (!string.IsNullOrWhiteSpace(customerIdString))
                {
                    if (long.TryParse(customerIdString, out long parsedId))
                    {
                        customerId = parsedId;
                    }
                }
            }
            return customerId;
        }

        /// <summary>
        /// Displays an alert to the user
        /// </summary>
        private async Task ShowAlertAsync(string title, string message, string buttonText)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, buttonText);
        }

        /// <summary>
        /// Handles database exceptions
        /// </summary>
        private async Task HandleDatabaseExceptionAsync(string context, MySqlException ex)
        {
            System.Diagnostics.Debug.WriteLine($"MySQL Error ({context}): {ex.Message}");
            await ShowAlertAsync("Database Error", $"{context}: {ex.Message}", "OK");
        }

        /// <summary>
        /// Handles general exceptions
        /// </summary>
        private async Task HandleGeneralExceptionAsync(string context, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error ({context}): {ex.Message}");
            await ShowAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Model class for financial goal items
    /// </summary>
    public class GoalItem
    {
        public int GoalId { get; set; }
        public long CustomerId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Progress { get; set; }
        public double RemainingAmount { get; set; }
        public DateTime TargetDate { get; set; }
        public DateTime CreatedDate { get; set; }

        // Calculated properties for UI display
        public string FormattedTargetDate => TargetDate.ToString("MMM dd, yyyy");
        public double ProgressPercentage => Progress * 100; // If Progress is stored as a decimal (0-1)
        public bool IsCompleted => Progress >= 1.0;
        public string StatusText => IsCompleted ? "Completed" : "In Progress";
        public string RemainingAmountFormatted => RemainingAmount.ToString("C");
    }

    /// <summary>
    /// Model class for goal progress chart data
    /// </summary>
    public class GoalProgressDataModel
    {
        public string GoalName { get; set; }
        public double Progress { get; set; }
        public double ProgressPercentage => Progress * 100;
    }
}