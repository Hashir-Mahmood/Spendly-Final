namespace MauiApp1234;
using MySqlConnector;
public partial class QuizPage6 : ContentPage
{
	public QuizPage6()
	{
		InitializeComponent();
        string connString = "L";
        MySqlConnection conn = new MySqlConnection(connString);
        conn.Open();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        long id = 0;



        if (Preferences.Default.ContainsKey("customer_id"))
        {
            string n = Preferences.Default.Get("customer_id", "");

            if (string.IsNullOrEmpty(n))
            {
                DisplayAlert("Error", "Please Log In with a valid user before proceeding.", "OK");
                return;
            }
            else
            {
                id = Convert.ToInt64(n);
            }


        }
        else
        {
            DisplayAlert("Error", "Please Log In with a valid user before proceeding.", "OK");
            return;
        }

        if (id <= 0)
        {
            DisplayAlert("Error", "Please Log In with a valid user before proceeding.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedDifficulty))
        {
            // Show an error message if no radio button was selected
            DisplayAlert("Error", "Please select a primary Financial Goal before proceeding.", "OK");
            return;
        }

        // Database connection string
        string connString = "";

        using (var conn = new MySqlConnection(connString))
        {
            try
            {
                conn.Open();

                // SQL Insert Query
                string query = @"
                INSERT INTO quiz (customerId, difficulty) 
                VALUES (@customerId, @difficulty)
                ON DUPLICATE KEY UPDATE 
                    difficulty = @difficulty";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    // Add parameter to avoid SQL injection
                    cmd.Parameters.AddWithValue("@customerId", id);
                    cmd.Parameters.AddWithValue("@difficulty", selectedDifficulty);

                    // Execute the query
                    cmd.ExecuteNonQuery();
                }

                // Notify the user
                DisplayAlert("Success", "Your selection has been saved successfully!", "OK");

                // Navigate to the next page
                Navigation.PushAsync(new InfoHub1());
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

    private string selectedDifficulty;
    private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        // Ensure the RadioButton is checked
        if (e.Value)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null)
            {
                // Save the selected answer
                selectedDifficulty = radioButton.Content.ToString();
                Console.WriteLine($"Selected difficulty: {selectedDifficulty}");
            }

        }
    }
}
