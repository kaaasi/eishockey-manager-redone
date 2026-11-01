using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Texte der rechten Infoleistenhälfte (Gegner, bisherige Begegnungen). [ORIGINAL] em.exe 0x43E21 (Overlay 0x110), Belege
/// in <c>docs/reverse-engineering.md</c>, „Infoleiste 0x43E21“ (Spur optionen-wirkung, Teil 3).
/// </summary>
public static class InfoBarTexts
{
    /// <summary>[ORIGINAL] Breite in Pixeln, ab der der älteste angezeigte Eintrag entfällt (Datei 0x44340: <c>cmp ax,0x55</c>).</summary>
    public const int MaxMeetingsWidth = 85;

    /// <summary>[ORIGINAL] Text ohne Begegnung (DGROUP 0x3574 über Zeiger 0xFD9, mit führendem Leerzeichen).</summary>
    public const string NoMeetings = " Noch keine";

    /// <summary>
    /// Gegnerfeld ohne Spiel. [ORIGINAL] Datei 0x44033–0x44084: ohne weiteren Wettbewerb „Saisonende“ (Zeiger 0xBE0), ohne
    /// Gegner am nächsten Termin „Spielfrei“ (Zeiger 0xC04). Vorher zeigte das Remake während der Saison nichts.
    /// </summary>
    public static string NoMatchText(GameState game) => game.Phase == SeasonPhase.Running ? "Spielfrei" : "Saisonende";

    /// <summary>
    /// Zeile „bisherige Begegnungen“, z. B. „A 5:0,A 5:0,H 4:0,H 8:4“ (folge06_00459). [ORIGINAL] Datei 0x44168–0x4438B:
    /// Einträge aus <see cref="GameState.MeetingEntries"/> mit „,“ verbunden; ohne Eintrag <see cref="NoMeetings"/>; ist der
    /// Text breiter als <see cref="MaxMeetingsWidth"/>, fällt alles bis zum ersten Komma weg (einmal). Ohne Gegner leer.
    /// </summary>
    public static string MeetingsText(GameState game, int? opponent, BitmapFont font)
    {
        if (opponent is not { } o)
            return "";
        var text = string.Join(",", game.MeetingEntries(o));
        if (text.Length == 0)
            return NoMeetings;
        if (font.Measure(text) > MaxMeetingsWidth && text.IndexOf(',') is var comma and >= 0)
            text = text[(comma + 1)..];
        return text;
    }
}
