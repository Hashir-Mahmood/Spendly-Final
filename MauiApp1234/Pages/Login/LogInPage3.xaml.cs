namespace MauiApp1234;

public partial class LogInPage3 : ContentPage
{
	public LogInPage3()
	{
		InitializeComponent();
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