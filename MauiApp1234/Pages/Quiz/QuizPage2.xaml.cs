namespace MauiApp1234;
using MySqlConnector;
public partial class QuizPage2 : ContentPage
{
	public QuizPage2()
	{
		InitializeComponent();
	}

    private void Button_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new QuizPage3());
    }

    private void Button1_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new InfoHub1());
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        await Launcher.OpenAsync("https://www.sofi.com/learn/content/financial-literacy-quiz/");
    }
}