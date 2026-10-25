using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Trocampus.Pages;

public partial class PublishPage : ContentPage
{
	public static IReadOnlyList<string> Categories { get; } = ["Manuels", "Meubles", "Appareils", "Vêtements", "Autre"];
	public static IReadOnlyList<string> Conditions { get; } = ["Neuf", "Très bon état", "Bon état", "État correct"];
	public static IReadOnlyList<string> Locations { get; } = ["Campus Lyon · La Doua", "Campus Lyon · Charpennes"];
	private bool isExchange;
	private IReadOnlyList<Trocampus.Models.CategoryResponse> categories = [];
	private IReadOnlyList<Trocampus.Models.ZoneResponse> zones = [];

	public PublishPage()
	{
		InitializeComponent();
		CategoryPicker.SelectedIndex = 0;
		ConditionPicker.SelectedIndex = 2;
		LocationPicker.SelectedIndex = 0;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		try
		{
			var api = App.Services.GetRequiredService<CampusApiService>();
			categories = await api.GetCategoriesAsync();
			zones = await api.GetZonesAsync();
			CategoryPicker.ItemsSource = categories.Select(item => item.Name).ToArray();
			LocationPicker.ItemsSource = zones.Select(item => $"{item.Campus} · {item.Name}").ToArray();
			CategoryPicker.SelectedIndex = categories.Count > 0 ? 0 : -1;
			LocationPicker.SelectedIndex = zones.Count > 0 ? 0 : -1;
		}
		catch (HttpRequestException)
		{
			await DisplayAlert("Serveur indisponible", "Impossible de charger les catégories et zones.", "OK");
		}
	}

	private async void OnBackTapped(object? sender, TappedEventArgs e) =>
		await Nav.GoAsync("//listings");

	private void OnSaleSelected(object? sender, EventArgs e)
	{
		isExchange = false;
		PriceSection.IsVisible = true;
		SaleButton.Background = Color.FromArgb("#23795E");
		SaleButton.TextColor = Colors.White;
		ExchangeButton.Background = Colors.White;
		ExchangeButton.TextColor = Color.FromArgb("#252D2A");
	}

	private void OnExchangeSelected(object? sender, EventArgs e)
	{
		isExchange = true;
		PriceSection.IsVisible = false;
		ExchangeButton.Background = Color.FromArgb("#23795E");
		ExchangeButton.TextColor = Colors.White;
		SaleButton.Background = Colors.White;
		SaleButton.TextColor = Color.FromArgb("#252D2A");
	}

	private async void OnPublishClicked(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(TitleEntry.Text) || string.IsNullOrWhiteSpace(DescriptionEditor.Text))
		{
			await DisplayAlert("Annonce incomplète", "Ajoutez un titre et une description.", "OK");
			return;
		}

		if (!isExchange && !decimal.TryParse(PriceEntry.Text, out _))
		{
			await DisplayAlert("Prix invalide", "Saisissez un prix valide en euros.", "OK");
			return;
		}

		if (CategoryPicker.SelectedIndex < 0 || LocationPicker.SelectedIndex < 0)
		{
			await DisplayAlert("Annonce incomplète", "Choisissez une catégorie et une zone.", "OK");
			return;
		}
		int? cents = null;
		if (!isExchange && decimal.TryParse(PriceEntry.Text, System.Globalization.NumberStyles.Number,
			System.Globalization.CultureInfo.CurrentCulture, out var euros))
			cents = decimal.ToInt32(decimal.Round(euros * 100m, 0));
		try
		{
			await App.Services.GetRequiredService<CampusApiService>().CreateListingAsync(new(
				categories[CategoryPicker.SelectedIndex].Id, zones[LocationPicker.SelectedIndex].Id,
				TitleEntry.Text.Trim(), DescriptionEditor.Text?.Trim(),
				ConditionPicker.SelectedIndex switch { 0 => "neuf", 1 or 2 => "bon_etat", _ => "usage" },
				isExchange ? "exchange" : "sale", cents, null, null, "active"));
			await DisplayAlert("Annonce publiée", "Votre annonce est visible par les autres comptes.", "OK");
			await Nav.GoAsync("//listings");
		}
		catch (HttpRequestException)
		{
			await DisplayAlert("Publication impossible", "Le serveur n’a pas pu enregistrer l’annonce.", "OK");
		}
	}

	private async void OnSaveDraftClicked(object? sender, EventArgs e) =>
		await DisplayAlert("Brouillon", "Le brouillon est conservé dans cette démonstration locale.", "OK");
}