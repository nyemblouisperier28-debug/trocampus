namespace Trocampus.Helpers;

public static class Res
{
    // Lit une couleur de Colors.xaml depuis le code C#.
    public static Color Get(string key) => (Color)Application.Current!.Resources[key];
}