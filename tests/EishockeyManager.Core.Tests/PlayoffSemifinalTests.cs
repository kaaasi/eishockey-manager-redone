using System.Buffers.Binary;
using System.Text;
using System.Text.Json.Nodes;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Fixlauf #676 (Leitstelle Runde 14): Halbfinale und Endspiel der Play-offs nach Tabelle DGROUP 0x144 (Datei 0x16DD0 + 0x144,
/// Schreiber 0x31BFF–0x31C07): Sieger von Platz 0 → 8, 4 → 9, 2 → 10, 6 → 11, also 1–8 gegen 3–6 und 2–7 gegen 4–5, Heimrecht in
/// Spiel 1 beim kleineren Platz. Dazu die Nachprüfung R14HN-03 (laufende Play-off-Runde, teilweise gespielte Aufstiegsrunde) und
/// die Importfälle aus #669 (A380: eigener Spieler auf dem Markt steht nicht im Kader; R14KS9-01: Überträge ab der zweiten Saison).
/// </summary>
public sealed class PlayoffSemifinalTests
{
    private const int Seed = 4711;

    /// <summary>Lässt <paramref name="winner"/> die Serie 3:0 gewinnen (Spiele wie <see cref="PlayoffSeries.NextPairing"/>).</summary>
    private static void Win(PlayoffSeries series, int winner)
    {
        for (var g = 0; g < PlayoffSeries.WinsNeeded; g++)
        {
            var (home, away) = series.NextPairing;
            series.Games.Add(new KnockoutMatch(g, home, away, home == winner ? 4 : 1, home == winner ? 1 : 4));
        }
    }

    private static PlayoffSeries Series(Playoffs playoffs, int round, int club) => playoffs.Rounds[round].Single(s => s.Involves(club));

