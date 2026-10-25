using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Trocampus.Pages;

public partial class ProfilePage : ContentPage
{
	public ProfilePage() => InitializeComponent();

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var auth = App.Services.GetRequiredService<AuthService>();
		try
		{
			var user = await auth.GetCurrentUserAsync();
			if (user is null)
			{
				await Nav.GoAsync("//login");
				return;
			}

			FullNameLabel.Text = string.Join(" ", new[] { user.FirstName, user.LastName }
				.Where(part => !string.IsNullOrWhiteSpace(part)));
			CampusRoleLabel.Text = $"{user.Campus} · {user.Zone}";
			EmailLabel.Text = user.Email;
			RatingLabel.Text = user.ReviewCount == 0
				? $"Aucun avis · {user.ExchangesCompleted} échange(s) réussi(s)"
				: $"★  {user.AverageRating:0.0} / 5 · {user.ReviewCount} avis · {user.ExchangesCompleted} échange(s) réussi(s)";
			PointsLabel.Text = user.Points.ToString();
			ExchangesLabel.Text = user.ExchangesCompleted.ToString();
		}
		catch (HttpRequestException)
		{
			await DisplayAlert("Serveur indisponible", "Impossible de charger votre profil. Vérifiez que l’API est démarrée.", "OK");
		}
	}

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

	private async void OnLogoutClicked(object? sender, EventArgs e)
	{
		try
		{
			await App.Services.GetRequiredService<AuthService>().LogoutAsync();
		}
		catch (HttpRequestException)
		{
			await App.Services.GetRequiredService<AuthSession>().ClearAsync();
		}
		await Nav.GoAsync("//login");
	}
}