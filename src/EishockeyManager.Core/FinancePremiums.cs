namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// [ORIGINAL] Pokalprämie 500.000 DM: em.exe Datei 0x55CD3–0x55D02 prüft den Verein gegen DGROUP 0x387 und bucht
    /// 0x0016E360 = 1.500.000 Drittel-DM. 0x387 ist der DEB-Pokalsieger (Titelbild 0x350B6: 0xD0E6 Deutscher Meister,
    /// 0x387 DEB-Pokalsieger, 0x388 Europacupsieger). Der Deutsche Meister bekommt nur ein Statusbit (0x55BE4), kein Geld.
    /// Karriere-Video: Pokalsieg 600.000 DM (500.000 + ein 100.000er), Deutscher Meister 200.000 DM (zwei 100.000er).
    /// Unabhängige Nachprüfung, Runde 5 (vorher als Meisterprämie gedeutet).
    /// </summary>
    public const long CupWinnerPremium = 500_000;

    /// <summary>
    /// [ORIGINAL] Einzelprämie: Datei 0x271A9/0x271EF/0x2723C bucht jeweils 0x000493E0 = 300.000 Drittel-DM = 100.000 DM:
    /// Symbol 12 bester Torhüter der eigenen Liga im Kader, Symbol 3 Topscorer der eigenen Liga im Kader (Rangliste
    /// 0xC8:0x20), Symbol 13 Fair-Play-Erster (0xE0:0x20 sortiert die Liga nach Strafminuten).
    /// [APPROX] Diese drei Ereignisse gibt es im Remake noch nicht; bis dahin wird keine Einzelprämie gezahlt
    /// (Restlücke der Spur finanzen-nachpruefung, eigenes Issue).
    /// </summary>
    public const long SinglePremium = 100_000;

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
    /// Tabellenprämie: gibt es im Original nicht. [ORIGINAL] Karriere-Video 0:35:54 „1. Platz der Zweite Liga, Prämien:
    /// keine“; die 100.000 DM bei 0x2723C sind die Fair-Play-Prämie (siehe <see cref="SinglePremium"/>).
    /// </summary>
    public static long Premium(League league, int rank) => 0;

    /// <summary>
    /// Prämien nach tatsächlichen Ereignissen, gemeinsam genau einmal über Book gebucht. Deutscher Meister und
    /// Europapokalsieg bringen im Original kein Geld (unabhängige Nachprüfung, Runde 5).
    /// </summary>
    public static long SeasonPremium(League league, int rank, IReadOnlyCollection<SeasonEvent> events)
    {
        var result = Premium(league, rank);
        if (events.Contains(SeasonEvent.Promotion)) result += PromotionPremium(league);
        if (events.Contains(SeasonEvent.CupWinner)) result += CupWinnerPremium;
        return result;
    }
}
