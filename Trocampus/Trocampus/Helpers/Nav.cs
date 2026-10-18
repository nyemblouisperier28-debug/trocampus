namespace Trocampus.Helpers;

public static class Nav
{
    // Navigation tolérante : une route d’un écran pas encore créé n’arrête pas l’application.
    public static async Task GoAsync(string route)
    {
        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Route indisponible : {route} ({ex.Message})");
        }
    }
}