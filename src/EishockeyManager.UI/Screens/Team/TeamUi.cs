using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>Gemeinsame Bausteine der Mannschafts-Bildschirme (Aufstellung, Taktik, Verträge, Transfer, Training).</summary>
public static class TeamUi
{
    /// <summary>Textfarben der Spielerlisten je Art (Video: Torhüter hellblau, Abwehr gelb, Sturm orange).</summary>
    public static byte RoleColor(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => 30,
        PlayerRole.Defender => 24,
        _ => 9,
    };

    /// <summary>Kopfzeilen der Listen.</summary>
    public const byte HeaderColor = Colors.White;

    /// <summary>Linien unter der Kopfzeile und über der Statuszeile.</summary>
    public const byte LineColor = Colors.White;

    /// <summary>Markierung der Zeile unter der Maus (Video: dunkelrote Linien über und unter der Zeile).</summary>
    public const byte HoverColor = 17;

    /// <summary>Gedämpfte Beschriftung („1.“ an den Blöcken, „TW“).</summary>
    public const byte DimColor = 1;

    public const int SmallButtonWidth = 17;
    public const int SmallButtonHeight = 11;

    /// <summary>Kleiner grauer Knopf aus kb\2 (17 × 11) mit mittiger Beschriftung, z. B. Rückennummern der Aufstellung.</summary>
    public static void SmallButton(Surface s, Assets a, int x, int y, string text, bool pressed = false)
    {
        s.Blit(a.Image(2), new Rect(0, 179, SmallButtonWidth, SmallButtonHeight), x, y);
        if (pressed)
            s.Outline(new Rect(x, y, SmallButtonWidth, SmallButtonHeight), Colors.Red);
        if (text.Length > 0)
            s.TextCentered(a.Mini, x + SmallButtonWidth / 2 + 1, y + 3, text, Colors.White);
    }

    /// <summary>Umschalter unten rechts (kb\9, 72 × 16): in der Aufstellung zur Taktik, in der Taktik zurück.</summary>
    public static readonly Rect SwitchButton = new(246, 224, 72, 16);

    public static void DrawSwitch(Surface s, Assets a, bool toTactics) =>
        s.Blit(a.Image(9), new Rect(toTactics ? 72 : 0, 24, 72, 16), SwitchButton.X, SwitchButton.Y);

    /// <summary>Statuszeile unter einer Liste: Erklärung der Spalte bzw. Wert unter der Maus.</summary>
    public static void StatusLine(Surface s, Assets a, int centerX, int y, string? text)
    {
        if (!string.IsNullOrEmpty(text))
            s.TextCentered(a.Mini, centerX, y, text, Colors.White);
    }

    /// <summary>Zeilenmarkierung wie im Original: Linien über und unter der Zeile.</summary>
    public static void HoverRow(Surface s, int x, int y, int width, int pitch)
    {
        s.HLine(x, y - 1, width, HoverColor);
        s.HLine(x, y + pitch - 1, width, HoverColor);
    }

    /// <summary>Kurzbezeichnung der Statusspalte.</summary>
    public static string StatusText(PlayerStatus status) => status switch
    {
        PlayerStatus.InTeam => "IM TEAM",
        PlayerStatus.Injured => "VERL.",
        PlayerStatus.Suspended => "GESP.",
        _ => "RESERVE",
    };

    public static string TrendText(PlayerTrend trend) => trend switch
    {
        PlayerTrend.Falling => "Fallend.",
        PlayerTrend.Rising => "Steigend.",
        _ => "Konstant.",
    };

    /// <summary>Verein des aktiven Managers.</summary>
    public static Club OwnClub(ScreenHost host) => host.Game!.Clubs[host.Game.ActiveManager.ClubIndex];

    /// <summary>Kader in Listenreihenfolge (Torhüter, Abwehr, Sturm; je nach Rückennummer).</summary>
    public static IReadOnlyList<Player> Rows(Club club) => club.SortedPlayers.ToList();
}

/// <summary>
/// Meldungsfenster über einem Bildschirm (z. B. „Der darf da nicht rein !“). Klick oder Taste schließt es.
/// [APPROX] Lage und Größe sind nicht aus dem Video belegt; die Texte stammen aus em.exe.
/// </summary>
public sealed class MessageBox
{
    private string[] _lines = [];

    public bool Visible => _lines.Length > 0;

    public void Show(params string[] lines) => _lines = lines;

    public void Close() => _lines = [];

    /// <summary>Verarbeitet Eingaben, solange die Meldung sichtbar ist. Liefert true, wenn sie die Eingabe verbraucht hat.</summary>
    public bool Update(InputState input)
    {
        if (!Visible)
            return false;
        if (input.LeftClick || input.RightClick || input.Keys.Count > 0 || input.Text.Length > 0)
            Close();
        return true;
    }

    public void Draw(Surface s, Assets a, int centerX = 160, int centerY = 120)
    {
        if (!Visible)
            return;
        var width = Math.Max(110, _lines.Max(l => a.Mini.Measure(l)) + 24);
        var height = 22 + 8 * _lines.Length;
        var r = new Rect(centerX - width / 2, centerY - height / 2, width, height);
        Widgets.Frame(s, a, r, FrameStyle.Orange);
        for (var i = 0; i < _lines.Length; i++)
            s.TextCentered(a.Mini, centerX, r.Y + 11 + 8 * i, _lines[i], Colors.White);
    }
}
