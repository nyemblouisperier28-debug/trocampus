using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;
namespace Trocampus.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var auth = App.Services.GetRequiredService<AuthService>();
        var session = await App.Services.GetRequiredService<AuthSession>().GetSessionAsync();
        if (session is null)
        {
            await Nav.GoAsync("//login");
            return;
        }

        GreetingLabel.Text = $"Bonjour, {session.FirstName} !";
        AvatarInitialLabel.Text = session.FirstName.Length > 0
            ? session.FirstName[..1].ToUpperInvariant()
            : "?";

        try
        {
            var summary = await auth.GetHomeSummaryAsync();
            if (summary is null)
            {
                await Nav.GoAsync("//login");
                return;
            }

            PointsBalanceSpan.Text = $"{summary.Points} ";
            PointsEarnedSpan.Text = $"+{summary.PointsEarned}";
            PointsSpentLabel.Text = $"−{summary.PointsSpent} dépensés";
            OffersQuickCard.Subtitle = summary.OffersReceived == 0
                ? "Aucune offre reçue"
                : $"{summary.OffersReceived} offre(s) reçue(s)";
            MessagesQuickCard.Subtitle = summary.MessagesUnread == 0
                ? "Aucun message non lu"
                : $"{summary.MessagesUnread} message(s) non lu(s)";
            HistoryQuickCard.Subtitle = summary.ExchangesCompleted == 0
                ? "Aucun échange terminé"
                : $"{summary.ExchangesCompleted} échange(s) terminé(s)";
        }
        catch (HttpRequestException)
        {
            PointsBalanceSpan.Text = "0 ";
            PointsEarnedSpan.Text = "+0";
            PointsSpentLabel.Text = "−0 dépensés";
            OffersQuickCard.Subtitle = "Aucune offre reçue";
            MessagesQuickCard.Subtitle = "Aucun message non lu";
            HistoryQuickCard.Subtitle = "Aucun échange terminé";
        }
    }
    private async void OnSearchCompleted(object? sender, EventArgs e) => await Nav.GoAsync("//search");
    private async void OnPublishClicked(object? sender, EventArgs e) => await Nav.GoAsync("//publish");
    private async void OnPointsTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//points");
    private async void OnListingsTapped(object? sender, EventArgs e) => await Nav.GoAsync("//listings");
    private async void OnOffersTapped(object? sender, EventArgs e) => await Nav.GoAsync("//offers");
    private async void OnMessagesTapped(object? sender, EventArgs e) => await Nav.GoAsync("//messages");
    private async void OnHistoryTapped(object? sender, EventArgs e) => await Nav.GoAsync("//history");
}