using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
namespace Trocampus.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }
    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FirstNameField.Text) ||
            string.IsNullOrWhiteSpace(EmailField.Text))
        {
            await DisplayAlert("Inscription", "Renseignez votre prénom et votre adresse e-mail.", "OK");
            return;
        }
        if (!new EmailAddressAttribute().IsValid(EmailField.Text.Trim()))
        {
            await DisplayAlert("Inscription", "Saisissez une adresse e-mail valide.", "OK");
            return;
        }
        if ((PasswordField.Text ?? string.Empty).Length < 10)
        {
            await DisplayAlert("Inscription", "Le mot de passe doit contenir 10 caractères minimum.", "OK");
            return;
        }

        if (sender is Button button)
            button.IsEnabled = false;
        try
        {
            var result = await App.Services.GetRequiredService<AuthService>()
                .RegisterAsync(FirstNameField.Text.Trim(), EmailField.Text.Trim(), PasswordField.Text ?? string.Empty);
            if (result.Session is null)
            {
                var message = result.AccountCreated
                    ? result.ErrorMessage ?? "Le compte est créé, mais la session n’a pas pu démarrer. Essayez de vous connecter."
                    : result.StatusCode == System.Net.HttpStatusCode.Conflict
                        ? "Un compte existe déjà avec cette adresse e-mail. Connectez-vous plutôt."
                        : result.ErrorMessage ?? "La création du compte a échoué. Vérifiez les informations puis réessayez.";
                await DisplayAlert("Inscription impossible", message, "OK");
                return;
            }

            await Nav.GoAsync("//home");
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Serveur indisponible", "Impossible de joindre l’API. Vérifiez qu’elle est démarrée.", "OK");
        }
        finally
        {
            if (sender is Button registerButton)
                registerButton.IsEnabled = true;
        }
    }
    private async void OnLoginTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//login");
}