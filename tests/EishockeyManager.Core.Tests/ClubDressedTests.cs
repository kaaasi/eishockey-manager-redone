using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur taktik-verein, Befund (c) aus #103: Die Vereinsaufstellung kannte die Grenze von 22 Eingesetzten nicht
/// (im Original derselbe Code wie beim Nationalteam, Datei 0x40E2A–0x40E61). Dazu Migration 4 → 5 und Mehrsaisonprobe.
/// </summary>
public class ClubDressedTests
{
    private static GameState Create(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = 2 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Holt einsatzfähige Spieler eines Computervereins, bis der Kader <paramref name="size"/> Spieler hat.</summary>
    private static void GrowSquad(GameState game, Club club, int size)
    {
        var donors = game.Clubs.Where(c => c != club && c.Players.Count > 0).ToList();
        foreach (var donor in donors)
        {
            foreach (var p in donor.Players.Where(p => p.Available && p.Role != PlayerRole.Goalkeeper).ToList())
            {
                if (club.Players.Count >= size)
                    return;
                donor.Lineup.Remove(p);
                donor.Players.Remove(p);
                club.Players.Add(p);
            }
        }
    }

    [Fact]
    public void Auto_DressesLineupAndBestOthers_UpToTwentyTwo()
    {
        var game = Create();
        var club = game.Clubs[2];
        Assert.All(club.Lineup.Players, p => Assert.True(club.IsDressed(p)));
        Assert.Equal(Math.Min(Club.MaxDressed, club.Players.Count(p => p.Available || club.Lineup.Contains(p))), club.DressedCount);

        GrowSquad(game, club, 27);
        Assert.Equal(Club.MaxDressed, club.DressedCount);
        Assert.All(club.Lineup.Players, p => Assert.True(club.IsDressed(p)));
    }

    /// <summary>Regressionstest (c): der 23. Eingesetzte wird abgewiesen („Schon 22 Mann / im Team !“).</summary>
    [Fact]
    public void TwentyThird_IsRefused_UntilSomeoneIsTakenOut()
    {
        var game = Create();
        var club = game.Clubs[2];
        GrowSquad(game, club, 27);
        // Spur kader-start: Der eigene Kader ist zu Spielbeginn stärker als die zugeholten Spieler; nicht eingesetzt
        // bleibt dann ein zugeholter Verteidiger statt eines Stürmers. Daher Platz im vierten Block nach Spielerart.
        var outside = club.Players.First(p => !club.IsDressed(p) && p.Role != PlayerRole.Goalkeeper);
        var slot = outside.Role == PlayerRole.Forward ? LineupSlot.Forward(3, 0) : LineupSlot.Defender(3, 0);
        Assert.Null(club.Lineup[slot]);

        Assert.Equal(LineupResult.RosterFull, club.AssignToLineup(slot, outside));
        Assert.Equal(LineupResult.RosterFull, club.Dress(outside));
        Assert.Null(club.Lineup[slot]);
        Assert.Equal(Club.MaxDressed, club.DressedCount);

        var reserve = club.Dressed.First(p => !club.Lineup.Contains(p));
        Assert.Equal(LineupResult.Undressed, club.Undress(reserve));
        Assert.Equal(LineupResult.Unchanged, club.Undress(club.Lineup.Players.First())); // Aufgestellte bleiben
        Assert.Equal(LineupResult.Assigned, club.AssignToLineup(slot, outside));
        Assert.Equal(Club.MaxDressed, club.DressedCount);
        Assert.False(club.IsDressed(reserve));

        // Platz leeren nimmt heraus (Datei 0x40CF8), danach ist Platz für den Reservisten.
        club.ClearLineupSlot(slot);
        Assert.False(club.IsDressed(outside));
        Assert.Equal(LineupResult.Dressed, club.Dress(reserve));
        Assert.Equal(Club.MaxDressed, club.DressedCount);

        club.AutoLineup();
        Assert.Equal(Club.MaxDressed, club.DressedCount);
    }

    [Fact]
    public void Assign_KeepsOtherMessages()
    {
        var game = Create();
        var club = game.Clubs[2];
        var forward = club.Players.First(p => p.Role == PlayerRole.Forward && p.Available);
        Assert.Equal(LineupResult.WrongRole, club.AssignToLineup(LineupSlot.Defender(0, 0), forward));
        Assert.Equal(LineupResult.NotInSquad, club.AssignToLineup(LineupSlot.Forward(0, 0), game.Clubs[3].Players[0]));
        forward.InjuredGames = 2;
        Assert.Equal(LineupResult.Injured, club.AssignToLineup(LineupSlot.Forward(3, 0), forward));
    }

    [Fact]
    public void RoundTrip_KeepsDressedAndTactics()
    {
        var game = Create();
        var club = game.Clubs[2];
        GrowSquad(game, club, 26);
        var reserve = club.Dressed.First(p => !club.Lineup.Contains(p));
        club.Undress(reserve);
        club.Tactics.EffortMode = TacticMode.Semi;
        club.Tactics.SetEffort(1, ScoreSituation.Tied, 5);

        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes).Game!.Clubs[2];
        Assert.Equal(club.Dressed.Select(p => p.Id), loaded.Dressed.Select(p => p.Id));
        Assert.Equal(5, loaded.Tactics.Effort(1, ScoreSituation.Tied));
        Assert.Equal(club.MatchStrength, loaded.MatchStrength);
        Assert.Equal(bytes, SaveGame.Save(SaveGame.Load(bytes).Game!));
    }

