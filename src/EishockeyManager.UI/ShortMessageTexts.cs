using EishockeyManager.Core;

namespace EishockeyManager.UI;

/// <summary>Zwei Zeilen einer Kurzmeldung (<see cref="ShortMessage"/>, Datei 0x0C0B8).</summary>
public readonly record struct ShortMessageText(string Line1, string Line2)
{
    /// <summary>Kurzmeldung mit der Überschrift des Aufrufers (Manager am Zug).</summary>
    public ShortMessage Create(string title) => new(title, Line1, Line2);
}

/// <summary>
/// Bausteine der Kurzmeldungen, deren Aufrufer im Remake in fremden Bildschirmen sitzen (Spur ui-meldungen, Teil 3, UM-4;
/// Aufrufer von Datei 0x0C0B8 nach <c>docs/analyse/re-runde-12.md</c> „UM-4“). Anschluss an die Aufrufer in der Integration
/// (Liste im PR); die Aufrufer zeigen sie mit <c>TeamUi.MessageBox.ShowShort(text.Line1, text.Line2)</c> oder
/// <see cref="ShortMessageText.Create"/>. Alle: ohne Knopf und ohne Eingabe, 75 Takte (Zeile 1 beginnt nie mit „Auto-“).
/// </summary>
public static class ShortMessageTexts
{
    /// <summary>
    /// [ORIGINAL] Datei 0x3DE9F, DGROUP 0x1239/0x123D: Stadion, Projekt gewählt, Baufirma beleidigt (DGROUP 0xCC09 + 11 · Manager +
    /// Projekt ≠ 0); danach keine Auswahl. Bedingung im Remake <see cref="IsBuilderOffended"/>; Aufrufer
    /// <c>StadiumScreen.Select</c> (zeigt bisher den Hinweistext <c>Notice</c>).
    /// </summary>
    public static readonly ShortMessageText BuilderOffended = new("Die Baufirma", "ist noch beleidigt...");

    /// <summary>
    /// [ORIGINAL] Datei 0x46DEF, DGROUP 0x12BD/0x12C1: Transferliste, Linksklick auf einen Spieler des eigenen Kaders mit
    /// angekündigtem Ruhestand (Kaderplatz +0x18 Bit 0x80); statt Angebot/Verkauf nur die Meldung. Bedingung
    /// <see cref="AnnouncedRetirement"/>; Aufrufer <c>TransferScreen</c> (Klick in der Kaderliste).
    /// </summary>
    public static readonly ShortMessageText PlayerRetires = new("Dieser Spieler hört", "am Saisonende auf.");

    /// <summary>
    /// [ORIGINAL] Datei 0x5C559, DGROUP 0x97C/0x980: Laden 0x5C3E3, Kennung „KRON_0.99“ stimmt, die Version danach nicht („EH0“,
    /// DGROUP 0x55E7); Datei schließen, zurück zur Auswahl. Im Remake: Kennung stimmt, Schema neuer als unterstützt oder ohne
    /// Migration (<c>SaveGame.Load</c>); Aufrufer <c>SaveLoadScreen.Load</c> (zeigt bisher ein Meldungsfenster mit dem Grund).
    /// </summary>
    public static readonly ShortMessageText WrongVersion = new("Spielstand aus", "falscher Version !");

    /// <summary>
    /// [ORIGINAL] Datei 0x40EF7, DGROUP 0x1231/0x1235: Torhüter in die Torhüterreihe, wenn Reihe 1 schon sechs Feldspieler und
    /// keinen Torhüter hat. Ohne Entsprechung: Die Aufstellung des Remakes belegt feste Plätze (<c>LineupSlot</c>), einen sechsten
    /// Feldspieler gibt es nicht. Nur als Text für eine spätere Umstellung.
    /// </summary>
    public static readonly ShortMessageText SixSkaters = new("Schon 6 Feldspieler", "auf dem Eis !");

    /// <summary>
    /// [ORIGINAL] Datei 0x414C6, DGROUP 0x50A7/0x50AC: Spieler in die gewählte Reihe, Abwehr- und Sturmhälfte voll. Aufrufer:
    /// Aufstellung des Nationalteams (<c>NationalLineupScreen</c>, A010, Spur nationalteam Runde 13); die Vereinsaufstellung
    /// wählt im Remake weiter den Platz selbst (<c>LineupSlot</c>).
    /// </summary>
    public static readonly ShortMessageText NoRoom = new("Kein", "Platz mehr !");

    /// <summary>[ORIGINAL] Bedingung vor dem Aufruf Datei 0x3DE9F für ein Stadionprojekt (DGROUP 0xCC09 + 11 · Manager + Projekt ≠ 0).</summary>
    public static bool IsBuilderOffended(Stadium stadium, StadiumProject project, StadiumFeature feature) =>
        stadium.IsOffended(project, feature);

    /// <summary>[ORIGINAL] Bedingung vor dem Aufruf Datei 0x46DEF (Kaderplatz +0x18 Bit 0x80 = Ruhestand angekündigt, im Remake <see cref="ContractNotice.Retiring"/>).</summary>
    public static bool AnnouncedRetirement(Player player) => player.Notice == ContractNotice.Retiring;
}
