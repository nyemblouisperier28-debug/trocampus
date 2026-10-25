using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;
namespace Trocampus.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }
    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailField.Text) ||
            string.IsNullOrWhiteSpace(PasswordField.Text))
        {
            await DisplayAlert("Connexion",
                "Renseignez votre adresse e-mail et votre mot de passe.", "OK");
            return;
        }

        if (sender is Button button)
            button.IsEnabled = false;
        try
        {
            var result = await App.Services.GetRequiredService<AuthService>()
                .LoginAsync(EmailField.Text.Trim(), PasswordField.Text);
            if (result.Session is null)
            {
                var message = result.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "Aucun compte actif ne correspond à cette adresse et à ce mot de passe."
                    : result.ErrorMessage ?? "La connexion a échoué. Vérifiez vos informations et réessayez.";
                await DisplayAlert("Connexion impossible", message, "OK");
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
            if (sender is Button loginButton)
                loginButton.IsEnabled = true;
        }
    }
    private async void OnForgotTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//forgot");
    private async void OnRegisterTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//register");
    private async void OnAdminTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//admin");
}