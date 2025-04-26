using MauiApp1234.Pages.Settings;

namespace MauiApp1234;
using MySqlConnector;
public partial class Settings : ContentPage
{
	public Settings()
	{
		InitializeComponent();
        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        MySqlConnection conn = new MySqlConnection(connString);
        conn.Open();

        if (App.Current.UserAppTheme == AppTheme.Dark)
        {
            darkModeSwitch.IsToggled = true;  // If the app is in Dark Mode, set the switch to 'on'
        }
        else
        {
            darkModeSwitch.IsToggled = false;  // Otherwise, set it to 'off'
        }

    }

    private void ResetExplain_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new PasswordResetInstructionsPage());
    }
    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)  // If the switch is ON, enable dark mode
        {
            App.Current.UserAppTheme = AppTheme.Dark;
        }
        else  // If the switch is OFF, enable light mode
        {
            App.Current.UserAppTheme = AppTheme.Light;
        }
    }

    private void CancelSubscriptionsExplain_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new CancelSubscriptionPage());
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new Secuirty());
    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }

    private void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
    {

    }

    private void Button_Clicked_1(object sender, EventArgs e)
    {
        Navigation.PushAsync(new LogInPage3());
    }

    private void Button_Clicked_2(object sender, EventArgs e)
    {
        Navigation.PushAsync(new LogInPage2());
    }

    private async void Button_Clicked_3(object sender, EventArgs e)
    {
        try
        {
            // Create the "mailto" URI
            var emailUri = new Uri("mailto:support@spendly.com?subject=Support Request&body=Please describe your issue here...");

            // Open the default email app using the URI
            await Launcher.OpenAsync(emailUri);
        }
        catch (Exception ex)
        {
            // Handle any potential errors
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private async void OnPhoneNumberTapped(object sender, EventArgs e)
    {
        try
        {
            // Open the phone dialer with the specified phone number
            var phoneUri = new Uri("tel:+44 7471 185479");
            await Launcher.OpenAsync(phoneUri);
        }
        catch (Exception ex)
        {
            // Handle any errors
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private async void OnEmailTapped(object sender, EventArgs e)
    {
        try
        {
            // Open the default email client with the "mailto:" URI
            var emailUri = new Uri("mailto:support@spendly.com?subject=Support Request&body=Please describe your issue here...");
            await Launcher.OpenAsync(emailUri);
        }
        catch (Exception ex)
        {
            // Handle any errors
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

}