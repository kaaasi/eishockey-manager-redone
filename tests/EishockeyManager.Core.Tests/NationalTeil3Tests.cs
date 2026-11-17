using System.Text;
using System.Text.Json.Nodes;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur national, Teil 3 (Runde 22): Kandidaten aus dem Pool (A020), Gleichstand in Poolfolge (A487), Form Deutschlands ohne
/// Bundestrainer (A493) und der Satz 5 als eigene Kaderplätze samt HOK-Import (A412). Fundstellen in
/// <c>CupsNational.GameState.cs</c>, <c>CupsNational.Slot.cs</c>, <c>CupsNational.cs</c> und <c>OriginalImport.National.cs</c>.
/// </summary>
public class NationalTeil3Tests
{
    private static (GameState Game, NationalCoach Coach) WorldOnlyCoach(int seed = 7)
    {
        var game = GameModeTests.Create(seed, false, true, 2);
        game.Continue();
        return (game, game.NationalCoach!);
    }

    /// <summary>
    /// A020: [ORIGINAL] Datei 0x3C5C9–0x3C5D0: Die Liste „Spieler in WM-Kader“ läuft über die Poolplätze 1 … 0xB3 in Poolfolge;
    /// Ausländer (Pool +0x13 ≠ 0) lassen sich nicht aufnehmen. Rot vor Teil 3: alle deutschen Spieler der Ligavereine (auch die nur im
    /// Remake geführten Computerkader) nach Stärke sortiert.
    /// </summary>
    [Fact]
    public void Candidates_ArePoolSlotsInPoolOrder()
    {
        var game = GameModeTests.Create(3, true, true, 2);
        game.AppointNationalCoach(game.Managers[0]);
        var last = game.TransferMarket.PoolLayout.LeagueLast;
        var expected = game.TransferMarket.Pool.Where(s => s.Index >= 1 && s.Index <= last && !s.Player.IsForeigner)
            .OrderBy(s => s.Index).Select(s => s.Player).ToList();
        var candidates = game.NationalCandidates();
        Assert.Equal(expected, candidates.Select(c => c.Player).Take(expected.Count));
        Assert.All(candidates, c => Assert.False(c.Player.IsForeigner));
        // Computerkader ohne Poolplatz stehen nicht zur Wahl.
        var computer = game.Clubs.Where(c => c.League is not null && game.Managers.All(m => m.ClubIndex != c.Index))
            .SelectMany(c => c.Players).First(p => !p.IsForeigner && game.TransferMarket.Pool.All(s => s.Player != p));
        Assert.DoesNotContain(candidates, c => c.Player == computer);
    }

    /// <summary>
    /// A487: [ORIGINAL] Datei 0x3CC01, strikter Vergleich Datei 0x3CCBA–0x3CCC4: Bei gleicher Summe gewinnt der zuerst durchsuchte
    /// Poolplatz. Zwei Stürmer gleicher Summe, der frühere Poolplatz hat die höhere ID: Rot vor Teil 3 (Gleichstand nach ID).
    /// </summary>
    [Fact]
    public void AutoNominate_TieGoesToEarlierPoolSlot()
    {
        var (game, coach) = WorldOnlyCoach();
        var forwards = game.NationalCandidates().Select(c => c.Player).Where(p => p.Role == PlayerRole.Forward).ToList();
        var (early, late) = forwards.SelectMany((a, i) => forwards.Skip(i + 1).Select(b => (a, b))).First(t => t.a.Id > t.b.Id);
        foreach (var p in new[] { early, late })
        {
            p.Condition = p.Technique = p.Form = Player.MaxValue;
            p.PoolForm = null;
        }
        game.AutoNominate();
        Assert.Equal([early, late], coach.Rows.Where(p => p.Role == PlayerRole.Forward).Take(2).Select(coach.SourceOf));
    }

    /// <summary>
    /// A493: [ORIGINAL] Datei 0x554CF–0x55570: Ohne Bundestrainer würfelt der Tag mit Markierung 255 je Abwehr/Sturm Kondition
    /// +0x18 + i, Technik +0x1A + i und Form +0x1C + i des Landes (random(45, 55)). Rot vor Teil 3: die Form wurde verworfen.
    /// </summary>
    [Fact]
    public void Reroll_WritesGermanForm()
    {
        var game = GameModeTests.Create(5, true, true, 2);
        var germany = game.Clubs[NationalTeams.Germany];
        MatchEngine.EnsureTeamValues(game);
        germany.TeamValues = MatchEngine.TeamValuesOf(germany) with { DefenseForm = 0, AttackForm = 0 };
        var copy = GameRandom.FromState(game.Random.Capture());
        game.RerollNationalRatings();
        int[] form = new int[2];
        for (var i = 0; i < 2; i++)
        {
            _ = copy.Next(84, 95);
            _ = copy.Next(79, 90);
            form[i] = copy.Next(45, 56);
        }
        Assert.Equal((form[0], form[1]), (germany.TeamValues!.Value.DefenseForm, germany.TeamValues.Value.AttackForm));
        Assert.Equal(form[0], game.NationalTeamValues(NationalTeams.Germany).Defense.Form);
    }

