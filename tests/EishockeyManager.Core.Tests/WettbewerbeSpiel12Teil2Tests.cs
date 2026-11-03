using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Runde 12, Teil 2 (A184): Belastung (Kaderplatz +0x13) als eigenes Feld <see cref="Player.Strain"/>
/// statt 150 − Energie. Befund vorher (Base af907c5): Die Belastung war aus der Energie abgeleitet und auf 50 … 150 gekappt;
/// die Spielminute zog lrandom(0, 4 + (Kondition &gt; Technik)), verwarf aber das Ergebnis (Datei 0x8C82 „inc byte
/// [es:bx+0x13]“ fehlte); nach dem Spiel gab es keine Grenzen (Datei 0x549A2); „WM Vorbereitung“ setzte die Energie statt der
/// Belastung auf 70 (Datei 0x3BB51 schreibt 0x5DD6 = Kaderplatz 0x5DC3 + 0x13).
/// </summary>
public class WettbewerbeSpiel12Teil2Tests
{
    private static Player P(PlayerRole role, int strain) =>
        new() { Name = "Probe", Role = role, Condition = 60, Technique = 50, Form = 50, Number = 1, Strain = strain };

    /// <summary>
    /// [ORIGINAL] Spielvorbereitung 0x84D7(0) (Datei 0x86B5): 101 − Belastung/8, höchstens 91, minus 1 – das Byte ohne
    /// Grenzen. Vorher rot: Belastung über 150 wurde als 150 gelesen (82 statt 75).
    /// </summary>
    [Theory]
    [InlineData(80, 90)]
    [InlineData(150, 82)]
    [InlineData(200, 75)]
    public void PreparedEnergy_ReadsStrainByte(int strain, int energy) =>
        Assert.Equal(energy, MatchEngine.PreparedPlayerEnergy(P(PlayerRole.Forward, strain)));

    /// <summary>
    /// [ORIGINAL] Trainingswirkung 0x659C liest +0x13 ohne Grenzen (Datei 0x6623) und schreibt Belastung − Erholung, danach
    /// 50 … 150 (Datei 0x6B08–0x6B2A). Standardplan: Erholung 0 → eine Belastung von 30 wird 50, eine von 140 bleibt.
    /// </summary>
    [Theory]
    [InlineData(30, 50)]
    [InlineData(140, 140)]
    public void Training_StandardPlan_ClampsStrain(int before, int after)
    {
        var p = P(PlayerRole.Defender, before);
        var plan = new TrainingPlan();
        plan.Prepare(2);
        var outcome = TrainingRules.Apply(p, plan, 2, new Random(1));
        Assert.Equal(0, outcome.Recovery);
        Assert.Equal(after, p.Strain);
    }

    /// <summary>
    /// [ORIGINAL] Spalte E = Belastung · 10 / 15 (Datei 0x4235E), Farbe 0x10 über 130 (Datei 0x42338); „WM Vorbereitung“ 70 → 46
    /// wie im DOSBox-Lauf (docs/dosbox-befunde.md).
    /// </summary>
    [Fact]
    public void ExhaustionDisplay_TenFifteenths()
    {
        Assert.Equal(46, TrainingRules.ExhaustionDisplay(P(PlayerRole.Forward, 70)));
        Assert.Equal(100, TrainingRules.ExhaustionDisplay(P(PlayerRole.Forward, 150)));
        Assert.False(TrainingRules.ExhaustionWarning(P(PlayerRole.Forward, 130)));
        Assert.True(TrainingRules.ExhaustionWarning(P(PlayerRole.Forward, 131)));
    }

    /// <summary>
    /// Regressionstest (vorher rot): In den Spielen des Managervereins steigt die Belastung der Eingesetzten (Datei 0x8C82),
    /// nach jedem Spiel liegt sie für alle Spieler des Vereins in 50 … 150 (Datei 0x549A2–0x549BB) – auch für einen nicht
    /// eingesetzten Spieler, der vorher außerhalb lag. Computervereine bleiben unberührt.
    /// </summary>
    [Fact]
    public void ManagedMatches_RaiseStrain_ClampAfterMatch()
    {
        var game = TestGames.Create(3);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var computer = game.Clubs.First(c => c.Index != club.Index && c.League == club.League);
        var before = club.Players.ToDictionary(p => p, p => p.Strain);
        var computerBefore = computer.Players.Select(p => p.Strain).ToArray();
        var bench = club.Players.First(p => !club.Lineup.Players.Contains(p));
        bench.Strain = 220;
        before[bench] = 220;

        for (var i = 0; i < 4; i++)
            TestSeason.PlayMatchDay(game);

        Assert.All(club.Players, p => Assert.InRange(p.Strain, TrainingRules.MinStrain, TrainingRules.MaxStrain));
        Assert.Equal(150, bench.Strain);
        Assert.Contains(club.Lineup.Players, p => p.Strain > before[p]);
        Assert.Equal(computerBefore, computer.Players.Select(p => p.Strain).ToArray());
    }

