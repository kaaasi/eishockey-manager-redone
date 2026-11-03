using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Was der Spielerliste über dem Mauszeiger bekannt ist und im Remake nicht im <see cref="Player"/> steht: im Original die
/// Kaderplatz-Felder +0x0A („eingesetzt“) und +0x15 (Reihe 1–4, Bit 0x80 = Marke).
/// </summary>
/// <param name="Dressed">Spieler ist eingesetzt (Kaderplatz +0x0A ≠ 0).</param>
/// <param name="Line">Reihe, in der er aufgestellt ist (Kaderplatz +0x15 &amp; 0x7F), 0 = in keiner.</param>
public readonly record struct PlayerHintInfo(bool Dressed, int Line);

/// <summary>Maße einer Spielerliste für die Hinweiszeile (die acht Argumente von 0x41683).</summary>
/// <param name="OriginX">Linker Rand der Spalten (Argument bp+6).</param>
/// <param name="OriginY">Oberkante der ersten Zeile (bp+8); Zeilenhöhe 6.</param>
/// <param name="RowMax">Letzte Zeile (bp+0xC, bp+0xA = 0 ist die erste).</param>
/// <param name="RightEdge">Rechter Rand, dahinter kein Hinweis (bp+0xE).</param>
/// <param name="Mask">Welche der 18 Spalten die Liste zeigt (bp+0x12, Bit i = Spalte i).</param>
public readonly record struct PlayerListLayout(int OriginX, int OriginY, int RowMax, int RightEdge, uint Mask)
{
    /// <summary>Aufstellung: Aufrufe 0x40077/0x400E0/0x407ED/0x40845 (Spaltenmaske 0xF0C7, Rand 0xE9).</summary>
    public static PlayerListLayout Lineup(int rows) => new(7, 29, rows - 1, 0xE9, 0xF0C7);

    /// <summary>Transferliste: Aufruf 0x46A83 (Spaltenmaske 0x90C6, Rand 155, erste Zeile y = 29).</summary>
    public static PlayerListLayout TransferList(int rows) => new(7, 29, rows - 1, 155, 0x90C6);
}

/// <summary>
/// Hinweiszeile der Spielerlisten („Rückennummer des Spielers“, „Name: …“, „Status: …“). [ORIGINAL] em.exe Datei
/// 0x41683–0x41D1F (Overlay 0x108 + 0x16A3, 1693 Byte, Stubeintrag 0x108:0x34). Die Funktion bestimmt aus der Mausstelle
/// Zeile und Spalte (Spaltenbreiten Tabelle DGROUP 0x39F), baut den Text mit <c>strcpy/strcat</c> (0x1B0B / 0x1A9C) und
/// schreibt ihn mit 0x30:0x885 in die Zeile unter der Liste. Sie ist hier ohne Zeichnen nachgebaut und zustandsrein.
/// </summary>
public static class PlayerHintLine
{
    /// <summary>Spaltenbreiten in Pixeln (DGROUP 0x39F, 18 Byte). Die Spalte einer Liste ist die i-te mit gesetztem Maskenbit.</summary>
    public static readonly int[] ColumnWidths = [11, 18, 57, 13, 12, 12, 14, 36, 12, 14, 12, 12, 13, 27, 40, 16, 35, 38];

    /// <summary>Spaltennummer unter der Maus (0–17) oder -1, wenn der Zeiger außerhalb liegt. Adressen 0x8D–0xB6.</summary>
    public static int Column(PlayerListLayout l, int mouseX)
    {
        if (mouseX < 0 || mouseX > l.RightEdge)
            return -1;
        var offset = mouseX - l.OriginX;
        var sum = 0;
        for (var i = 0; i < ColumnWidths.Length; i++)
        {
            if ((l.Mask >> i & 1) == 0)
                continue;
            sum += ColumnWidths[i];
            if (offset < sum)
                return i;
        }
        return ColumnWidths.Length;
    }

    /// <summary>
    /// Zeile unter der Maus oder -1. Die Teilung läuft zur Null hin (idiv): y von OriginY − 5 bis OriginY + 5 gehört zu
    /// Zeile 0. Adressen 0x2D–0x41, 0x9E–0xAC.
    /// </summary>
    public static int Row(PlayerListLayout l, int mouseY)
    {
        var row = (mouseY - l.OriginY) / 6;
        return row >= 0 && row <= l.RowMax ? row : -1;
    }

    private static readonly string[] RoleNames = ["Torwart", "Abwehrspieler", "Stürmer"];
    private static readonly string[] Trends = ["Fallend.", "Konstant.", "Steigend."];