    /// <summary>
    /// A412: [ORIGINAL] 0x3CABF → 0x42CB1 legt für jeden Aufgenommenen einen eigenen Kaderplatz im Satz 5 an (Werte aus dem Pool
    /// +0x15–0x17, Belastung random(80, 120), Vertrag 1, Tore 0). Bonus und Belastung der Freundschaftsspiele treffen nur diesen Platz;
    /// der Spieler im Verein bzw. Pool bleibt, der Platz entfällt am Saisonende. Rot vor Teil 3: der Bonus erhöhte die Werte des
    /// Spielers selbst.
    /// </summary>
    [Fact]
    public void Nomination_CreatesOwnSlot_BonusLeavesSourceUntouched()
    {
        var (game, coach) = WorldOnlyCoach();
        game.AutoNominate();
        Assert.Equal(NationalCoach.MaxSquad, coach.Squad.Count);
        var before = coach.Squad.Select(m => (m.Source.Condition, m.Source.Technique, m.Source.Form, m.Source.Strain)).ToList();
        Assert.All(coach.Squad, m =>
        {
            Assert.NotSame(m.Source, m.Player);
            Assert.Equal((m.Source.Condition, m.Source.Technique, m.Source.PoolForm ?? m.Source.Form),
                (m.Player.Condition, m.Player.Technique, m.Player.Form));
            Assert.InRange(m.Player.Strain, 80, 120);
            Assert.Equal(1, m.Player.ContractYears);
            Assert.Equal(0, m.Player.Goals);
            Assert.True(m.Player.Id > 0 && m.Player.Id != m.Source.Id);
        });
        Assert.Empty(game.ValidatePlayerIdentity());

        // Freundschaftsspiele mit großem Bonus (teurer Gegner, zu Hause): Werte des Platzes steigen, die des Spielers nicht.
        game.NationalAccount = 10_000_000;
        var played = 0;
        for (var opponent = NationalTeams.First; opponent <= NationalTeams.Last && played < 3; opponent++)
            if (opponent != coach.Nation && game.PlayFriendly(opponent, home: true) == FriendlyResult.Played)
                played++;
        Assert.Equal(3, played);
        Assert.Equal(before, coach.Squad.Select(m => (m.Source.Condition, m.Source.Technique, m.Source.Form, m.Source.Strain)));
        Assert.All(coach.Squad, m => Assert.Equal(NationalCoach.FriendlyEnergy, m.Player.Strain));
        Assert.Contains(coach.Squad, m => m.Player.Condition + m.Player.Technique + m.Player.Form
                                          > m.Source.Condition + m.Source.Technique + (m.Source.PoolForm ?? m.Source.Form));
        // Länderspiele zählen auch am Spieler (Anzeige im Spielerfenster).
        Assert.All(coach.Lineup().Players, p => Assert.Equal(p.Caps, coach.SourceOf(p).Caps));

        // Herausnehmen über den Spieler im Verein/Pool oder den Platz.
        var member = coach.Squad[^1];
        Assert.Equal(NominationResult.Removed, game.ToggleNomination(member.Source));
        Assert.False(coach.Contains(member.Player));
        Assert.False(coach.Formation.Contains(member.Player));
    }

