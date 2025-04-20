namespace MauiApp1234;
using MySqlConnector;
public partial class QuizPage2 : ContentPage
{
	public QuizPage2()
	{
		InitializeComponent();
        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        MySqlConnection conn = new MySqlConnection(connString);
        conn.Open();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(x))
        {
            // Show an error message if no radio button was selected
            DisplayAlert("Error", "Please select a Financial Literacy level before proceeding.", "OK");
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
                string query = "INSERT INTO quiz (literacy) VALUES (@FinancialLiteracy)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    // Add parameter to avoid SQL injection
                    cmd.Parameters.AddWithValue("@FinancialLiteracy", selectedFinancialLiteracy);

                    // Execute the query
                    cmd.ExecuteNonQuery();
                }

                // Notify the user
                DisplayAlert("Success", "Your selection has been saved successfully!", "OK");

                // Navigate to the next page
                Navigation.PushAsync(new QuizPage3());
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

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        await Launcher.OpenAsync("https://www.sofi.com/learn/content/financial-literacy-quiz/");
    }

    private int selectedFinancialLiteracy;
    string x;
    private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        // Ensure the RadioButton is checked
        if (e.Value)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null)
            {
                // Save the selected answer
                x = radioButton.Content.ToString();
                selectedFinancialLiteracy = Convert.ToInt16(x);
                Console.WriteLine($"Selected Financial Literacy: {selectedFinancialLiteracy}");
            }

        }
    }
}