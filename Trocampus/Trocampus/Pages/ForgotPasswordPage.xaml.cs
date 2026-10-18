using Trocampus.Helpers;
namespace Trocampus.Pages;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage()
    {
        InitializeComponent();
    }
    private async void OnSendLinkClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailField.Text))
        {
            await DisplayAlert("Mot de passe oublié", "Renseignez votre adresse e-mail.", "OK");
            return;
        }
        await DisplayAlert("Mot de passe oublié",
            "Si cette adresse existe, un lien vient de vous être envoyé.", "OK");
        await Nav.GoAsync("//login");
    }
    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//login");
}