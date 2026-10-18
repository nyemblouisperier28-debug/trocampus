using Trocampus.Helpers;

namespace Trocampus.Pages;

public sealed class AdminDashboardPage : ContentPage
{
    private static readonly Color Ink = Color.FromArgb("#24312C");
    private static readonly Color Muted = Color.FromArgb("#68746E");
    private static readonly Color Green = Color.FromArgb("#23795E");
    private static readonly Color Canvas = Color.FromArgb("#F7F9F7");

    public AdminDashboardPage()
    {
        Title = "Administration";
        BackgroundColor = Canvas;

        var content = new VerticalStackLayout { Padding = new Thickness(24, 28), Spacing = 20 };
        content.Children.Add(new Label
        {
            Text = "Bonjour, Camille",
            FontFamily = "InterRegular",
            FontSize = 28,
            TextColor = Ink
        });
        content.Children.Add(new Label
        {
            Text = "Tableau de bord · Données de démonstration",
            FontSize = 14,
            TextColor = Muted
        });

        var metrics = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 12,
            RowSpacing = 12
        };
        AddMetric(metrics, "864", "Utilisateurs actifs", 0, 0);
        AddMetric(metrics, "312", "Annonces publiées", 0, 1);
        AddMetric(metrics, "184", "Échanges conclus", 1, 0);
        AddMetric(metrics, "18", "Dossiers ouverts", 1, 1);
        content.Children.Add(metrics);

        content.Children.Add(new Label
        {
            Text = "À traiter",
            FontFamily = "InterSemiBold",
            FontSize = 18,
            TextColor = Ink
        });
        content.Children.Add(Action("Plaintes et demandes · 18 dossiers", "//admin-complaints"));
        content.Children.Add(Action("Comptes · 32 suspensions à examiner", "//admin-accounts"));
        content.Children.Add(Action("Explorer un dossier d’enquête", "//admin-case"));

        content.Children.Add(new Label
        {
            Text = "Pilotage",
            FontFamily = "InterSemiBold",
            FontSize = 18,
            TextColor = Ink,
            Margin = new Thickness(0, 8, 0, 0)
        });
        content.Children.Add(Action("Trafic et statistiques", "//admin-traffic"));
        content.Children.Add(Action("Avis et notes", "//admin-reviews"));
        content.Children.Add(Action("Journal d’administration", "//admin-journal"));
        content.Children.Add(Action("Paramètres administrateur", "//admin-settings"));

        Content = new ScrollView { Content = content };
    }

    private static void AddMetric(Grid grid, string value, string title, int row, int column)
    {
        var layout = new VerticalStackLayout { Spacing = 6 };
        layout.Children.Add(new Label { Text = title, FontSize = 13, TextColor = Muted });
        layout.Children.Add(new Label { Text = value, FontSize = 26, FontFamily = "InterSemiBold", TextColor = Ink });
        var border = new Border
        {
            Content = layout,
            Padding = 14,
            Background = Colors.White,
            Stroke = Color.FromArgb("#D9E0DA"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }
        };
        grid.Add(border, column, row);
    }

    private static Button Action(string title, string route) => new()
    {
        Text = title,
        TextColor = Green,
        Background = Colors.White,
        BorderColor = Color.FromArgb("#D9E0DA"),
        BorderWidth = 1,
        CornerRadius = 8,
        HorizontalOptions = LayoutOptions.Fill,
        HeightRequest = 52,
        Command = new Command(async () => await Nav.GoAsync(route))
    };
}

public sealed class AdminSectionPage : ContentPage
{
    private static readonly Color Ink = Color.FromArgb("#24312C");
    private static readonly Color Muted = Color.FromArgb("#68746E");
    private static readonly Color Green = Color.FromArgb("#23795E");
    private readonly VerticalStackLayout content;

    public AdminSectionPage()
    {
        BackgroundColor = Color.FromArgb("#F7F9F7");
        content = new VerticalStackLayout { Padding = new Thickness(24, 24), Spacing = 16 };
        Content = new ScrollView { Content = content };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var route = Shell.Current?.CurrentState.Location.OriginalString ?? "admin-accounts";
        BuildSection(route.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "admin-accounts");
    }

