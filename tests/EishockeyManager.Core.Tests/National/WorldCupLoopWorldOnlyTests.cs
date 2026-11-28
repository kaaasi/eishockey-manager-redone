using EishockeyManager.Core;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur national, Teil 2 (Runde 22): WM-Schleife und Modus „nur Weltkugel“ – Heilung ohne den WM-Kader (A497), Schritt (c) nach der
/// letzten Strafe beim Land des Bundestrainers (A498), Markt-Verfall ohne Untergrenze (A499), Zähler der WM-Spiele am Satz 5 statt am
/// Vereinsplatz (A628) und Mischung der Länder am 29. April (A825). Fundstellen in <c>CupsNational.WorldCupLoop.cs</c>,
/// <c>MatchEngine.National.cs</c> und <c>CupsWorld.cs</c>.
/// </summary>
public class WorldCupLoopWorldOnlyTests
{
    private static GameState CreateWorldOnly(int seed)
    {
        var game = GameModeTests.Create(seed, flag: false, globe: true, 5);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        return game;
    }

    /// <summary>Sprung zur WM und Ernennung, Kader „auto“.</summary>
    private static NationalCoach Appoint(GameState game)
    {
        game.Continue();
        game.AutoNominate();
        Assert.NotNull(game.NationalCoach);
        Assert.NotEmpty(game.NationalCoach!.Squad);
        return game.NationalCoach;
    }

    private static void ContinueToSeasonOver(GameState game, Action<GameState>? eachDay = null)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 3000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            if (game.NationalTurn && game.NationalCoach is { Squad.Count: 0 })
                game.AutoNominate();
            eachDay?.Invoke(game);
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    private static MatchReport Report(params (int Minute, int Side)[] penalties)
    {
        var report = new MatchReport();
        foreach (var (minute, side) in penalties)
            report.Events.Add(new MatchEvent(minute, side, MatchEventKind.Penalty, 2));
        return report;
    }

    /// <summary>
    /// A497: [ORIGINAL] Datei 0x55683–0x5571F und 0x07172: Die Heilung 0x07C12 läuft nur je Manager m &lt; DGROUP [0x386], der
    /// Satz 5 (WM-Kader) heilt nie. Ein verletzter Spieler des WM-Kaders bleibt über die ganze WM verletzt; ein verletzter Spieler
    /// außerhalb des Kaders heilt weiter (Gegenprobe). Rot vor Teil 2: Modus 1 heilte alle Spieler (<c>HealInjuries</c>), der
    /// Kaderspieler hatte am Saisonende 2 statt 4 Spiele Sperre.
    /// </summary>
    [Fact]
    public void WorldOnly_InjuredNationalSquadPlayer_DoesNotHeal()
    {
        var game = CreateWorldOnly(seed: 11);
        var coach = Appoint(game);
        var member = coach.Squad[^1].Player;
        member.InjuredGames = 4;
        var other = game.Clubs.Where(c => c.League == League.ZweiteLiga).SelectMany(c => c.Players)
            .First(p => !coach.Contains(p) && p.InjuredGames == 0);
        other.InjuredGames = 4;
        ContinueToSeasonOver(game);
        Assert.Equal(4, member.InjuredGames);
        Assert.True(other.InjuredGames < 4, "Gegenprobe: Spieler außerhalb des WM-Kaders heilt nicht");
    }

    /// <summary>
    /// A498: [ORIGINAL] Datei 0x35AA8–0x35AC6: Jede Strafe schreibt 0xA70E := Feld +0x16 des bestraften Vereins (− 1, wenn ≠ 0);
    /// Reihenfolge Minute für Minute über alle Paarungen (0x37A62). Die letzte Strafe zählt.
    /// </summary>
    [Fact]
    public void NoteWorldCupPenalties_LastPenaltyDecides()
    {
        var game = CreateWorldOnly(seed: 11);
        var coach = Appoint(game);
        var germany = coach.Nation;
        var other = NationalTeams.First == germany ? NationalTeams.First + 1 : NationalTeams.First;
        var third = Enumerable.Range(NationalTeams.First, 12).First(n => n != germany && n != other);
        const int day = 222;

        game.NoteWorldCupPenalties(day, [(germany, other, Report((10, 1), (40, 0)))]);
        Assert.Equal(day, game.WorldCupLastPenaltyCoachDay); // zuletzt Deutschland (Minute 40, Heim)

        game.NoteWorldCupPenalties(day, [(germany, other, Report((40, 0))), (third, other, Report((41, 1)))]);
        Assert.Equal(0, game.WorldCupLastPenaltyCoachDay); // spätere Minute einer anderen Paarung

        game.NoteWorldCupPenalties(day, [(third, other, Report((40, 1))), (other, germany, Report((40, 1)))]);
        Assert.Equal(day, game.WorldCupLastPenaltyCoachDay); // gleiche Minute: die spätere Paarung

        game.NoteWorldCupPenalties(day, [(germany, other, Report((40, 0))), (third, other, Report((40, 0)))]);
        Assert.Equal(0, game.WorldCupLastPenaltyCoachDay);

        game.NoteWorldCupPenalties(day, [(germany, other, Report((20, 0), (20, 1)))]); // Kettenstrafe des Gegners danach
        Assert.Equal(0, game.WorldCupLastPenaltyCoachDay);

        game.NoteWorldCupPenalties(day, [(germany, other, Report())]); // keine Strafe: 0x36B5D setzt 0
        Assert.Equal(0, game.WorldCupLastPenaltyCoachDay);
    }

