namespace MauiApp1234.Pages.Settings;

public partial class PasswordResetInstructionsPage : ContentPage
{
	public PasswordResetInstructionsPage()
	{
		InitializeComponent();
	}

    private void ResetPasswordButton_Clicked(object sender, EventArgs e)
    {
        //Go to reset Password Page
        Navigation.PushAsync(new LogInPage3());
    }

    private async void ContactSupportTap_Tapped(object sender, EventArgs e)
    {
        // Mostrar opciones para contactar al soporte
        string action = await DisplayActionSheet(
            "Contact Support",
            "Cancel",
            null,
            "Send Email",
            "Call Support",
            "Live Chat");

        switch (action)
        {
            case "Send Email":
                // Abrir cliente de email o mostrar dirección
                await DisplayAlert("Email de Soporte", "support@spendly.com", "OK");
                break;
            case "Call Support":
                // Iniciar llamada o mostrar número de teléfono
                await DisplayAlert("Teléfono de Soporte", "+1-800-SPENDLY", "OK");
                break;
            case "Live Chat":
                // Navegar a la página de chat
                await DisplayAlert("Chat en Vivo", "Esta función estará disponible próximamente", "OK");
                break;
        }
    }
}