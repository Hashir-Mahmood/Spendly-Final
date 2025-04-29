using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System;
using MauiApp1234.Pages.AI;
using MauiApp1234.Pages.Settings;
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
                new DebtChartDataModel { Month = "Nov", Value = 6000 },
                new DebtChartDataModel { Month = "Dec", Value = 10000 },
                new DebtChartDataModel { Month = "Jan", Value = 5000 },
                new DebtChartDataModel { Month = "Feb", Value = 3000 },
                new DebtChartDataModel { Month = "Mar", Value = 12000 },
                new DebtChartDataModel { Month = "Apr", Value = 13000 }
            };
        }

        private async void ChatbotIcon_Tapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Ai());
        }

        private async void OnNotificationsIconTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new notifications());
        }

        private async void SettingsIcon_Tapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Settings());
        }

        private async void InvestmentPageButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Investing());
        }

        private void ReturnButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }
    }

    public class DebtChartDataModel
    {
        public string Month { get; set; }
        public double Value { get; set; }
    }
}