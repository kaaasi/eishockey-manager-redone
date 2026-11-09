using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-start</c>, Runde 14, Teil 2b (<c>docs/analyse/re-runde-14.md</c>): Taktik-Befund DGROUP 375h (Neuaufstellung
/// nach den Spielen), A046 (Tabellenfeld der Zuschauerformel), A135 (Tore und Spiele je Wettbewerb).
/// </summary>
public class KaderStartRunde14Teil2bTests
{
    private static (GameState Game, Club Own, Club Other) ManagerMatch()
    {
        var game = TestGames.Create();
        var own = game.Clubs[game.Managers[0].ClubIndex];
        var other = game.Clubs.First(c => c.League == own.League && c != own);
        return (game, own, other);
    }

    private static LineupSlot FirstFilled(Club club) => LineupSlot.All.First(s => club.Lineup[s] is not null);

    // ---- Taktik 375h ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Datei 0x54D8C–0x54D9E/0x552F9–0x5530B: nach den Spielen stellt 0x426BC mit der Einstellung neu auf – auch ohne
    /// Verletzung. Vorher stellte das Remake nur nach einer Verletzung neu auf; die Lücke blieb bis zum nächsten Tageslauf.
    /// </summary>
    [Fact]
    public void AutoLineup_AfterMatchWithoutInjury_LinesUpAgain()
    {
        var (game, own, other) = ManagerMatch();
        Assert.True(own.AutoLineupEnabled);
        var slot = FirstFilled(own);
        own.Lineup.Clear(slot); // Lücke ohne Eingriff über die Oberfläche (Einstellung bleibt an)
        Assert.True(own.AutoLineupEnabled);

        MatchEngine.CreditPlayers(game, own, other, MatchCompetition.League, new MatchReport { HomeGoals = 1 }, new Random(1));

        Assert.NotNull(own.Lineup[slot]);
    }

    /// <summary>Ohne die Einstellung tut 0x426BC nichts: ohne Verletzung bleibt die Aufstellung, wie sie ist.</summary>
    [Fact]
    public void ManualLineup_AfterMatchWithoutInjury_Unchanged()
    {
        var (game, own, other) = ManagerMatch();
        var slot = FirstFilled(own);
        own.ClearLineupSlot(slot);
        Assert.False(own.AutoLineupEnabled);

        MatchEngine.CreditPlayers(game, own, other, MatchCompetition.League, new MatchReport { HomeGoals = 1 }, new Random(1));

        Assert.Null(own.Lineup[slot]);
    }

    // ---- A135 -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// FILE38 → FILE39: Pokalspiel → +0x07/+0x1E + 1, +0x06 unverändert; Tor → +0x04/+0x24, +0x03 unverändert. Spielerwert
    /// liest Pokalspiele beim Verein in +0x1E (Gewicht 20), nicht in +0x1C (Gewicht 50).
    /// </summary>
    [Fact]
    public void CupMatch_CountsInCupFields()
    {
        var (game, own, other) = ManagerMatch();
        var scorer = own.Lineup.Players.First(p => p.Role != Formats.PlayerRole.Goalkeeper);
        var report = new MatchReport { HomeGoals = 1 };
        report.Events.Add(new MatchEvent(10, 0, MatchEventKind.Goal, Player: scorer));

        MatchEngine.CreditPlayers(game, own, other, MatchCompetition.DebCup, report, new Random(1));

        Assert.Equal(1, scorer.Games);
        Assert.Equal(1, scorer.SeasonGames(CompetitionSlot.Cup));
        Assert.Equal(0, scorer.SeasonGames(CompetitionSlot.League));
        Assert.Equal(1, scorer.SeasonGoals(CompetitionSlot.Cup));
        Assert.Equal(0, scorer.SeasonGoals(CompetitionSlot.League));
        Assert.Equal(1, scorer.ClubGames(CompetitionSlot.Cup));
        Assert.Equal(0, scorer.GamesForClub);
        Assert.Equal(1, scorer.ClubGoals(CompetitionSlot.Cup));
        var inputs = PlayerValue.Inputs(scorer, own);
        Assert.Equal((0, 1, 0), (inputs.Goals0, inputs.Goals1, inputs.Goals2));
        Assert.Equal((0, 1, 0), (inputs.Games0, inputs.Games1, inputs.Games2));
    }

    /// <summary>Europapokal: +0x08/+0x20 und +0x05/+0x26 (Datei 0x313C5 setzt DGROUP 0xD0BF = 2).</summary>
    [Fact]
    public void EuropeanCupMatch_CountsInEuropeFields()
    {
        var (game, own, other) = ManagerMatch();
        var scorer = own.Lineup.Players.First(p => p.Role != Formats.PlayerRole.Goalkeeper);
        var report = new MatchReport { HomeGoals = 1 };
        report.Events.Add(new MatchEvent(10, 0, MatchEventKind.Goal, Player: scorer));

        MatchEngine.CreditPlayers(game, own, other, MatchCompetition.EuropeanCup, report, new Random(1));

        Assert.Equal((1, 1), (scorer.SeasonGames(CompetitionSlot.Europe), scorer.SeasonGoals(CompetitionSlot.Europe)));
        Assert.Equal((0, 0), (scorer.CupGames, scorer.CupGoals));
        var inputs = PlayerValue.Inputs(scorer, own);
        Assert.Equal((0, 0, 1), (inputs.Goals0, inputs.Goals1, inputs.Goals2));
        Assert.Equal((0, 0, 1), (inputs.Games0, inputs.Games1, inputs.Games2));
    }

    /// <summary>Play-offs und Aufstiegsrunden zählen wie die Liga (Datei 0x30EA6/0x31A1E: DGROUP 0xD0BF = 0).</summary>
    [Theory]
    [InlineData(MatchCompetition.Playoff)]
    [InlineData(MatchCompetition.Relegation)]
    public void Postseason_CountsAsLeague(MatchCompetition competition)
    {
        Assert.Equal(CompetitionSlot.League, CompetitionSlots.Of(competition));
    }

    /// <summary>Neuer Kaderplatz (Datei 0x42CB1): alle Vereinszähler 0, Saisonzähler bleiben.</summary>
    [Fact]
    public void Join_ResetsClubCountersOnly()
    {
        var (game, own, other) = ManagerMatch();
        var p = other.Players.First(p => p.Role != Formats.PlayerRole.Goalkeeper);
        p.CountGame(CompetitionSlot.League);
        p.CountGame(CompetitionSlot.Cup);
        p.CountGoal(CompetitionSlot.Cup);
        other.Players.Remove(p);

        PlayerFactory.Join(own, p, 2, loan: false);

        Assert.Equal((1, 1, 1), (p.SeasonGames(CompetitionSlot.League), p.CupGames, p.CupGoals));
        Assert.All(Enum.GetValues<CompetitionSlot>(), s => Assert.Equal((0, 0), (p.ClubGames(s), p.ClubGoals(s))));
    }

    // ---- A046 -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Spielstart (FILE29/30/01/12): das Tabellenfeld ist für alle Vereine 0 – am 1. Spieltag rechnet die Zuschauerformel mit
    /// Platz 0/0, nicht mit der Reihenfolge der Liga.
    /// </summary>
    [Fact]
    public void FirstMatchDay_NewGame_UsesRankZero()
    {
        var game = TestGames.Create();
        var league = game.LeagueClubs(League.Bundesliga).ToList();
        var home = league[^1];
        var away = league[^2];
        var stadium = new Stadium { Seats = 15_000, Standing = 15_000, TicketPrice = 12 };
        Assert.All(game.Clubs, c => Assert.Equal(0, c.StoredTableRank));

        var actual = game.Spectators(home, away, stadium, 1, new Random(7));
        var expected = SpectatorRules.Compute(stadium, SpectatorRules.Strength(game, home), SpectatorRules.Strength(game, away),
            0, 0, 0, 0, 0, ManagerSetup.LevelCount - 1 - 3, new Random(7));

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiver und aktiver Manager): Vor dem 1. Spieltag steht im Tabellenfeld der Endstand
    /// der Vorsaison (FILE14/17/23), auch nach Auf- oder Abstieg; Zähler je Wettbewerb ergeben die Summe, die Saisonzähler
    /// beginnen bei 0, die Vereinszähler laufen weiter; Laden/Speichern erhält beides.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_TableRankAndCompetitionCounters(bool active)
    {
        var game = GameModeTests.Create(5, true, true, 2, 30);
        var manager = game.Managers[0];
        for (var change = 0; change <= 4; change++)
        {
            Dictionary<int, int>? finalRanks = null;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = 10_000_000;
                TestSquadKeeper.KeepPlayable(game, manager);
                if (active && guard % 7 == 3)
                    game.Clubs[manager.ClubIndex].AutoLineup();
                game.Continue();
                if (game.RegularSeasonOver && finalRanks is null)
                {
                    finalRanks = Enum.GetValues<League>().SelectMany(l =>
                            LeagueTable.Compute(game, l, TableView.Gesamt, game.PlayedMatchDays).Select(r => (r.Club.Index, r.Rank - 1)))
                        .ToDictionary(x => x.Item1, x => x.Item2);
                }
                foreach (var p in game.Clubs.SelectMany(c => c.Players))
                {
                    Assert.Equal(p.Games, Enum.GetValues<CompetitionSlot>().Sum(p.SeasonGames));
                    Assert.Equal(p.Goals, Enum.GetValues<CompetitionSlot>().Sum(p.SeasonGoals));
                }
            }
            Assert.NotNull(finalRanks);
            var own = game.Clubs[manager.ClubIndex];
            var clubGames = own.Players.ToDictionary(p => p.Id, p => Enum.GetValues<CompetitionSlot>().Sum(p.ClubGames));

            var loaded = SaveGame.Load(SaveGame.Save(game));
            Assert.True(loaded.Success, loaded.Error);
            Assert.All(game.Clubs, c => Assert.Equal(c.StoredTableRank, loaded.Game!.Clubs[c.Index].StoredTableRank));
            if (change == 4)
                break;
            game.StartNextSeason();

            Assert.Equal(0, game.PlayedMatchDays);
            // Neu in die Oberliga aufgenommene Vereine standen in keiner Tabelle; sie behalten ihr Feld.
            foreach (var club in game.Clubs.Where(c => c.League is not null && finalRanks!.ContainsKey(c.Index)))
                Assert.Equal(finalRanks![club.Index], club.StoredTableRank);
            foreach (var p in game.Clubs.SelectMany(c => c.Players))
                Assert.Equal((0, 0, 0, 0), (p.Games, p.Goals, p.CupGames, p.EuropeGoals));
            foreach (var p in game.Clubs[manager.ClubIndex].Players.Where(p => clubGames.ContainsKey(p.Id)))
                Assert.Equal(clubGames[p.Id], Enum.GetValues<CompetitionSlot>().Sum(p.ClubGames));
        }
        Assert.Equal(5, game.History.Count);
    }

    /// <summary>Schema 42 → 43: alte Stände zählen alles als Liga; Tabellenfeld aus dem letzten gespeicherten Platzfeld.</summary>
    [Fact]
    public void Migration_OldSave_LeagueOnlyAndTableRankFromRankSlots()
    {
        var game = TestGames.Create();
        for (var i = 0; i < 3; i++)
            TestSeason.PlayMatchDay(game);
        game.RecordRankSlots();
        var json = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game), documentOptions: new System.Text.Json.JsonDocumentOptions { MaxDepth = 1024 })!.AsObject();
        json["schema"] = 42;
        foreach (var club in json["vereine"]!.AsArray().OfType<System.Text.Json.Nodes.JsonObject>())
            club.Remove("_tableRank");
        foreach (var player in json["spieler"]!.AsArray().OfType<System.Text.Json.Nodes.JsonObject>())
            foreach (var name in new[] { "CupGames", "EuropeGames", "CupGoals", "EuropeGoals", "ClubCupGamesCarry",
                         "ClubEuropeGamesCarry", "ClubGoalsCarry", "ClubCupGoalsCarry", "ClubEuropeGoalsCarry" })
                player.Remove(name);

        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));

        Assert.True(loaded.Success, loaded.Error);
        var slot = game.RankSlotsRecorded - 1;
        Assert.All(loaded.Game!.Clubs, c => Assert.Equal(c.StoredRankSlot(slot), c.StoredTableRank));
        Assert.All(loaded.Game.Clubs.SelectMany(c => c.Players), p => Assert.Equal(p.Games, p.SeasonGames(CompetitionSlot.League)));
    }
}
