using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// R26-G1 (Gesamtreview Runde 26, Issue #1052): Live-Anzeige des Spieltags (DGROUP 0x5A56) für alle Paarungen des Ablaufs.
/// [ORIGINAL] Die Spielabrechnung 0x544C0 setzt 0x5A56 für alle Paarungen vor der Minutenschleife (Datei 0x54C63–0x54C94);
/// der Strafgrund (Datei 0x35C1C–0x35C7B → 0x52520, Rückkehr bei 0x5A56 = 0 in Datei 0x5252A) und das Verletzungsregister
/// (Datei 0x5419A) lesen in den Minuten diesen Tageswert. Originalprobe (Spielerwahl einer Zweiminutenstrafe, Heimverein mit
/// Manager, ohne Zusehmarkierung, Tempo 60): Zug random(0, 12) nur mit 0x5A56 = 1 (<c>0 []</c> / <c>1 [(0, 12)]</c>).
/// Vor dem Fix rot: Paarungen vor der ersten Anzeigereihe behielten den Wert aus ihrer eigenen Vorbereitung (aus).
/// </summary>
public sealed class LiveTagesmerkerTests
{
    /// <summary>Zwei Manager, Manager A Live aus, Manager B schaut zu (Vereine 2 und 20 in verschiedenen Ligen).</summary>
    private static GameState TwoManagersFirstUnwatched(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 20, StartLeague = League.ZweiteLiga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        game.Options.MatchSpeed = 60;
        game.Options.LiveMode = 0;
        game.ToggleWatch(game.Clubs[game.Managers[0].ClubIndex]);
        Assert.False(game.Clubs[game.Managers[0].ClubIndex].WatchedLive);
        Assert.True(game.Clubs[game.Managers[1].ClubIndex].WatchedLive);
        return game;
    }

    /// <summary>
    /// Nach der Vorbereitung aller Paarungen (<see cref="LiveMatchDay.Begin"/>) gilt die Live-Anzeige des Tages auch für die
    /// Paarung des Managers ohne Zusehmarkierung, die vor der ersten Reihe steht: Strafgrund (<c>DayActive</c>) und
    /// Verletzungsregister (<see cref="TurnManagerRegister.LiveDisplay"/>). Synthetisch, ohne Treiber.
    /// </summary>
    [Fact]
    public void Begin_PairingBeforeFirstRow_UsesDayValue()
    {
        var game = TwoManagersFirstUnwatched(5);
        var a = game.Clubs[game.Managers[0].ClubIndex];
        var b = game.Clubs[game.Managers[1].ClubIndex];
        var random = new Random(1);
        // Paarung von A zuerst (ohne Reihe), dann die von B (mit Reihe) – wie im Ligaablauf Ligen 0 → 2.
        var opponentA = game.Clubs.First(c => c.League == a.League && c != a && !c.WatchedLive);
        var opponentB = game.Clubs.First(c => c.League == b.League && c != b && !c.WatchedLive);
        var day = LiveMatchDay.Begin(game,
        [
            new LiveFixture(a, opponentA, MatchCompetition.League, false, random),
            new LiveFixture(b, opponentB, MatchCompetition.League, false, random),
        ]);
        var first = day.Matches[0];
        Assert.Null(first.Row);
        Assert.NotNull(day.Matches[1].Row);
        Assert.True(first.Setup.DayActive);
        Assert.True(first.HomeTeam!.TurnManager!.LiveDisplay);
        day.Finish();
    }

    /// <summary>
    /// Gegenprobe: ohne Reihe am Tag bleibt die Anzeige aus (DGROUP 0x5A56 = 0); mit Dauer-Eisfeld (Darstellung 2) gilt das
    /// Register in den Minuten als ohne Anzeige (A472, Datei 0x3710B–0x37171), der Strafgrund prüft das Eisfeld selbst.
    /// </summary>
    [Fact]
    public void Begin_DayValue_OffWithoutRowAndRegisterOffOnPermanentRink()
    {
        var game = TwoManagersFirstUnwatched(5);
        game.ToggleWatch(game.Clubs[game.Managers[1].ClubIndex]);
        var a = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs.First(c => c.League == a.League && c != a);
        var day = LiveMatchDay.Begin(game, [new LiveFixture(a, opponent, MatchCompetition.League, false, new Random(1))]);
        Assert.False(day.Matches[0].Setup.DayActive);
        Assert.False(day.Matches[0].HomeTeam!.TurnManager!.LiveDisplay);
        day.Finish();

        var rink = TwoManagersFirstUnwatched(6);
        rink.Options.LiveMode = GameOptions.PermanentRinkLiveMode;
        var ra = rink.Clubs[rink.Managers[0].ClubIndex];
        var rb = rink.Clubs[rink.Managers[1].ClubIndex];
        var r = new Random(1);
        var rinkDay = LiveMatchDay.Begin(rink,
        [
            new LiveFixture(ra, rink.Clubs.First(c => c.League == ra.League && c != ra && !c.WatchedLive), MatchCompetition.League, false, r),
            new LiveFixture(rb, rink.Clubs.First(c => c.League == rb.League && c != rb && !c.WatchedLive), MatchCompetition.League, false, r),
        ]);
        Assert.True(rinkDay.Matches[0].Setup.DayActive);
        Assert.False(rinkDay.Matches[0].HomeTeam!.TurnManager!.LiveDisplay);
        rinkDay.Finish();
    }