    /// <summary>
    /// A498 im Ablauf (Modus 1): Schritt (c) (Markt-Verfall von Manager 0, Datei 0x0718F–0x071CC) läuft nach den WM-Spielen der Tage mit
    /// Zähler ≡ 0/4 (mod 7) – außer die letzte Strafe des Tages traf das Land des Bundestrainers (0xA70E = 5, Datei 0x07172).
    /// Über mehrere Startwerte kommen beide Fälle vor. Rot vor Teil 2: Verfall auch an den Tagen mit letzter Strafe beim eigenen Land.
    /// </summary>
    [Fact]
    public void WorldOnly_StepC_SkippedWhenLastPenaltyHitCoachNation()
    {
        int skipped = 0, run = 0;
        for (var seed = 1; seed <= 12 && (skipped == 0 || run == 0); seed++)
        {
            var game = CreateWorldOnly(seed);
            Appoint(game);
            foreach (var listing in game.TransferMarket.Listings)
                listing.Player.Strain = 150;
            while (game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver)
            {
                var day = game.CurrentDay;
                var before = game.TransferMarket.Listings.Select(l => l.Player.Strain).ToList();
                var listed = game.TransferMarket.Listings.Select(l => l.Player).ToList();
                if (game.NationalTurn && game.NationalCoach is { Squad.Count: 0 })
                    game.AutoNominate();
                game.Continue();
                if (game.CurrentDay != day + 1 || !SeasonCalendar.Events(day).HasFlag(DayEvents.WorldCup)
                    || !MatchEngine.IsDriftDay(game.Counter(day)))
                    continue;
                Assert.Equal(listed, game.TransferMarket.Listings.Select(l => l.Player));
                var decayed = game.TransferMarket.Listings.Select(l => l.Player.Strain).SequenceEqual(before.Select(s => s - 10));
                var unchanged = game.TransferMarket.Listings.Select(l => l.Player.Strain).SequenceEqual(before);
                if (game.WorldCupLastPenaltyCoachDay == day)
                {
                    Assert.True(unchanged, $"Startwert {seed}, Tag {day}: Verfall trotz letzter Strafe beim eigenen Land");
                    skipped++;
                }
                else
                {
                    Assert.True(decayed, $"Startwert {seed}, Tag {day}: kein Verfall");
                    run++;
                }
            }
        }
        Assert.True(skipped > 0, "kein Tag mit letzter Strafe beim eigenen Land gefunden");
        Assert.True(run > 0, "kein Tag mit Schritt (c) gefunden");
    }

    /// <summary>
    /// A499: [ORIGINAL] Datei 0x071BB–0x071C8: Verfall ohne Untergrenze (FILE18 → FILE48: 57–59 → 47–49). Rot vor Teil 2: 50.
    /// </summary>
    [Fact]
    public void MarketDecay_HasNoFloor()
    {
        var game = CreateWorldOnly(seed: 7);
        var listings = game.TransferMarket.Listings;
        Assert.True(listings.Count >= 3, "zu wenige Marktspieler");
        for (var i = 0; i < 3; i++)
            listings[i].Player.Strain = 57 + i;
        game.DailyRunMarket(SeasonCalendar.ClubSeasonEnd + 3, game.Managers[0]);
        Assert.Equal([47, 48, 49], listings.Take(3).Select(l => l.Player.Strain));
        Assert.Equal(47, GameState.MinMarketStrain);
        Assert.All(listings.Take(3), l => Assert.DoesNotContain(PlayerFactory.Validate(game.Clubs[0], l.Player),
            p => p.StartsWith("Belastung", StringComparison.Ordinal)));
    }

    /// <summary>
    /// A628: [ORIGINAL] 0x30E7B (Torindex 0, Datei 0x30EA6; Wettbewerb 8, Datei 0x30F5F/0x31025) bucht Spiele, Tore und Strafen der WM
    /// am Kaderplatz des Satzes 5, nicht am Vereinsplatz (Datei 0x5492E–0x5499E, 0x542F4–0x54333). Über eine ganze WM bleiben Spiele,
    /// Tore und Strafen der Kaderspieler deshalb unverändert. Rot vor Teil 2: jedes WM-Spiel zählte als Ligaspiel am Spieler.
    /// </summary>
    [Fact]
    public void WorldCup_OfCoach_LeavesClubCountersOfSquad()
    {
        var game = CreateWorldOnly(seed: 11);
        var coach = Appoint(game);
        var counters = coach.Squad.ToDictionary(m => m.Source, m => Counters(m.Source)); // A412: Vereinsplatz = Source
        ContinueToSeasonOver(game);
        Assert.Equal(GameState.FinalWorldCupStage, game.WorldCupStage);
        Assert.Contains(game.WorldCup!.Matches, m => m.Home == coach.Nation || m.Away == coach.Nation);
        foreach (var (player, before) in counters)
            Assert.Equal(before, Counters(player));
    }

