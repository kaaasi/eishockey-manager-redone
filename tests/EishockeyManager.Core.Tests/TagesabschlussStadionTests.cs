using System.Reflection;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur tagesabschluss Teil 2 (Runde 26, R26-E1 = B): wöchentliche Stadionschäden 0x3CFA0, aufgerufen zuerst im Stadionbau 0x3D316
/// (Datei 0x3D330), im Hauptweg je Manager nach dessen Tageslauf 0x07169 (Datei 0x552A6–0x552C3). Beleg:
/// tools/reverse-engineering/tagesabschluss/stadionschaeden.py (Fälle 1–8) und hauptweg.py Teil 1. Gegen 36323e2 rot.
/// </summary>
public class TagesabschlussStadionTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>Antwortet random(a, b) aus einer Liste (int.MinValue = Untergrenze, int.MaxValue bzw. Listenende = Obergrenze).</summary>
    private sealed class Scripted(params int[] answers) : Random
    {
        private readonly Queue<int> _answers = new(answers);
        public List<(int Lo, int Hi)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            var hi = maxValue - 1;
            Calls.Add((minValue, hi));
            var v = _answers.Count > 0 ? _answers.Dequeue() : int.MaxValue;
            return v == int.MinValue ? minValue : v == int.MaxValue ? hi : v;
        }
    }

    private const int Lo = int.MinValue, Hi = int.MaxValue;

    /// <summary>Stufen wie im Skript: [2, 1, 1, 2, 2, 1, 1, 1, 1], 3000 Sitz- und 4500 Stehplätze.</summary>
    private static Stadium ScriptStadium(int seats = 3000, int standing = 4500, int[]? levels = null)
    {
        var stadium = new Stadium { Seats = seats, Standing = standing };
        levels ??= [2, 1, 1, 2, 2, 1, 1, 1, 1];
        for (var p = 0; p < levels.Length; p++)
            stadium.SetLevel((StadiumFeature)p, levels[p]);
        return stadium;
    }

    private static int[] Levels(Stadium stadium) => Enumerable.Range(0, Stadium.FeatureCount).Select(p => stadium.Level((StadiumFeature)p)).ToArray();

    /// <summary>[ORIGINAL] Fall 1: random(0, 2·d + 8) ≠ 0 (Datei 0x3CFC5) → nur dieser eine Wurf.</summary>
    [Fact]
    public void Gate_NonZero_OneRollNoDamage()
    {
        var random = new Scripted(1);
        var stadium = ScriptStadium();
        Assert.Null(StadiumDamageRules.Draw(stadium, random, levelValue: 2, league: 0));
        Assert.Equal([(0, 12)], random.Calls);
        Assert.Equal([2, 1, 1, 2, 2, 1, 1, 1, 1], Levels(stadium));
    }

    /// <summary>
    /// [ORIGINAL] Fall 2: Sperre 0, alle Chancenwürfe verfehlt – Bereiche random(0, 5000), Sitzkomfort/Zustand random(0, 2500), Plätze
    /// random(0, 3000); Kühlanlage und Verkehrsanbindung (p = 2, 5) nur bei d = 0.
    /// </summary>
    [Theory]
    [InlineData(0, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
    [InlineData(2, new[] { 0, 1, 3, 4, 6, 7, 8, 9, 10 })]
    [InlineData(4, new[] { 0, 1, 3, 4, 6, 7, 8, 9, 10 })]
    public void Gate_Zero_ChanceRollsInProjectOrder(int levelValue, int[] projects)
    {
        var random = new Scripted(0);
        Assert.Null(StadiumDamageRules.Draw(ScriptStadium(), random, levelValue, league: 0));
        var expected = new List<(int, int)> { (0, 2 * levelValue + 8) };
        expected.AddRange(projects.Select(p => (0, p switch { >= 9 => 3000, 3 or 4 => 2500, _ => 5000 })));
        Assert.Equal(expected, random.Calls);
    }

    /// <summary>[ORIGINAL] Fälle 3 und 4: Treffer an Überdachung bzw. Sitzkomfort – Stufe − 1, Meldung, Ende (höchstens ein Schaden).</summary>
    [Fact]
    public void FeatureHit_LowersLevelAndStops()
    {
        var random = new Scripted(0, 0, 3);
        var stadium = ScriptStadium();
        var damage = StadiumDamageRules.Draw(stadium, random, 2, 0);
        Assert.Equal(new StadiumDamage(0, 0), damage);
        Assert.Equal([(0, 12), (0, 5000), (1, 3)], random.Calls);
        Assert.Equal([1, 1, 1, 2, 2, 1, 1, 1, 1], Levels(stadium));
        Assert.Equal(["Der Zahn der Zeit nagt auch an IHREM Stadion...", "Er knabberte etwas an Ihrer Überdachung."], damage!.Lines);

        random = new Scripted(0, Hi, Hi, 0, 3);
        stadium = ScriptStadium();
        damage = StadiumDamageRules.Draw(stadium, random, 2, 0);
        Assert.Equal(new StadiumDamage(3, 0), damage);
        Assert.Equal([(0, 12), (0, 5000), (0, 5000), (0, 2500), (1, 3)], random.Calls);
        Assert.Equal([2, 1, 1, 1, 2, 1, 1, 1, 1], Levels(stadium));
        Assert.Equal(["Ihre Fans sind einfach zu stürmisch...", "Der Sitzkomfort muß leider etwas schlechter beurteilt werden."], damage!.Lines);
    }

    /// <summary>
    /// [ORIGINAL] Fälle 5, 5b, 7: Plätze − ((random(Plätze / 20, Plätze / 6) + 150) / 100) · 100 (Datei 0x3D083–0x3D0C0) –
    /// 5000 Sitzplätze: 400 bzw. 900, 4500 Stehplätze: 300.
    /// </summary>
    [Theory]
    [InlineData(2, 0, 9, 5000, Lo, 250, 833, 400)]
    [InlineData(2, 0, 9, 5000, Hi, 250, 833, 900)]
    [InlineData(0, 2, 10, 4500, Lo, 225, 750, 300)]
    public void PlacesHit_ClosesRoundedHundreds(int levelValue, int league, int project, int places, int roll, int lo, int hi, int closed)
    {
        var misses = levelValue == 0 ? project : project - 2;
        var random = new Scripted([0, .. Enumerable.Repeat(Hi, misses), 0, 3, roll]);
        var stadium = ScriptStadium(seats: project == 9 ? places : 3000, standing: project == 10 ? places : 4500);
        var damage = StadiumDamageRules.Draw(stadium, random, levelValue, league);
        Assert.Equal(new StadiumDamage(project, closed), damage);
        Assert.Equal((lo, hi), random.Calls[^1]);
        Assert.Equal((1, 3), random.Calls[^2]);
        Assert.Equal(places - closed, project == 9 ? stadium.Seats : stadium.Standing);
        Assert.Equal(["Der DEB hat die Sicherheit Ihres Stadions gerügt !",
            $"{closed} {(project == 9 ? "Sitzplätze" : "Stehplätze")} wurden gesperrt."], damage!.Lines);
    }

    /// <summary>
    /// [ORIGINAL] Fall 6: Ligawurf random(1, 3) ≤ Liga → weiter mit dem nächsten Projekt; Bereich auf Stufe 0 → weiter, der Ligawurf fällt
    /// trotzdem (Prüfung der Stufe erst danach, Datei 0x3D256–0x3D268).
    /// </summary>
    [Fact]
    public void LeagueRollAndEmptyFeature_GoOn()
    {
        var random = new Scripted(0, 0, 1, 0, 3);
        var stadium = ScriptStadium(levels: [2, 0, 1, 2, 2, 1, 1, 1, 1]);
        Assert.Null(StadiumDamageRules.Draw(stadium, random, 2, league: 1));
        Assert.Equal([(0, 12), (0, 5000), (1, 3), (0, 5000), (1, 3), (0, 2500), (0, 2500), (0, 5000), (0, 5000), (0, 5000), (0, 3000), (0, 3000)],
            random.Calls);
        Assert.Equal([2, 0, 1, 2, 2, 1, 1, 1, 1], Levels(stadium));
    }

    /// <summary>[ORIGINAL] Fall 8: Meldung je Projekt (Endung „m“ nur beim Kino, Datei 0x3D29A; „Verkehrsanb.“ + „.“).</summary>
    [Theory]
    [InlineData(1, "Er knabberte etwas an Ihrer Anzeigetafel.")]
    [InlineData(2, "Er knabberte etwas an Ihrer Kühlanlage.")]
    [InlineData(4, "Das schlägt sich auch auf den Allgemeinzustand des Stadions nieder.")]
    [InlineData(5, "Er knabberte etwas an Ihrer Verkehrsanb..")]
    [InlineData(6, "Er knabberte etwas an Ihrer Gastronomie.")]
    [InlineData(7, "Er knabberte etwas an Ihrem Kino.")]
    [InlineData(8, "Er knabberte etwas an Ihrer Bowlingbahn.")]
    [InlineData(9, "1.200 Sitzplätze wurden gesperrt.")]
    public void Message_PerProject(int project, string second) =>
        Assert.Equal(second, new StadiumDamage(project, 1200).Lines[1]);

    private static GameState TwoManagers(int seed, int level)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 0, StartLeague = League.Bundesliga, Level = level });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 1, StartLeague = League.Bundesliga, Level = level });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static object? Call(GameState game, string method, params object[] args) =>
        (typeof(GameState).GetMethod(method, Private, args.Select(a => a.GetType()).ToArray())
            ?? throw new InvalidOperationException($"{method} fehlt")).Invoke(game, args);

    private static (int[] Levels, int Seats, int Standing) Snapshot(GameState game, Manager m)
    {
        var s = game.Finance(m).Stadium;
        return (Levels(s), s.Seats, s.Standing);
    }

    /// <summary>
    /// [ORIGINAL] Hauptweg Datei 0x55298–0x552D3 (hauptweg.py Teil 1): Talentsuche, kleine Wanderung, dann je Manager 0x07169 und
    /// sein Stadionbau 0x3D316 mit den Schäden 0x3CFA0. Orakel aus den Einzelteilen in dieser Folge an einem Bautag (Zähler ≡ 0, kein
    /// Angebotstag, kein Monatsletzter), Level 5 (d = 0), Saat mit Schaden bei Manager 1. Vorher (Teil 1) baute das Remake für alle vor
    /// der Wanderung, und der Bau würfelte nicht.
    /// </summary>
    [Fact]
    public void MainPathBuildDay_DamagePerManagerAfterDailyRun()
    {
        var day = Enumerable.Range(1, SeasonCalendar.ClubSeasonEnd - 1).First(d =>
        {
            var date = SeasonCalendar.LayoutDate(d);
            return DayCounter.Of(d) % 7 == 0 && !SponsorRules.IsOfferDay(date) && date.Day != 1
                && date.Day != DateTime.DaysInMonth(date.Year, date.Month);
        });
        var seed = Enumerable.Range(1, 400).First(s =>
        {
            var probe = TwoManagers(s, level: 5);
            probe.AdvanceFinance(day - 1, clubDay: true);
            var before = Snapshot(probe, probe.Managers[1]);
            Call(probe, "OpenClubSeasonDay", day);
            var after = Snapshot(probe, probe.Managers[1]);
            return !before.Levels.SequenceEqual(after.Levels) || before.Seats != after.Seats || before.Standing != after.Standing;
        });
        var game = TwoManagers(seed, level: 5);
        var oracle = TwoManagers(seed, level: 5);
        foreach (var g in new[] { game, oracle })
            g.AdvanceFinance(day - 1, clubDay: true);

        Call(game, "OpenClubSeasonDay", day);

        Call(oracle, "ResolveRenewals");
        oracle.FinanceDay = day;
        Call(oracle, "ExpireMessages");
        oracle.RecordRankSlots();
        for (var i = 0; i < 2; i++)
        {
            Call(oracle, "DayClose", SeasonCalendar.LayoutDate(day), i);
            oracle.UpdateSponsorRating(oracle.Managers[i]);
        }
        Call(oracle, "DailyTalentSearch", day);
        oracle.AdvanceStocks();
        Assert.True(MatchEngine.IsDriftDay(DayCounter.Of(day)));
        MatchEngine.DriftTeamValues(oracle, TeamValueDrift.Weekly);
        Call(oracle, "HealUnmanaged", day);
        foreach (var m in oracle.Managers)
        {
            Call(oracle, "ManagerDailyRun", day, m);
            Call(oracle, "WeeklyManagerFinances", m);
        }
        Call(oracle, "MainPathEventDayLineups", day);
        Call(oracle, "MainPathListChanges", day);

        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(Snapshot(oracle, oracle.Managers[i]).Levels, Snapshot(game, game.Managers[i]).Levels);
            Assert.Equal(Snapshot(oracle, oracle.Managers[i]).Seats, Snapshot(game, game.Managers[i]).Seats);
            Assert.Equal(Snapshot(oracle, oracle.Managers[i]).Standing, Snapshot(game, game.Managers[i]).Standing);
            Assert.Equal(oracle.Finance(oracle.Managers[i]).Inbox.Select(x => string.Join("|", x.Lines)),
                game.Finance(game.Managers[i]).Inbox.Select(x => string.Join("|", x.Lines)));
        }
        Assert.Equal(oracle.Random.Next(), game.Random.Next());
    }

    private static string Signature(GameState game)
    {
        var parts = game.Managers.Select((m, i) =>
        {
            var snap = Snapshot(game, m);
            return $"{string.Join(",", snap.Levels)}/{snap.Seats}/{snap.Standing}/{m.Money}/"
                + string.Join("|", game.Finance(m).Inbox.Select(x => string.Join("~", x.Lines)));
        });
        return string.Join(";", parts) + "#" + game.Random.Next();
    }

    private static int BuildDay(Func<DateOnly, int, bool> extra) =>
        Enumerable.Range(1, SeasonCalendar.ClubSeasonEnd - 1).First(d =>
        {
            var date = SeasonCalendar.LayoutDate(d);
            return DayCounter.Of(d) % 7 == 0 && !SponsorRules.IsOfferDay(date) && date.Day != 1
                && date.Day != DateTime.DaysInMonth(date.Year, date.Month) && extra(date, d);
        });

    /// <summary>
    /// R1034-01 (#1036): [ORIGINAL] WM-Schleife Datei 0x556B5–0x556CD (hauptweg.py Teil 2): je Manager erst Tagesabschluss 0x3ED01 und
    /// Werbebewertung 0x3F833, dann sein Stadionbau 0x3D316 (mit den Schäden 0x3CFA0), dann der nächste Manager; die Börse einmal am
    /// Tagesende (#188). Orakel aus den Einzelteilen an einem Bautag, Level 5, zwei Manager. Gegenprobe „Bau für alle erst nach der
    /// Schleife“ ergibt eine andere Zufallsfolge und muss sich unterscheiden (Empfindlichkeit). Rot bei der Mutation
    /// <c>WeeklyFinances()</c> hinter der Schleife in <c>AdvanceFinance</c>.
    /// </summary>
    [Fact]
    public void WorldCupLoopBuildDay_BuildPerManagerAfterItsRating()
    {
        var day = BuildDay((_, _) => true);
        var found = false;
        for (var seed = 1; seed <= 400 && !found; seed++)
        {
            string Oracle(bool buildAfterLoop)
            {
                var g = TwoManagers(seed, level: 5);
                g.AdvanceFinance(day - 1, clubDay: true);
                Call(g, "ExpireMessages");
                g.RecordRankSlots();
                for (var i = 0; i < 2; i++)
                {
                    Call(g, "DayClose", SeasonCalendar.LayoutDate(day), i);
                    g.UpdateSponsorRating(g.Managers[i]);
                    if (!buildAfterLoop)
                        Call(g, "WeeklyManagerFinances", g.Managers[i]);
                }
                if (buildAfterLoop)
                    foreach (var m in g.Managers)
                        Call(g, "WeeklyManagerFinances", m);
                g.AdvanceStocks();
                return Signature(g);
            }

            var expected = Oracle(buildAfterLoop: false);
            if (expected == Oracle(buildAfterLoop: true))
                continue;
            found = true;
            var game = TwoManagers(seed, level: 5);
            game.AdvanceFinance(day - 1, clubDay: true);
            game.AdvanceFinance(day, clubDay: false);
            Assert.Equal(expected, Signature(game));
        }
        Assert.True(found, "keine Saat gefunden, bei der die Reihenfolge des Baus sichtbar wird");
    }

    /// <summary>
    /// R1034-01 (#1036): [ORIGINAL] Sommerpause Datei 0x564F5–0x56512 (A526): je Kalendertag und Manager erst der Stadionbau 0x3D316
    /// (Zähler ≡ 0 mod 7), dann der Tagesabschluss 0x3ED01, dann die Heilung; die Börse einmal am Tagesende. Orakel aus den Einzelteilen,
    /// Level 5, zwei Manager; Gegenprobe „Bau nach <c>DayClose</c>“ muss sich unterscheiden. Rot bei der Mutation „Bau nach DayClose“
    /// in <c>RunSummerDay</c>.
    /// </summary>
    [Fact]
    public void SummerDay_BuildBeforeDayClosePerManager()
    {
        var counter = GameState.SummerBreakCounters().First(c => c % 7 == 0);
        var found = false;
        for (var seed = 1; seed <= 400 && !found; seed++)
        {
            string Oracle(bool buildAfterClose)
            {
                var g = TwoManagers(seed, level: 5);
                var date = SeasonCalendar.LayoutDate(counter - 1);
                for (var i = 0; i < 2; i++)
                {
                    var m = g.Managers[i];
                    if (!buildAfterClose)
                        Call(g, "WeeklyManagerFinances", m);
                    Call(g, "DayClose", date, i);
                    if (buildAfterClose)
                        Call(g, "WeeklyManagerFinances", m);
                    if (PlayerInjuries.IsHealingDay(counter) && !m.Dismissed)
                        Call(g, "HealSquad", g.Clubs[m.ClubIndex]);
                }
                g.AdvanceStocks();
                return Signature(g);
            }

            var expected = Oracle(buildAfterClose: false);
            if (expected == Oracle(buildAfterClose: true))
                continue;
            found = true;
            var game = TwoManagers(seed, level: 5);
            Call(game, "RunSummerDay", counter);
            Assert.Equal(expected, Signature(game));
        }
        Assert.True(found, "keine Saat gefunden, bei der die Reihenfolge des Baus sichtbar wird");
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv/aktiv, zwei Manager, Geld), Level 5: Schäden treten auf (Stufe oder Plätze sinken ohne
    /// Bau, mit Meldung, soweit der Nachrichtenspeicher Platz hat), Plätze bleiben ≥ 0, zwei gleiche Läufe ergeben denselben Abdruck und dieselben Kontostände. Vor Teil 2
    /// sank nie etwas (0x3CFA0 fehlte).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_StadiumDamage(bool active)
    {
        (string Print, long[] Money, int Damages, int Messages) Run()
        {
            // Fixlauf live-verschraenkt, Rebase auf main 9d9ae39 (Runde 26, R21-D2 = A, R15-E2 = C): Saat 24 statt 23 –
            // verschränkte Liga- und Pokaltermine (0x37A62, #1032) und Stadionschäden je Manager (0x3CFA0, Datei 0x3D330, #1034)
            // zusammen ergeben einen anderen Zufallsverlauf als jede Änderung allein; bei 23 treten im aktiven Lauf nur 3 Schäden
            // auf (mindestens 4), 24 passiv und aktiv. Rückschaltprobe: src/ von 9d9ae39 (main) + Saat 24 passiv rot (Test erst
            // seit #1034). Die Probe hängt an der Saat.
            var game = TwoManagers(24, level: 5);
            var last = game.Managers.Select(m => Snapshot(game, m)).ToArray();
            var seen = new HashSet<Message>(ReferenceEqualityComparer.Instance);
            var damages = 0;
            var messages = 0;
            var print = RngTwinProbeTests.Run(game, active, afterStep: g =>
            {
                for (var i = 0; i < g.Managers.Count; i++)
                {
                    var now = Snapshot(g, g.Managers[i]);
                    Assert.True(now.Seats >= 0 && now.Standing >= 0);
                    damages += now.Levels.Zip(last[i].Levels).Count(x => x.First < x.Second)
                        + (now.Seats < last[i].Seats ? 1 : 0) + (now.Standing < last[i].Standing ? 1 : 0);
                    last[i] = now;
                    foreach (var msg in g.Finance(g.Managers[i]).Inbox.Where(x => seen.Add(x)))
                        if (msg.Lines is ["Der Zahn der Zeit nagt auch an IHREM Stadion...", ..] or ["Ihre Fans sind einfach zu stürmisch...", ..]
                            or ["Der DEB hat die Sicherheit Ihres Stadions gerügt !", ..])
                            messages++;
                }
            });
            return (print, game.Managers.Select(m => m.Money).ToArray(), damages, messages);
        }

        var run = Run();
        var again = Run();
        Assert.True(run.Damages >= 4, $"nur {run.Damages} Schäden");
        // Meldungen höchstens so viele wie Schäden: der Nachrichtenspeicher des Originals kann voll sein (Post, MessageCapacity).
        Assert.InRange(run.Messages, 1, run.Damages);
        Assert.Equal(run.Print, again.Print);
        Assert.Equal(run.Money, again.Money);
    }
}
