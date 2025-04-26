using MySqlConnector;
using System.ComponentModel.DataAnnotations;

namespace MauiApp1234.Pages.Budgeting;

public partial class AddSubscription : ContentPage
{
    public AddSubscription()
    {
        InitializeComponent();
        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        MySqlConnection conn = new MySqlConnection(connString);
        conn.Open();
    }

    private async void AddSubscriptionButton_Clicked(object sender, EventArgs e)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
            string.IsNullOrWhiteSpace(PriceEntry.Text) ||
            string.IsNullOrWhiteSpace(CategoryEntry.Text))
        {
            await DisplayAlert("Error", "Please fill in all fields.", "OK");
            return;
        }

        if (!decimal.TryParse(PriceEntry.Text, out decimal price))
        {
            await DisplayAlert("Error", "Invalid price format. Please enter a number.", "OK");
            return;
        }

        if (!float.TryParse(SubscriptionChangeEntry.Text, out float subscriptionChange))
        {
            if (!string.IsNullOrWhiteSpace(SubscriptionChangeEntry.Text))
            {
                await DisplayAlert("Error", "Invalid subscription change format. Please enter a number.", "OK");
                return;
            }
        }
        DateTime renewalDate = RenewalDatePicker.Date; 
        
        long id = 0;

        if (Preferences.Default.ContainsKey("customer_id"))
        {
            string n = Preferences.Default.Get("customer_id", "");

            if (string.IsNullOrWhiteSpace(n))
            {
                DisplayAlert("Error", "Please Log In with a valid user before proceeding.", "OK");
                return;
            }
            else
            {
                id = Convert.ToInt64(n); // Use Convert.ToInt32 for a larger range
            }
        }
        else
        {
            DisplayAlert("Error", "Please Log In with a valid user before proceeding.", "OK");
            return;
        }




        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        using (var conn = new MySqlConnection(connString))
        {
            try
            {
                await conn.OpenAsync();
                string sql = "INSERT INTO subscription (customerId, name, price, renewal, category, subscriptionChange) " +
                             "VALUES (@customerId, @name, @price, @renewal, @category, @subscriptionChange)";  // Corrected SQL

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    // Add parameters to prevent SQL injection
                    cmd.Parameters.AddWithValue("@customerId", id); //  Hardcoded,  You should get this from your user session.
                    cmd.Parameters.AddWithValue("@name", NameEntry.Text);
                    cmd.Parameters.AddWithValue("@price", price);
                    cmd.Parameters.AddWithValue("@renewal", renewalDate);
                    cmd.Parameters.AddWithValue("@category", CategoryEntry.Text);
                    if (string.IsNullOrWhiteSpace(SubscriptionChangeEntry.Text))
                    {
                        cmd.Parameters.AddWithValue("@subscriptionChange", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@subscriptionChange", subscriptionChange);
                    }


                    await cmd.ExecuteNonQueryAsync();
                    await DisplayAlert("Success", "Subscription added successfully.", "OK");
                    await Navigation.PopAsync(); // Go back to the previous page
                }
            }
            catch (MySqlException ex)
            {
                // Handle MySQL-specific errors
                System.Diagnostics.Debug.WriteLine($"MySQL Error adding subscription: {ex.Message}");
                await DisplayAlert("Database Error", $"Failed to add subscription: {ex.Message}", "OK");
            }
            catch (Exception ex)
            {
                // Handle other errors
                System.Diagnostics.Debug.WriteLine($"Error adding subscription: {ex.Message}");
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }

        }

    }
}