    /// <summary>Treiber ohne Eingriff, der die Paarungen der Manager ohne Reihe mitschreibt.</summary>
    private sealed class Probe : ILiveMatchDriver
    {
        public List<string> Lines { get; } = [];

        public void Run(LiveMatchDay day)
        {
            var matches = day.Matches
                .Where(m => m.Stepped && m.Row is null && (m.HomeTeam!.Managed || m.AwayTeam!.Managed)).ToList();
            foreach (var m in matches)
                Lines.Add($"Flag={m.Setup.DayActive}, Register={m.HomeTeam!.TurnManager!.LiveDisplay}");
            while (day.Step())
            {
            }
            foreach (var m in matches)
                Lines.Add($"{m.Home.Index}/{m.Away.Index} {m.Report.HomeGoals}:{m.Report.AwayGoals}");
        }
    }

    /// <summary>
    /// Remake-Gegenprobe aus dem Review (FILE09, Saat 26, zwei Managerzüge, Manager A Live aus, Tempo 60, Darstellung 0):
    /// Die Paarung 0/1 steht vor der ersten Reihe. Mit dem Tageswert (DGROUP 0x5A56 = 1) zieht ihre Zweiminutenstrafe den
    /// Strafgrund, sie endet 1:3 (am Head vor dem Fix 3:5 mit <c>Flag=False, Register=False</c>).
    /// </summary>
    [GameDataFact]
    public void File09_UnwatchedManagerPairing_UsesDayValue()
    {
        var game = OriginalImport.Load(OriginalImportTests.OriginalSave("FILE09.HOK"), ClubDatabase.Load(TestGameData.Load()), 26);
        game.Options.MatchSpeed = 60;
        game.Options.LiveMode = 0;
        game.ToggleWatch(game.Clubs[game.Managers[0].ClubIndex]);
        var probe = new Probe();
        using (MatchEngine.UseLiveDriver(probe))
        {
            game.Continue();
            game.Continue();
        }
        Assert.Equal(["Flag=True, Register=True", "0/1 1:3"], probe.Lines);
    }

    /// <summary>
    /// Ohne Treiber (Ligaspieltag über <c>MatchEngine.PlayMatchDay</c>) dasselbe Ergebnis 1:3 wie mit dem Tageswert und
    /// derselbe Spielstand wie mit Treiber ohne Eingriff (Testlücke zu R26-G1: der Treibervergleich in
    /// <c>LiveVerschraenktTests</c> nutzt auf beiden Seiten dieselbe Vorbereitung und ließ den Fehler durch; hier ist das
    /// Ergebnis selbst festgehalten).
    /// </summary>
    [GameDataFact]
    public void File09_WithoutDriver_SameAsDayValue()
    {
        GameState Run(bool driver)
        {
            var game = OriginalImport.Load(OriginalImportTests.OriginalSave("FILE09.HOK"), ClubDatabase.Load(TestGameData.Load()), 26);
            game.Options.MatchSpeed = 60;
            game.Options.LiveMode = 0;
            game.ToggleWatch(game.Clubs[game.Managers[0].ClubIndex]);
            using (driver ? MatchEngine.UseLiveDriver(new Probe()) : null)
            {
                game.Continue();
                game.Continue();
            }
            return game;
        }
        var without = Run(false);
        var with = Run(true);
        Assert.Equal(SaveGame.Save(with), SaveGame.Save(without));
        var result = without.Results.Last(r => r.Home == 0 && r.Away == 1);
        Assert.Equal((1, 3), (result.HomeGoals, result.AwayGoals));
    }
}