    private static readonly string[] FixedTexts =
    [
        "Rückennummer des Spielers", "Art des Spielers: ", "Name: ", "", "", "",
        "Spiele gesamt (Liga und Pokal)", "Kondition, Technik, Form ", "Durchschnittsstärke", "", "", "",
        "Tore gesamt (Liga und Pokal)", "2/5 Min.-Strafzeiten", "Momentaner Status des Spielers",
        "Tendenz des Spielers: ", "Dauer des Vertrages mit dem Spieler (in Saisons)", "Monatliches Gehalt des Spielers",
    ];

    /// <summary>
    /// Text der Hinweiszeile für Spalte <paramref name="column"/> (aus <see cref="Column"/>) oder null (leer). Mit
    /// <paramref name="showExhaustion"/> hängt die Tendenz „ (Erschöpfung: n)“ an – nur in der Aufstellung (Rand 0xE9,
    /// Adresse 0x487). <paramref name="exhaustion"/> ist der Wert Kaderplatz +0x13.
    /// Die Verletzungsart (Kaderplatz +0x17) steht in <see cref="Player.InjuryKind"/> (Spur kader-transfer, Teil 2).
    /// [APPROX] Die Sperre in Minuten (+0x0D mit +0x09 Bit 0) ist im Original die Strafbank im laufenden Spiel (Strafe 0x35C02:
    /// +0x0D = 2 bzw. 5, je Minute −1 an 0x5488C/0x5490C, Ende 0x35795, nach dem Spiel gelöscht 0x551E0); das Remake zeigt die
    /// verbleibenden Spiele aus <see cref="Player.SuspendedGames"/>. Die Belastung (+0x13) kommt aus <see cref="Player.Energy"/>
    /// (100 − Energie). Unbekannte Art (ältere Spielstände): „Verletzt.“ ohne Klammer.
    /// </summary>
    public static string? Text(Player p, int column, PlayerHintInfo info, bool showExhaustion)
    {
        string text;
        switch (column)
        {
            case 0:
                text = FixedTexts[0] + (info.Line != 0 ? $" (Reihe: {info.Line})" : "");
                break;
            case 1:
                text = RoleNames[(int)RoleIndex(p)] + (p.OnLoan ? " (ausgeliehen)" : "");
                break;
            case 2:
                text = $"Name: {p.Name} ({p.Age} Jahre)";
                break;
            case 14:
                text = "Status: " + Status(p, info);
                break;
            case 15:
                text = FixedTexts[15] + Trends[(int)p.Trend];
                if (showExhaustion)
                    text += $" (Erschöpfung:{Exhaustion(p) * 10 / 15})";
                break;
            default:
                if (column < 0 || column >= FixedTexts.Length)
                    return null; // Spalte 18 (Schleife ohne Treffer) ist mit Rand und Maske nicht erreichbar
                text = FixedTexts[column];
                if (column == 7)
                    text += $"({p.Strength})";
                break;
        }
        return text.Length == 0 ? null : text;
    }

    private static PlayerRoleIndex RoleIndex(Player p) => p.Role switch
    {
        Formats.PlayerRole.Goalkeeper => PlayerRoleIndex.Goalkeeper,
        Formats.PlayerRole.Defender => PlayerRoleIndex.Defender,
        _ => PlayerRoleIndex.Forward,
    };

    private enum PlayerRoleIndex { Goalkeeper, Defender, Forward }

    /// <summary>Belastung (Kaderplatz +0x13) – [APPROX] aus der Energie des Remakes.</summary>
    private static int Exhaustion(Player p) => Math.Clamp(p.Strain - 50, 0, 100); // [APPROX] wie bisher 100 − Energie, aus der Belastung (R12I-01)

    /// <summary>
    /// Statustext nach 0x41683, Adressen 0x308–0x42F: eingesetzt ohne Reihe → „RESERVE“; sonst nach Kaderplatz +0x09 &amp; 3:
    /// 0 → „IM TEAM“ (eingesetzt) bzw. „Hängt halt so rum...“, 1 → „Noch n Minuten Gesperrt.“, 2 → „Verletzt.“ und „(Art)“ ohne Leerzeichen (DGROUP 0xB84, 0x50BE, Name aus 0xA00 + 4 · +0x17, 0x50C0;
    /// Versätze 0x3D4–0x421). Bei Verletzung und Sperre zugleich gilt die Verletzung (wie <see cref="Club.StatusOf"/>).
    /// </summary>
    private static string Status(Player p, PlayerHintInfo info)
    {
        if (info.Dressed && info.Line == 0)
            return "RESERVE";
        if (p.InjuredGames > 0)
            return p.InjuryName is { } kind ? $"Verletzt.({kind})" : "Verletzt.";
        if (p.SuspendedGames > 0)
            return $"Noch {p.SuspendedGames} Minuten Gesperrt.";
        return info.Dressed ? "IM TEAM" : "Hängt halt so rum...";
    }
}
