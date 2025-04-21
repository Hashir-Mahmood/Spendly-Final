using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System;

namespace MauiApp1234
{
    public partial class Debt : ContentPage
    {
        public ObservableCollection<ChartDataModel> ChartData { get; set; }

        public Debt()
        {
            InitializeComponent();
            InitializeChartData();

            // Set the binding context to this page
            this.BindingContext = this;
        }

        private void InitializeChartData()
        {
            ChartData = new ObservableCollection<ChartDataModel>
            {
                new ChartDataModel { Month = "Nov", Value = 3000 },
                new ChartDataModel { Month = "Dec", Value = 1000 },
                new ChartDataModel { Month = "Jan", Value = 5430 },
                new ChartDataModel { Month = "Feb", Value = 4329 },
                new ChartDataModel { Month = "Mar", Value = 6800 },
                new ChartDataModel { Month = "Apr", Value = 6400 }
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

    public class ChartDataModel
    {
        public string Month { get; set; }
        public double Value { get; set; }
    }
}
