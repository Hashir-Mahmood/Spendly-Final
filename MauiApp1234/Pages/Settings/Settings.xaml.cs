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