    /// <summary>
    /// Synthetisch: Seeds 101 … 108 (Platz 1 … 8). Viertelfinale in Platzfolge 0, 2, 4, 6 (1–8, 2–7, 3–6, 4–5). Gewinnen 8, 2, 3 und 5,
    /// spielt im Halbfinale 8 (Platz 8, Heimrecht) gegen 3 und 2 gegen 5; gewinnen 3 und 2, hat im Endspiel 3 Heimrecht (Platz 12),
    /// obwohl 2 besser platziert ist. Vorher paarte das Remake 1–8 gegen 4–5 mit Heimrecht beim besser Platzierten.
    /// </summary>
    [Fact]
    public void Playoffs_NextRound_PairsByTable0x144_HomeAtSmallerSlot()
    {
        int[] seeds = [101, 102, 103, 104, 105, 106, 107, 108];
        var playoffs = new Playoffs(seeds);
        Assert.Equal(new[] { (101, 108), (102, 107), (103, 106), (104, 105) },
            playoffs.Rounds[0].Select(s => (s.HomeClub, s.AwayClub)));
        Win(Series(playoffs, 0, 101), 108);
        Win(Series(playoffs, 0, 102), 102);
        Win(Series(playoffs, 0, 103), 103);
        Win(Series(playoffs, 0, 104), 105);

        var semi = playoffs.SeriesOn(Playoffs.DatesPerRound, 108);
        Assert.NotNull(semi);
        Assert.Equal(new[] { (108, 103), (102, 105) }, playoffs.Rounds[1].Select(s => (s.HomeClub, s.AwayClub)));
        Assert.Equal((108, 103), playoffs.Rounds[1][0].NextPairing); // Spiel 1 beim kleineren Platz (8), nicht beim Dritten

        Win(Series(playoffs, 1, 103), 103);
        Win(Series(playoffs, 1, 102), 102);
        Assert.NotNull(playoffs.SeriesOn(2 * Playoffs.DatesPerRound, 102));
        Assert.Equal((103, 102), (playoffs.Rounds[2][0].HomeClub, playoffs.Rounds[2][0].AwayClub));
        Assert.Equal((103, 102), playoffs.Rounds[2][0].NextPairing);
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiver Oberligist und aktiver Bundesligist im Hot-Seat, alles über
    /// <see cref="GameState.Continue"/>): In jeder Saison paaren Halbfinale und Endspiel nach Tabelle DGROUP 0x144, Spiel 1 beim
    /// kleineren Platz; der Meister steht im Saisonrückblick. Der aktive Manager ändert den Eintrittspreis, verlängert angebotene
    /// Verträge und kauft alle 20 Schritte einen Spieler vom Markt (R683-03).
    /// </summary>
    [Fact]
    public void MultiSeason_FourChanges_PlayoffsPairByTable0x144()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "PASSIV", Head = 0, ClubIndex = 30 });
        setup.Managers.Add(new ManagerSetup { Name = "AKTIV", Head = 1, ClubIndex = 6 });
        // Spur kader (Fixlauf Runde 15, nach Rebase auf tagesablauf #725): Saat 7 → 8. Jugend am Saisonende (#698), Startkader-Tendenz
        // (A134) und R15-E3 ändern zusammen mit Tag 0 die Zufallsfolge; mit Saat 7 hat dann in fünf Saisons kein schlechter Platzierter
        // Heimrecht (die Probe deckt den Fall nicht mehr ab, die Paarungsregel bleibt grün). Rückschaltprobe: Quellen von main → Saat 7
        // und 8 grün.
        // Runde 16, Spur abgleich-rest (#772, R15-E2 = C): Saat 8 → 9. Mit der Spielvorbereitung 0x8365 hat bei Saat 8 in fünf Saisons
        // kein schlechter Platzierter Heimrecht (gleicher Grund wie oben). Rückschaltprobe: siehe PR.
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 9);
        var active = game.Managers[1];
        var upsets = 0;
        var transfers = 0;
        for (var season = 0; season < 5; season++)
        {
            var step = 0;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                {
                    m.Money = Math.Max(m.Money, 10_000_000); // keine Entlassung im Testlauf
                    TestSquadKeeper.KeepPlayable(game, m);
                }
                game.Finance(active).Stadium.TicketPrice = 5 + step++ % 7; // aktiver Manager handelt in jedem Schritt
                GameModeTests.ActiveSecondManager(game); // R683-03: verlängert angebotene Verträge
                if (step % 20 == 0)
                    transfers += BuyOne(game, active); // R683-03: kauft einen Spieler vom Markt
                game.Continue();
            }
            var playoffs = game.Playoffs!;
            Assert.Equal(3, playoffs.Rounds.Count);
            var s = playoffs.Seeds;
            Assert.Equal(new[] { (s[0], s[7]), (s[1], s[6]), (s[2], s[5]), (s[3], s[4]) },
                playoffs.Rounds[0].Select(x => (x.HomeClub, x.AwayClub)));
            for (var round = 1; round < 3; round++)
            {
                var previous = playoffs.Rounds[round - 1];
                var half = previous.Count / 2;
                for (var k = 0; k < playoffs.Rounds[round].Count; k++)
                {
                    var series = playoffs.Rounds[round][k];
                    Assert.Equal((previous[k].Winner, previous[k + half].Winner), (series.HomeClub, series.AwayClub));
                    Assert.Equal(series.HomeClub, series.Games[0].Home);
                    upsets += s.ToList().IndexOf(series.HomeClub) > s.ToList().IndexOf(series.AwayClub) ? 1 : 0;
                }
            }
            Assert.Equal(playoffs.Champion, game.History[^1].Titles[Title.GermanChampion]);
            if (season < 4)
                game.StartNextSeason();
        }
        Assert.True(transfers > 0, "aktiver Manager hat keinen Spieler gekauft");
        Assert.Equal(5, game.History.Count);
        // Probe trifft auch den Fall, dass der schlechter Platzierte Heimrecht hat (Sieger der oberen Paarung).
        Assert.True(upsets > 0, "kein Heimrecht beim schlechter Platzierten in fünf Saisons");
    }

    /// <summary>
    /// FILE44 und FILE22 (Endspiel gespielt): Spielt das Remake die übernommenen Viertelfinal- und Halbfinalergebnisse selbst durch,
    /// entstehen Halbfinale und Endspiel mit denselben Paarungen und demselben Heimrecht wie im HOK (Plätze 8–13 von DGROUP 0x5D3B).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE44.HOK")]
    [InlineData("FILE22.HOK")]
    public void Playoffs_OwnRounds_PairLikeTheOriginal(string file)
    {
        var (save, game, clubOf) = Import(file);
        var imported = game.Playoffs!;
        Assert.Equal(3, imported.Rounds.Count);
        var slots = OriginalImport.PlayoffSlots(save);
        // HOK: Halbfinale Platz 8/9 und 10/11 in Setzreihenfolge (nach dem Rundenwechsel wiederhergestellt, 0x31C3A).
        Assert.Equal(new[] { (clubOf[slots[8]], clubOf[slots[9]]), (clubOf[slots[10]], clubOf[slots[11]]) },
            imported.Rounds[1].Select(s => (s.HomeClub, s.AwayClub)));

        var own = new Playoffs(imported.Seeds);
        for (var round = 0; round < 3; round++)
        {
            Assert.NotNull(own.SeriesOn(round * Playoffs.DatesPerRound, imported.Rounds[round][0].HomeClub));
            Assert.Equal(imported.Rounds[round].Select(s => (s.HomeClub, s.AwayClub)),
                own.Rounds[round].Select(s => (s.HomeClub, s.AwayClub)));
            for (var k = 0; k < own.Rounds[round].Count; k++)
                own.Rounds[round][k].Games.AddRange(imported.Rounds[round][k].Games);
        }
        Assert.Equal(imported.Champion, own.Champion);
    }

    /// <summary>
    /// R14HN-03: laufendes Viertelfinale, aus FILE44 zurückgerechnet. Nach <c>CCA3</c> = k Spielen hat 0x31505 die Paarungen k-mal
    /// getauscht (Plätze, Siege und Spiele 0 … k − 1, Aufruf 0x316C9 nach jedem Spiel 0–3); bei ungeradem k steht der besser
    /// Platzierte auf Platz 2k + 1. Der Import muss das zurücktauschen und nur die Spiele 0 … k − 1 übernehmen.
    /// </summary>
    [GameDataTheory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Import_RunningQuarterFinal_SwapsAtOddGameCount(int played)
    {
        var (full, fullGame, _) = Import("FILE44.HOK");
        var save = Read("FILE44.HOK");
        var slots = save.Block("5CFB").AsSpan(0x40, 24);
        var results = save.Block("CCB2");
        var wins = save.Block("CCA4");
        var original = OriginalImport.PlayoffSlots(full).ToArray();
        var originalResults = full.Block("CCB2").ToArray();
        for (var i = 8; i < 24; i++)
            slots[i] = 0xFF; // 0x31C72: hinter den Plätzen der laufenden Runde 0xFF
        Array.Clear(results);
        Array.Clear(wins);
        for (var p = 0; p < 8; p += 2)
        {
            var w = new int[2];
            for (var g = 0; g < played; g++)
            {
                var (a, b) = (originalResults[14 * g + p], originalResults[14 * g + p + 1]);
                (results[14 * g + p], results[14 * g + p + 1]) = (a, b);
                if (a >= OriginalImport.CupNotPlayed)
                    continue;
                // Heim in Spiel 1, 3, 5 ist Platz 2k; Gleichstand zählt für den Gast (0x3166E).
                var homeFirst = g % 2 == 0;
                var firstWins = a % 30 > b % 30 || (a % 30 == b % 30 && !homeFirst);
                w[firstWins ? 0 : 1]++;
            }
            (wins[p], wins[p + 1]) = ((byte)w[0], (byte)w[1]);
            if (played % 2 == 1)
            {
                (slots[p], slots[p + 1]) = (original[p + 1], original[p]);
                (wins[p], wins[p + 1]) = (wins[p + 1], wins[p]);
                for (var g = 0; g < played; g++)
                    (results[14 * g + p], results[14 * g + p + 1]) = (results[14 * g + p + 1], results[14 * g + p]);
            }
        }
        save.Block("07A9")[7] = 0;
        save.Block("CCA3")[0] = (byte)played;
        save.Block("CFF8")[2] = 0; // DGROUP 0xCFFA: Anfang der Runde

        var game = GameState.ImportOriginal(save, ClubDatabase.Load(TestGameData.Load()), Seed);
        var playoffs = game.Playoffs!;
        Assert.Single(playoffs.Rounds);
        Assert.Null(playoffs.Champion);
        var expected = fullGame.Playoffs!.Rounds[0];
        for (var k = 0; k < 4; k++)
        {
            var s = playoffs.Rounds[0][k];
            Assert.Equal((expected[k].HomeClub, expected[k].AwayClub), (s.HomeClub, s.AwayClub));
            Assert.Equal(expected[k].Games.Take(played), s.Games);
        }
    }

    /// <summary>
    /// R14HN-03: Aufstiegsrunde nach 3 von 6 Terminen (FILE44 zurückgerechnet: Punkte <c>CD58</c> nur aus den Terminen 1–3). Gespielte
    /// Termine = Punktsumme / 4 ([APPROX], kein Terminzähler im HOK); die Paarungen aller sechs Termine bleiben stehen.
    /// </summary>
    [GameDataFact]
    public void Import_RelegationGroup_ThreeOfSixDates()
    {
        const int played = 3;
        var (_, fullGame, _) = Import("FILE44.HOK");
        var save = Read("FILE44.HOK");
        var points = save.Block("CD58");
        for (var g = 0; g < 2; g++)
        {
            var teams = OriginalImport.GroupTeams(save, 1).Slice(4 * g, 4).ToArray();
            var sum = new int[4];
            for (var d = 0; d < played; d++)
                foreach (var (home, away, hg, ag) in OriginalImport.GroupDate(save, 1, g, d, 2))
                {
                    var (h, a) = (Array.IndexOf(teams, (byte)home), Array.IndexOf(teams, (byte)away));
                    sum[h] += hg > ag ? 2 : hg == ag ? 1 : 0;
                    sum[a] += ag > hg ? 2 : hg == ag ? 1 : 0;
                }
            for (var i = 0; i < 4; i++)
                points[12 + 6 * g + i] = (byte)sum[i];
        }

        var game = GameState.ImportOriginal(save, ClubDatabase.Load(TestGameData.Load()), Seed);
        for (var g = 0; g < 2; g++)
        {
            var group = game.RelegationGroups[g];
            var full = fullGame.RelegationGroups[g];
            Assert.Equal(played, group.PlayedRounds);
            Assert.Equal(full.Results.Where(r => r.MatchDay <= played), group.Results);
            Assert.Equal(6, group.Rounds.Count);
            Assert.Equal(full.Rounds.Select(r => r.Matches), group.Rounds.Select(r => r.Matches));
        }
    }

    /// <summary>
    /// #669 (A380) im Import: Ein eigener Spieler auf dem Markt-Platz (Satz 4, Pool +0x1A = Manager) steht nur dort, nicht im Kader –
    /// wie nach „Verkaufen“ im Remake (<see cref="TransferMarket.Offer"/>). FILE41: HOHENADL (Markt-Platz 7), FILE26: HOCK.
    /// Der Kader hat genau die belegten Plätze des Managersatzes.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE41.HOK", "HOHENADL")]
    [InlineData("FILE26.HOK", "HOCK")]
    public void Import_OwnListedPlayer_OnlyOnMarket(string file, string name)
    {
        var (save, game, _) = Import(file);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var own = Assert.Single(game.TransferMarket.Listings, l => l.Seller == game.Managers[0]);
        Assert.Equal(name, own.Player.Name);
        Assert.Same(club, own.From);
        Assert.DoesNotContain(own.Player, club.Players);
        Assert.Contains(own, game.TransferMarket.OwnListingsOutsideSquad(club));
        Assert.Contains(own.Player, game.AllPlayers);
        Assert.False(club.Lineup.Contains(own.Player));
        Assert.Equal(OccupiedSlots(save, 0).Count, club.Players.Count);
        Assert.Equal(club.Players.Count, club.Players.Select(p => p.Name).Distinct().Count());
    }

    /// <summary>
    /// R14KS9-01 (Import-Teil): Überträge je Wettbewerb (<c>+0x1C</c> − <c>+0x06</c>, <c>+0x1E</c> − <c>+0x07</c>, …) an allen
    /// Ständen mit Managerkader; mindestens ein Stand ab der zweiten Saison hat einen Pokalübertrag (+0x1E &gt; +0x07), sonst bliebe
    /// <c>ClubCupGamesCarry = 0</c> (Mutation I3) unbemerkt. Europapokal: an keinem Stand belegt (nur 0x5494C–0x5499E).
    /// </summary>
    [GameDataFact]
    public void Import_CarriesPerCompetition_AllStands()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var folder = Path.GetDirectoryName(OriginalImportTests.OriginalSave("FILE01.HOK"))!;
        var cupCarries = 0;
        var checkedStands = 0;
        foreach (var path in Directory.GetFiles(folder, "FILE*.HOK").Order(StringComparer.Ordinal))
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(path));
            var game = GameState.ImportOriginal(save, database, Seed);
            if (game.WorldOnly)
                continue;
            checkedStands++;
            for (var m = 0; m < game.Managers.Count; m++)
            {
                var club = game.Clubs[game.Managers[m].ClubIndex];
                var slots = OccupiedSlots(save, m);
                Assert.Equal(slots.Count, club.Players.Count);
                for (var i = 0; i < slots.Count; i++)
                {
                    var (s, p) = (slots[i], club.Players[i]);
                    int Word(int at) => BinaryPrimitives.ReadInt16LittleEndian(s.AsSpan(at));
                    var where = $"{Path.GetFileName(path)} Manager {m} Platz {i} ({p.Name})";
                    Assert.True(Word(0x1C) - s[0x06] == p.ClubGamesCarry, where);
                    Assert.True(Word(0x1E) - s[0x07] == p.ClubCupGamesCarry, where);
                    Assert.True(Word(0x20) - s[0x08] == p.ClubEuropeGamesCarry, where);
                    Assert.True(Word(0x1E) == p.ClubGames(CompetitionSlot.Cup), where);
                    if (p.Role != PlayerRole.Goalkeeper)
                    {
                        Assert.True(Word(0x24) - s[0x04] == p.ClubCupGoalsCarry, where);
                        Assert.True(Word(0x24) == p.ClubGoals(CompetitionSlot.Cup), where);
                    }
                    cupCarries += Word(0x1E) > s[0x07] ? 1 : 0;
                }
            }
        }
        Assert.True(checkedStands > 20, $"nur {checkedStands} Stände mit Managerkader");
        Assert.True(cupCarries > 0, "kein Stand mit Pokalübertrag");
    }

    /// <summary>
    /// R683-01 (Migration 43 → 44): Ein Stand mit laufendem Viertelfinale (4 Spiele gespielt) aus der Zeit vor #676 führte die
    /// Serien als 1–8, 4–5, 2–7, 3–6 und die Felder <c>HigherSeed</c>/<c>LowerSeed</c>. Der Test baut diesen Stand aus einem heutigen
    /// (Schema 44 → 43, alte Folge, Objektnummern neu). Nach dem Laden stehen die Serien in Platzfolge, der Stand ist Byte für
    /// Byte der ungesicherte, und das Halbfinale paart nach Tabelle 0x144 wie ohne Sichern: Sieger 1–8 gegen 3–6, 2–7 gegen 4–5.
    /// Ohne Migration paarte dieser Stand 1–8 gegen 2–7 (Gegenbeispiel des Reviews: Sieger 1, 4, 2, 3 → 1–2 | 4–3 statt 1–3 | 2–4).
    /// </summary>
    [Fact]
    public void Load_Schema43_RunningQuarterFinal_SemifinalByTable0x144()
    {
        // #784 (Runde 17, Gast nach dem Heimverein): Saat 3 → 4 – mit 3 erreicht der Managerverein die Play-offs nicht mehr,
        // der Halt nach vier Viertelfinalspielen entfällt (12 gespielt); Vorbedingung unten unverändert.
        var game = TestGames.Create(4);
        for (var guard = 0; game.Playoffs is not { } p || p.Rounds[0].Sum(x => x.Games.Count) < 4; guard++)
        {
            Assert.True(guard < 5000, "Viertelfinale nicht erreicht");
            game.Continue();
        }
        Assert.Equal(4, game.Playoffs!.Rounds[0].Sum(x => x.Games.Count));
        var current = SaveGame.Save(game);

        var old = SaveGame.Load(ToSchema43(current));
        Assert.True(old.Success, old.Error);
        var loaded = old.Game!;
        var s = loaded.Playoffs!.Seeds;
        Assert.Equal(new[] { (s[0], s[7]), (s[1], s[6]), (s[2], s[5]), (s[3], s[4]) },
            loaded.Playoffs.Rounds[0].Select(x => (x.HomeClub, x.AwayClub)));
        Assert.True(current.AsSpan().SequenceEqual(SaveGame.Save(loaded)), "migrierter Stand weicht vom ungesicherten ab");

        foreach (var g in new[] { game, loaded })
            for (var guard = 0; g.Playoffs!.Rounds.Count < 2 || g.Playoffs.Rounds[1].Sum(x => x.Games.Count) == 0; guard++)
            {
                Assert.True(guard < 500, "Halbfinale nicht erreicht");
                g.Continue();
            }
        var quarter = loaded.Playoffs!.Rounds[0];
        Assert.Equal(new[] { (quarter[0].Winner, quarter[2].Winner), (quarter[1].Winner, quarter[3].Winner) },
            loaded.Playoffs.Rounds[1].Select(x => ((int?)x.HomeClub, (int?)x.AwayClub)));
        Assert.Equal(game.Playoffs!.Rounds[1].Select(x => (x.HomeClub, x.AwayClub)),
            loaded.Playoffs.Rounds[1].Select(x => (x.HomeClub, x.AwayClub)));
    }

    /// <summary>Kauft den ersten Spieler vom Markt, der nicht aus dem eigenen Verein kommt, zum Listenpreis (Kader höchstens 26).</summary>
    private static int BuyOne(GameState game, Manager manager)
    {
        if (game.Clubs[manager.ClubIndex].Players.Count >= 26)
            return 0;
        foreach (var listing in game.TransferMarket.Listings.Where(l => l.From.Index != manager.ClubIndex).ToList())
            if (game.TransferMarket.Buy(game, manager, listing, listing.Price, loan: false) == TransferResult.Accepted)
                return 1;
        return 0;
    }

    /// <summary>Heutigen Stand in die Form von Schema 43 vor #676 bringen (nur Testhilfe).</summary>
    private static byte[] ToSchema43(byte[] data)
    {
        var root = JsonNode.Parse(data)!.AsObject();
        root["schema"] = 43;
        var playoffs = root["spiel"]!["Playoffs"]!.AsObject();
        foreach (var series in playoffs["_rounds"]!["items"]!.AsArray().SelectMany(r => r!["items"]!.AsArray()).Select(x => x!.AsObject()))
        {
            var (home, away) = (series["HomeClub"]!, series["AwayClub"]!);
            series.Remove("HomeClub");
            series.Remove("AwayClub");
            series["HigherSeed"] = home;
            series["LowerSeed"] = away;
        }
        var first = playoffs["_rounds"]!["items"]![0]!["items"]!.AsArray();
        var neu = first.ToList();
        first.Clear();
        foreach (var k in new[] { 0, 3, 1, 2 }) // vor #676: 1–8, 4–5, 2–7, 3–6
            first.Add(neu[k]);
        var map = new Dictionary<int, int>();
        var next = 0;
        Renumber(root);
        return Encoding.UTF8.GetBytes(root.ToJsonString());

        void Renumber(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["$id"] is JsonValue id)
                {
                    map[id.GetValue<int>()] = next;
                    obj["$id"] = next++;
                }
                else if (obj.Count == 1 && obj["$ref"] is JsonValue r)
                    obj["$ref"] = map[r.GetValue<int>()];
                foreach (var (key, child) in obj.ToArray())
                    if (key is not "$id" and not "$ref")
                        Renumber(child);
            }
            else if (node is JsonArray array)
                foreach (var child in array.ToArray())
                    Renumber(child);
        }
    }

    private static List<byte[]> OccupiedSlots(OriginalSaveGame save, int record) =>
        Enumerable.Range(0, OriginalSaveGame.SquadSlotsPerManager)
            .Select(i => OriginalImport.SquadSlot(save, record, i).ToArray())
            .Where(s => s.Any(b => b != 0))
            .ToList();

    private static OriginalSaveGame Read(string file) => OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));

    private static (OriginalSaveGame Save, GameState Game, int[] ClubOf) Import(string file)
    {
        var save = Read(file);
        var database = ClubDatabase.Load(TestGameData.Load());
        return (save, GameState.ImportOriginal(save, database, Seed), OriginalImport.ClubIndices(save, database));
    }
}
