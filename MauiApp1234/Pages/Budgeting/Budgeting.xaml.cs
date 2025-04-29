using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MauiApp1234.Pages.AI;
using MauiApp1234.Pages.Dashboard; // For navigating back to Dashboard if needed
using MauiApp1234.Pages.Settings;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // For Preferences
using MySqlConnector;

namespace MauiApp1234
{
    public partial class Budgeting : ContentPage
    {
        // --- Member Fields ---
        private long _customerId = 0;
        // Connection string (NOT RECOMMENDED FOR PRODUCTION)
        private readonly string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=YOUR_PASSWORD;database=b66855mm"; // Replace YOUR_PASSWORD

        // Data storage
        private List<SpendingCategoryWithTransactions> _spendingCategories = new List<SpendingCategoryWithTransactions>();
        private decimal _totalMonthlyBudget = 0m; // Example: Fetch this if you have an overall budget
        private decimal _totalMonthlySpending = 0m;

        // --- Data Classes ---
        public class TransactionDetail
        {
            public DateTime Date { get; set; }
            public string Reference { get; set; }
            public decimal Amount { get; set; }
            public string DisplayText => $"{Date:dd MMM}: {Reference} (£{Amount:N2})";
        }

        public class SpendingCategoryWithTransactions
        {
            public string Name { get; set; }
            public decimal TotalSpent { get; set; }
            public decimal BudgetAmount { get; set; } // Add budget if needed for display
            public List<TransactionDetail> Transactions { get; set; } = new List<TransactionDetail>();
            public string Icon { get; set; } // Emoji icon based on category
            public string DisplayText => $"£{TotalSpent:N2} spent"; // Simplified display for header
        }

        // --- Constructor ---
        public Budgeting()
        {
            InitializeComponent();
        }

        // --- Page Lifecycle ---
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("Budgeting Page OnAppearing");
            await LoadDataAsync(); // Changed LoadData to be async
        }

