using EishockeyManager.Core;
using EishockeyManager.Formats;
using Xunit;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielberechnung, Teil 1: Rechenteil des Originals ohne Anzeige (<see cref="OriginalMatch"/>,
/// <see cref="MatchStatistics"/>), Fundstellen in docs/spielberechnung.md. Die Ablauftests geben die Zufallszüge vor
/// und prüfen Reihenfolge und Grenzen der Züge gegen den Ablauf in em.exe.
/// </summary>
public class SpielberechnungTests
{
    /// <summary>Liefert vorgegebene Züge und prüft, dass jeder in den verlangten Grenzen liegt.</summary>
    private sealed class ScriptedRandom(params int[] values) : Random
    {
        private readonly Queue<int> _values = new(values);
        public List<(int Min, int Max)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue - 1));
            Assert.True(_values.Count > 0, $"Kein Zug mehr vorgegeben für random({minValue}, {maxValue - 1}).");
            var v = _values.Dequeue();
            Assert.InRange(v, minValue, maxValue - 1);
            return v;
        }

        public bool Exhausted => _values.Count == 0;
    }

    [Fact]
    public void FullResult_EqualTeams_FollowsTheOriginalDrawOrder()
    {
        // d = 0: Angriffszuschläge random(0,0) ×2, Heim random(1,2), Zweig d ≤ 0: Heim + random(1,3), x/y = random(1,2),
        // Gast + x; dann zwei Münzwürfe (0 → Heim; 1 und random(0,0) = 0 → Gast).
        var random = new ScriptedRandom(0, 0, 1, 2, 1, 2, 0, 1, 0);
        var result = OriginalMatch.FullResult(new MatchSide(150), new MatchSide(150), worldCup: false, playoff: false, random);
        Assert.Equal((4, 2), result);
        Assert.True(random.Exhausted);
        Assert.Equal([(0, 0), (0, 0), (1, 2), (1, 3), (1, 2), (1, 2), (0, 1), (0, 1), (0, 0)], random.Calls);
    }

    [Fact]
    public void FullResult_ClearlyStrongerHome_UsesBetterOfTwoAndCapFive()
    {
        // d = 90/3 − 30/3 = 20 > 19 → Grenze 5; d > 8 und random(0,2) = 0 → besserer von x/y (Münze 1, 5 > 3 → 5);
        // d = 20 < 25 → Gast + random(1,2).
        var random = new ScriptedRandom(0, 0, 2, 5, 3, 0, 1, 1, 0, 1, 0);
        var result = OriginalMatch.FullResult(new MatchSide(90), new MatchSide(30), false, false, random);
        // Heim 2 + 5 = 7, Gast 1; Münzen: (0) → Heim 8; (1, random(0,0)=0) → Gast 2.
        Assert.Equal((8, 2), result);
        Assert.True(random.Exhausted);
    }

    [Fact]
    public void FullResult_ShortHandedOrWithoutLineup_FollowsTheOriginal()
    {
        var even = new int[] { 0, 0, 1, 2, 1, 2, 0, 1, 0 }; // wie oben: 4:2
        Assert.Equal((1, 2), OriginalMatch.FullResult(new MatchSide(150, Skaters: 4), new MatchSide(150), false, false, new ScriptedRandom(even)));
        Assert.Equal((4, 0), OriginalMatch.FullResult(new MatchSide(150), new MatchSide(150, Skaters: 3), false, false, new ScriptedRandom(even)));
        // Play-off: je Seite x / 2 + 1.
        Assert.Equal((3, 2), OriginalMatch.FullResult(new MatchSide(150), new MatchSide(150), false, playoff: true, new ScriptedRandom(even)));

        // Ohne Stärke (keine Aufstellung): 0 Tore, der Gegner bekommt 9, wenn er über 20 hat.
        var r = new Random(1);
        Assert.Equal((0, 9), OriginalMatch.FullResult(new MatchSide(0), new MatchSide(100), false, false, r));
        Assert.Equal((9, 0), OriginalMatch.FullResult(new MatchSide(100), new MatchSide(0), false, false, r));
    }

    [Fact]
    public void FullResult_Distribution_IsInTheRangeOfIceHockey()
    {
        var random = new Random(7);
        long home = 0, away = 0, homeWins = 0;
        const int games = 20_000;
        for (var i = 0; i < games; i++)
        {
            var (h, a) = OriginalMatch.FullResult(new MatchSide(160), new MatchSide(160), false, false, random);
            home += h;
            away += a;
            if (h > a)
                homeWins++;
        }
        // Erwartung aus dem Ablauf: Heim 1,5 + 2 + 1, Gast 1,5 + 1 (ohne Angriffszuschlag ist random(0, 0) immer 0,
        // jede Seite bekommt ihren Punkt) → 4,5 : 2,5. Der Heimvorteil steckt im Zweig d ≤ 0 (Datei 0xA19B).
        Assert.InRange(home / (double)games, 4.35, 4.65);
        Assert.InRange(away / (double)games, 2.35, 2.65);
        Assert.True(homeWins > games / 2);
    }

    [Fact]
    public void FullResult_StrongerTeamScoresMore()
    {
        var random = new Random(3);
        long strong = 0, weak = 0;
        for (var i = 0; i < 5_000; i++)
        {
            var (h, a) = OriginalMatch.FullResult(new MatchSide(120), new MatchSide(210), false, false, random);
            weak += h;
            strong += a;
        }
        Assert.True(strong > weak * 1.3, $"stark {strong}, schwach {weak}");
    }

    [Fact]
    public void MinuteChances_TwoAwayHitsCancelLikeTheOriginal()
    {
        // Budget wie oben 4:2 (9 Züge), dann vier Heim-Züge ohne Treffer, zwei Gast-Treffer → Zähler 4, Bit 1 aus.
        var random = new ScriptedRandom(0, 0, 1, 2, 1, 2, 0, 1, 0, 2, 2, 2, 2, 1, 1);
        var flags = OriginalMatch.MinuteChances(new MatchSide(150), new MatchSide(150), 10, 20, false, false, random);
        Assert.Equal(4, flags);
        Assert.Equal(0, flags & 2);
        Assert.True(random.Exhausted);
    }

    [Fact]
    public void MinuteChances_PerMinuteRate_FollowsTheBudget()
    {
        var random = new Random(11);
        int home = 0, away = 0;
        const int minutes = 60_000;
        for (var m = 0; m < minutes; m++)
        {
            var f = OriginalMatch.MinuteChances(new MatchSide(160), new MatchSide(160), 1 + m % 60, 20 * (1 + m % 60 / 20), false, false, random);
            home += f & 1;
            away += (f & 2) >> 1;
        }
        // Chance je Minute 1 − (19/20)^Budget (Heim etwa 4,5 Punkte); Gast-Bit nur bei ungerader Trefferzahl (etwa 2,5 Punkte).
        Assert.InRange(home / (double)minutes, 0.15, 0.22);
        Assert.InRange(away / (double)minutes, 0.07, 0.13);
    }

    [Fact]
    public void LineValuesAndTeamStrength_MatchTheHandCalculation()
    {
        var team = new PartValues(80, 70, 50);
        // Reihe 0: +4; ohne Spiel 100 % Energie, keine Einsatzminderung.
        Assert.Equal(new PartValues(84, 74, 54), OriginalMatch.LineValues(team, 0, attack: false, 0, 0, 0, inMatch: false));
        // Reihe 2: −4; im Spiel Energie 100 → 100 % (100/4 + 75), Einsatz 60: 76 − (100 − 45)/4 = 76 − 13 = 63.
        Assert.Equal(new PartValues(63, 66, 46), OriginalMatch.LineValues(team, 2, attack: true, 0, 100, 60, inMatch: true));
        // Unterzahl in der Abwehr halbiert die Technik je fehlendem Spieler, Überzahl im Sturm + Technik >> (3 − 1).
        Assert.Equal(37, OriginalMatch.LineValues(team, 0, attack: false, -1, 0, 0, false).Technique);
        Assert.Equal(92, OriginalMatch.LineValues(team, 0, attack: true, 1, 0, 0, false).Technique);

        var defense = new PartValues(84, 74, 54);
        var attack = new PartValues(80, 90, 60);
        // Minute 0: 212 und 230; Modus 1: 106 + 115 = 221, Vereinsform 51 → + 3.
        Assert.Equal(224, OriginalMatch.TeamStrength(defense, attack, 1, 0, 51));
        // Minute 60: Technik 74 − 600/75 = 66, 90 − 600/91 = 84 → 204/224 → 102 + 112 + 3.
        Assert.Equal(217, OriginalMatch.TeamStrength(defense, attack, 1, 60, 51));
        // Modus 0 (85/15): 180 + 34 + 3.
        Assert.Equal(217, OriginalMatch.TeamStrength(defense, attack, 0, 0, 51));
        Assert.Equal(0, OriginalMatch.TeamStrength(new PartValues(0, 0, 0), new PartValues(0, 0, 0), 1, 0, 53));
    }

    [Fact]
    public void PenaltyMinutes_FollowsTheOriginalRoll()
    {
        Assert.Equal(0, OriginalMatch.PenaltyMinutes(60, true, new ScriptedRandom(3, 7)));
        Assert.Equal(2, OriginalMatch.PenaltyMinutes(60, true, new ScriptedRandom(3, 0, 4)));
        Assert.Equal(5, OriginalMatch.PenaltyMinutes(60, true, new ScriptedRandom(0, 0)));
        Assert.Equal(2, OriginalMatch.PenaltyMinutes(60, false, new ScriptedRandom(0)));
        var calls = new ScriptedRandom(1, 1);
        OriginalMatch.PenaltyMinutes(60, true, calls);
        Assert.Equal([(0, 12), (0, 50)], calls.Calls);

        // Je Minute 1/13 + 12/13 · 1/51 ≈ 9,5 % bei Einsatz 60.
        var random = new Random(5);
        var hits = 0;
        for (var i = 0; i < 100_000; i++)
            hits += OriginalMatch.PenaltyMinutes(60, true, random) > 0 ? 1 : 0;
        Assert.InRange(hits / 100_000.0, 0.090, 0.100);
    }

    private static List<Player> Squad(int goalies, int defenders, int forwards)
    {
        var players = new List<Player>();
        for (var i = 0; i < goalies; i++)
            players.Add(new Player { Name = $"T{i}", Role = PlayerRole.Goalkeeper });
        for (var i = 0; i < defenders; i++)
            players.Add(new Player { Name = $"V{i}", Role = PlayerRole.Defender });
        for (var i = 0; i < forwards; i++)
            players.Add(new Player { Name = $"S{i}", Role = PlayerRole.Forward });
        return players;
    }

    [Fact]
    public void CreditSkaters_EventsAreConsistentWithTheScore()
    {
        var random = new Random(9);
        for (var game = 0; game < 2_000; game++)
        {
            var players = Squad(2, 8, 12);
            var goals = game % 9;
            MatchStatistics.CreditSkaters(players, goals, random);
            var scored = players.Sum(p => p.Goals);
            var assists = players.Sum(p => p.Assists);
            Assert.InRange(scored, 0, goals);
            Assert.InRange(assists, 0, scored);
            Assert.All(players.Where(p => p.Role == PlayerRole.Goalkeeper), p => Assert.Equal(0, p.Goals + p.Assists));
            // [ORIGINAL] Spiele zählen nur, wenn der Verein getroffen hat (Datei 0x5077).
            Assert.All(players, p => Assert.Equal(goals > 0 ? 1 : 0, p.Games));
        }
    }

    [Fact]
    public void CreditSkaters_CreditsAboutFourFifthsOfTheGoals_MostlyToForwards()
    {
        var random = new Random(2);
        var players = Squad(2, 8, 12);
        const int games = 4_000;
        for (var g = 0; g < games; g++)
            MatchStatistics.CreditSkaters(players, 4, random);
        var goals = players.Sum(p => p.Goals);
        var assists = players.Sum(p => p.Assists);
        Assert.InRange(goals / (4.0 * games), 0.76, 0.84);
        Assert.InRange(assists / (double)goals, 0.85, 1.0);
        var forwards = players.Where(p => p.Role == PlayerRole.Forward).Sum(p => p.Goals);
        Assert.True(forwards > goals * 0.6, $"Stürmer {forwards} von {goals}");
    }

    [Fact]
    public void CreditSkaters_ScriptedGoal_ScorerAndOneAssist()
    {
        // Ein Verteidiger (Kandidat: 33 + 25 > 10) und zwei Stürmer, ein Tor.
        var players = Squad(1, 1, 2);
        // Kandidaten: je random(5,25) und random(10,99) für 3 Feldspieler; Grenze n = 3 → Tabelle 3 − 1 = 2;
        // random(0,4) = 1 (Tor zählt), random(0,2) = 1, Kandidat 2 (Torschütze), random(0,2) = 2, Kandidat 0 (Vorlage).
        var random = new ScriptedRandom(25, 10, 5, 10, 5, 10, 1, 1, 2, 2, 0);
        MatchStatistics.CreditSkaters(players, 1, random);
        Assert.True(random.Exhausted);
        Assert.Equal(1, players[3].Goals);
        Assert.Equal(1, players[1].Assists);
        Assert.Equal(1, players.Sum(p => p.Goals));
        Assert.Equal(1, players.Sum(p => p.Assists));
    }

    [Fact]
    public void CreditGoalies_SumsUpToGoalsAgainstAndSixtyMinutes()
    {
        var random = new Random(4);
        var goalies = Squad(3, 0, 0);
        var against = 0;
        for (var g = 0; g < 40; g++)
        {
            var ga = g % 7;
            against += ga;
            MatchStatistics.CreditGoalies(goalies, ga, random);
        }
        Assert.Equal(against, goalies.Sum(p => p.GoalsAgainst));
        for (var g = 40; g < 500; g++)
            MatchStatistics.CreditGoalies(goalies, 0, random);
        Assert.Equal(500 * 60, goalies.Sum(p => p.GoalieMinutes));
        Assert.Equal(0, goalies[2].GoalieMinutes + goalies[2].GoalsAgainst); // nur die ersten beiden
        Assert.InRange(goalies[1].GoalieMinutes, 1, 500 * 60 / 5); // Wechsel mit 1/9

        // [ORIGINAL] Byte-Grenze 255: darüber bleibt der Zähler stehen (Datei 0x4ED0).
        var full = Squad(1, 0, 0);
        full[0].GoalsAgainst = 254;
        MatchStatistics.CreditGoalies(full, 2, random);
        Assert.Equal(254, full[0].GoalsAgainst);

        // Ohne Wechsel: random(0,8) ≠ 0 → alles für den ersten.
        var two = Squad(2, 0, 0);
        MatchStatistics.CreditGoalies(two, 3, new ScriptedRandom(4));
        Assert.Equal((3, 60, 0, 0), (two[0].GoalsAgainst, two[0].GoalieMinutes, two[1].GoalsAgainst, two[1].GoalieMinutes));
        // Wechsel: 50 % von 3 → 1, Rest 2; Minuten random(2,100) = 25 → 15 und 45.
        MatchStatistics.CreditGoalies(two, 3, new ScriptedRandom(0, 50, 25));
        Assert.Equal((4, 75, 2, 45), (two[0].GoalsAgainst, two[0].GoalieMinutes, two[1].GoalsAgainst, two[1].GoalieMinutes));
    }

    [Fact]
    public void SchemaTenSave_IsMigrated_GoalieCountersZero()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 12, StartLeague = League.Oberliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        TestSeason.PlayMatchDay(game);
        // Seit dem Anschluss (Teil 3) zählt der Spieltag die Torhüter; ein Spielstand aus Schema 10 (boerse-start,
        // Integration Runde 7) kennt die Felder nicht.
        foreach (var p in game.Clubs.SelectMany(c => c.Players))
            p.GoalsAgainst = p.GoalieMinutes = 0;
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 10;
        var removed = 0;
        foreach (var node in root["spieler"]!.AsArray())
        {
            var player = node!.AsObject();
            Assert.True(player.Remove("GoalsAgainst"));
            Assert.True(player.Remove("GoalieMinutes"));
            removed++;
        }
        Assert.True(removed > 0);

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game!));
    }
}
