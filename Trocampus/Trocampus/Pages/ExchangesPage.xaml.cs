using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Trocampus.Pages;

public partial class ExchangesPage : ContentPage
{
	public ExchangesPage() => InitializeComponent();

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		try
		{
			var api = App.Services.GetRequiredService<CampusApiService>();
			var appointments = await api.GetAppointmentsAsync();
			AppointmentsList.Children.Clear();
			AppointmentsTitle.Text = $"Rendez-vous à venir · {appointments.Count}";
			foreach (var appointment in appointments)
			{
				var content = new VerticalStackLayout { Spacing = 8 };
				content.Children.Add(new Label { Text = $"{appointment.ListingTitle} · avec {appointment.OtherParty}\n{appointment.Place} · {appointment.ScheduledAt.ToLocalTime():dd/MM/yyyy HH:mm}", Style = (Style)Resources["TcBody14"] });
				content.Children.Add(new Label { Text = "Confirmez le même résultat que l’autre participant pour clôturer l’échange.", Style = (Style)Resources["TcMeta12"] });
				var actions = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 };
				var success = new Button { Text = "Échange réussi", Style = (Style)Resources["TcPrimaryButton"] };
				success.Clicked += async (_, _) => await ConfirmAsync(api, appointment.Id, "success");
				var failed = new Button { Text = "Échange échoué", Style = (Style)Resources["TcSecondaryButton"] };
				failed.Clicked += async (_, _) => await ConfirmAsync(api, appointment.Id, "failed");
				Grid.SetColumn(failed, 1);
				actions.Children.Add(success);
				actions.Children.Add(failed);
				content.Children.Add(actions);
				AppointmentsList.Children.Add(new Border { Padding = 12, StrokeThickness = 0, Content = content });
			}
			if (appointments.Count == 0) AppointmentsList.Children.Add(new Label { Text = "Aucun rendez-vous à confirmer.", Style = (Style)Resources["TcMeta12"] });
		}
		catch (HttpRequestException)
		{
			await DisplayAlert("Serveur indisponible", "Impossible de charger vos échanges.", "OK");
		}
	}

	private async Task ConfirmAsync(CampusApiService api, long appointmentId, string outcome)
	{
		try
		{
			await api.ConfirmExchangeAsync(appointmentId, outcome);
			await DisplayAlert("Confirmation enregistrée", "L’historique sera finalisé lorsque l’autre participant aura confirmé le même résultat.", "OK");
			OnAppearing();
		}
		catch (HttpRequestException)
		{
			await DisplayAlert("Confirmation impossible", "Le serveur n’a pas pu enregistrer le résultat.", "OK");
		}
	}

	private async void OnMessagesClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//messages");

	private async void OnHistoryClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//history");

	private async void OnOffersClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//offers");
}