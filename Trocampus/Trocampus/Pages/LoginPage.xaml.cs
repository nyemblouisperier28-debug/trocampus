using Trocampus.Helpers;
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
        await Nav.GoAsync("//home");
    }
    private async void OnForgotTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//forgot");
    private async void OnRegisterTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//register");
    private async void OnAdminTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//admin");
}