using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System;

namespace MauiApp1234
{
    public partial class Subscription : ContentPage
    {
        public ObservableCollection<SpendingDataModel> MonthlySpendingData { get; set; }

        public Subscription()
        {
            InitializeComponent();
            InitializeChartData();
            // Set the binding context to this page
            this.BindingContext = this;
        }

        private void InitializeChartData()
        {
            MonthlySpendingData = new ObservableCollection<SpendingDataModel>
            {
                new SpendingDataModel { Month = "Jul", Amount = 50 },
                new SpendingDataModel { Month = "Aug", Amount = 90 },
                new SpendingDataModel { Month = "Sep", Amount = 60 },
                new SpendingDataModel { Month = "Oct", Amount = 100 },
                new SpendingDataModel { Month = "Nov", Amount = 80 },
                new SpendingDataModel { Month = "Dec", Amount = 120 }
            };
        }

        private async void ChatbotIcon_Tapped(object sender, EventArgs e)
        {
            await DisplayAlert("Chatbot", "Chatbot feature coming soon!", "OK");
        }

        private async void OnNotificationsIconTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Notifications", "No new notifications", "OK");
        }

        private async void SettingsIcon_Tapped(object sender, EventArgs e)
        {
            await DisplayAlert("Settings", "Settings page coming soon!", "OK");
        }

        private async void ViewDetailedAnalytics_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Analytics", "Detailed analytics coming soon!", "OK");
        }

        private async void AnalyticsLabel_Tapped(object sender, EventArgs e)
        {
            await DisplayAlert("Subscription Analytics", "Subscription analytics feature coming soon!", "OK");
        }

        private void DismissWarning_Clicked(object sender, EventArgs e)
        {
            // Hide the warning banner
            WarningBanner.IsVisible = false;
        }

        private async void ViewOptimizationPlan_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Subscription Optimization",
                "Recommended Actions:\n\n" +
                "1. Cancel Netflix Standard (£10.99/mo)\n" +
                "2. Share Netflix Premium with family members\n" +
                "3. Potential annual savings: £131.88",
                "Close");
        }
    }

    public class SpendingDataModel
    {
        public string Month { get; set; }
        public double Amount { get; set; }
    }
}