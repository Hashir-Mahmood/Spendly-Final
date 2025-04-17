namespace MauiApp1234;
using MySqlConnector;
public partial class Budgeting : ContentPage
{
	public Budgeting()
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
}