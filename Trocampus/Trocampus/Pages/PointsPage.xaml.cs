using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;
using Trocampus.Models;
namespace Trocampus.Pages;

public partial class PointsPage : ContentPage
{
    public PointsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var summary = await App.Services.GetRequiredService<AuthService>().GetHomeSummaryAsync();
            if (summary is null)
            {
                await Nav.GoAsync("//login");
                return;
            }

            BalanceLabel.Text = $"{summary.Points} point(s)";
            EarnedLabel.Text = $"+{summary.PointsEarned} gagnés";
            SpentLabel.Text = $"−{summary.PointsSpent} dépensés";
            var movementCount = summary.PointsEarned + summary.PointsSpent;
            TransactionsSummaryLabel.Text = movementCount == 0
                ? "Aucun mouvement · solde initial : 0 pt"
                : $"{movementCount} mouvement(s) · solde : {summary.Points} pt";

            var transactions = await App.Services.GetRequiredService<CampusApiService>().GetPointTransactionsAsync();
            TransactionsList.Children.Clear();
            foreach (var transaction in transactions)
                TransactionsList.Children.Add(new Label
                {
                    Text = $"{transaction.Label} · {transaction.CreatedAt.ToLocalTime():dd/MM/yyyy} · {(transaction.Amount > 0 ? "+" : string.Empty)}{transaction.Amount} pt",
                    Style = (Style)Resources["TcBody14"]
                });
            if (transactions.Count == 0)
                TransactionsList.Children.Add(new Label { Text = "Aucun mouvement enregistré.", Style = (Style)Resources["TcMeta12"] });
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Serveur indisponible", "Impossible de charger vos points. Vérifiez que l’API est démarrée.", "OK");
        }
    }
    private async void OnInviteClicked(object? sender, EventArgs e)
    {
        var email = await DisplayPromptAsync("Inviter une personne", "Adresse e-mail de la personne invitée", "Envoyer", "Annuler", keyboard: Keyboard.Email);
        if (string.IsNullOrWhiteSpace(email)) return;
        try
        {
            await App.Services.GetRequiredService<CampusApiService>().CreateInvitationAsync(email.Trim());
            await DisplayAlert("Invitation enregistrée", "Vous gagnerez 10 points lorsque cette personne créera son compte avec cette adresse.", "OK");
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Invitation impossible", "Vérifiez l’adresse ou réessayez plus tard.", "OK");
        }
    }
    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//profile");
}