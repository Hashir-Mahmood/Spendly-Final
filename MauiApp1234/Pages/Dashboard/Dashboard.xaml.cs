namespace MauiApp1234;

using MauiApp1234.Pages.Dashboard;
using MySqlConnector;
public partial class Dashboard : ContentPage
{
	public Dashboard()
	{
		InitializeComponent();
        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        MySqlConnection conn = new MySqlConnection(connString);
        conn.Open();
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

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new Diagnostics());
    }
}