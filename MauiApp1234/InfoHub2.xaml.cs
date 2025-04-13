namespace MauiApp1234;

public partial class InfoHub2 : ContentPage
{
	public InfoHub2()
	{
		InitializeComponent();
	}

    private void applyChanges_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new InfoHub1());
    }

    private void cancel_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new InfoHub1());
    }
}