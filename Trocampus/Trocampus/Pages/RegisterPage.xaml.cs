using Trocampus.Helpers;
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
        if ((PasswordField.Text ?? string.Empty).Length < 8)
        {
            await DisplayAlert("Inscription", "Le mot de passe doit contenir 8 caractères minimum.", "OK");
            return;
        }
        await Nav.GoAsync("//home");
    }
    private async void OnLoginTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//login");
}