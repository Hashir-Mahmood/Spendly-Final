namespace MauiApp1234;
using MySqlConnector;
using System;

public partial class LogInPage3 : ContentPage
{
    private readonly string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";

    public LogInPage3()
    {
        InitializeComponent();
    }

    private async void ReturnButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

    private async void ResetPasswordButton_Clicked(object sender, EventArgs e)
    {
        // Get values from the entries (you'll need to add x:Name attributes to your entries)
        string email = EmailEntry.Text;
        string customerId = CustomerIdEntry.Text;
        string newPassword = NewPasswordEntry.Text;

        // Validate inputs
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(newPassword))
        {
            await DisplayAlert("Error", "Please fill in all fields", "OK");
            return;
        }

        // Check if the email and customer ID match
        if (await ValidateUserCredentials(email, customerId))
        {
            // Update the password
            if (await UpdatePassword(email, customerId, newPassword))
            {
                await DisplayAlert("Success", "Password has been reset successfully", "OK");
                await Navigation.PushAsync(new LogInPage2());
            }
            else
            {
                await DisplayAlert("Error", "Failed to reset password. Please try again.", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "Email or Customer ID is incorrect", "OK");
        }
    }

    private async Task<bool> ValidateUserCredentials(string email, string customerId)
    {
        using (var conn = new MySqlConnection(connString))
        {
            try
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(*) FROM customer WHERE email = @Email AND customer_id = @CustomerId";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);

                    int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Database Error", $"An error occurred: {ex.Message}", "OK");
                return false;
            }
        }
    }

    private async Task<bool> UpdatePassword(string email, string customerId, string newPassword)
    {
        using (var conn = new MySqlConnection(connString))
        {
            try
            {
                await conn.OpenAsync();
                string query = "UPDATE customer SET password = @NewPassword WHERE email = @Email AND customer_id = @CustomerId";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NewPassword", newPassword);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Database Error", $"An error occurred: {ex.Message}", "OK");
                return false;
            }
        }
    }

    private void ReturnButton_Clicked_1(object sender, EventArgs e)
    {

    }
}