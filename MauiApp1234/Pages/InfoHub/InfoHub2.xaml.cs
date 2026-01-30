namespace MauiApp1234;
using MySqlConnector;
public partial class InfoHub2 : ContentPage
{
	public InfoHub2()
	{
		InitializeComponent();
        string connString = "";
        MySqlConnection conn = new MySqlConnection(connString);
        conn.Open();
    }

    private void applyChanges_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new InfoHub1());
    }

    private void cancel_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new InfoHub1());
    }

    private void ReturnButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }
}