    /// <summary>Abnahme (3): Schema 4 → 5 – alte Taktikwerte (−3…+3) werden zu Startwerten, Eingesetzt automatisch.</summary>
    [Fact]
    public void SchemaFourSave_IsMigrated()
    {
        var game = Create();
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var club = game.Clubs[2];
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 4;
        foreach (var c in root["vereine"]!.AsArray().Select(n => n!.AsObject()))
        {
            Assert.True(c.Remove("_dressed"));
            Assert.True(c.Remove("_dressedByHand"));
        }
        // Integration Runde 5: Schema 4 kennt auch den Zug des Bundestrainers (Migration 5 → 6) noch nicht – Kette 4 → 5 → 6.
        Assert.True(root["spiel"]!.AsObject().Remove("_nationalTurn"));
        var old = root["vereine"]![2]!["Tactics"]!.AsObject();
        old["EffortMode"] = nameof(TacticMode.Manual);
        old["_effort"]!["items"]![0] = -3;
        root["spiel"]!["NationalCoach"]!["Tactics"]!["_attack"]!["items"]![5] = 3;

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        Assert.False(loaded.NationalTurn);
        var fresh = new Tactics();
        var tactics = loaded.Clubs[2].Tactics;
        Assert.Equal(TacticMode.Manual, tactics.EffortMode);
        foreach (var r in Enum.GetValues<ScoreSituation>())
        {
            Assert.Equal(fresh.Effort(0, r), tactics.Effort(0, r));
            Assert.Equal(fresh.Attack(0, r), loaded.NationalCoach!.Tactics.Attack(0, r));
        }
        Assert.Equal(club.Dressed.Select(p => p.Id), loaded.Clubs[2].Dressed.Select(p => p.Id));
        Assert.Equal(game.Clubs[2].MatchStrength, loaded.Clubs[2].MatchStrength);
        var again = SaveGame.Save(loaded);
        Assert.Equal(again, SaveGame.Save(SaveGame.Load(again).Game!));
    }

    /// <summary>
    /// Abnahme (4): Mehrsaisonprobe, 4 Saisonwechsel, passiver und aktiver Manager. Der aktive setzt jede Saison von
    /// Hand ein und aus und spielt mit eigener Taktik („SEMI“, Einsatz 100 %). Geprüft: Eingesetzte gehören zum Kader,
    /// Aufgestellte sind eingesetzt, höchstens 22 (außer die Aufstellung allein hat mehr), Taktik wirkt nur beim aktiven
    /// Manager, Spielstand bleibt ladbar.
    /// </summary>
    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [InlineData(true, 11)]
    public void MultiSeason_DressedAndTactics(bool active, int seed)
    {
        var game = Create(seed);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        var others = game.Clubs.Where(c => c != club && c.Players.Count > 0).ToList();

        for (var change = 0; change <= 4; change++)
        {
            if (active)
            {
                var reserve = club.Dressed.FirstOrDefault(p => !club.Lineup.Contains(p));
                if (reserve is not null)
                    Assert.Equal(LineupResult.Undressed, club.Undress(reserve));
                club.Tactics.EffortMode = TacticMode.Semi;
                for (var b = 0; b < Tactics.Blocks; b++)
                {
                    foreach (var r in Enum.GetValues<ScoreSituation>().Skip(1))
                        club.Tactics.SetEffort(b, r, 0);
                    club.Tactics.SetEffort(b, ScoreSituation.Base, Tactics.MaxEffort);
                }
            }

            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                manager.Money = 10_000_000;
                game.Continue();
                if (guard % 25 == 0)
                    Check();
            }
            Check();
            Assert.All(others, c => Assert.Equal(0, c.Tactics.EffortStrengthBonus()));
            Assert.Equal(active ? 2 : 0, club.Tactics.EffortStrengthBonus(), 6);

            var bytes = SaveGame.Save(game);
            var loaded = SaveGame.Load(bytes);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(club.Dressed.Select(p => p.Id), loaded.Game!.Clubs[club.Index].Dressed.Select(p => p.Id));
            if (change < 4)
                game.StartNextSeason();
        }
        Assert.Equal(5, game.History.Count);

        void Check()
        {
            var dressed = club.Dressed;
            Assert.All(dressed, p => Assert.Contains(p, club.Players));
            Assert.All(club.Lineup.Players, p => Assert.Contains(p, dressed));
            Assert.True(dressed.Count <= Math.Max(Club.MaxDressed, club.Lineup.Count), $"{dressed.Count} Eingesetzte");
            Assert.Equal(dressed.Count, dressed.Distinct().Count());
        }
    }
}
