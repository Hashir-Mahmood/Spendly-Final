using MauiApp1234.Pages.Settings;

namespace MauiApp1234;
using MySqlConnector;
public partial class Settings : ContentPage
{
	public Settings()
	{
		InitializeComponent();

	}

    private void ResetExplain_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new PasswordResetInstructionsPage());
    }

    private void CancelSubscriptionsExplain_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new CancelSubscriptionPage());
    }
}