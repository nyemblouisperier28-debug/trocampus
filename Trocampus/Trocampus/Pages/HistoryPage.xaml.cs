using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;
namespace Trocampus.Pages;

public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var history = await App.Services.GetRequiredService<CampusApiService>().GetExchangeHistoryAsync();
            HistoryItems.Children.Clear();
            HistorySummaryLabel.Text = $"{history.Count} échange(s) terminé(s)";
            HistoryEmptyLabel.Text = history.Count == 0 ? "Aucun échange enregistré pour le moment." : string.Empty;
            foreach (var item in history)
                HistoryItems.Children.Add(new Label
                {
                    Text = $"{item.ListingTitle} · {item.OtherParty} · {(item.Outcome == "success" ? "Réussi" : "Échoué")} · {item.CompletedAt.ToLocalTime():dd/MM/yyyy}{(string.IsNullOrWhiteSpace(item.FailureNote) ? string.Empty : $"\n{item.FailureNote}")}",
                    Style = (Style)Resources["TcBody14"]
                });
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Serveur indisponible", "Impossible de charger l’historique.", "OK");
        }
    }
    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//exchanges");
    private async void OnAppointmentsTapped(object? sender, TappedEventArgs e) =>
        await Nav.GoAsync("//exchanges");
    private async void OnPointsTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//points");
}