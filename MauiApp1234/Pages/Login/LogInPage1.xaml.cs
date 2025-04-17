using System.ComponentModel.DataAnnotations;

namespace MauiApp1234;

public partial class LogInPage1 : ContentPage
{
	public LogInPage1()
	{
		InitializeComponent();
	}

    private void CreateAccount_Clicked(object sender, EventArgs e)
    {
        // Make previous errors invisible 
        ErrorLabel.IsVisible = false;

        // Validar campo de email
        if (string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            ShowError("No has llenado el campo de Email correctamente");
            return;
        }
        else if (!IsValidEmail(EmailEntry.Text))
        {
            ShowError("Por favor ingresa un email válido");
            return;
        }

        // Validar campo de contraseña
        if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ShowError("No has llenado el campo de Contraseña correctamente");
            return;
        }
        else if (PasswordEntry.Text.Length < 8)
        {
            ShowError("La contraseña debe tener al menos 8 caracteres");
            return;
        }

        // Validar campo de nombre
        if (string.IsNullOrWhiteSpace(FirstNameEntry.Text))
        {
            ShowError("No has llenado el campo de Nombre correctamente");
            return;
        }

        // Validar campo de apellido
        if (string.IsNullOrWhiteSpace(LastNameEntry.Text))
        {
            ShowError("No has llenado el campo de Apellido correctamente");
            return;
        }

        Navigation.PushAsync(new LogInPage2());
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private bool IsValidEmail(string email)
    {
        // Expresión regular simple para validar formato de email
        var regex = new RegularExpressionAttribute(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
        return regex.IsValid(email);
    }

    private void TogglePasswordButton_Clicked(object sender, EventArgs e)
    {
        // Cambiar visibilidad de la contraseña
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

        // También podrías cambiar la imagen del botón aquí
        // TogglePasswordButton.Source = PasswordEntry.IsPassword ? "eye_icon.png" : "eye_slash_icon.png";
    }

    

    

    private void StartSession_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new LogInPage2());
    }
}