using System.ComponentModel.DataAnnotations;

namespace MauiApp1234;

public partial class LogInPage1 : ContentPage
{
	public LogInPage1()
	{
		InitializeComponent();
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