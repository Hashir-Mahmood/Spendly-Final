namespace MauiApp1234;
using MySqlConnector;
public partial class Budgeting : ContentPage
{
    private readonly string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";

    public Budgeting()
    {
        InitializeComponent();
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync();
                // Do your database operations here

                // The connection will automatically close when the using block ends
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Database Error", ex.Message, "OK");
        }
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

    private void Button_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new Subscription());
    }
}