using Trocampus.Helpers;

namespace Trocampus.Pages;

public partial class ProfilePage : ContentPage
{
	public ProfilePage() => InitializeComponent();

	private async void OnPointsTapped(object? sender, TappedEventArgs e) =>
		await Nav.GoAsync("//points");

	private async void OnHistoryTapped(object? sender, TappedEventArgs e) =>
		await Nav.GoAsync("//history");

	private async void OnListingsClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//listings");

	private async void OnSettingsClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//settings");

	private async void OnEditClicked(object? sender, EventArgs e) =>
		await DisplayAlert("Modifier le profil", "La modification du profil sera disponible lorsque le service de compte sera connecté.", "OK");

	private async void OnLogoutClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//login");
}