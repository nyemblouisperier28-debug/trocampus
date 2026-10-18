using Trocampus.Helpers;

namespace Trocampus.Pages;

public partial class SettingsPage : ContentPage
{
	public static IReadOnlyList<string> Languages { get; } = ["Français", "English"];
	public static IReadOnlyList<string> Themes { get; } = ["Clair", "Sombre", "Système"];

	public SettingsPage()
	{
		InitializeComponent();
		LocationSwitch.IsToggled = Preferences.Get("settings_location", true);
		MessagesSwitch.IsToggled = Preferences.Get("settings_messages", true);
		OffersSwitch.IsToggled = Preferences.Get("settings_offers", true);
		LanguagePicker.SelectedIndex = 0;
		ThemePicker.SelectedIndex = 0;
	}

	private void OnPreferenceChanged(object? sender, ToggledEventArgs e) => SavePreferences();

	private void SavePreferences()
	{
		Preferences.Set("settings_location", LocationSwitch.IsToggled);
		Preferences.Set("settings_messages", MessagesSwitch.IsToggled);
		Preferences.Set("settings_offers", OffersSwitch.IsToggled);
	}

	private async void OnSaveClicked(object? sender, EventArgs e)
	{
		SavePreferences();
		Application.Current!.UserAppTheme = ThemePicker.SelectedIndex switch
		{
			1 => AppTheme.Dark,
			2 => AppTheme.Unspecified,
			_ => AppTheme.Light
		};
		await DisplayAlert("Paramètres", "Vos préférences ont été enregistrées sur cet appareil.", "OK");
	}

	private async void OnDeleteClicked(object? sender, EventArgs e)
	{
		var confirmed = await DisplayAlert("Supprimer le compte",
			"Cette action ne peut pas être annulée. La suppression réelle nécessite le service de compte.",
			"Continuer", "Annuler");
		if (confirmed)
			await Nav.GoAsync("//login");
	}
}