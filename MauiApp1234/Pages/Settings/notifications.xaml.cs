namespace MauiApp1234.Pages.Settings;

public partial class notifications : ContentPage
{
	public notifications()
	{
		InitializeComponent();
	}

    private void ReturnButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }
}