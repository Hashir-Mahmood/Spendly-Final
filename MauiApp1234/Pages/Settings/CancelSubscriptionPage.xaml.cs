namespace MauiApp1234.Pages.Settings;

public partial class CancelSubscriptionPage : ContentPage
{
	public CancelSubscriptionPage()
	{
		InitializeComponent();
	}

    private void ManageSubscriptionsButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new Subscription());
    }

    private void ReturnButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }
}