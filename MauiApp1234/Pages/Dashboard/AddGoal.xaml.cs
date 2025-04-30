using MySqlConnector;
using System.Diagnostics;

namespace MauiApp1234.Pages.Dashboard;

public partial class AddGoal : ContentPage
{
    public AddGoal()
    {
        InitializeComponent();
    }

    private async void AddGoalButton_Clicked(object sender, EventArgs e)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(GoalNameEntry.Text) ||
            string.IsNullOrWhiteSpace(DescriptionEntry.Text) ||
            string.IsNullOrWhiteSpace(RemainingAmountEntry.Text))
        {
            await DisplayAlert("Error", "Please fill in all required fields.", "OK");
            return;
        }

        if (!double.TryParse(RemainingAmountEntry.Text, out double remainingAmount))
        {
            await DisplayAlert("Error", "Invalid amount format. Please enter a number.", "OK");
            return;
        }

        if (!double.TryParse(ProgressEntry.Text, out double progress))
        {
            if (!string.IsNullOrWhiteSpace(ProgressEntry.Text))
            {
                await DisplayAlert("Error", "Invalid progress format. Please enter a number.", "OK");
                return;
            }
            progress = 0.0; // Default value if empty
        }

        DateTime targetDate = TargetDatePicker.Date;
        DateTime createdDate = DateTime.Now;

        long customerId = 0;

        if (Preferences.Default.ContainsKey("customer_id"))
        {
            string customerIdString = Preferences.Default.Get("customer_id", "");

            if (string.IsNullOrWhiteSpace(customerIdString))
            {
                await DisplayAlert("Error", "Please Log In with a valid user before proceeding.", "OK");
                return;
            }
            else
            {
                customerId = Convert.ToInt64(customerIdString);
            }
        }
        else
        {
            await DisplayAlert("Error", "Please Log In with a valid user before proceeding.", "OK");
            return;
        }

        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        using (var conn = new MySqlConnection(connString))
        {
            try
            {
                await conn.OpenAsync();
                // Using backticks around column names and proper table name based on error message
                string sql = "INSERT INTO `financial_goals` (`customer-id`, `GoalName`, `Description`, `Progress`, `RemainingAmount`, `TargetDate`, `CreatedDate`) " +
                             "VALUES (@customerId, @goalName, @description, @progress, @remainingAmount, @targetDate, @createdDate)";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    // Add parameters to prevent SQL injection
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.Parameters.AddWithValue("@goalName", GoalNameEntry.Text);
                    cmd.Parameters.AddWithValue("@description", DescriptionEntry.Text);
                    cmd.Parameters.AddWithValue("@progress", progress);
                    cmd.Parameters.AddWithValue("@remainingAmount", remainingAmount);
                    cmd.Parameters.AddWithValue("@targetDate", targetDate);
                    cmd.Parameters.AddWithValue("@createdDate", createdDate);

                    await cmd.ExecuteNonQueryAsync();
                    await DisplayAlert("Success", "Goal added successfully.", "OK");
                    await Navigation.PopAsync(); // Go back to the previous page
                }
            }
            catch (MySqlException ex)
            {
                // Handle MySQL-specific errors
                System.Diagnostics.Debug.WriteLine($"MySQL Error adding goal: {ex.Message}");
                await DisplayAlert("Database Error", $"Failed to add goal: {ex.Message}", "OK");
            }
            catch (Exception ex)
            {
                // Handle other errors
                System.Diagnostics.Debug.WriteLine($"Error adding goal: {ex.Message}");
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        // Navigate to the Goal summary page - you might need to create this page
        try { await Shell.Current.GoToAsync("//dashboard"); } // Navigate to Dashboard route
        catch (Exception ex) {;}
    }

    private void ReturnButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }
}