    /// <summary>
    /// A412: Eine Verletzung im WM-Spiel trifft den Kaderplatz des Satzes 5 (Datei 0x38D18–0x38D2C), nicht den Spieler im Verein; WM-Spiele
    /// zählen am Platz im Europapokal-Feld (+0x08, Datei 0x5494C–0x5499E). Über die WM bleiben die Spieler im Verein/Pool unverletzt und ohne
    /// Spiele (Modus „nur Weltkugel“, keine Vereinsspiele). Rot vor Teil 3: dieselben Spieler.
    /// </summary>
    [Fact]
    public void WorldCup_InjuriesAndGamesHitTheSlot()
    {
        var injuredSeen = false;
        foreach (var seed in new[] { 7, 11, 13, 17, 19 })
        {
            var (game, coach) = WorldOnlyCoach(seed);
            game.AutoNominate();
            var members = coach.Squad.ToList();
            for (var guard = 0; game.NationalCoach is not null && game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 400);
                game.Continue();
            }
            Assert.All(members, m => Assert.Equal(0, m.Source.InjuredGames));
            Assert.All(members, m => Assert.Equal(0, m.Source.EuropeGames));
            Assert.Contains(members, m => m.Player.EuropeGames > 0);
            injuredSeen |= members.Any(m => m.Player.InjuredGames > 0);
            if (injuredSeen)
                break;
        }
        Assert.True(injuredSeen, "keine Verletzung im WM-Kader über fünf Saaten");
    }

    /// <summary>
    /// A412 (HOK-Import): [ORIGINAL] FILE18 (Modus „nur Weltkugel“): Die Plätze des Satzes 5 tragen eigene Werte; KÜNAST steht in
    /// Satz 0 (Kader des Managers, 82/80/53) und in Satz 5 (FILE18 79/81/53, A023). Rot vor Teil 3 (#732): die Werte des Satzes 5
    /// galten auch für den Spieler aus Satz 0.
    /// </summary>
    [GameDataFact]
    public void Import_File18_Set5ValuesStayOnSlot()
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE18.HOK")));
        var game = GameState.ImportOriginal(save, ClubDatabase.Load(TestGameData.Load()), 4711);
        var coach = Assert.IsType<NationalCoach>(game.NationalCoach);
        var kuenast = coach.Squad.Single(m => m.Source.Name == "KÜNAST");
        Assert.Contains(kuenast.Source, game.Clubs[game.Managers[0].ClubIndex].Players);
        Assert.Equal((82, 80, 53), (kuenast.Source.Condition, kuenast.Source.Technique, kuenast.Source.Form));
        Assert.Equal((79, 81, 53), (kuenast.Player.Condition, kuenast.Player.Technique, kuenast.Player.Form));
        Assert.Empty(game.ValidatePlayerIdentity());
        var saved = SaveGame.Save(game);
        Assert.Equal(saved, SaveGame.Save(SaveGame.Load(saved).Game!));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): Modus „nur Weltkugel“ und Modus 4 über vier Saisonwechsel, passiv und aktiv (Kader „auto“,
    /// Freundschaftsspiele), mit Speichern/Laden: eigene Plätze des Satzes 5 mit gültigen IDs, keine Spuren am Spieler im Verein, nach
    /// dem Saisonwechsel kein Kader.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MultiSeason_SlotsPerSeason(bool flag, bool active)
    {
        // Modus 4: zwei Manager wie NationalTeil2Tests (Bundestrainer nicht jede Saison), Modus 1: Bundestrainer jede Saison.
        var second = Enumerable.Range(0, 200).First(i => NewGame.DatabaseLeague(i) == League.ZweiteLiga);
        var game = flag ? GameModeTests.Create(5, true, true, 2, second) : GameModeTests.Create(9, false, true, 2);
        var nominated = 0;
        for (var season = 0; season < 5; season++)
        {
            var members = new List<NationalSquadMember>();
            var europe = new Dictionary<Player, int>();
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = Math.Max(m.Money, 10_000_000);
                if (flag && active && game.CurrentDay <= SeasonCalendar.ClubSeasonEnd)
                {
                    TestSeason.KeepSquad(game, game.Managers[0], extendExpired: true);
                    ManagedSquads.PlayAtTopStrength(game, 0); // Platz unter den ersten fünf → Bundestrainer am 13. April
                }
                if (game.NationalCoach is { Squad.Count: 0 } coach)
                {
                    game.AutoNominate();
                    if (active)
                    {
                        game.NationalAccount = Math.Max(game.NationalAccount, 1_000_000);
                        _ = game.PlayFriendly(coach.Nation == NationalTeams.First ? NationalTeams.First + 1 : NationalTeams.First, home: false);
                    }
                    members = coach.Squad.ToList();
                    nominated++;
                    Assert.All(members, m => Assert.NotSame(m.Source, m.Player));
                    Assert.Empty(game.ValidatePlayerIdentity());
                    var saved = SaveGame.Save(game);
                    game = SaveGame.Load(saved).Game!;
                    Assert.Equal(saved, SaveGame.Save(game));
                    members = game.NationalCoach!.Squad.ToList();
                    europe = members.ToDictionary(m => m.Source, m => m.Source.EuropeGames);
                }
                game.Continue();
            }
            Assert.All(members, m => Assert.Equal(europe[m.Source], m.Source.EuropeGames));
            if (season < 4)
            {
                game.StartNextSeason();
                Assert.Null(game.NationalCoach);
                Assert.Empty(game.ValidatePlayerIdentity());
            }
        }
        Assert.True(nominated >= (!flag ? 5 : active ? 1 : 0), $"nur {nominated} Saisons mit WM-Kader");
    }

    /// <summary>
    /// Migration 54 → 55 (Teil 3): ein Stand, dessen Kader auf die Spieler selbst verweist (ohne <c>NationalSquadMember.Source</c>),
    /// lädt mit Source = Spieler; das aktuelle Schema ohne das Feld lehnt der strenge Leser ab.
    /// </summary>
    [Fact]
    public void Schema54_SquadWithoutSource_LoadsWithPlayerAsSource()
    {
        var (game, coach) = WorldOnlyCoach();
        game.AutoNominate();
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        var squad = root["spiel"]!["NationalCoach"]!["_squad"]!["items"]!.AsArray();
        foreach (var member in squad.OfType<JsonObject>())
        {
            member["Player"] = member["Source"]!.DeepClone();
            Assert.True(member.Remove("Source"));
        }
        Assert.False(SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString())).Success);

        root["schema"] = 54;
        var result = SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!.NationalCoach!;
        Assert.Equal(coach.Squad.Select(m => m.Source.Id), loaded.Squad.Select(m => m.Player.Id));
        Assert.All(loaded.Squad, m => Assert.Same(m.Source, m.Player));
    }
}