    /// <summary>
    /// Regressionstest (vorher rot): ein einzelnes Spiel des Managervereins ohne Trainingstag dazwischen – danach liegt die
    /// Belastung aller Spieler des Vereins in 50 … 150 (Datei 0x549A2–0x549BB), auch für nicht Eingesetzte.
    /// </summary>
    [Fact]
    public void SingleManagedMatch_ClampsWholeSquad()
    {
        var game = TestGames.Create(3);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var opponent = game.Clubs.First(c => c.Index != club.Index && c.League == club.League);
        var bench = club.Players.Where(p => !club.Lineup.Players.Contains(p)).Take(2).ToArray();
        bench[0].Strain = 220;
        bench[1].Strain = 10;
        var report = MatchEngine.PlayWithEvents(game, club, opponent, MatchCompetition.League, knockOut: false, new Random(5));
        Assert.False(report.Forfeit);
        Assert.Equal(150, bench[0].Strain);
        Assert.Equal(50, bench[1].Strain);
    }

    /// <summary>Schema 32 → 33: Belastung = 150 − Energie (bisherige Ableitung), das Feld Energy entfällt.</summary>
    [Fact]
    public void Migration_32To33_StrainFromEnergy()
    {
        var game = TestGames.Create(3);
        var player = game.Clubs[game.ActiveManager.ClubIndex].Players[0];
        player.Strain = 87;
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 32;
        var players = root["spieler"]!.AsArray().OfType<JsonObject>().ToList();
        Assert.All(players, p => Assert.Null(p["Energy"]));
        foreach (var p in players)
        {
            var strain = p["Strain"]!.GetValue<int>();
            p.Remove("Strain");
            p["Energy"] = 150 - strain;
        }
        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(loaded.Success, loaded.Error);
        var all = game.Clubs.SelectMany(c => c.Players).ToList();
        var allLoaded = loaded.Game!.Clubs.SelectMany(c => c.Players).ToList();
        Assert.Equal(all.Select(p => p.Strain), allLoaded.Select(p => p.Strain));
        Assert.Equal(87, loaded.Game.Clubs[game.ActiveManager.ClubIndex].Players[0].Strain);
    }

    /// <summary>
    /// Mehrsaisonprobe (A184 berührt den Kader): vier Saisonwechsel, passiver und aktiver Manager. Nach jeder Saison liegt die
    /// Belastung aller Spieler der Managervereine in 50 … 150, Computerkader bleiben gültig (<see cref="PlayerFactory.Validate"/>
    /// prüft jetzt die Belastung), Sichern und Laden erhält die Belastung.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_StrainStaysInRange(bool active)
    {
        var game = GameModeTests.Create(5, true, true, 2, 30);
        var manager = game.Managers[0];
        for (var change = 0; change <= 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = 10_000_000;
                TestSquadKeeper.KeepPlayable(game, manager);
                if (active)
                    foreach (var p in game.Clubs[manager.ClubIndex].Players.Take(3))
                        p.Strain = 60; // aktiver Manager: schont drei Spieler (wie freie Trainingstage)
                game.Continue();
            }
            foreach (var m in game.Managers)
                Assert.All(game.Clubs[m.ClubIndex].Players,
                    p => Assert.InRange(p.Strain, TrainingRules.MinStrain, TrainingRules.MaxStrain));
            foreach (var club in game.Clubs.Where(c => game.Managers.All(m => m.ClubIndex != c.Index) && c.Index < 48))
                Assert.All(club.Players, p => Assert.DoesNotContain(PlayerFactory.Validate(club, p), t => t.StartsWith("Belastung")));
            var loaded = SaveGame.Load(SaveGame.Save(game));
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(game.Clubs.SelectMany(c => c.Players).Select(p => p.Strain),
                loaded.Game!.Clubs.SelectMany(c => c.Players).Select(p => p.Strain));
            if (change < 4)
                game.StartNextSeason();
        }
        Assert.Equal(5, game.History.Count);
    }
}