    private void BuildSection(string route)
    {
        content.Children.Clear();
        var (title, description, items) = route switch
        {
            "admin-account-detail" => ("Détail du compte", "Fiche de compte · données de démonstration", new[]
            {
                "Nina Moreau · nina.moreau@campus.fr",
                "Université Lyon 1 · compte actif depuis septembre 2025",
                "8 avis · 5 échanges terminés · 120 points",
                "Aucun signalement confirmé sur ce compte"
            }),
            "admin-complaints" => ("Plaintes et demandes", "File commune · examiner les éléments avant de décider", new[]
            {
                "P-042 · plainte · objet non conforme · priorité haute · en enquête",
                "P-041 · signalement · annonce potentiellement trompeuse · priorité haute",
                "D-017 · demande de suppression de compte · nouvelle",
                "D-016 · correction d’établissement · en attente"
            }),
            "admin-case" => ("Dossier P-042 · plainte", "Objet non conforme · reçu le 7 octobre", new[]
            {
                "Compte concerné : Sami Benali · auteur : Nina Dubois",
                "Annonce : Bouilloire compacte · échange accepté le 6 octobre",
                "Éléments transmis : description, photos et messages joints par le plaignant",
                "Affectation : Camille Bernard · statut : en enquête"
            }),
            "admin-traffic" => ("Trafic et statistiques", "Période de démonstration · 1 au 7 octobre 2026", new[]
            {
                "Sessions : 6 420 · pages vues : 18 460",
                "Utilisateurs actifs : 864 sur 1 280 comptes",
                "Annonces publiées : 312 · échanges : 184",
                "Activité quotidienne : 720, 830, 900, 1 040, 780, 920, 1 230 sessions"
            }),
            "admin-reviews" => ("Avis et notes", "200 avis publiés · 3 avis signalés à examiner", new[]
            {
                "4,8 / 5 · Nina Moreau · échange terminé le 2 octobre",
                "Avis signalé · Sami Benali · objet non conforme",
                "4,9 / 5 · Léa Martin · transaction validée",
                "Avis signalé · annonce de manuels · contenu à vérifier"
            }),
            "admin-journal" => ("Journal d’administration", "Décisions et actions traçables", new[]
            {
                "8 oct. · 10:12 · Camille Bernard · dossier P-042 affecté",
                "8 oct. · 09:48 · Sophie Laurent · compte Nina Dubois consulté",
                "7 oct. · 17:22 · Camille Bernard · demande D-016 mise en attente",
                "7 oct. · 15:03 · Sophie Laurent · signalement P-041 ouvert"
            }),
            "admin-settings" => ("Paramètres administrateur", "Accès et préférences de l’équipe", new[]
            {
                "Compte administrateur : Camille Bernard",
                "Rôle : équipe de modération",
                "Notifications de nouveaux dossiers : activées",
                "Accès aux décisions : journalisation obligatoire"
            }),
            _ => ("Gestion des comptes", "1 280 comptes · 32 suspendus · filtres disponibles", new[]
            {
                "Rechercher un nom ou une adresse e-mail",
                "Nina Moreau · actif · 8 avis · 5 échanges",
                "Sami Benali · à examiner · 1 signalement ouvert",
                "Emma Roux · actif · demande de suppression reçue"
            })
        };

        content.Children.Add(new Label
        {
            Text = "‹  Tableau de bord",
            FontSize = 14,
            TextColor = Green,
            GestureRecognizers = { new TapGestureRecognizer { Command = new Command(async () => await Nav.GoAsync("//admin")) } }
        });
        content.Children.Add(new Label { Text = title, FontSize = 27, TextColor = Ink, FontFamily = "InterRegular" });
        content.Children.Add(new Label { Text = description, FontSize = 14, TextColor = Muted });

        foreach (var item in items)
        {
            var label = new Label { Text = item, FontSize = 14, TextColor = Ink, VerticalOptions = LayoutOptions.Center };
            var border = new Border
            {
                Content = label,
                Padding = 14,
                Background = Colors.White,
                Stroke = Color.FromArgb("#D9E0DA"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }
            };
            if (route == "admin-accounts")
                border.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => await Nav.GoAsync("//admin-account-detail"))
                });
            if (route == "admin-complaints")
                border.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => await Nav.GoAsync("//admin-case"))
                });
            content.Children.Add(border);
        }

        if (route == "admin-case")
        {
            var decision = new Button
            {
                Text = "Ajouter une note au dossier",
                Background = Green,
                TextColor = Colors.White,
                CornerRadius = 8,
                HeightRequest = 50,
                Command = new Command(async () => await DisplayAlert("Dossier", "La note est simulée dans cette démonstration.", "OK"))
            };
            content.Children.Add(decision);
        }
    }
}
