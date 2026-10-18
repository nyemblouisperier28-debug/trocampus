using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using Trocampus.Helpers;
namespace Trocampus.Controls;

// Puces à retour à la ligne : <controls:FilterChips Items="Tous,Vente,Échange" />
public class FilterChips : ContentView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
        nameof(Items), typeof(string), typeof(FilterChips), string.Empty,
        propertyChanged: (b, _, _) => ((FilterChips)b).Rebuild());
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex), typeof(int), typeof(FilterChips), 0,
        propertyChanged: (b, _, _) => ((FilterChips)b).Rebuild());
    // L’espacement de 8 est porté par la marge de chaque puce ; -8 compense le dernier rang.
    private readonly FlexLayout _flex = new()
    {
        Wrap = FlexWrap.Wrap,
        Margin = new Thickness(0, 0, 0, -8)
    };
    public string Items
    {
        get => (string)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }
    public event EventHandler<int>? SelectionChanged;
    public FilterChips()
    {
        Content = _flex;
    }
    private void Rebuild()
    {
        _flex.Children.Clear();
        var labels = Items.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < labels.Length; i++)
        {
            var index = i;
            var selected = i == SelectedIndex;
            var chip = new Border
            {
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 9),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                BackgroundColor = selected ? Res.Get("TcSurfaceTint") : Colors.White,
                Stroke = new SolidColorBrush(selected ? Res.Get("Primary") : Res.Get("TcBorder")),
                Content = new Label
                {
                    Text = labels[i],
                    FontSize = 13,
                    FontFamily = selected ? "InterSemiBold" : "InterRegular",
                    TextColor = selected ? Res.Get("Primary") : Res.Get("TcMuted")
                }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                SelectedIndex = index;
                SelectionChanged?.Invoke(this, index);
            };
            chip.GestureRecognizers.Add(tap);
            _flex.Children.Add(chip);
        }
    }
}