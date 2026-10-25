using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;
namespace Trocampus.Pages;

public partial class OffersPage : ContentPage
{
	public OffersPage() => InitializeComponent();

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		try
		{
			var api = App.Services.GetRequiredService<CampusApiService>();
			var offers = await api.GetOffersAsync();
			OffersResults.Children.Clear();
			var pending = offers.Where(offer => offer.Status == "pending").ToArray();
			OffersCountLabel.Text = $"À traiter · {pending.Length}";
			foreach (var offer in pending)
			{
				var row = new VerticalStackLayout { Spacing = 8 };
				row.Children.Add(new Label { Text = $"{offer.ListingTitle} · {offer.OtherParty}\n{offer.OfferedPoints} points · {offer.Message}", Style = (Style)Resources["TcBody14"] });
				var actions = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 };
				var accept = new Button { Text = "Accepter", Style = (Style)Resources["TcPrimaryButton"] };
				accept.Clicked += async (_, _) => await DecideAsync(api, offer.Id, "accepted");
				var refuse = new Button { Text = "Refuser", Style = (Style)Resources["TcSecondaryButton"] };
				refuse.Clicked += async (_, _) => await DecideAsync(api, offer.Id, "refused");
				Grid.SetColumn(refuse, 1);
				actions.Children.Add(accept);
				actions.Children.Add(refuse);
				row.Children.Add(actions);
				OffersResults.Children.Add(new Border { Padding = 12, StrokeThickness = 0, Content = row });
			}
			if (pending.Length == 0) OffersResults.Children.Add(new Label { Text = "Aucune offre en attente.", Style = (Style)Resources["TcMeta12"] });
		}
		catch (HttpRequestException)
		{
			await DisplayAlert("Serveur indisponible", "Impossible de charger les offres.", "OK");
		}
	}

	private async Task DecideAsync(CampusApiService api, long offerId, string status)
	{
		try
		{
			await api.DecideOfferAsync(offerId, status);
			OnAppearing();
		}
		catch (HttpRequestException)
		{
			await DisplayAlert("Action impossible", "La proposition n’a pas pu être mise à jour.", "OK");
		}
	}

	private async void OnOfferTapped(object? sender, EventArgs e) =>
		await Nav.GoAsync("//offer-detail");

	private async void OnListingsClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//listings");
}