using System.ComponentModel.DataAnnotations;

namespace MauiApp1234;
using MySqlConnector;
public partial class LogInPage2 : ContentPage
{
	public LogInPage2()
	{
		InitializeComponent();
        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        MySqlConnection conn = new MySqlConnection(connString);
        conn.Open();
    }

    private void ForgotPassword_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new LogInPage3());
    }

    private void SignIn_Clicked(object sender, EventArgs e)
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
            ShowError("Please enter a valid email");
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
            ShowError("Password must have at least 8 characters");
            return;
        }

        Navigation.PushAsync(new Budgeting());
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private bool IsValidEmail(string email)
    {
        // Validate email format
        var regex = new RegularExpressionAttribute(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
        return regex.IsValid(email);
    }

    private void TogglePasswordButton_Clicked(object sender, EventArgs e)
    {
        // Change Password Visibility
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

        // También podrías cambiar la imagen del botón aquí
        // TogglePasswordButton.Source = PasswordEntry.IsPassword ? "eye_icon.png" : "eye_slash_icon.png";
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new LogInPage1());
    }
}
