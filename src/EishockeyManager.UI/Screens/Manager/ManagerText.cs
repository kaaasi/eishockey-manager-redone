using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>Textbausteine der Manager-Bildschirme.</summary>
internal static class ManagerText
{
    /// <summary>Text einer Nachricht für den Original-Font: „%“ fehlt dort, em.exe schreibt dafür „#“.</summary>
    public static string ForFont(string text) => text.Replace('%', '#');

    /// <summary>„1.234 DM“ ohne Vorzeichenwechsel (Formatierung wie im Original).</summary>
    public static string Money(long amount) => Core.GameState.FormatMoney(amount);

    /// <summary>Stückzahl mit Tausenderpunkt („1.000 St.“).</summary>
    public static string Pieces(long count) =>
        count.ToString("#,0", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " St.";
}
