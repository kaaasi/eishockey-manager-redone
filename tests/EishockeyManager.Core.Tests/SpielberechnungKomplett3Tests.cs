using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielberechnung-komplett, Teil 3: Verletzungen im Spiel (0x38B8F, Verletzung 0x05B1A, Heilung 0x7C12) und
/// Torschütze 0x541D9/0x386D1. Fundstellen: <c>docs/spielberechnung.md</c> §17.
/// </summary>
public class SpielberechnungKomplett3Tests
{
    /// <summary>Liefert vorgegebene Werte (geprüft gegen die Grenzen) und merkt sich die Grenzen jedes Zugs.</summary>
    private sealed class ScriptedRandom(params int[] values) : Random(0)
    {
        private readonly Queue<int> _values = new(values);
        public List<(int Min, int Max)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue - 1));
            var v = _values.Count > 0 ? _values.Dequeue() : minValue;
            Assert.InRange(v, minValue, maxValue - 1);
            return v;
        }
    }

    private sealed class CountingRandom(int seed) : Random(seed)
    {
        public List<(int Min, int Max)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue - 1));
            return base.Next(minValue, maxValue);
        }
    }

    private static Player P(PlayerRole role, int number) =>
        new() { Name = $"P{number}", Role = role, Number = number, Condition = 60, Technique = 50, Form = 50, Energy = 100 };

    /// <summary>Neun Kaderplätze: Torhüter, zwei Verteidiger und drei Stürmer in Reihe 1, zwei Stürmer in Reihe 2, ein Ersatz.</summary>
    private static LiveSquad Squad()
    {
        var s = new LiveSquad();
        int[] positions = [1, 1, 1, 0x81, 0x81, 0x81, 0x82, 0x82, 0];
        PlayerRole[] roles = [PlayerRole.Goalkeeper, PlayerRole.Defender, PlayerRole.Defender, PlayerRole.Forward,
            PlayerRole.Forward, PlayerRole.Forward, PlayerRole.Forward, PlayerRole.Forward, PlayerRole.Forward];
        for (var i = 0; i < positions.Length; i++)
            s.Players.Add(new LivePlayer { Player = P(roles[i], i + 1), Position = positions[i], Dressed = true, Energy = 90 });
        return s;
    }

    /// <summary>
    /// [ORIGINAL] 0x05B1A mit Schalter Spiel: Form − random(10, 15) (über 20), Art random(0, 21) bis random(0, 15) ≤
    /// Häufigkeit (DGROUP 0x1B9: Art 5 → 1, Art 2 → 6), Dauer random(2, 4) Wochen (DGROUP 0x181, Art 2).
    /// </summary>
    [Fact]
    public void Inflict_FollowsOriginal()
    {
        var player = P(PlayerRole.Forward, 9);
        var random = new ScriptedRandom(12, 5, 3, 2, 6, 3);
        var kind = PlayerInjuries.Inflict(player, inMatch: true, difficulty: 2, random);
        Assert.Equal(2, kind);
        Assert.Equal(38, player.Form);
        Assert.Equal(3, player.InjuredGames);
        Assert.False(player.Available);
        Assert.Equal([(10, 15), (0, 21), (0, 15), (0, 21), (0, 15), (2, 4)], random.Calls);
    }

    /// <summary>
    /// [ORIGINAL] 0x5BED–0x5C33: schwere Art (DGROUP 0x483B, hier Art 0 mit 14 … 28 Wochen) heilt mit
    /// random(0, 2 · (5 − Level) + 10 − 14 / 4) = 0 nie (0xFF); außerhalb des Spiels Arten bis 27, Form ≤ 20 bleibt.
    /// </summary>
    [Fact]
    public void Inflict_SeriousKind_Permanent()
    {
        var player = P(PlayerRole.Defender, 4);
        player.Form = 20;
        var random = new ScriptedRandom(0, 1, 20, 0);
        PlayerInjuries.Inflict(player, inMatch: false, difficulty: 2, random);
        Assert.Equal(20, player.Form);
        Assert.Equal(PlayerInjuries.Permanent, player.InjuredGames);
        Assert.Equal([(0, 27), (0, 15), (14, 28), (0, 11)], random.Calls);
    }

    /// <summary>[ORIGINAL] 0x7C12: eine Woche je sieben Tage weniger, 0xFF bleibt.</summary>
    [Fact]
    public void Heal_Weekly_PermanentStays()
    {
        var game = TestGames.Create(3);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var a = club.Players[0];
        var b = club.Players[1];
        a.InjuredGames = 2;
        b.InjuredGames = PlayerInjuries.Permanent;
        game.HealInjuries(13);
        Assert.Equal(2, a.InjuredGames);
        game.HealInjuries(14);
        game.HealInjuries(21);
        Assert.Equal(0, a.InjuredGames);
        game.HealInjuries(28);
        Assert.Equal(0, a.InjuredGames);
        Assert.Equal(PlayerInjuries.Permanent, b.InjuredGames);
    }

    /// <summary>
    /// [ORIGINAL] 0x38B8F: ein Zug random(0, 20 · clamp(60 − M, 0, 40) + 200 · (5 − Level) + 200) je Managerverein und
    /// Minute; bei 0 Spieler 0x378C5, verletzt und vom Eis. Vorher: keine Verletzungen im Spiel (kein Zug).
    /// </summary>
    [Fact]
    public void Injuries_DrawPerManagedClub_AndInjurePlayerOnIce()
    {
        var team = new LiveTeam { Squad = Squad(), Managed = true, InjuryManager = 0, ManagerValue = 25 };
        var random = new ScriptedRandom(1);
        OriginalMinutes.Injuries(team, new LiveTeam(), 5, new MatchReport(), random);
        Assert.Equal([(0, 20 * 35 + 200 * 2 + 200)], random.Calls);

        var injured = 0;
        for (var seed = 0; seed < 5000 && injured == 0; seed++)
        {
            var t = new LiveTeam { Squad = Squad(), Managed = true, InjuryManager = 0, ManagerValue = 25 };
            var report = new MatchReport();
            OriginalMinutes.Injuries(t, new LiveTeam(), 5, report, new Random(seed));
            foreach (var e in report.Events)
            {
                injured++;
                Assert.Equal(MatchEventKind.Injury, e.Kind);
                var live = t.Squad!.Players.Single(p => p.Player == e.Player);
                Assert.False(live.Dressed);
                Assert.Equal(0, live.Line); // nur Reihe auf dem Eis (0x378C5)
                Assert.InRange(e.Player!.InjuredGames, 1, PlayerInjuries.Permanent);
            }
        }
        Assert.True(injured > 0);
    }

    /// <summary>
    /// [ORIGINAL] 0x38C27/0x38C3F: Managerwert 100 oder weniger als 4 eingesetzte Spieler der Aufstellung beenden die
    /// Funktion – auch für den Manager danach (kein Zug).
    /// </summary>
    [Fact]
    public void Injuries_ShortHandedFirstManager_StopsAll()
    {
        var few = Squad();
        foreach (var p in few.Players.Skip(3))
            p.Dressed = false;
        var first = new LiveTeam { Squad = few, Managed = true, InjuryManager = 0 };
        var second = new LiveTeam { Squad = Squad(), Managed = true, InjuryManager = 1 };
        var random = new CountingRandom(1);
        OriginalMinutes.Injuries(second, first, 5, new MatchReport(), random);
        Assert.Empty(random.Calls);

        var full = new LiveTeam { Squad = Squad(), Managed = true, InjuryManager = 0, ManagerValue = 100 };
        OriginalMinutes.Injuries(full, second, 5, new MatchReport(), random);
        Assert.Empty(random.Calls);
    }

    /// <summary>
    /// [ORIGINAL] 0x386D1: Platz random(0, n), nur Feldspieler der Reihe auf dem Eis; Gewicht Technik · 3 / 5 +
    /// Kondition / 5 + Form / 5 + Gruppe · 72 + Tore · 46 + 700 im Sturm gegen random(0, 3000). Stürmer der Reihe 1:
    /// 30 + 12 + 10 + 144 + 700 = 896.
    /// </summary>
    [Fact]
    public void ScorerIndex_WeightAndLine()
    {
        var team = new LiveTeam { Squad = Squad(), Managed = true };
        // Torhüter (0), Stürmer Reihe 2 (6), Ersatz (8) übersprungen; Stürmer 4: random(0, 3000) = 896 → nein; 897? nein,
        // gewählt erst bei < 896.
        var random = new ScriptedRandom(0, 6, 8, 4, 896, 4, 895);
        Assert.Equal(4, OriginalMinutes.ScorerIndex(team, team.Squad!, assist: false, formula: true, random));
        Assert.Equal([(0, 8), (0, 8), (0, 8), (0, 8), (0, 3000), (0, 8), (0, 3000)], random.Calls);

        // Ohne Schalter (Abschluss ohne Tor): Grundwert random(120, 170) statt der Spielerwerte.
        var miss = new ScriptedRandom(1, 120, 0);
        Assert.Equal(1, OriginalMinutes.ScorerIndex(team, team.Squad!, assist: false, formula: false, miss));
        Assert.Equal([(0, 8), (120, 170), (0, 3000)], miss.Calls);

        // Vorlage: Technik · 3 / 2 = 75, Gruppe · 55; Verteidiger 75 + 12 + 10 + 55 = 152.
        var assist = new ScriptedRandom(2, 152, 2, 151);
        Assert.Equal(2, OriginalMinutes.ScorerIndex(team, team.Squad!, assist: true, formula: true, assist));
        Assert.Equal(4, assist.Calls.Count);
    }

    /// <summary>[ORIGINAL] 0x388AE–0x388EE: nach 250 Zügen ohne Treffer der erste Feldspieler.</summary>
    [Fact]
    public void ScorerIndex_NoHit_FirstSkater()
    {
        var team = new LiveTeam { Squad = Squad(), Managed = true };
        var random = new CountingRandom(5);
        foreach (var p in team.Squad!.Players)
            p.Dressed = false;
        Assert.Equal(1, OriginalMinutes.ScorerIndex(team, team.Squad, assist: false, formula: true, random));
        Assert.Equal(OriginalMinutes.MaxScorerDraws, random.Calls.Count);
    }

    /// <summary>
    /// Tore eines Managervereins: Schütze und Vorlage aus 0x386D1 stehen im Ereignis, sind Feldspieler der Aufstellung
    /// und verschieden; die Gutschrift zählt genau diese. Vorher: Tore nach 0x5028 nach dem Spiel, ohne Spieler im Ereignis.
    /// </summary>
    [Fact]
    public void ManagedGoals_ScorerAndAssistInEvents()
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var lineup = club.Lineup.Players.ToHashSet();
        var goals = 0;
        for (var round = 0; round < 6; round++)
        {
            var opponent = game.Clubs[(club.Index + 1 + round) % 12];
            var before = club.Players.Sum(p => p.Goals);
            var assists = club.Players.Sum(p => p.Assists);
            var report = MatchEngine.PlayWithEvents(game, club, opponent, MatchCompetition.DebCup, knockOut: false,
                new Random(round));
            var side = 0;
            var own = report.Events.Where(e => e.Kind == MatchEventKind.Goal && e.Side == side).ToList();
            Assert.Equal(report.HomeGoals, own.Count);
            foreach (var e in own)
            {
                var scorer = Assert.IsType<Player>(e.Player);
                var assist = Assert.IsType<Player>(e.Assist);
                Assert.NotEqual(scorer, assist);
                Assert.Contains(scorer, lineup);
                Assert.NotEqual(PlayerRole.Goalkeeper, scorer.Role);
                Assert.NotEqual(PlayerRole.Goalkeeper, assist.Role);
            }
            Assert.Equal(before + own.Count, club.Players.Sum(p => p.Goals));
            Assert.Equal(assists + own.Count, club.Players.Sum(p => p.Assists));
            goals += own.Count;
        }
        Assert.True(goals > 0);
    }

    /// <summary>
    /// Mehrsaisonprobe (Kader): über vier Saisonwechsel (passiver und aktiver Manager) verletzen sich Spieler des
    /// Managervereins im Spiel und heilen wieder; die Wochen bleiben im Bereich. Vorher: nie eine Verletzung.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_InjuriesHappenAndHeal(bool active)
    {
        var game = TestGames.Create(7);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var injured = new Dictionary<Player, int>();
        var newInjuries = 0;
        var healed = 0;
        var start = game.Season;
        RngTwinProbeTests.Run(game, active, afterStep: g =>
        {
            foreach (var p in club.Players)
            {
                Assert.InRange(p.InjuredGames, 0, PlayerInjuries.Permanent);
                var before = injured.GetValueOrDefault(p);
                if (p.InjuredGames > before)
                    newInjuries++;
                if (before > 0 && p.InjuredGames == 0)
                    healed++;
                injured[p] = p.InjuredGames;
            }
        }, changes: 4);
        Assert.Equal(start + 4, game.Season);
        Assert.True(newInjuries > 0, "keine Verletzung");
        Assert.True(healed > 0, "keine Heilung");
    }
}
