using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace MauiApp1234
{
    public partial class Investing : ContentPage
    {
        public ObservableCollection<ChartDataModel> ChartData { get; set; }

        public Investing()
        {
            // Initialize chart data before InitializeComponent
            InitializeChartData();

            InitializeComponent();

            // Set binding context for the page
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

        private void ChatbotIcon_Tapped(object sender, EventArgs e)
        {
            // Handle chatbot icon tap
        }

        private void OnNotificationsIconTapped(object sender, EventArgs e)
        {
            // Handle notifications icon tap
        }

        private void SettingsIcon_Tapped(object sender, EventArgs e)
        {
            // Handle settings icon tap
        }

        private async void DebtPageButton_Clicked(object sender, EventArgs e)
        {
            // Navigate to the Debt page
            await Navigation.PushAsync(new Debt());
        }
    }

    // Data model for chart points
    public class ChartDataModel
    {
        public string Month { get; set; }
        public double Value { get; set; }
    }
}