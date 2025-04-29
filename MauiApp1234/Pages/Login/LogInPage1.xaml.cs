using System.ComponentModel.DataAnnotations;

namespace MauiApp1234;
using MySqlConnector;
public partial class LogInPage1 : ContentPage
{
	public LogInPage1()
	{
		InitializeComponent();
        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        try
        {
            MySqlConnection conn = new MySqlConnection(connString);
            conn.Open();
        }
        catch (Exception ex)
        {
            DisplayAlert("Database Error", ex.Message, "OK");
        }          
    }

    private void CreateAccount_Clicked(object sender, EventArgs e)
    {
        // Make previous errors invisible 
        ErrorLabel.IsVisible = false;

        // Verify email
        if (string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            ShowError("You have not filled the e-mail field correctly");
            return;
        }
        else if (!IsValidEmail(EmailEntry.Text))
        {
            ShowError("Please input a valid e-mail");
            return;
        }

        // Verify Password
        if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ShowError("Password is not filled correctly");
            return;
        }
        else if (PasswordEntry.Text.Length < 8)
        {
            ShowError("Password must be at least 8 characters");
            return;
        }

        // Verify Name
        if (string.IsNullOrWhiteSpace(FirstNameEntry.Text))
        {
            ShowError("You have not filled the Name field correctly");
            return;
        }

        // Verify Last Name
        if (string.IsNullOrWhiteSpace(LastNameEntry.Text))
        {
            ShowError("You have not filled the Last Name field correctly");
            return;
        }

        // Insert data into SQL database
        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        using (var conn = new MySqlConnection(connString))
        {
            try
            {
                conn.Open();
                string query = "INSERT INTO customer (email, password, name, surname) VALUES (@Email, @Password, @FirstName, @LastName)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    // Add parameters to prevent SQL injection
                    cmd.Parameters.AddWithValue("@Email", EmailEntry.Text.Trim());
                    cmd.Parameters.AddWithValue("@Password", PasswordEntry.Text.Trim()); // Consider hashing the password for security
                    cmd.Parameters.AddWithValue("@FirstName", FirstNameEntry.Text.Trim());
                    cmd.Parameters.AddWithValue("@LastName", LastNameEntry.Text.Trim());

                    // Execute the query
                    cmd.ExecuteNonQuery();
                    DisplayAlert("Success", "Account created successfully!", "OK");

                    // Navigate to login page
                    Navigation.PushAsync(new LogInPage2());
                }
            }
            catch (Exception ex)
            {
                ShowError($"Database error: {ex.Message}");
                return;
            }
        }


        Navigation.PushAsync(new LogInPage2());
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private bool IsValidEmail(string email)
    {
        // Regular expression logic to verify email
        var regex = new RegularExpressionAttribute(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
        return regex.IsValid(email);
    }

    private void TogglePasswordButton_Clicked(object sender, EventArgs e)
    {
        // Change Password visibility
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

        // También podrías cambiar la imagen del botón aquí
        // TogglePasswordButton.Source = PasswordEntry.IsPassword ? "eye_icon.png" : "eye_slash_icon.png";
    }

    

    

    private void StartSession_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new LogInPage2());
    }
}