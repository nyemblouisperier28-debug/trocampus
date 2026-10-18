using Trocampus.Helpers;

namespace Trocampus.Pages;

public partial class PublishPage : ContentPage
{
	public static IReadOnlyList<string> Categories { get; } = ["Manuels", "Meubles", "Appareils", "Vêtements", "Autre"];
	public static IReadOnlyList<string> Conditions { get; } = ["Neuf", "Très bon état", "Bon état", "État correct"];
	public static IReadOnlyList<string> Locations { get; } = ["Campus Lyon · La Doua", "Campus Lyon · Charpennes"];
	private bool isExchange;

	public PublishPage()
	{
		InitializeComponent();
		CategoryPicker.SelectedIndex = 0;
		ConditionPicker.SelectedIndex = 2;
		LocationPicker.SelectedIndex = 0;
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

		await DisplayAlert("Annonce prête", "L’annonce est enregistrée dans cette démonstration locale.", "OK");
		await Nav.GoAsync("//listings");
	}

	private async void OnSaveDraftClicked(object? sender, EventArgs e) =>
		await DisplayAlert("Brouillon", "Le brouillon est conservé dans cette démonstration locale.", "OK");
}