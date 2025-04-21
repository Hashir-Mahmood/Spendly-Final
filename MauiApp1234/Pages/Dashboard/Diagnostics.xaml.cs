using Microsoft.Maui.Controls;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace MauiApp1234.Pages.Dashboard
{
    // Model class for expense items
    public class ExpenseItem
    {
        public string Category { get; set; }
        public double Amount { get; set; }

        public ExpenseItem(string category, double amount)
        {
            Category = category;
            Amount = amount;
        }
    }

    public partial class Diagnostics : ContentPage
    {
        public Diagnostics()
        {
            InitializeComponent();

            // Set data source for pie chart
            var expenseData = new List<ExpenseItem>
    {
        new ExpenseItem("Mortgage", 750),
        new ExpenseItem("Utility", 210),
        new ExpenseItem("Food", 320),
        new ExpenseItem("Shopping", 180),
        new ExpenseItem("Leisure", 150)
    };

            // If you're using the Syncfusion chart with named series in XAML, bind it here
            if (ExpensePieSeries != null)
            {
                ExpensePieSeries.ItemsSource = expenseData;
                // Set XBindingPath and YBindingPath to tell the chart which properties to use
                ExpensePieSeries.XBindingPath = "Category";
                ExpensePieSeries.YBindingPath = "Amount";

                // Remove the PaletteBrushes assignment here as it's already set in XAML
            }
        }

        // Minimal event handlers to avoid XAML binding errors
        private void OnTimePeriodChanged(object sender, CheckedChangedEventArgs e)
        {
            // This will be called when a radio button is checked
        }

        private void OnAddNewAccountClicked(object sender, EventArgs e)
        {
            // Handle add account button
            DisplayAlert("Add Account", "This would add a new account", "OK");
        }

        private void OnCategoryRemoved(object sender, TappedEventArgs e)
        {
            // Handle category removal
            string category = e.Parameter as string;
            DisplayAlert("Remove Category", $"Would remove category: {category}", "OK");
        }

        private void OnAddCategoryTapped(object sender, TappedEventArgs e)
        {
            // Show category picker popup
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = true;
            }
        }

        private void OnCancelCategorySelection(object sender, EventArgs e)
        {
            // Hide category picker popup
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = false;
            }
        }

        private void OnAddSelectedCategories(object sender, EventArgs e)
        {
            // Handle adding selected categories
            if (CategoryPickerPopup != null)
            {
                CategoryPickerPopup.IsVisible = false;
            }
            DisplayAlert("Add Categories", "Would add selected categories", "OK");
        }

        private void OnViewDetailedReportTapped(object sender, TappedEventArgs e)
        {
            // Handle view detailed report
            DisplayAlert("Detailed Report", "Would show detailed report", "OK");
        }
    }
}