using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Bei welchem Spieler und für wen das Spielerdaten-Fenster Marktwert und Gehalt zeigt. [ORIGINAL] em.exe Datei
/// 0x041AC, Adressen 0x7D6–0x82B: Gehört der Spieler zu einem anderen Verein als dem des Managers (Vergleich Manager +0x14
/// mit Spielerpool +0x1D, Adressen 0x7D6–0x801), ist das Gehalt immer ausgeblendet (Wert 0 → „-“) und der Marktwert ab
/// „5 − Level“ &lt; 3 (Level 3–5). Transfermarkt-Spieler gehören nie zum Verein des Managers (Befund R8-D01, #253).
/// </summary>
/// <param name="MarketValue">Spielerwert in DM (Drittel-DM / 3) oder 0 für „unbekannt“.</param>
/// <param name="SameClub">Der Spieler gehört zum Verein des Managers.</param>
/// <param name="LevelValue">„5 − Level“ (DGROUP 0xED8, <see cref="TrainingRules.LevelValue"/>).</param>
public readonly record struct PlayerDataContext(long MarketValue, bool SameClub, int LevelValue)
{
    /// <summary>Marktwert sichtbar? (Ausblenden nur bei fremdem Verein und „5 − Level“ &lt; 3.)</summary>
    public bool ShowMarketValue => SameClub || LevelValue >= 3;

    /// <summary>Gehalt sichtbar? (Bei fremdem Verein nie.)</summary>
    public bool ShowSalary => SameClub;

    /// <summary>Ohne Angaben des Aufrufers (Aufruf <c>Show(Player, long)</c>): Marktwert unbekannt, Gehalt sichtbar.</summary>
    public static PlayerDataContext Unknown => new(0, true, 0);

    /// <summary>
    /// Zusammenstellung aus dem Spielzustand. Der Spielerwert wird nach 0x118:0x70 berechnet (<see cref="PlayerValueKind.Price"/>,
    /// Drittel-DM) und durch 3 geteilt (0x041AC, Adressen 0x82C–0x84D). Ändert den Spielzustand nicht: die Zufallszahl
    /// (nur beim Preis eines Spielers mit Angebot) kommt aus einem festen Startwert. [APPROX] Das Original zieht dort die
    /// Zufallszahl des Spiels.
    /// </summary>
    public static PlayerDataContext Create(GameState game, Player player, Club? playerClub, Club ownClub, int level)
    {
        var thirds = game.PlayerValueOf(player, playerClub, PlayerValueKind.Price, new Random(player.Id), level);
        var same = playerClub is not null && ReferenceEquals(playerClub, ownClub);
        return new PlayerDataContext(thirds / 3, same, TrainingRules.LevelValue(level));
    }
}

/// <summary>
/// Zeilen, Felder und Zeichenregeln des Spielerdaten-Fensters. [ORIGINAL] em.exe Datei 0x041AC–0x04C6A (2761 Byte,
/// Basissegment 0x238 + 0x2C), Fensterursprung (89 | 55); Abschnitte siehe <c>docs/reverse-engineering.md</c>, „Spur
/// spielerinfo“. Alles hier ist rein (keine Zeit, kein Zufall, kein Spielzustand).
/// </summary>
public static class PlayerDataWindow
{
    /// <summary>Die sechs Datenfelder in der Reihenfolge des Originals (Tabelle DGROUP 0xC18).</summary>
    public static readonly string[] DataHeads =
        ["Kaufpreis", "Marktwert", "Versicherung", "Sperren in Min.", "Gehalt/M.", "Vertrag"];

    /// <summary>Spaltenköpfe der Tore-/Spiele-Tabelle (Tabelle DGROUP 0xC0C).</summary>
    public static readonly string[] CompetitionHeads = ["Liga", "DEB-Pokal", "Europacup"];

    /// <summary>
    /// Ausschnitt des Gesichts in kb\10 (17 × 21): Spalte = 3 − min((Alter − 18) / 5, 3), Zeile = Talent. [ORIGINAL]
    /// 0x041AC, Adressen 0x335–0x3D1 (Aufruf 0x1115:0xA79 mit Quellversatz Talent · 21 und (3 − Altersstufe) · 17).
    /// </summary>
    public static Rect Face(Player p)
    {
        var stage = Math.Clamp((p.Age - 18) / 5, 0, 3);
        return new Rect((3 - stage) * 17, Math.Clamp(p.Talent, 0, 9) * 21, 17, 21);
    }

