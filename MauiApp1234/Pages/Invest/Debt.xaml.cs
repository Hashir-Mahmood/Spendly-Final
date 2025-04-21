using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System;
namespace MauiApp1234
{
    public partial class Debt : ContentPage
    {
        public ObservableCollection<DebtChartDataModel> DebtChartData { get; set; }

        public Debt()
        {
            InitializeComponent();
            InitializeChartData();
            // Set the binding context to this page
            this.BindingContext = this;
        }

        private void InitializeChartData()
        {
            DebtChartData = new ObservableCollection<DebtChartDataModel>
            {
                new DebtChartDataModel { Month = "Nov", Value = 3000 },
                new DebtChartDataModel { Month = "Dec", Value = 1000 },
                new DebtChartDataModel { Month = "Jan", Value = 5430 },
                new DebtChartDataModel { Month = "Feb", Value = 4329 },
                new DebtChartDataModel { Month = "Mar", Value = 6800 },
                new DebtChartDataModel { Month = "Apr", Value = 6400 }
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

        private async void InvestmentPageButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Investing());
        }
    }

    public class DebtChartDataModel
    {
        public string Month { get; set; }
        public double Value { get; set; }
    }
}