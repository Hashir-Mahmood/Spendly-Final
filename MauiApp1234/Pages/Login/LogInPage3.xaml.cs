namespace MauiApp1234;
using MySqlConnector;
public partial class LogInPage3 : ContentPage
{
	public LogInPage3()
	{
		InitializeComponent();
        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        MySqlConnection conn = new MySqlConnection(connString);
        conn.Open();
    }

    private void ReturnButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new LogInPage2());
    }

    private void ResetPasswordButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new LogInPage2());
    }
}