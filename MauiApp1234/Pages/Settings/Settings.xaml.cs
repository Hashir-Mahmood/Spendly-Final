using MauiApp1234.Pages.Settings;

namespace MauiApp1234;

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

    private void SecurityExplain_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void CancelSubscriptionsExplain_Tapped(object sender, TappedEventArgs e)
    {

    }
}