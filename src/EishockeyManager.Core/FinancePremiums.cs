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
    /// 0x000493E0 = 300.000 Drittel-DM = 100.000 DM.
    /// [APPROX] Zuordnung zu Unterliga-Meister, Pokal und Europapokal; die beiden übergebenen
    /// Kennungen und der Aufruf 0xE0:0x20 sind noch nicht vollständig bis zum Erzeuger rekonstruiert.
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
    /// Tabellenprämie ohne Aufstieg/Titel. [APPROX] Nur Platz 1 einer Unterliga bekommt die noch nicht
    /// vollständig zugeordnete 100.000-DM-Einzelprämie. Für Platz 2/3 oder Bundesliga-Hauptrundenplatz 1
    /// gibt es im untersuchten Saisonendepfad keinen belegten pauschalen Betrag.
    /// Oberliga 1 + Aufstieg ergibt damit die im Oberflächenvideo beobachteten 400.000 DM.
    /// </summary>
    public static long Premium(League league, int rank) =>
        league != League.Bundesliga && rank == 1 ? OtherTitlePremium : 0;

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
