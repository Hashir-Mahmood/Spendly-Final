using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization; // Needed for CultureInfo if formatting currency explicitly
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;
using MauiApp1234.Pages.AI; // Assuming namespace exists
using MauiApp1234.Pages.Settings; // Assuming namespace exists
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // For Preferences
using MySqlConnector;
using static System.Reflection.Metadata.BlobBuilder;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MauiApp1234 // Adjust namespace if needed
{
    public partial class Budgeting : ContentPage
    {
        // --- Member Fields ---
        private long _customerId = 0;

        // Connection string (Replace YOUR_PASSWORD)
        // Consider moving to a configuration file/service for better security
        private readonly string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=YOUR_PASSWORD;database=b66855mm";

        // Data storage
        private List<SpendingCategoryWithTransactions> _spendingCategories = new List<SpendingCategoryWithTransactions>();
        private decimal _totalMonthlyBudget = 0m;
        private decimal _totalMonthlySpending = 0m;

        // --- Data Classes ---

        /// <summary>
        /// Represents details of a single transaction.
        /// </summary>
        public class TransactionDetail
        {
            public DateTime Date { get; set; }
            public string Reference { get; set; }
            public decimal Amount { get; set; }
            // Formatted display text for UI binding
            public string DisplayText => $"{Date:dd MMM}: {Reference} (£{Amount:N2})";
        }

        /// <summary>
        /// Represents a spending category, including its total spending for the period,
        /// its budget, and the list of transactions within it.
        /// </summary>
        public class SpendingCategoryWithTransactions
        {
            public string Name { get; set; }
            public decimal TotalSpent { get; set; }
            public decimal BudgetAmount { get; set; }
            public List<TransactionDetail> Transactions { get; set; } = new List<TransactionDetail>();
            public string Icon { get; set; } // Emoji or FontAwesome code
            // Formatted display text for UI binding
            public string DisplayText => $"£{TotalSpent:N2} spent";
            // Calculated property for budget progress (0.0 to 1.0)
            public double BudgetProgress => BudgetAmount > 0 ? Math.Min(1.0, (double)(TotalSpent / BudgetAmount)) : 0;
            // Color for the progress bar based on spending vs budget
            public Color BudgetProgressColor => BudgetProgress < 0.5 ? Colors.Green : (BudgetProgress < 0.9 ? Colors.Orange : Colors.Red); // Adjusted threshold
        }

        // --- Constructor ---
        public Budgeting()
        {
            InitializeComponent();

            // Note: Updating UI based on SizeChanged can sometimes be complex due to layout cycles.
            // Ensure the logic inside UpdateMonthlyBudgetCard is efficient and handles potential nulls.
            this.SizeChanged += (s, e) => {
                // Only update UI if we've successfully loaded data and the page has a size
                if (_spendingCategories.Any() && this.Width > 0 && this.Height > 0)
                {
                    UpdateMonthlyBudgetCard();
                }
            };
        }

        // --- Page Lifecycle ---
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("Budgeting Page OnAppearing");

            // Use Task.Run to avoid blocking the UI thread during initial load,
            // but ensure UI updates happen back on the MainThread.
            Task.Run(async () => {
                try
                {
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during background data load: {ex.Message}");
                    // Display error on the main thread
                    MainThread.BeginInvokeOnMainThread(async () => {
                        if (this.Window != null) await DisplayAlert("Error", "An error occurred loading budget data. Please try again.", "OK");
                        SetLoadingState(false); // Ensure loading indicator is turned off on error
                    });
                }
            });
        }

        // --- Data Loading Orchestration ---
        /// <summary>
        /// Main method to load all necessary data for the budgeting page.
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                SetLoadingState(true); // Show loading indicator
                _spendingCategories.Clear();
                _totalMonthlySpending = 0m;
                _totalMonthlyBudget = 0m; // Reset budget as well

                // 1. Fetch Customer ID
                if (!await GetCustomerIdAsync())
                {
                    Debug.WriteLine("Customer ID not available for Budgeting page.");
                    if (this.Window != null) await DisplayAlert("Login Required", "Could not retrieve customer information. Please log in again.", "OK");
                    SetLoadingState(false);
                    return; // Stop loading if no customer ID
                }

                // 2. Define Fixed Date Range (Currently December 2024)
                // TODO: Consider making this dynamic (e.g., current month or user selectable)
                DateTime startDate = new DateTime(2024, 12, 1);
                DateTime endDate = new DateTime(2025, 1, 1); // Use Jan 1st (exclusive) for Dec 31st (inclusive)

                // 3. Fetch Data from Database
                using (var conn = new MySqlConnection(connString))
                {
                    await conn.OpenAsync();
                    Debug.WriteLine("Database connection successful for Budgeting page.");

                    // Fetch data concurrently where possible
                    Task<List<(string Category, TransactionDetail Detail)>> transactionsTask = FetchTransactionsWithCategoriesAsync(conn, startDate, endDate);
                    Task<Dictionary<string, decimal>> budgetsTask = FetchBudgetsAsync(conn);
                    Task<decimal> totalBudgetTask = FetchTotalMonthlyBudgetAsync(conn); // Fetches overall budget

                    // Wait for all data fetching tasks to complete
                    await Task.WhenAll(transactionsTask, budgetsTask, totalBudgetTask);

                    var allTransactionsWithCategories = await transactionsTask;
                    var categoryBudgets = await budgetsTask;
                    _totalMonthlyBudget = await totalBudgetTask; // Assign the fetched overall budget

                    // 4. Process Data
                    GroupTransactionsAndCreateCategories(allTransactionsWithCategories, categoryBudgets);

                    // Calculate overall spending for the period based on fetched transactions
                    _totalMonthlySpending = _spendingCategories.Sum(c => c.TotalSpent);
                    Debug.WriteLine($"Total Spending for Dec 2024: {_totalMonthlySpending}");
                    Debug.WriteLine($"Overall Monthly Budget: {_totalMonthlyBudget}");

                } // Connection automatically closed

                // 5. Update UI (on Main Thread)
                MainThread.BeginInvokeOnMainThread(() => {
                    UpdateMonthlyBudgetCard();
                    PopulateSpendingCategoriesUI();
                });

            }
            catch (MySqlException ex)
            {
                Debug.WriteLine($"Database error in LoadDataAsync: {ex.Message} (Number: {ex.Number})");
                if (this.Window != null) await DisplayAlert("Database Error", "Failed to retrieve budget data. Please check your connection and try again.", "OK");
                // Clear UI on error
                MainThread.BeginInvokeOnMainThread(() => {
                    if (SpendingCategoriesContainer != null) SpendingCategoriesContainer.Clear();
                    // Optionally display an error message in the container
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General error loading budgeting data: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (this.Window != null) await DisplayAlert("Error", $"Failed to load budgeting details: {ex.Message}", "OK");
                // Clear UI on error
                MainThread.BeginInvokeOnMainThread(() => {
                    if (SpendingCategoriesContainer != null) SpendingCategoriesContainer.Clear();
                    // Optionally display an error message in the container
                });
            }
            finally
            {
                SetLoadingState(false); // Hide loading indicator
            }
        }

        // --- Database Fetching Methods ---

        /// <summary>
        /// Fetches expense transactions within the specified date range for the customer.
        /// </summary>
        private async Task<List<(string Category, TransactionDetail Detail)>> FetchTransactionsWithCategoriesAsync(
            MySqlConnection conn, DateTime startDate, DateTime endDate)
        {
            var transactionsWithCategory = new List<(string Category, TransactionDetail Detail)>();
            // SQL query to get relevant transaction details, focusing on expenses
            string sql = @"
                SELECT t.`transaction-date`, ABS(t.`transaction-amount`) AS Amount,
                       t.`transaction-category` AS Category, t.`transaction-reference`
                FROM `transaction` t
                JOIN `account` a ON t.`account-id` = a.`account-id`
                WHERE a.`customer-id` = @customerId
                  AND t.`transaction-date` >= @startDate AND t.`transaction-date` < @endDate
                  AND t.`transaction-amount` < 0  -- Only expenses
                ORDER BY t.`transaction-category`, t.`transaction-date` DESC"; // Order for grouping and display

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@customerId", _customerId);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate); // Use exclusive end date

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Handle potential null category or reference from DB
                        string category = reader.IsDBNull(reader.GetOrdinal("Category"))
                            ? "Uncategorized" // Assign a default category if null
                            : reader.GetString("Category");

                        string reference = reader.IsDBNull(reader.GetOrdinal("transaction-reference"))
                            ? "N/A" // Assign default reference if null
                            : reader.GetString("transaction-reference");

                        transactionsWithCategory.Add((
                            category,
                            new TransactionDetail
                            {
                                Date = reader.GetDateTime("transaction-date"),
                                Amount = reader.GetDecimal("Amount"), // Already ABS in SQL
                                Reference = reference
                            }
                        ));
                    }
                } // Reader disposed
            } // Command disposed

            Debug.WriteLine($"Fetched {transactionsWithCategory.Count} expense transactions for the period.");
            return transactionsWithCategory;
        }

        /// <summary>
        /// Fetches category-specific budgets from the `spending-budget` table.
        /// </summary>
        private async Task<Dictionary<string, decimal>> FetchBudgetsAsync(MySqlConnection conn)
        {
            var budgets = new Dictionary<string, decimal>();
            string sql = @"
                SELECT `category-name` AS Category, `budget-amount` AS BudgetAmount
                FROM `spending-budget`
                WHERE `customer-id` = @customerId";

            try
            {
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@customerId", _customerId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Ensure category name is not null before adding
                            string categoryName = reader.GetString("Category");
                            if (!string.IsNullOrEmpty(categoryName))
                            {
                                budgets[categoryName] = reader.GetDecimal("BudgetAmount");
                            }
                        }
                    } // Reader disposed
                } // Command disposed
                Debug.WriteLine($"Fetched {budgets.Count} category budgets.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching category budgets: {ex.Message}");
                // Return empty dictionary on error, allows the app to continue
            }
            return budgets;
        }

        /// <summary>
        /// Fetches the overall monthly budget stored in the `customer` table.
        /// </summary>
        private async Task<decimal> FetchTotalMonthlyBudgetAsync(MySqlConnection conn)
        {
            decimal defaultBudget = 2500m; // Default value if not found or error occurs
            try
            {
                // Assumes a 'monthly_budget' column exists in the 'customer' table
                string sql = "SELECT `monthly_budget` FROM `customer` WHERE `customer_id` = @customerId";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@customerId", _customerId);
                    object result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value && decimal.TryParse(result.ToString(), out decimal fetchedBudget))
                    {
                        Debug.WriteLine($"Fetched overall monthly budget: {fetchedBudget}");
                        return fetchedBudget;
                    }
                    else
                    {
                        Debug.WriteLine($"Overall monthly budget not found or invalid for customer {_customerId}. Using default.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching overall monthly budget: {ex.Message}");
                // Fall through to return default value
            }
            return defaultBudget;
        }

        // --- Data Processing ---

        /// <summary>
        /// Groups the fetched transactions by category and creates the final list
        /// of SpendingCategoryWithTransactions objects, incorporating budget data.
        /// </summary>
        private void GroupTransactionsAndCreateCategories(
            List<(string Category, TransactionDetail Detail)> transactionsWithCategory,
            Dictionary<string, decimal> categoryBudgets)
        {
            // Group transactions by category name
            var groupedData = transactionsWithCategory
                .GroupBy(t => t.Category)
                .Select(g => new SpendingCategoryWithTransactions
                {
                    Name = g.Key, // The category name
                    TotalSpent = g.Sum(item => item.Detail.Amount), // Sum amounts for this category
                    Transactions = g.Select(item => item.Detail) // Get all transactions for this category
                                   .OrderByDescending(td => td.Date) // Order transactions by date (most recent first)
                                   .ToList(),
                    // Get budget for this category, default to 0 if not found
                    BudgetAmount = categoryBudgets.TryGetValue(g.Key, out decimal budget) ? budget : 0m,
                    Icon = GetIconForCategory(g.Key) // Assign an icon based on category name
                })
                .OrderByDescending(c => c.TotalSpent) // Order categories by amount spent (highest first)
                .ToList();

            // Optionally, add categories that have a budget but no spending in the period
            foreach (var budgetEntry in categoryBudgets)
            {
                if (!groupedData.Any(sc => sc.Name.Equals(budgetEntry.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    groupedData.Add(new SpendingCategoryWithTransactions
                    {
                        Name = budgetEntry.Key,
                        TotalSpent = 0m,
                        BudgetAmount = budgetEntry.Value,
                        Icon = GetIconForCategory(budgetEntry.Key),
                        Transactions = new List<TransactionDetail>() // Empty list
                    });
                }
            }

            // Re-sort if categories with only budgets were added
            _spendingCategories = groupedData.OrderByDescending(c => c.TotalSpent).ToList();

            Debug.WriteLine($"Created {_spendingCategories.Count} spending category groups.");
        }

        // --- UI Population & Updates ---

        /// <summary>
        /// Shows or hides a loading indicator in the main content area.
        /// </summary>
        private void SetLoadingState(bool isLoading)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Ensure the container exists
                if (SpendingCategoriesContainer == null)
                {
                    Debug.WriteLine("SetLoadingState: SpendingCategoriesContainer is null.");
                    return;
                }

                SpendingCategoriesContainer.Clear(); // Clear previous content
                if (isLoading)
                {
                    SpendingCategoriesContainer.Add(new ActivityIndicator
                    {
                        IsRunning = true,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        Margin = new Thickness(20)
                    });
                    SpendingCategoriesContainer.Add(new Label
                    {
                        Text = "Loading budget details...",
                        TextColor = Colors.Gray,
                        HorizontalOptions = LayoutOptions.Center,
                    });
                }
                // If not loading, the PopulateSpendingCategoriesUI method will add content later.
            });
        }

        /// <summary>
        /// Updates the UI elements within the monthly budget card (progress bar, labels).
        /// </summary>
        private void UpdateMonthlyBudgetCard()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // --- Safety Checks for UI Elements ---
                    if (MonthlyBudgetRemainingLabel == null || MonthlyBudgetTotalLabel == null ||
                        MonthlyBudgetProgressBar == null || MonthlyBudgetProgressLabel == null)
                    {
                        Debug.WriteLine("UpdateMonthlyBudgetCard: One or more UI elements are null. Skipping update.");
                        return;
                    }
                    // Also check parent for width calculation
                    if (MonthlyBudgetProgressBar.Parent is not VisualElement parentElement)
                    {
                        Debug.WriteLine("UpdateMonthlyBudgetCard: ProgressBar parent is not a VisualElement. Cannot get width.");
                        return;
                    }
                    //-----------------------------------------

                    decimal remaining = _totalMonthlyBudget - _totalMonthlySpending;
                    // Calculate progress, ensuring no division by zero and capping at 1.0
                    double progress = _totalMonthlyBudget > 0
                        ? Math.Min(1.0, (double)(_totalMonthlySpending / _totalMonthlyBudget))
                        : 0;

                    // Update Labels
                    MonthlyBudgetRemainingLabel.Text = $"Remaining: {remaining:C0}"; // Format as currency, no decimals
                    MonthlyBudgetTotalLabel.Text = $"{_totalMonthlyBudget:C0}"; // Format as currency, no decimals
                    MonthlyBudgetProgressLabel.Text = $"{progress:P0}"; // Format as percentage, no decimals

                    // --- Update Progress Bar Width ---
                    // Use parent's width for calculation, provides better responsiveness
                    double containerWidth = parentElement.Width;
                    double availableWidth = containerWidth - MonthlyBudgetProgressBar.Margin.HorizontalThickness; // Account for margins

                    // Ensure width is positive before calculation
                    if (availableWidth <= 0)
                    {
                        Debug.WriteLine($"UpdateMonthlyBudgetCard: Invalid available width ({availableWidth}). Using fallback.");
                        availableWidth = 200; // Provide a sensible fallback width
                    }

                    double progressBarWidth = availableWidth * progress;
                    MonthlyBudgetProgressBar.WidthRequest = progressBarWidth;

                    // --- Position Percentage Label ---
                    // Position label slightly after the progress bar end using TranslationX
                    // Adjust the '+ 5' offset as needed for visual spacing
                    MonthlyBudgetProgressLabel.TranslationX = progressBarWidth + 5;

                }
                catch (Exception ex)
                {
                    // Catch potential errors during UI update (e.g., layout issues)
                    Debug.WriteLine($"Error updating budget card UI: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Clears and repopulates the main container with expandable category views.
        /// </summary>
        private void PopulateSpendingCategoriesUI()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (SpendingCategoriesContainer == null)
                {
                    Debug.WriteLine("PopulateSpendingCategoriesUI: SpendingCategoriesContainer is null.");
                    return; // Cannot populate if container doesn't exist
                }

                SpendingCategoriesContainer.Clear(); // Remove previous items

                if (_spendingCategories.Count == 0)
                {
                    // Display a message if no categories were found/created
                    SpendingCategoriesContainer.Add(new Label
                    {
                        Text = "No spending data found for December 2024.",
                        TextColor = Colors.Gray,
                        HorizontalOptions = LayoutOptions.Center,
                        Padding = 20
                    });
                    return;
                }

                // Create and add a view for each spending category
                foreach (var category in _spendingCategories)
                {
                    SpendingCategoriesContainer.Add(CreateExpandableCategoryView(category));
                }
                Debug.WriteLine($"Populated UI with {_spendingCategories.Count} category views.");
            });
        }

        /// <summary>
        /// Creates a single expandable view for a spending category, including header and transactions list.
        /// </summary>
        private View CreateExpandableCategoryView(SpendingCategoryWithTransactions category)
        {
            // --- Main Container for this category ---
            var categoryLayout = new VerticalStackLayout { Spacing = 0 };

            // --- Header Section (Frame containing Grid) ---
            var headerFrame = new Frame
            {
                Padding = new Thickness(15, 10, 15, 10), // Adjusted padding
                HasShadow = false, // Cleaner look
                BackgroundColor = Colors.White,
                CornerRadius = 8, // Slightly smaller radius
                Margin = new Thickness(0, 0, 0, 1) // Thin separator line effect
            };

            var headerGrid = new Grid
            {
                ColumnDefinitions = {
                    new ColumnDefinition(GridLength.Auto),    // Icon
                    new ColumnDefinition(GridLength.Star),   // Category Name & Amount
                    new ColumnDefinition(GridLength.Auto)    // Expand Indicator
                },
                ColumnSpacing = 10 // Spacing between columns
            };

            // Column 0: Icon
            headerGrid.Add(new Label
            {
                Text = category.Icon,
                FontSize = 22, // Slightly smaller icon
                VerticalOptions = LayoutOptions.Center
            }, 0, 0); // Add to column 0, row 0

            // Column 1: Category Info (Name and Total Spent)
            var infoLayout = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            infoLayout.Add(new Label { Text = category.Name, FontAttributes = FontAttributes.Bold });
            infoLayout.Add(new Label { Text = category.DisplayText, TextColor = Colors.Gray, FontSize = 13 }); // Smaller secondary text
            headerGrid.Add(infoLayout, 1, 0); // Add to column 1, row 0

            // Column 2: Expand/Collapse Indicator
            var expandLabel = new Label
            {
                Text = "▼", // Initial state: collapsed
                FontSize = 18,
                TextColor = Colors.Gray,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };
            headerGrid.Add(expandLabel, 2, 0); // Add to column 2, row 0

            headerFrame.Content = headerGrid;

            // --- Transactions Section (Initially Hidden) ---
            var transactionsListLayout = new VerticalStackLayout
            {
                IsVisible = false, // Start collapsed
                Padding = new Thickness(20, 10, 20, 10), // Indent transactions slightly
                BackgroundColor = Color.FromArgb("#f9f9f9"), // Very light gray background
                Spacing = 5 // Space between transaction items
            };

            // Populate transactions list
            if (category.Transactions.Any())
            {
                foreach (var transaction in category.Transactions)
                {
                    transactionsListLayout.Add(CreateTransactionView(transaction));
                }
            }
            else
            {
                // Message if no transactions exist for this category
                transactionsListLayout.Add(new Label
                {
                    Text = "No transactions in this category for Dec 2024.",
                    TextColor = Colors.Gray,
                    FontSize = 12,
                    Padding = new Thickness(0, 5) // Add some padding
                });
            }

            // --- Expand/Collapse Logic ---
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => {
                bool wasVisible = transactionsListLayout.IsVisible;
                transactionsListLayout.IsVisible = !wasVisible; // Toggle visibility
                expandLabel.Text = transactionsListLayout.IsVisible ? "▲" : "▼"; // Update indicator
                // Optional: Animate the expansion/collapse
                // transactionsListLayout.FadeTo(transactionsListLayout.IsVisible ? 1 : 0, 150);
            };
            headerFrame.GestureRecognizers.Add(tapGesture); // Add gesture to the header

            // --- Assemble the View ---
            categoryLayout.Add(headerFrame);
            categoryLayout.Add(transactionsListLayout);

            return categoryLayout;
        }

        /// <summary>
        /// Creates a simple view for displaying a single transaction detail row.
        /// </summary>
        private View CreateTransactionView(TransactionDetail transaction)
        {
            // Use a Grid for better alignment of date/reference and amount
            var transactionLayout = new Grid
            {
                ColumnDefinitions = {
                    new ColumnDefinition(GridLength.Star), // Date and Reference (takes available space)
                    new ColumnDefinition(GridLength.Auto)  // Amount (takes needed space)
                },
                Margin = new Thickness(0, 2) // Small vertical margin
            };

            // Column 0: Date and Reference
            transactionLayout.Add(new Label
            {
                Text = $"{transaction.Date:dd MMM}: {transaction.Reference}",
                FontSize = 12,
                TextColor = Colors.DarkSlateGray,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation // Truncate long references
            }, 0, 0);

            // Column 1: Amount
            transactionLayout.Add(new Label
            {
                Text = $"-£{transaction.Amount:N2}", // Format as negative currency
                FontSize = 12,
                FontAttributes = FontAttributes.Bold, // Make amount stand out
                TextColor = Colors.DarkSlateGray,
                HorizontalOptions = LayoutOptions.End, // Align to the right
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);

            // Optional: Add a thin separator line below each transaction
            var separator = new BoxView
            {
                HeightRequest = 1,
                Color = Colors.LightGray,
                Margin = new Thickness(0, 4, 0, 0) // Margin above the next transaction
            };

            // Return a StackLayout containing the Grid and the separator
            return new VerticalStackLayout { Spacing = 0, Children = { transactionLayout /*, separator */ } };
            // Uncomment separator if desired
        }


        // --- Helper Methods ---

        /// <summary>
        /// Returns an appropriate emoji icon based on the category name.
        /// </summary>
        private string GetIconForCategory(string categoryName)
        {
            // Use case-insensitive matching and handle potential null input
            switch (categoryName?.ToLowerInvariant())
            {
                case "food":
                case "dining":
                case "food & dining":
                case "groceries": return "🍽️"; // Food/Dining
                case "utility":
                case "utilities":
                case "bills": return "💡"; // Utilities (Lightbulb)
                case "shopping": return "🛒"; // Shopping
                case "leisure":
                case "entertainment": return "🎭"; // Entertainment/Leisure (Theater masks)
                case "health":
                case "healthcare": return "⚕️"; // Health (Medical symbol)
                case "transport":
                case "transportation": return "🚗"; // Transport
                case "mortgage":
                case "rent": return "🏠"; // Housing
                case "transfer": return "💸"; // Transfer
                case "gambling": return "🎰"; // Gambling
                case "life event": return "🎉"; // Life Event
                case "monthly fees": return "💳"; // Fees (Credit card)
                case "withdrawal": return "🏧"; // Withdrawal
                case "uncategorized": return "❓"; // Uncategorized
                default: return "💰"; // Default (Money bag)
            }
        }

        /// <summary>
        /// Retrieves the customer ID from Preferences.
        /// </summary>
        /// <returns>True if successful, False otherwise.</returns>
        private async Task<bool> GetCustomerIdAsync()
        {
            // Check if already loaded
            if (_customerId != 0) return true;

            // Try to get from preferences
            string customerIdStr = Preferences.Get("customer_id", string.Empty);

            if (!string.IsNullOrWhiteSpace(customerIdStr) && long.TryParse(customerIdStr, out _customerId))
            {
                Debug.WriteLine($"Budgeting Page: Retrieved Customer ID: {_customerId}");
                return true; // Successfully retrieved and parsed
            }
            else
            {
                // ID not found or invalid in preferences
                _customerId = 0;
                Debug.WriteLine("Budgeting Page: Customer ID not found or invalid in Preferences.");
                // Don't show alert here, let LoadDataAsync handle it
                return false;
            }
        }

        // --- Event Handlers ---

        // Top Bar Icons
        private async void ChatbotIcon_Tapped(object sender, TappedEventArgs e)
        {
            // Navigate to AI page using Shell routing if possible, fallback to PushAsync
            try { await Shell.Current.GoToAsync("//ai"); }
            catch { if (this.Window != null) await Navigation.PushAsync(new Ai()); } // Ensure Ai page exists
        }

        private async void OnNotificationsIconTapped(object sender, TappedEventArgs e)
        {
            // Placeholder for notifications
            if (this.Window != null) await DisplayAlert("Notifications", "No new notifications.", "OK");
        }

        private async void SettingsIcon_Tapped(object sender, TappedEventArgs e)
        {
            // Navigate to Settings page using Shell routing if possible, fallback to PushAsync
            try { await Shell.Current.GoToAsync("//settings"); }
            catch { if (this.Window != null) await Navigation.PushAsync(new Settings()); } // Ensure Settings page exists
        }

        // Buttons within the page content (if any)
        private async void SubscriptionButton_Clicked(object sender, EventArgs e)
        {
            if (this.Window != null) await DisplayAlert("Subscription", "Manage subscription feature coming soon.", "OK");
        }

        private void FreezeStreakButton_Clicked(object sender, EventArgs e)
        {
            // Example: Update an icon (ensure IconLabel exists in your XAML)
            if (IconLabel != null) IconLabel.Text = "❄️";
            if (this.Window != null) DisplayAlert("Streak Frozen", "Your budget streak is now frozen!", "OK");
        }

        // Bottom Tab Bar Navigation (Example using Shell)
        private async void home_Tapped(object sender, TappedEventArgs e)
        {
            try { await Shell.Current.GoToAsync("//dashboard"); } // Navigate to Dashboard route
            catch (Exception ex) { Debug.WriteLine($"Shell Navigation Error: {ex.Message}"); }
        }

        private async void budget_Tapped(object sender, TappedEventArgs e)
        {
            // Already on this page, maybe refresh data?
            await LoadDataAsync();
        }

        private async void investment_Tapped(object sender, TappedEventArgs e)
        {
            try { await Shell.Current.GoToAsync("//investments"); } // Navigate to Investments route
            catch (Exception ex) { Debug.WriteLine($"Shell Navigation Error: {ex.Message}"); }
        }

        private async void infohub_Tapped(object sender, TappedEventArgs e)
        {
            try { await Shell.Current.GoToAsync("//infohub"); } // Navigate to InfoHub route
            catch (Exception ex) { Debug.WriteLine($"Shell Navigation Error: {ex.Message}"); }
        }

        // --- XAML Element References ---
        // Ensure these names match the x:Name attributes in your Budgeting.xaml file
        // Example:
        // <Label x:Name="MonthlyBudgetRemainingLabel" ... />
        // <Label x:Name="MonthlyBudgetTotalLabel" ... />
        // <BoxView x:Name="MonthlyBudgetProgressBar" ... /> // Or whatever element you use
        // <Label x:Name="MonthlyBudgetProgressLabel" ... />
        // <VerticalStackLayout x:Name="SpendingCategoriesContainer" ... />
        // <Label x:Name="IconLabel" ... /> // For the FreezeStreak example

    } // End of Budgeting class
} // End of namespace
