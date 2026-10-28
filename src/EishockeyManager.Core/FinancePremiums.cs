namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// [ORIGINAL] Meisterprämie 500.000 DM: em.exe Datei 0x55CD3–0x55D02 prüft den Verein gegen
    /// den Playoff-Sieger DGROUP 0x387 und bucht 0x0016E360 = 1.500.000 Drittel-DM.
    /// Der Sieger wird im Playoff-Endpfad Datei 0x34ED0–0x34ED9 gespeichert.
    /// </summary>
    public const long ChampionPremium = 500_000;

    /// <summary>
    /// [ORIGINAL] Weitere Einzelprämie: Datei 0x271A9/0x271EF/0x2723C bucht jeweils
    /// 0x000493E0 = 300.000 Drittel-DM = 100.000 DM. 0x2723C ist der Hauptrunden-Erste (<see cref="Premium"/>).
    /// [APPROX] 0x271A9 (Symbol 12) und 0x271EF (Symbol 3) hängen an zwei Kennungen aus dem Saisonwechsel
    /// (0xC8:0x20, High-/Low-Byte je Manager), hier Pokal- und Europapokalsieg zugeordnet.
    /// </summary>
    public const long OtherTitlePremium = 100_000;

    /// <summary>
    /// [ORIGINAL] Aufstieg (Statusbit 1 gesetzt, Bit 2 nicht gesetzt): Datei 0x270A8–0x27124.
    /// Nach Umstellung auf Liga 0: 0x0016E360 / 3 = 500.000 DM, sonst 0x000DBBA0 / 3 = 300.000 DM.
    /// </summary>
    public static long PromotionPremium(League from) => from switch
    {
        League.ZweiteLiga => 500_000,
        League.Oberliga => 300_000,
        _ => 0,
    };

    /// <summary>
    /// Tabellenprämie: Erster der Hauptrunde in jeder Liga. [ORIGINAL] Datei 0x27210: <c>0xE0:0x20(0)</c> sortiert die
    /// zwölf Vereine der eigenen Liga nach Punkten und liefert 1, wenn der Verein des Managers vorn liegt (0x2A29D);
    /// dann Symbol 13 und 300.000 Drittel-DM = 100.000 DM (0x2723C). Gilt auch in der Bundesliga (Spur finanzen-2).
    /// </summary>
    public static long Premium(League league, int rank) => rank == 1 ? OtherTitlePremium : 0;

    /// <summary>Prämien nach tatsächlichen Ereignissen, gemeinsam genau einmal über Book gebucht.</summary>
    public static long SeasonPremium(League league, int rank, IReadOnlyCollection<SeasonEvent> events)
    {
        var result = Premium(league, rank);
        if (events.Contains(SeasonEvent.Promotion)) result += PromotionPremium(league);
        if (events.Contains(SeasonEvent.GermanChampion)) result += ChampionPremium;
        if (events.Contains(SeasonEvent.CupWinner)) result += OtherTitlePremium;
        if (events.Contains(SeasonEvent.EuropeanCupWinner)) result += OtherTitlePremium;
        return result;
    }
}