        // --- Data Loading ---
        private async Task LoadDataAsync()
        {
            SetLoadingState(true);
            _spendingCategories.Clear(); // Clear previous data
            _totalMonthlySpending = 0m;
            // Fetch Customer ID (Essential)
            if (!await GetCustomerIdAsync())
            {
                Debug.WriteLine("Customer ID not available for Budgeting page.");
                await DisplayAlert("Error", "Could not retrieve customer information. Please log in again.", "OK");
                SetLoadingState(false);
                // Optionally navigate back to login or dashboard
                // await Navigation.PopAsync();
                return;
            }

            // Define Fixed Date Range (December 2024)
            DateTime startDate = new DateTime(2024, 12, 1);
            DateTime endDate = new DateTime(2024, 12, 31);

            try
            {
                using (var conn = new MySqlConnection(connString))
                {
                    await conn.OpenAsync();
                    Debug.WriteLine("Database connection successful for Budgeting page.");

                    // 1. Fetch all expense transactions for the period
                    var allTransactions = await FetchTransactionsAsync(conn, startDate, endDate);

                    // 2. Fetch budget amounts (optional, but good for context)
                    var categoryBudgets = await FetchBudgetsAsync(conn);

                    // 3. Group transactions by category and calculate totals
                    GroupTransactionsAndCreateCategories(allTransactions, categoryBudgets);

                    // 4. Calculate overall monthly spending (sum of all category totals)
                    _totalMonthlySpending = _spendingCategories.Sum(c => c.TotalSpent);

                    // 5. Fetch/Calculate Total Monthly Budget (Example - needs implementation)
                    _totalMonthlyBudget = await FetchTotalMonthlyBudgetAsync(conn); // Placeholder

                } // Connection closed automatically

                // 6. Update UI elements
                UpdateMonthlyBudgetCard();
                PopulateSpendingCategoriesUI(); // This will now build the expandable UI

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading budgeting data: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                await DisplayAlert("Database Error", $"Failed to load budgeting details: {ex.Message}", "OK");
                // Clear UI on error
                MainThread.BeginInvokeOnMainThread(() => SpendingCategoriesContainer.Clear());
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task<List<TransactionDetail>> FetchTransactionsAsync(MySqlConnection conn, DateTime startDate, DateTime endDate)
        {
            var transactions = new List<TransactionDetail>();
            string sql = @"
                SELECT t.`transaction-date`, ABS(t.`transaction-amount`) AS Amount, t.`transaction-category`, t.`transaction-reference`
                FROM `transaction` t
                JOIN `account` a ON t.`account-id` = a.`account-id`
                WHERE a.`customer-id` = @customerId
                  AND t.`transaction-date` BETWEEN @startDate AND @endDate
                  AND t.`transaction-amount` < 0  -- Only expenses
                ORDER BY t.`transaction-category`, t.`transaction-date` DESC"; // Order for grouping and display

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@customerId", _customerId);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Check for DBNull before accessing potentially null columns like reference
                        string reference = reader.IsDBNull(reader.GetOrdinal("transaction-reference"))
                                           ? "N/A"
                                           : reader.GetString("transaction-reference");

                        transactions.Add(new TransactionDetail
                        {
                            Date = reader.GetDateTime("transaction-date"),
                            Amount = reader.GetDecimal("Amount"), // Already ABS() in SQL
                            Reference = reference,
                            // We need the category for grouping, store it temporarily or pass it along
                            // For simplicity, we'll group later based on the category name read here.
                            // Category = reader.GetString("transaction-category") // Implicitly used for grouping next
                        });
                    }
                }
            }
            Debug.WriteLine($"Fetched {transactions.Count} expense transactions.");
            return transactions;
        }

        private async Task<Dictionary<string, decimal>> FetchBudgetsAsync(MySqlConnection conn)
        {
            var budgets = new Dictionary<string, decimal>();
            string sql = @"
                SELECT `category-name` AS Category, `budget-amount` AS BudgetAmount
                FROM `spending-budget`
                WHERE `customer-id` = @customerId";
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@customerId", _customerId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        budgets[reader.GetString("Category")] = reader.GetDecimal("BudgetAmount");
                    }
                }
            }
            Debug.WriteLine($"Fetched {budgets.Count} category budgets.");
            return budgets;
        }

        // Example placeholder - Replace with your actual logic if you store an overall budget
        private async Task<decimal> FetchTotalMonthlyBudgetAsync(MySqlConnection conn)
        {
            // Option 1: Sum of individual category budgets
            // var budgets = await FetchBudgetsAsync(conn); // Fetch again or pass dictionary
            // return budgets.Sum(kvp => kvp.Value);

            // Option 2: Fetch from a dedicated field/table (e.g., customer table)
            string sql = "SELECT monthly_budget FROM customer WHERE customer_id = @customerId"; // Fictional column
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@customerId", _customerId);
                object result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
                }
            }
            // Default if not found
            return 2500m; // Default value used in original XAML example
        }


        private void GroupTransactionsAndCreateCategories(List<TransactionDetail> allTransactions, Dictionary<string, decimal> categoryBudgets)
        {
            // Need to re-read category from the transaction fetch or modify fetch logic.
            // Assuming FetchTransactionsAsync implicitly provides category info for grouping.
            // Let's refine FetchTransactionsAsync slightly to return category with details.

            // --- Refined Fetch Logic (Conceptual - Adapt FetchTransactionsAsync) ---
            // Modify FetchTransactionsAsync to return List<Tuple<string, TransactionDetail>>
            // or a class holding both category and transaction details.
            // For now, we'll simulate grouping based on the category name which *should* have been fetched.
            // This requires modifying the FetchTransactionsAsync SQL and return type.
            // --- End Refined Fetch Logic ---

            // *** Assuming FetchTransactionsAsync was modified to return category info ***
            // Example structure needed from Fetch:
            // class TransactionWithCategory { public string Category; public TransactionDetail Detail; }
            // List<TransactionWithCategory> fetchedData = await FetchTransactionsAsync(...);

            // --- TEMPORARY WORKAROUND: Re-query to get category mapping (Inefficient) ---
            // This is NOT ideal but works with the current FetchTransactionsAsync structure.
            // A better solution integrates category into the initial fetch.
            var transactionsWithCategory = new List<(string Category, TransactionDetail Detail)>();
            try
            {
                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT t.`transaction-date`, ABS(t.`transaction-amount`) AS Amount, t.`transaction-category` AS Category, t.`transaction-reference`
                        FROM `transaction` t
                        JOIN `account` a ON t.`account-id` = a.`account-id`
                        WHERE a.`customer-id` = @customerId
                          AND t.`transaction-date` BETWEEN @startDate AND @endDate -- Need start/end date here too
                          AND t.`transaction-amount` < 0";
                    // Define startDate/endDate again or pass them
                    DateTime startDate = new DateTime(2024, 12, 1);
                    DateTime endDate = new DateTime(2024, 12, 31);

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@customerId", _customerId);
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string category = reader.GetString("Category");
                                string reference = reader.IsDBNull(reader.GetOrdinal("transaction-reference")) ? "N/A" : reader.GetString("transaction-reference");
                                transactionsWithCategory.Add((
                                    category,
                                    new TransactionDetail
                                    {
                                        Date = reader.GetDateTime("transaction-date"),
                                        Amount = reader.GetDecimal("Amount"),
                                        Reference = reference
                                    }
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Error in temporary category fetch: {ex.Message}"); return; }
            // --- End TEMPORARY WORKAROUND ---


            var groupedData = transactionsWithCategory // Use the list that includes category names
                .GroupBy(t => t.Category) // Group by the fetched category name
                .Select(g => new SpendingCategoryWithTransactions
                {
                    Name = g.Key,
                    TotalSpent = g.Sum(item => item.Detail.Amount),
                    Transactions = g.Select(item => item.Detail)
                                    .OrderByDescending(td => td.Date) // Order transactions within category
                                    .ToList(),
                    BudgetAmount = categoryBudgets.TryGetValue(g.Key, out decimal budget) ? budget : 0m, // Get budget
                    Icon = GetIconForCategory(g.Key) // Assign an icon
                })
                .OrderByDescending(c => c.TotalSpent) // Order categories by amount spent
                .ToList();

            _spendingCategories = groupedData;
            Debug.WriteLine($"Created {_spendingCategories.Count} spending category groups.");
        }

        // --- UI Population & Updates ---

        private void SetLoadingState(bool isLoading)
        {
            // Simple version: Show/hide a placeholder in the container
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SpendingCategoriesContainer.Clear(); // Clear previous content or placeholder
                if (isLoading)
                {
                    SpendingCategoriesContainer.Add(new Label { Text = "Loading categories...", TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center, Padding = 20 });
                }
                // You could also disable buttons etc. here
            });
        }

        private void UpdateMonthlyBudgetCard()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                decimal remaining = _totalMonthlyBudget - _totalMonthlySpending;
                double progress = _totalMonthlyBudget > 0 ? Math.Min(1.0, (double)(_totalMonthlySpending / _totalMonthlyBudget)) : 0;

                if (MonthlyBudgetRemainingLabel != null) MonthlyBudgetRemainingLabel.Text = $"Remaining: {remaining:C0}"; // Format as £ without pence
                if (MonthlyBudgetTotalLabel != null) MonthlyBudgetTotalLabel.Text = $"{_totalMonthlyBudget:C0}";

                if (MonthlyBudgetProgressBar != null && MonthlyBudgetProgressLabel != null)
                {
                    // Calculate width based on parent container width (approximation)
                    // This requires the parent Grid/Frame to have rendered. Might need adjustment or SizeChanged event.
                    double parentWidth = this.Width - 40; // Estimate based on page padding
                    if (parentWidth <= 0) parentWidth = 250; // Fallback width

                    double progressBarWidth = parentWidth * progress;
                    MonthlyBudgetProgressBar.WidthRequest = progressBarWidth;

                    MonthlyBudgetProgressLabel.Text = $"{progress:P0}"; // Format as percentage (e.g., 50%)
                                                                        // Adjust label position - place it slightly after the progress bar ends
                    MonthlyBudgetProgressLabel.Margin = new Thickness(progressBarWidth + 5, 0, 0, 0);
                }
            });
        }


        private void PopulateSpendingCategoriesUI()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SpendingCategoriesContainer.Clear(); // Clear loading message or old categories

                if (_spendingCategories.Count == 0)
                {
                    SpendingCategoriesContainer.Add(new Label { Text = "No spending data found for Dec 2024.", TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center, Padding = 20 });
                    return;
                }

                foreach (var category in _spendingCategories)
                {
                    SpendingCategoriesContainer.Add(CreateExpandableCategoryView(category));
                }
            });
        }

        private View CreateExpandableCategoryView(SpendingCategoryWithTransactions category)
        {
            // Container for the category (header + transactions list)
            var categoryLayout = new VerticalStackLayout { Spacing = 0 }; // No space between header and list

            // Frame for the visible header part
            var headerFrame = new Frame
            {
                Padding = 15,
                HasShadow = false,
                BackgroundColor = Colors.White,
                CornerRadius = 12,
                Margin = new Thickness(0, 0, 0, 2) // Small margin below header
            };

            var headerGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };

            // Icon
            headerGrid.Add(new Label { Text = category.Icon, FontSize = 24, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 15, 0) }, 0, 0);

            // Category Name & Total Spent
            var infoLayout = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center }; // Reduced spacing
            infoLayout.Add(new Label { Text = category.Name, FontAttributes = FontAttributes.Bold });
            infoLayout.Add(new Label { Text = category.DisplayText, TextColor = Color.FromArgb("#6b7280"), FontSize = 14 });
            headerGrid.Add(infoLayout, 1, 0);

            // Expand/Collapse Indicator
            var expandLabel = new Label { Text = "▼", FontSize = 20, TextColor = Color.FromArgb("#6b7280"), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };
            headerGrid.Add(expandLabel, 2, 0);

            headerFrame.Content = headerGrid;

            // Hidden container for transactions
            var transactionsListLayout = new VerticalStackLayout
            {
                IsVisible = false, // Initially hidden
                Padding = new Thickness(15, 10, 15, 10), // Indent transactions slightly
                BackgroundColor = Color.FromArgb("#f0f0f0"), // Slightly different background
                Spacing = 8
            };

            // Populate transactions
            if (category.Transactions.Any())
            {
                foreach (var transaction in category.Transactions)
                {
                    transactionsListLayout.Add(CreateTransactionView(transaction));
                }
            }
            else
            {
                transactionsListLayout.Add(new Label { Text = "No transactions in this category.", TextColor = Colors.Gray, FontSize = 12 });
            }

            // Add Tap Gesture to the Header Frame to toggle visibility
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => {
                transactionsListLayout.IsVisible = !transactionsListLayout.IsVisible; // Toggle visibility
                expandLabel.Text = transactionsListLayout.IsVisible ? "▲" : "▼"; // Change indicator
            };
            headerFrame.GestureRecognizers.Add(tapGesture);

            // Add header and transaction list to the main category layout
            categoryLayout.Add(headerFrame);
            categoryLayout.Add(transactionsListLayout);

            return categoryLayout;
        }

        private View CreateTransactionView(TransactionDetail transaction)
        {
            // Simple view for a single transaction
            var transactionLayout = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            transactionLayout.Add(new Label { Text = $"{transaction.Date:dd MMM}: {transaction.Reference}", FontSize = 12, VerticalOptions = LayoutOptions.Center }, 0, 0);
            transactionLayout.Add(new Label { Text = $"-£{transaction.Amount:N2}", FontSize = 12, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center }, 1, 0);

            // Add a thin separator line
            var separator = new BoxView { HeightRequest = 1, Color = Colors.LightGray, Margin = new Thickness(0, 4, 0, 0) };

            return new VerticalStackLayout { Spacing = 0, Children = { transactionLayout, separator } };
        }

        // Helper to get an icon based on category name (customize as needed)
        private string GetIconForCategory(string categoryName)
        {
            switch (categoryName?.ToLower())
            {
                case "food":
                case "dining":
                case "food & dining":
                case "groceries":
                    return "🍽️";
                case "utility":
                case "utilities":
                case "bills":
                    return "🛠️"; // Hammer and Wrench
                case "shopping":
                    return "🛒";
                case "leisure":
                case "entertainment":
                    return "🏖️"; // Beach with Umbrella (or 🎬 Clapper Board)
                case "health":
                case "healthcare":
                    return "⚕️"; // Medical Symbol
                case "transport":
                case "transportation":
                    return "🚗"; // Car (or 🚌 Bus)
                case "mortgage":
                case "rent":
                    return "🏠"; // House
                case "transfer":
                    return "💸"; // Money with Wings
                case "gambling":
                    return "🎰"; // Slot Machine
                case "life event":
                    return "🎉"; // Party Popper
                case "monthly fees":
                    return "💳"; // Credit Card
                case "withdrawal":
                    return "🏧"; // ATM sign
                default:
                    return "💰"; // Money Bag (default)
            }
        }

        // --- Placeholder/Helper for Customer ID ---
        private async Task<bool> GetCustomerIdAsync()
        {
            // Reuse logic from Dashboard or implement your auth flow
            long storedId = Preferences.Get("customer_id", 0L);
            if (storedId != 0)
            {
                _customerId = storedId;
                Debug.WriteLine($"Budgeting Page: Retrieved Customer ID: {_customerId}");
                return true;
            }
            else
            {
                _customerId = 0;
                Debug.WriteLine("Budgeting Page: Customer ID not found.");
                return false;
            }
        }


        // --- Event Handlers ---

        // Top Bar Icons
        private async void ChatbotIcon_Tapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new Ai());
        }

        private async void OnNotificationsIconTapped(object sender, TappedEventArgs e)
        {
            // Assuming 'notifications' is a page class
            // await Navigation.PushAsync(new notifications());
            await DisplayAlert("Notifications", "No new notifications.", "OK"); // Placeholder
        }

        private async void SettingsIcon_Tapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new Settings()); // Assuming SettingsPage exists
        }

        // Card Buttons
        private async void SubscriptionButton_Clicked(object sender, EventArgs e)
        {
            // Assuming 'Subscription' is a page class
            // await Navigation.PushAsync(new Subscription());
            await DisplayAlert("Subscription", "Manage subscription feature coming soon.", "OK"); // Placeholder
        }

        private void FreezeStreakButton_Clicked(object sender, EventArgs e)
        {
            // Update UI directly for demo
            if (IconLabel != null) IconLabel.Text = "❄️";
            DisplayAlert("Streak Frozen", "Your budget streak is now frozen!", "OK"); // Placeholder action
        }

        // Bottom Navigation
        private async void home_Tapped(object sender, TappedEventArgs e)
        {
            // Navigate back to Dashboard, potentially popping this page
            await Navigation.PopToRootAsync(); // Or PushAsync(new Dashboard()) if preferred
        }

        private async void budget_Tapped(object sender, TappedEventArgs e)
        {
            // Already on this page, maybe refresh?
            Debug.WriteLine("Budget navigation tapped - Refreshing data.");
            await LoadDataAsync();
        }

        private async void investment_Tapped(object sender, TappedEventArgs e)
        {
            // Assuming 'Investing' is a page class
            // await Navigation.PushAsync(new Investing());
            await DisplayAlert("Navigate", "Navigate to Investing page (Not Implemented)", "OK"); // Placeholder
        }

        private async void infohub_Tapped(object sender, TappedEventArgs e)
        {
            // Assuming 'InfoHub1' is a page class
            // await Navigation.PushAsync(new InfoHub1());
            await DisplayAlert("Navigate", "Navigate to Info Hub page (Not Implemented)", "OK"); // Placeholder
        }
    }
}
