namespace MauiApp1234;
using MySqlConnector;
public partial class Budgeting : ContentPage
{
	public Budgeting()
	{
		InitializeComponent();
	}

    private void ChatbotIcon_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void OnNotificationsIconTapped(object sender, TappedEventArgs e)
    {

    }

    private void SettingsIcon_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new Settings());
    }
}