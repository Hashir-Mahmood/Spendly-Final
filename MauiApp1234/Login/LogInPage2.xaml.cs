namespace MauiApp1234;

public partial class LogInPage2 : ContentPage
{
	public LogInPage2()
	{
		InitializeComponent();
	}

    private void ForgotPassword_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new LogInPage3());
    }

    private void SignIn_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new Budgeting());
    }
}