    /// <summary>
    /// Sperren in Minuten: 5 · 5-Minuten-Strafen + 2 · 2-Minuten-Strafen. [ORIGINAL] 0x041AC, Adressen 0x763–0x77A
    /// (Kaderplatz +0x00 · 5 + +0x01 · 2).
    /// </summary>
    public static int PenaltyMinutes(Player p) => p.Penalties5 * 5 + p.Penalties2 * 2;

    /// <summary>
    /// Anzahl der Wettbewerbsspalten bei Tore/Spiele: Torhüter nur „Liga“, sonst drei. [ORIGINAL] 0x041AC, Adressen
    /// 0x551–0x570 (Schleifengrenze 3 − 2 · [Rollenwert = 0]).
    /// </summary>
    public static int CompetitionColumns(Player p) => p.Role == Formats.PlayerRole.Goalkeeper ? 1 : 3;

    /// <summary>Beschriftung der Torzeile: bei Torhütern „GTore:“ (Gegentore), sonst „Tore:“. [ORIGINAL] 0x041AC, 0x45D–0x47B.</summary>
    public static string GoalsLabel(Player p) => p.Role == Formats.PlayerRole.Goalkeeper ? "GTore:" : "Tore:";

    /// <summary>Zahl in der Tore-/Spiele-Zeile eines Torhüters bzw. Feldspielers (Liga). Torhüter: Gegentore.</summary>
    public static int LeagueGoals(Player p) => p.Role == Formats.PlayerRole.Goalkeeper ? p.GoalsAgainst : p.Goals;

    /// <summary>
    /// Text der Zeile „Tore:“/„Spiele:“ je Spalte: <c>Zahl( -)</c> bzw. <c>-( -)</c>. [APPROX] Zähler in Klammern (Wert
    /// Vorsaison, Kaderplatz +0x22/+0x1C) und Pokalspalten kennt das Remake nicht (Spur wettbewerbe).
    /// </summary>
    public static string CompetitionCell(Player p, int column, bool goals)
    {
        if (column != 0 || p.Games <= 0)
            return "-( -)";
        return (goals ? LeagueGoals(p) : p.Games) + "( -)";
    }

    /// <summary>
    /// Kaufpreis eines Kaderspielers (Feld 0). [ORIGINAL] 0x041AC Feld 0 = Kaderplatz +0x2C ohne Bit 31 (Start- bzw. Kaufpreis,
    /// <see cref="Player.PurchasePrice"/>).
    /// </summary>
    public static long PurchasePrice(Player p) => p.PurchasePrice;

    /// <summary>Geldbetrag in DM; 0 wird als „-“ gezeigt. [ORIGINAL] 0x041AC, 0x861–0x889 (Zahl 0 = „-“, sonst „ DM“ angehängt).</summary>
    public static string Money(long amount) => amount > 0 ? TransferScreen.Money(amount) : "-";

    /// <summary>
    /// Inhalt der sechs Datenfelder. Reihenfolge wie <see cref="DataHeads"/>. [ORIGINAL] 0x041AC, Adressen 0x684–0x947:
    /// Betrag 0 → „-“, Versicherung 0 → „keine“, sonst die Prämie +0x30 (<see cref="Player.InsurancePremium"/>) in DM,
    /// Vertrag „1 Jahr“ / „n Jahre“, bei geliehenen Spielern „GELIEHEN“. Kaufpreis: <paramref name="price"/> (Transferliste:
    /// Listenpreis = +0x2C des Marktplatzes); für Kaderspieler <see cref="PurchasePrice"/>.
    /// </summary>
    public static string[] DataValues(Player p, long price, PlayerDataContext context) =>
    [
        Money(price),
        context.ShowMarketValue ? Money(context.MarketValue) : "-",
        p.InsurancePremium > 0 ? Money(p.InsurancePremium) : "keine",
        PenaltyMinutes(p) > 0 ? PenaltyMinutes(p).ToString() : "-",
        context.ShowSalary ? Money(p.MonthlySalary) : "-",
        p.OnLoan ? "GELIEHEN" : p.ContractYears > 0 ? Player.YearsText(p.ContractYears) : "-",
    ];

    /// <summary>
    /// Länge des Balkens in Pixeln (1–94). [ORIGINAL] 0x041AC, Adressen 0xBBA–0xBCA: Wert unter 1 wird 1, über 94 wird 94.
    /// </summary>
    public static int BarPixels(int value094) => Math.Clamp(value094, 1, 94);

    /// <summary>Wert 0–99 der Balken-Skala des Remakes in Pixel (0–94) umgerechnet.</summary>
    public static int BarPixelsFromPercent(int value) => BarPixels(Math.Clamp(value, 0, 99) * 94 / 99);
}