    private static string Counters(Player p) =>
        $"{p.Games}/{p.CupGames}/{p.EuropeGames}/{p.Goals}/{p.CupGoals}/{p.EuropeGoals}/{p.Penalties2}/{p.Penalties5}";

    /// <summary>
    /// A825: [ORIGINAL] 0x35EDD mischt die Länder am 29. April (Datei 0x5644E, 20 Tausche = 40 Würfe aus dem gemeinsamen Strom), der
    /// WM-Start beim Saisonwechsel (Datei 0x565F6) würfelt nicht. Rot vor Teil 2: die Reihenfolge stand am 29. April noch auf der
    /// alten Saison und wechselte erst in <see cref="GameState.StartNextSeason"/>.
    /// </summary>
    [Fact]
    public void WorldCupOrder_MixedOnSeasonEnd_NotOnSeasonStart()
    {
        var game = CreateWorldOnly(seed: 11);
        var start = game.WorldCup!.Order.ToList();
        ContinueToSeasonOver(game);
        Assert.Equal(start, game.WorldCup.Order); // Gruppen und Ausfuhr der laufenden WM bleiben
        var mixed = game.WorldCup.NextOrder!.ToList();
        Assert.NotEqual(start, mixed);
        var saved = SaveGame.Save(game);
        Assert.Equal(saved, SaveGame.Save(SaveGame.Load(saved).Game!));
        game.StartNextSeason();
        Assert.Null(game.WorldCup.NextOrder);
        Assert.Equal(mixed, game.WorldCup.Order);
        Assert.Equal(mixed.Take(WorldCup.GroupSize), game.WorldCup.Groups[0].Teams);
        Assert.Equal(mixed.Skip(WorldCup.GroupSize), game.WorldCup.Groups[1].Teams);
    }

    /// <summary>A825 ohne WM (Modus „nur Verein“): 0x35EDD zieht trotzdem 20 · 2 Würfe (ohne Modusprüfung an Datei 0x5644E).</summary>
    [Fact]
    public void MixThrows_DrawsFortyValues()
    {
        var a = new GameRandom(5);
        var b = new GameRandom(5);
        WorldCup.MixThrows(a);
        for (var i = 0; i < 2 * WorldCup.Swaps; i++)
            b.Next(0, WorldCup.Teams);
        Assert.Equal(b.Next(), a.Next());
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): Modus 4, vier Saisonwechsel, passiver und aktiver Manager (Bundestrainer möglich). Je Saison:
    /// Länder am 29. April gemischt und beim Saisonwechsel übernommen (A825), Kaderspieler ohne WM-Zähler am Vereinsplatz (A628),
    /// gültige Spieler (Belastung ab 47, A499), Speichern und Laden ohne Unterschied.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mode4_FourSeasonChanges_WorldCupLoopTeil2(bool active)
    {
        var second = Enumerable.Range(0, 200).First(i => NewGame.DatabaseLeague(i) == League.ZweiteLiga);
        var game = GameModeTests.Create(5, true, true, 2, second);
        for (var season = 0; season < 5; season++)
        {
            Dictionary<Player, string>? squad = null;
            ContinueToSeasonOver(game, g =>
            {
                if (active)
                {
                    TestSeason.KeepSquad(g, g.Managers[0], extendExpired: true);
                    ManagedSquads.PlayAtTopStrength(g, 0);
                }
                if (squad is null && g.NationalCoach is { Squad.Count: > 0 } c && g.CurrentDay > SeasonCalendar.ClubSeasonEnd)
                    squad = c.Squad.ToDictionary(m => m.Source, m => Counters(m.Source)); // A412: Vereinsplatz = Source
            });
            if (squad is not null)
                foreach (var (player, before) in squad)
                    Assert.Equal(before, Counters(player));
            foreach (var club in game.Managers.Select(m => game.Clubs[m.ClubIndex]))
                foreach (var p in club.Players)
                    Assert.True(p.Strain is >= GameState.MinMarketStrain and <= TrainingRules.MaxStrain,
                        $"Saison {season}: {p.Name} Belastung {p.Strain}");
            var saved = SaveGame.Save(game);
            Assert.Equal(saved, SaveGame.Save(SaveGame.Load(saved).Game!));
            var mixed = game.WorldCup!.NextOrder!.ToList();
            if (season < 4)
            {
                game.StartNextSeason();
                Assert.Equal(mixed, game.WorldCup.Order);
            }
        }
    }
}
