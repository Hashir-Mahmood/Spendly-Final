using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System;
namespace MauiApp1234;
using MySqlConnector;
using System.ComponentModel;
using System.Windows.Input;
using Syncfusion.Maui.Core.Carousel;
using MauiApp1234.Pages.Budgeting;



// Data Model for a Subscription Item
public class SubscriptionItem : INotifyPropertyChanged
    {
        public int subscriptionid { get; set; }
        public long customerid { get; set; }
        public string name { get; set; }
        public decimal price { get; set; }
        public DateTime renewal { get; set; }
        public string category { get; set; }
        public float subscriptionChange { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ViewModel for the Subscription Page
    public class SubscriptionViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SubscriptionItem> _subscriptions;
        public ObservableCollection<SubscriptionItem> Subscriptions
        {
            get => _subscriptions;
            set
            {
                _subscriptions = value;
                OnPropertyChanged(nameof(Subscriptions));
            }
        }

        public ICommand CancelSubscriptionCommand { get; }
        public ICommand AddSubscriptionCommand { get; }

        private string _connectionString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";

        public ObservableCollection<SpendingDataModel> MonthlySpendingData { get; set; }

        public SubscriptionViewModel()
        {
            Subscriptions = new ObservableCollection<SubscriptionItem>();
            MonthlySpendingData = new ObservableCollection<SpendingDataModel>
            {
                new SpendingDataModel { Month = "Jul", Amount = 50 },
                new SpendingDataModel { Month = "Aug", Amount = 90 },
                new SpendingDataModel { Month = "Sep", Amount = 60 },
                new SpendingDataModel { Month = "Oct", Amount = 100 },
                new SpendingDataModel { Month = "Nov", Amount = 80 },
                new SpendingDataModel { Month = "Dec", Amount = 120 }
            };
            LoadSubscriptions();
            CancelSubscriptionCommand = new Command<int>(async (subscriptionId) => await CancelSubscription(subscriptionId));
            AddSubscriptionCommand = new Command(async () => await AddSubscription());
        }

        // Method to load subscriptions from the MySQL database
        private async Task LoadSubscriptions()
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    await conn.OpenAsync();
                    string sql = "SELECT subscriptionId, customerId, name, price, renewal, category, subscriptionChange FROM subscription"; //  Corrected table name
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        ObservableCollection<SubscriptionItem> subscriptionList = new ObservableCollection<SubscriptionItem>(); // Use ObservableCollection
                        while (await reader.ReadAsync())
                        {
                            SubscriptionItem subscription = new SubscriptionItem
                            {
                                subscriptionid = reader.GetInt32("subscriptionId"),
                                customerid = reader.GetInt64("customerId"),
                                name = reader.GetString("name"),
                                price = reader.GetDecimal("price"), // Use GetDecimal
                                renewal = reader.GetDateTime("renewal"),
                                category = reader.GetString("category"),
                                subscriptionChange = reader.IsDBNull(reader.GetOrdinal("subscriptionChange")) ? 0 : reader.GetFloat("subscriptionChange") //Handle potential null
                            };
                            subscriptionList.Add(subscription);
                        }
                        Subscriptions = subscriptionList; // Assign the populated collection
                    }
                }
                catch (MySqlException ex)
                {
                    // Handle MySQL-specific errors
                    System.Diagnostics.Debug.WriteLine($"MySQL Error fetching subscriptions: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert("Database Error", $"Failed to load subscriptions: {ex.Message}", "OK");
                }
                catch (Exception ex)
                {
                    // Handle other errors
                    System.Diagnostics.Debug.WriteLine($"Error fetching subscriptions: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                }
            }
        }

        // Method to cancel a subscription (delete from MySQL database)
        private async Task CancelSubscription(int subscriptionId)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    await conn.OpenAsync();
                    string sql = "DELETE FROM subscription WHERE subscriptionId = @subscriptionId";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@subscriptionId", subscriptionId);
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            await LoadSubscriptions(); // Refresh
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Subscription with ID {subscriptionId} not found.");
                            await Application.Current.MainPage.DisplayAlert("Error", "Subscription not found.", "OK");
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    // Handle MySQL-specific errors
                    System.Diagnostics.Debug.WriteLine($"MySQL Error cancelling subscription: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert("Database Error", $"Failed to cancel subscription: {ex.Message}", "OK");
                }
                catch (Exception ex)
                {
                    // Handle other errors
                    System.Diagnostics.Debug.WriteLine($"Error cancelling subscription: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                }
            }
        }

        // Method to handle adding a new subscription (navigate to add subscription page)
        private async Task AddSubscription()
        {
            //await Application.Current.MainPage.Navigation.PushAsync(new AddSubscriptionPage());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Code-behind for the Subscription Page
    public partial class Subscription : ContentPage
    {
        private SubscriptionViewModel viewModel; // Declare the viewModel

        public Subscription()
        {
            InitializeComponent();
            viewModel = new SubscriptionViewModel(); // Initialize it here
            BindingContext = viewModel; // Set BindingContext
        }

        private async void AddSubscriptionButton_Clicked(object sender, EventArgs e)
        {
            // Use the AddSubscriptionCommand from the ViewModel
            if (viewModel.AddSubscriptionCommand.CanExecute(null))
            {
                viewModel.AddSubscriptionCommand.Execute(null);
            }
        }

        

        private void ChatbotIcon_Tapped(object sender, EventArgs e)
        {
            DisplayAlert("Chatbot", "Chatbot feature coming soon!", "OK");
        }

        private void OnNotificationsIconTapped(object sender, EventArgs e)
        {
            DisplayAlert("Notifications", "No new notifications", "OK");
        }

        private void SettingsIcon_Tapped(object sender, EventArgs e)
        {
            DisplayAlert("Settings", "Settings page coming soon!", "OK");
        }

        private void ViewDetailedAnalytics_Clicked(object sender, EventArgs e)
        {
            DisplayAlert("Analytics", "Detailed analytics coming soon!", "OK");
        }

        private void AnalyticsLabel_Tapped(object sender, EventArgs e)
        {
            DisplayAlert("Subscription Analytics", "Subscription analytics feature coming soon!", "OK");
        }

        private void DismissWarning_Clicked(object sender, EventArgs e)
        {
            // Hide the warning banner
            WarningBanner.IsVisible = false;
        }

        private void ViewOptimizationPlan_Clicked(object sender, EventArgs e)
        {
            DisplayAlert("Subscription Optimization",
                "Recommended Actions:\n\n" +
                "1. Cancel Netflix Standard (£10.99/mo)\n" +
                "2. Share Netflix Premium with family members\n" +
                "3. Potential annual savings: £131.88",
                "Close");
        }

        private void Add_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new AddSubscription());
        }
    }

    public class SpendingDataModel
    {
        public string Month { get; set; }
        public double Amount { get; set; }
    }
