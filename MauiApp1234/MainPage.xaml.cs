using Microsoft.Maui.Controls;
using MySqlConnector;
namespace MauiApp1234
{
    public partial class MainPage : ContentPage
    {
      

        public MainPage()
        {
            InitializeComponent();
            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
            MySqlConnection conn = new MySqlConnection(connString);
            conn.Open();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(selectedBudgetType))
            {
                // Show an error message if no radio button was selected
                DisplayAlert("Error", "Please select a budget type before proceeding.", "OK");
                return;
            }

            // Database connection string
            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";

            using (var conn = new MySqlConnection(connString))
            {
                try
                {
                    conn.Open();

                    // SQL Insert Query
                    string query = "INSERT INTO quiz (budgeterType) VALUES (@BudgetType)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        // Add parameter to avoid SQL injection
                        cmd.Parameters.AddWithValue("@BudgetType", selectedBudgetType);

                        // Execute the query
                        cmd.ExecuteNonQuery();
                    }

                    // Notify the user
                    DisplayAlert("Success", "Your selection has been saved successfully!", "OK");

                    // Navigate to the next page
                    Navigation.PushAsync(new QuizPage2());
                }
                catch (Exception ex)
                {
                    // Show error message in case of a database error
                    DisplayAlert("Error", $"Database error: {ex.Message}", "OK");
                }
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
