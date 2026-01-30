using Microsoft.Maui.Controls;
using MySqlConnector;
using System.Reflection.Metadata.Ecma335;
namespace MauiApp1234
{
    public partial class MainPage : ContentPage
    {
      

        public MainPage()
        {
            InitializeComponent();
            string connString = "";
            MySqlConnection conn = new MySqlConnection(connString);
            conn.Open();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
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

            // Ensure a difficulty is selected
            if (string.IsNullOrWhiteSpace(selectedBudgetType))
            {
                DisplayAlert("Error", "Please select a primary Financial Goal before proceeding.", "OK");
                return;
            }

            // Database connection string
            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";

            using (var conn = new MySqlConnection(connString))
            {
                try
                {
                    conn.Open();

                    // SQL query to check if a row exists for the current customerId
                    string query = @"
                INSERT INTO quiz (customerId, budgeterType)
                VALUES (@customerId, @BudgetType)
                ON DUPLICATE KEY UPDATE
                    budgeterType = @BudgetType";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        // Add parameters to prevent SQL injection
                        cmd.Parameters.AddWithValue("@customerId", id);
                        cmd.Parameters.AddWithValue("@BudgetType", selectedBudgetType);

                        // Execute the query
                        int rowsAffected = cmd.ExecuteNonQuery();
                        Console.WriteLine($"Rows affected: {rowsAffected}");

                        DisplayAlert("Success", "Your selection has been saved successfully!", "OK");
                        Navigation.PushAsync(new QuizPage2());
                    }
                }
                catch (Exception ex)
                {
                    // Log and display database error
                    Console.WriteLine($"Database error: {ex.Message}");
                    DisplayAlert("Error", $"Database error: {ex.Message}", "OK");
                }
            }

            if (string.IsNullOrWhiteSpace(selectedBudgetType))
            {
                // Show an error message if no radio button was selected
                DisplayAlert("Error", "Please select a budget type before proceeding.", "OK");
                return;
            }
        }

        private void Button1_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new InfoHub1());
        }

        private string selectedBudgetType;

        private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            // Ensure the RadioButton is checked
            if (e.Value)
            {
                var radioButton = sender as RadioButton;
                if (radioButton != null)
                {
                    // Save the selected answer
                    selectedBudgetType = radioButton.Content.ToString();
                    Console.WriteLine($"Selected Budget Type: {selectedBudgetType}");
                }

            }
        }
    }

}
