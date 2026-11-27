using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>feldspieler-sechs</c>, Runde 23, Teil 1 (#939, A937, Rest A683): sechster Feldspielerplatz in Reihe 1 ohne Torhüter.
/// [ORIGINAL] em.exe Datei 0x413C9–0x414B4 (Platz öffnen), 0x412CF–0x412DB (Abwehrhälfte mit drei Zellen), 0x40EC4–0x40EFF
/// („Schon 6 Feldspieler“), 0x40B95/0x41043 (Nachrücken), Gitteraufbau 0x40478–0x404FD. Synthetisch, ohne Originaldaten.
/// </summary>
public class FeldspielerSechsTests
{
    private static int s_nextId = 93_900;

    private static Player NewPlayer(PlayerRole role, string name)
    {
        var p = new Player { Name = name, Role = role, Condition = 70, Technique = 70, Form = 50, Age = 25, ContractYears = 2 };
        typeof(Player).GetProperty(nameof(Player.Id))!.SetValue(p, Interlocked.Increment(ref s_nextId));
        return p;
    }

    /// <summary>2 Torhüter, 8 Verteidiger, 12 Stürmer; „A“ stellt drei Blöcke auf, Einstellung aus, dann Torhüter heraus.</summary>
    private static Club NewClubWithoutGoalie()
    {
        var club = new Club { Index = 3, Name = "Test", Strength = 70 };
        for (var i = 0; i < 2; i++) club.Players.Add(NewPlayer(PlayerRole.Goalkeeper, $"T{i}"));
        for (var i = 0; i < 8; i++) club.Players.Add(NewPlayer(PlayerRole.Defender, $"V{i}"));
        for (var i = 0; i < 12; i++) club.Players.Add(NewPlayer(PlayerRole.Forward, $"S{i}"));
        club.RenumberPlayers();
        club.AutoLineup();
        club.AutoLineupEnabled = false;
        club.ClearLineupSlot(LineupSlot.Goalkeeper);
        return club;
    }

    private static Player Free(Club club, PlayerRole role, int skip = 0) =>
        club.Players.Where(p => p.Role == role && !club.Lineup.Contains(p)).OrderBy(p => !club.IsDressed(p)).Skip(skip).First();

    /// <summary>
    /// A937: Reihe 1 voll (3 + 2), kein Torhüter: ein weiterer Verteidiger kommt auf den sechsten Feldspielerplatz, eingesetzt, Kaderplatz
    /// +0x15 = 1 (Abwehr, Reihe 1). Ein siebter findet keinen Platz. Vorher: „Kein / Platz mehr !“ schon beim sechsten.
    /// </summary>
    [Fact]
    public void ClubClick_Line1FullWithoutGoalie_OpensSixthSkaterSlot()
    {
        var club = NewClubWithoutGoalie();
        int? line = 0;
        var sixth = Free(club, PlayerRole.Defender);

        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(sixth, ref line, right: false));

        Assert.True(club.Lineup.Contains(sixth));
        Assert.True(club.IsDressed(sixth));
        Assert.Equal(1, club.Lineup.PositionOf(sixth));
        Assert.Equal(6, LineupSlot.All.Where(s => s.Block == 0 && s.Role != PlayerRole.Goalkeeper).Count(s => club.Lineup[s] is not null));
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(Free(club, PlayerRole.Defender), ref line, right: false));
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(Free(club, PlayerRole.Forward), ref line, right: false));
    }

    /// <summary>A937: Auch ein Stürmer kommt auf den sechsten Platz (Datei 0x413E8 prüft die Art nicht), mit +0x15 = 1 (Abwehrhälfte).</summary>
    [Fact]
    public void ClubClick_ForwardIntoSixthSkaterSlot_PositionIsDefenceHalf()
    {
        var club = NewClubWithoutGoalie();
        int? line = 0;
        var forward = Free(club, PlayerRole.Forward);

        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(forward, ref line, right: false));

        Assert.Same(forward, club.Lineup[LineupSlot.SixthSkater]);
        Assert.Equal(1, club.Lineup.PositionOf(forward));
        Assert.True(club.Lineup.SixthSkaterOpen);
    }

    /// <summary>Andere Reihen und Reihe 1 mit Torhüter öffnen keinen sechsten Platz (Datei 0x413C9, 0x413DD).</summary>
    [Fact]
    public void ClubClick_OtherLineOrWithGoalie_NoRoom()
    {
        var club = NewClubWithoutGoalie();
        int? line = 1;
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(Free(club, PlayerRole.Defender), ref line, right: false));

        line = Lineup.GoalkeeperLine;
        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(Free(club, PlayerRole.Goalkeeper), ref line, right: false));
        line = 0;
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(Free(club, PlayerRole.Defender), ref line, right: false));
        Assert.Null(club.Lineup.SixthSkater);
    }

    /// <summary>
    /// Datei 0x40EC4–0x40EFF: Ist der sechste Platz offen, weist ein Linksklick auf einen Torhüter bei gewählter Torhüterreihe mit
    /// „Schon 6 Feldspieler / auf dem Eis !“ ab; nichts ändert sich. Ein Rechtsklick setzt ihn wie sonst ein bzw. heraus.
    /// </summary>
    [Fact]
    public void ClubClick_GoalieWhileSixthOpen_SixSkatersMessage()
    {
        var club = NewClubWithoutGoalie();
        int? line = 0;
        club.ClickPlayer(Free(club, PlayerRole.Defender), ref line, right: false);
        var before = LineupSlot.All.Select(s => club.Lineup[s]).ToList();
        var goalie = club.Players.First(p => p.Role == PlayerRole.Goalkeeper && club.IsDressed(p));

        line = Lineup.GoalkeeperLine;
        Assert.Equal(LineupResult.SixSkaters, club.ClickPlayer(goalie, ref line, right: false));
        Assert.Equal(before, LineupSlot.All.Select(s => club.Lineup[s]).ToList());
        Assert.Equal(LineupResult.Undressed, club.ClickPlayer(goalie, ref line, right: true));
    }

    /// <summary>
    /// Datei 0x40B95–0x40B9B und 0x41043–0x41049: Wird ein Abwehrplatz der Reihe 1 frei (Platz geleert oder aus der Kaderliste
    /// herausgenommen), rückt der sechste Feldspieler hinein und der sechste Platz schließt; danach kann ein Torhüter wieder hinein.
    /// </summary>
    [Fact]
    public void FreedDefenceSlotInLine1_SixthSkaterMovesIn()
    {
        var club = NewClubWithoutGoalie();
        int? line = 0;
        var sixth = Free(club, PlayerRole.Defender);
        club.ClickPlayer(sixth, ref line, right: false);

        club.ClearLineupSlot(LineupSlot.Defender(0, 0));
        Assert.Same(sixth, club.Lineup[LineupSlot.Defender(0, 0)]);
        Assert.Null(club.Lineup.SixthSkater);
        Assert.False(club.Lineup.SixthSkaterOpen);

        var again = Free(club, PlayerRole.Defender);
        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(again, ref line, right: false));
        Assert.Same(again, club.Lineup.SixthSkater);
        var taken = club.Lineup[LineupSlot.Defender(0, 1)]!;
        Assert.Equal(LineupResult.TakenOut, club.ClickPlayer(taken, ref line, right: false));
        Assert.Same(again, club.Lineup[LineupSlot.Defender(0, 1)]);
        Assert.Null(club.Lineup.SixthSkater);

        line = Lineup.GoalkeeperLine;
        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(Free(club, PlayerRole.Goalkeeper), ref line, right: false));
    }

    /// <summary>„C“ (alle Plätze leeren) leert auch den sechsten Platz; niemand rückt in einen eben geleerten Platz nach.</summary>
    [Fact]
    public void ClearLineup_ClearsSixthSkaterToo()
    {
        var club = NewClubWithoutGoalie();
        int? line = 0;
        club.ClickPlayer(Free(club, PlayerRole.Defender), ref line, right: false);

        club.ClearLineup();

        Assert.Equal(0, club.Lineup.Count);
    }

    /// <summary>
    /// A672 und #939: Ohne freien Torhüter steht der Feldspieler aus der Torhütersuche der Automatik auf dem sechsten Platz (+0x15 = 1)
    /// und spielt mit (<see cref="LivePlayer.Played"/>); im Spiel zählt er als dritter Verteidiger, ohne Torhüter (Datei 0x38A36).
    /// </summary>
    [Fact]
    public void AutoLineupWithoutGoalie_StandInOnSixthSkaterSlot_SixSkatersOnIce()
    {
        var club = NewClubWithoutGoalie();
        foreach (var g in club.Players.Where(p => p.Role == PlayerRole.Goalkeeper))
            g.InjuredGames = 5;
        club.AutoLineup();

        var standIn = club.Lineup.SixthSkater;
        Assert.NotNull(standIn);
        Assert.Same(standIn, club.Lineup.GoalieStandIn);
        Assert.True(club.Lineup.SixthSkaterOpen);
        var squad = LiveSquad.Create(club, 1, null);
        var live = squad.Players.Single(p => ReferenceEquals(p.Player, standIn));
        Assert.Equal(1, live.Position);
        Assert.True(live.Played);
        Assert.Equal(0, MatchEngine.CountOnIce(squad, 0, 0, 0));
        Assert.Equal(3, MatchEngine.CountOnIce(squad, 1, 0, 0));
        Assert.Equal(3, MatchEngine.CountOnIce(squad, 2, 1, 0));
    }

    /// <summary>Sichern und Laden halten den sechsten Feldspieler; Speichern → Laden → Speichern byte-gleich.</summary>
    [Fact]
    public void SaveGame_RoundTrip_KeepsSixthSkater()
    {
        var game = TestGames.Create();
        var club = game.Clubs[game.Managers[0].ClubIndex];
        club.AutoLineupEnabled = false;
        club.ClearLineupSlot(LineupSlot.Goalkeeper);
        int? line = 0;
        for (var guard = 0; club.Lineup.SixthSkater is null; guard++)
        {
            Assert.True(guard < 40, "Reihe 1 nicht voll");
            var free = club.Players.FirstOrDefault(p => p.Role != PlayerRole.Goalkeeper && p.Available && !club.Lineup.Contains(p)
                && club.IsDressed(p)) ?? club.Players.First(p => p.Role != PlayerRole.Goalkeeper && p.Available && !club.Lineup.Contains(p));
            club.ClickPlayer(free, ref line, right: false);
        }
        var sixthId = club.Lineup.SixthSkater!.Id;

        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes);

        Assert.True(loaded.Success, loaded.Error);
        var loadedClub = loaded.Game!.Clubs[club.Index];
        Assert.Equal(sixthId, loadedClub.Lineup.SixthSkater?.Id);
        Assert.Equal(bytes, SaveGame.Save(loaded.Game!));
    }

    /// <summary>
    /// HOK-Export und -Import (A937): Der sechste Feldspieler steht im Kaderplatz mit +0x15 = 1 (Abwehr, Reihe 1) wie die übrigen
    /// Verteidiger der Reihe 1; der Import ordnet den dritten Spieler der Abwehrhälfte in Reihe 1 dem sechsten Platz zu (Gitter Datei
    /// 0x40478–0x404D7). Vorher: dritter Verteidiger beim Import verworfen.
    /// </summary>
    [GameDataFact]
    public void OriginalExportImport_KeepsSixthSkater()
    {
        var raw = OriginalExportTests.RawDatabase();
        var database = ClubDatabase.Parse(raw);
        var game = OriginalExportTests.NewGameForExport(database);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        club.AutoLineupEnabled = false;
        club.ClearLineupSlot(LineupSlot.Goalkeeper);
        int? line = 0;
        var sixth = club.Players.First(p => p.Role == PlayerRole.Defender && p.Available && !club.Lineup.Contains(p) && club.IsDressed(p));
        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(sixth, ref line, right: false));
        var squadIndex = club.Players.IndexOf(sixth);

        var exported = OriginalSaveGame.Read(OriginalExport.Export(game, raw));
        var slot = exported.Block("5DC3").AsSpan(squadIndex * OriginalSaveGame.SquadSlotLength,
            OriginalSaveGame.SquadSlotLength);
        Assert.Equal(1, slot[0x15]);
        Assert.Equal(1, slot[0x0A]);

        var again = GameState.ImportOriginal(exported, database, 4711);
        var importedClub = again.Clubs[club.Index];
        // Zelle 5 bekommt im Gitter der dritte Spieler der Abwehrhälfte in Kaderfolge (Datei 0x40478), nicht der zuletzt aufgestellte.
        static IEnumerable<string> DefenceHalf(Lineup l) =>
            Lineup.SlotsOfLine(0, PlayerRole.Defender).Append(LineupSlot.SixthSkater).Select(s => l[s]?.Name ?? "").Order();
        Assert.Equal(DefenceHalf(club.Lineup), DefenceHalf(importedClub.Lineup));
        Assert.NotNull(importedClub.Lineup.SixthSkater);
        Assert.Null(importedClub.Lineup.Goalkeeper);
        Assert.True(importedClub.Lineup.SixthSkaterOpen);
    }

    /// <summary>
    /// R963-01: Nationalaufstellung ohne Torhüter mit sechstem Feldspieler (Verteidiger oder Stürmer) übersteht HOK-Export und
    /// -Import. Der Export schreibt +0x15 = 1 (Abwehr, ohne Bit 0x80); der Import des Satzes 5 ordnete vorher nach
    /// <see cref="Player.Role"/> mit zwei Abwehrplätzen und verwarf den Spieler (Formation 16 → 15, nur eingesetzt).
    /// </summary>
    [GameDataTheory]
    [InlineData(PlayerRole.Defender)]
    [InlineData(PlayerRole.Forward)]
    public void OriginalExportImport_National_KeepsSixthSkater(PlayerRole role)
    {
        var raw = OriginalExportTests.RawDatabase();
        var database = ClubDatabase.Parse(raw);
        var game = OriginalExportTests.NewGameForExport(database);
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.AutoFormation();
        coach.ClearSlot(LineupSlot.Goalkeeper);
        int? line = 0;
        var sixth = coach.Squad.Select(m => m.Player).First(p => p.Role == role && p.Available && !coach.Formation.Contains(p));
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(sixth, ref line, right: false));
        Assert.Same(sixth, coach.Formation.SixthSkater);
        var count = coach.Formation.Count;

        var again = GameState.ImportOriginal(OriginalSaveGame.Read(OriginalExport.Export(game, raw)), database, 4711);

        var imported = again.NationalCoach!;
        Assert.Equal(count, imported.Formation.Count);
        Assert.Equal(sixth.Name, imported.Formation.SixthSkater?.Name);
        Assert.Null(imported.Formation.Goalkeeper);
        Assert.True(imported.Formation.SixthSkaterOpen);
        static IEnumerable<string> Line1(Lineup l) =>
            Lineup.SlotsOfLine(0, PlayerRole.Forward).Concat(Lineup.SlotsOfLine(0, PlayerRole.Defender)).Append(LineupSlot.SixthSkater)
                .Select(s => l[s]?.Name ?? "").Order();
        Assert.Equal(Line1(coach.Formation), Line1(imported.Formation));
    }

    private static int Rename(JsonNode? node, string from, string to)
    {
        var n = 0;
        switch (node)
        {
            case JsonObject obj:
                if (obj.ContainsKey(from))
                {
                    var value = obj[from]?.DeepClone();
                    obj.Remove(from);
                    obj[to] = value;
                    n++;
                }
                foreach (var child in obj.Select(kv => kv.Value).ToList())
                    n += Rename(child, from, to);
                break;
            case JsonArray array:
                foreach (var child in array)
                    n += Rename(child, from, to);
                break;
        }
        return n;
    }
}
