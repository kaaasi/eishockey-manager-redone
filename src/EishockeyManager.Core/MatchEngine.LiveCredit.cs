using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

/// <summary>
/// S07 (A898, Spur aufstellung Runde 25): Spiele eines Managervereins zählen wie im Original vor dem Spiel und beim Aufstellen im Spiel,
/// nicht am Spielende.
/// <list type="bullet">
/// <item>[ORIGINAL] Die Spielabrechnung 0x544C0 läuft vor den Minuten (<c>MatchGate.cs</c>) und zählt dort jeden Aufgestellten (+0x15 ≠ 0
///   und +0x0A ≠ 0) mit Spiel +0x06/+0x1C + Wettbewerb und setzt „hat gespielt“ (+0x1A Bit 7, Datei 0x54910–0x54973,
///   0x5494C–0x5499E).</item>
/// <item>[ORIGINAL] Im Spiel zählt das Aufstellen 0x41533 jeden neu Aufgestellten ohne Bit 7 sofort, im Wettbewerb nach DGROUP 0xD0C2
///   (Datei 0x415B7–0x41622, <see cref="InMatchSlot"/>); wer danach wieder herausgenommen wird – auch im selben Eingriff –, behält
///   das Spiel.</item>
/// <item>[ORIGINAL] S07 (A874): Für das Land des Bundestrainers gilt dasselbe am Kaderplatz des Satzes 5 (Abrechnung 0x544C0 mit
///   Manager 5, Aufstellen 0x41533 im Spiel mit Kennwert DGROUP 0x2D6 + 5, Datei 0x415B7–0x41622; <see cref="NationalCoach.ClickPlayerInMatch"/>).
///   Gezählt wird im Feld der bisherigen Abrechnung am Spielende (<see cref="NationalSlot"/>).</item>
/// </list>
/// Darstellungszustand der laufenden Paarung, nicht im Spielstand (wie <see cref="LiveTeam"/>); ohne Anmeldung beim Spielbeginn
/// (<see cref="CreditPlayers"/> direkt aufgerufen) zählt die Abrechnung am Spielende wie bisher.
/// </summary>
public static partial class MatchEngine
{
    private sealed record LiveCredit(LiveTeam Team, MatchCompetition Competition, bool National = false);

    /// <summary>Paarung, in der ein Managerverein gerade spielt und deren Spiele schon gezählt sind.</summary>
    private static readonly ConditionalWeakTable<Club, LiveCredit> LiveCredits = new();

    /// <summary>Spielkader, deren Aufgestellte beim Spielbeginn gezählt wurden (<see cref="CreditLineup"/> zählt sie nicht erneut).</summary>
    private static readonly ConditionalWeakTable<LiveSquad, object> CountedSquads = new();

    /// <summary>
    /// Anschluss in <see cref="BeginMatch"/> nach dem Anlegen der Spielkader: die Aufgestellten eines Managervereins zählen ihr Spiel
    /// ([ORIGINAL] Datei 0x54910–0x5499E, vor den Minuten). Das Land des Bundestrainers zählt die Aufgestellten und Eingesetzten
    /// seines Spielkaders (A874, Kaderplätze des Satzes 5). Ohne Spielkader (keine Managerpaarung: kein Eingriff möglich) und für
    /// Computervereine nichts (deren Abrechnung bleibt am Spielende).
    /// </summary>
    private static void CountAtKickOff(GameState game, Club club, LiveTeam? team, MatchCompetition competition)
    {
        LiveCredits.Remove(club);
        if (team?.Squad is not { } squad)
            return;
        if (game.NationalCoach is { } coach && club.Index == coach.Nation)
        {
            if (!squad.NationalCoach)
                return;
            // [ORIGINAL] Datei 0x54910–0x5499E am Satz 5: +0x0A ≠ 0 und +0x15 ≠ 0, danach „hat gespielt“.
            foreach (var p in squad.Players.Where(p => p.Position != 0 && p.Dressed))
            {
                p.Played = true;
                p.Player.CountGame(NationalSlot(competition));
            }
            CountedSquads.AddOrUpdate(squad, squad);
            LiveCredits.AddOrUpdate(club, new LiveCredit(team, competition, National: true));
            return;
        }
        if (club.Index >= FirstForeignClub || club.Players.Count == 0 || !IsManaged(game, club))
            return;
        var slot = CompetitionSlots.Of(competition);
        foreach (var p in squad.Players.Where(p => p.Played))
            p.Player.CountGame(slot);
        CountedSquads.AddOrUpdate(squad, squad);
        LiveCredits.AddOrUpdate(club, new LiveCredit(team, competition));
    }

    /// <summary>Wurden die Spiele dieses Spielkaders schon beim Spielbeginn gezählt?</summary>
    private static bool CountedAtKickOff(LiveSquad squad) => CountedSquads.TryGetValue(squad, out _);

    /// <summary>
    /// Aufstellen im Spiel (<see cref="Club.ClickPlayerInMatch"/>, <see cref="LiveLineup.Apply"/>): ein Spieler ohne „hat gespielt“ bekommt
    /// das Kennzeichen und sofort ein Spiel im Wettbewerb nach DGROUP 0xD0C2 ([ORIGINAL] Datei 0x415B7–0x41622). Ohne laufende Paarung
    /// mit Zählung beim Spielbeginn nichts.
    /// </summary>
    /// <returns>true, wenn der Spieler jetzt gezählt wurde.</returns>
    internal static bool CountPlacedInMatch(Club club, Player player)
    {
        if (!LiveCredits.TryGetValue(club, out var credit) || credit.Team.Squad is not { } squad
            || squad.Players.FirstOrDefault(p => ReferenceEquals(p.Player, player)) is not { } live || live.Played)
            return false;
        live.Played = live.PlayedFromBench = true;
        player.CountGame(credit.National ? NationalSlot(credit.Competition) : InMatchSlot(credit.Competition));
        return true;
    }

    /// <summary>
    /// Feld der Länderspiele (A874). [ORIGINAL] WM: Wettbewerb 8 → Europapokal-Feld +0x08/+0x20 (Datei 0x5494C–0x5499E,
    /// <see cref="CreditWorldCupSquad"/>; beim Aufstellen im Spiel ebenso Feld 2, Datei 0x415E9–0x41606). [APPROX] Andere Länderspiele
    /// (Freundschaftsspiel, Code 0xD) wie die bisherige Abrechnung am Spielende im Liga-Feld (<c>CreditLineup</c>).
    /// </summary>
    private static CompetitionSlot NationalSlot(MatchCompetition competition) =>
        competition == MatchCompetition.WorldCup ? CompetitionSlot.Europe : CompetitionSlot.League;

    /// <summary>Wurden die Spiele des Spielkaders der laufenden Paarung schon beim Spielbeginn gezählt (A874, Abrechnung am Spielende)?</summary>
    private static bool CountedAtKickOff(LiveTeam? team) => team?.Squad is { } squad && CountedAtKickOff(squad);

    /// <summary>Hat die laufende Paarung des Vereins ihre Spiele schon gezählt (S07)?</summary>
    internal static bool CountsInMatch(Club club) => LiveCredits.TryGetValue(club, out _);
}
