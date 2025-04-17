namespace MauiApp1234;
using MySqlConnector;
public partial class QuizPage3 : ContentPage
{
	public QuizPage3()
	{
		InitializeComponent();
        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        MySqlConnection conn = new MySqlConnection(connString);
        conn.Open();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new QuizPage4());
    }

    private void Button1_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new InfoHub1());
    }

}