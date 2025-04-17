namespace MauiApp1234;
using MySqlConnector;
public partial class QuizPage4 : ContentPage
{
	public QuizPage4()
	{
		InitializeComponent();
	}

    private void Button_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new QuizPage5());
    }

    private void Button1_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new InfoHub1());
    }

}