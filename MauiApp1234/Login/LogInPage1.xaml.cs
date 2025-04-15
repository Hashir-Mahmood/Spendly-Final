namespace MauiApp1234;

public partial class LogInPage1 : ContentPage
{
	public LogInPage1()
	{
		InitializeComponent();
	}

    private void CreateAccount_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new LogInPage2());
    }

}