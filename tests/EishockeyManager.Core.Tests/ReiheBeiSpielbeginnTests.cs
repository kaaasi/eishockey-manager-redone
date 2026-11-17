using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #872 (Runde 20, Spur pruefung-karte Teil 2): Reihe auf dem Eis bei Spielbeginn.
/// [ORIGINAL] Die Spielvorbereitung 0x84D7 (Datei 0x84EB–0x857D, Argument 0) setzt je Verein nur die Minuten auf dem Eis
/// (Vereinsfelder 0x2F/0x31, Datei 0x853F) auf 0 und den Wechselmodus der Computervereine (0x2C Bits 0–2, Datei 0x8521); die
/// Reihen 0x2E/0x30 bleiben aus dem Vorspiel stehen und stehen so auch im Spielstand (Satz far:9657 + 0x3D · Verein). Beleg
/// DOSBox `runde-19`: KÖLNER EC in FILE50 mit Abwehr 0 / Sturm 2, an Spieltag 2 beginnt Köln mit genau diesen Reihen
/// (<see cref="AbgleichDosboxLinesTests.File61"/>: Abwehr „000…“, Sturm „222…“ bis Minute 3); FILE61 1/1, FILE63 0/0, der Gegner
/// ESV KAUFBEUREN nach Spieltag 3 1/1, alle Vereine ohne Managerspiel 0/0.
/// </summary>
public class ReiheBeiSpielbeginnTests
{
    /// <summary>
    /// Regressionstest #872 (vor dem Fix rot: das Remake begann jedes Spiel mit Reihe 0/0): die Reihen des Managervereins und
    /// seines Gegners beim Anpfiff des nächsten Managerspiels sind die des Spielstands.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE50.HOK", "EC RATINGEN", "KÖLNER EC", 0, 0, 0, 2)]
    [InlineData("FILE61.HOK", "KÖLNER EC", "ESV KAUFBEUREN", 1, 1, 0, 0)]
    [InlineData("FILE63.HOK", "KÖLNER EC", "ESV KAUFBEUREN", 0, 0, 1, 1)]
    public void NextMatch_StartsWithLinesOfSave(string saveName, string homeName, string awayName,
        int homeDefense, int homeAttack, int awayDefense, int awayAttack)
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = HokCompare.Import(OriginalImportTests.OriginalSave(saveName), database, HokCompare.DefaultSeed);
        var managerClub = game.Clubs[game.Managers[0].ClubIndex];
        var home = game.Clubs.Single(c => c.Name == homeName);
        var away = game.Clubs.Single(c => c.Name == awayName);
        var homeTeam = MatchEngine.LiveTeamOf(game, home, away, managed: home == managerClub, managerMatch: true);
        var awayTeam = MatchEngine.LiveTeamOf(game, away, home, managed: away == managerClub, managerMatch: true);
        MatchEngine.PrepareMatch(game, home, away, homeTeam, awayTeam);

        Assert.Equal((homeDefense, homeAttack), (homeTeam.DefenseLine, homeTeam.AttackLine));
        Assert.Equal((awayDefense, awayAttack), (awayTeam.DefenseLine, awayTeam.AttackLine));
        Assert.Equal([0, 0], homeTeam.OnIceMinutes);
        Assert.Equal([0, 0], awayTeam.OnIceMinutes);
    }

    /// <summary>
    /// Review #881 P3-2 (#882): Rundlauf Original-Spielstand → Remake → Export (<c>OriginalExport.League.cs</c> 0x2E/0x30) → Import.
    /// Die Reihen auf dem Eis der Vereine stehen im Satz wie im Original (FILE61: KÖLNER EC und ESV KAUFBEUREN 1/1, FILE50: Köln
    /// 0/2); Mutation <c>r[0x30] = 0</c> oder <c>r[0x2E] = 0</c> im Export macht den Test rot.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE50.HOK", "KÖLNER EC", 0, 2)]
    [InlineData("FILE61.HOK", "KÖLNER EC", 1, 1)]
    [InlineData("FILE63.HOK", "ESV KAUFBEUREN", 1, 1)]
    public void Export_WritesMatchLines_RoundTrip(string saveName, string clubName, int defense, int attack)
    {
        var raw = OriginalExportTests.RawDatabase();
        var original = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(saveName)));
        var game = GameState.ImportOriginal(original, ClubDatabase.Parse(raw), HokCompare.DefaultSeed);
        var club = game.Clubs.Single(c => c.Name == clubName);
        Assert.Equal((defense, attack), (club.Tactics.MatchDefenseLine, club.Tactics.MatchAttackLine));

        var again = GameState.ImportOriginal(OriginalSaveGame.Read(OriginalExport.Export(game, raw)),
            ClubDatabase.Parse(raw), HokCompare.DefaultSeed);
        var wrong = game.Clubs.Where(c => !TransferMarket.IsNationalTeam(c) && c.Index < again.Clubs.Count)
            .Where(c => (c.Tactics.MatchDefenseLine, c.Tactics.MatchAttackLine) !=
                (again.Clubs[c.Index].Tactics.MatchDefenseLine, again.Clubs[c.Index].Tactics.MatchAttackLine))
            .Select(c => $"{c.Name}: {c.Tactics.MatchDefenseLine}/{c.Tactics.MatchAttackLine} → " +
                $"{again.Clubs[c.Index].Tactics.MatchDefenseLine}/{again.Clubs[c.Index].Tactics.MatchAttackLine}").ToList();
        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
        var after = again.Clubs[club.Index].Tactics;
        Assert.Equal((defense, attack), (after.MatchDefenseLine, after.MatchAttackLine));
    }

    /// <summary>
    /// Die Reihen am Ende eines Managerspiels stehen im Verein (Vereinsfelder 0x2E/0x30) und sind die Reihen beim Anpfiff des
    /// nächsten Spiels; ein Computerverein ohne Managerpaarung wechselt nicht und behält seine Reihen.
    /// </summary>
    [Fact]
    public void ManagerMatch_KeepsEndLines_ForNextMatch()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        MatchEngine.LoadTeamValues(game);
        var (managed, opponent, other) = (game.Clubs[2], game.Clubs[3], game.Clubs[4]);
        var seen = new HashSet<(int, int)>();
        for (var seed = 0; seed < 12; seed++)
        {
            MatchEngine.PlayWithEvents(game, managed, opponent, MatchCompetition.League, knockOut: false, new Random(seed));
            var lines = (managed.Tactics.MatchDefenseLine, managed.Tactics.MatchAttackLine);
            seen.Add(lines);
            Assert.InRange(lines.MatchDefenseLine, 0, 3);
            Assert.InRange(lines.MatchAttackLine, 0, 3);
            var next = MatchEngine.LiveTeamOf(game, managed, other, managed: true, managerMatch: true);
            Assert.Equal(lines, (next.DefenseLine, next.AttackLine));
        }
        // Ohne Übernahme stünde jedes Spiel bei 0/0; über zwölf Spiele endet mindestens eins mit einer anderen Reihe.
        Assert.Contains(seen, l => l != (0, 0));

        var before = (other.Tactics.MatchDefenseLine, other.Tactics.MatchAttackLine);
        MatchEngine.PlayWithEvents(game, other, game.Clubs[5], MatchCompetition.League, knockOut: false, new Random(1));
        Assert.Equal(before, (other.Tactics.MatchDefenseLine, other.Tactics.MatchAttackLine));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5, #872 wirkt auf Belastung und Ergebnisse): zwei Manager, Manager 0 passiv, Manager 1 aktiv
    /// (verlängert seine Verträge), vier Saisonwechsel. Die Reihen bleiben je Verein im Bereich 0 … 3, die Managervereine enden
    /// ihre Spiele nicht immer mit 0/0, und Speichern/Laden ist am Ende bytegleich.
    /// </summary>
    [Fact]
    public void MultiSeason_FourSeasonChanges_PassiveAndActive_LinesCarryOver()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Passiv", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Aktiv", Head = 1, ClubIndex = 5 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 7);
        var active = game.Managers[1];
        var managedClubs = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var seen = new HashSet<(int, int)>();

        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 5000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                    manager.Money = 10_000_000; // keine Entlassung
                TestSeason.KeepSquad(game, active, extendExpired: true);
                game.Continue();
                foreach (var index in managedClubs)
                    seen.Add((game.Clubs[index].Tactics.MatchDefenseLine, game.Clubs[index].Tactics.MatchAttackLine));
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            Assert.All(game.Clubs, c => Assert.InRange(c.Tactics.MatchDefenseLine, 0, 3));
            Assert.All(game.Clubs, c => Assert.InRange(c.Tactics.MatchAttackLine, 0, 3));
            game.StartNextSeason();
        }

        Assert.Equal(GameState.FirstSeason + 4, game.Season);
        Assert.All(game.Managers, m => Assert.False(m.Dismissed));
        Assert.Contains(seen, l => l != (0, 0));
        var loaded = SaveGame.Load(SaveGame.Save(game));
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded.Game!));
        Assert.Equal(game.Clubs.Select(c => (c.Tactics.MatchDefenseLine, c.Tactics.MatchAttackLine)),
            loaded.Game!.Clubs.Select(c => (c.Tactics.MatchDefenseLine, c.Tactics.MatchAttackLine)));
    }

    /// <summary>
    /// Migration 50 → 51 (<c>SaveGame.Migration.PruefungKarte.cs</c>): Ein Stand ohne <c>_matchDefenseLine</c>/<c>_matchAttackLine</c>
    /// lädt mit 0/0 (wie FILE02); das aktuelle Schema ohne die Felder lehnt der strenge Leser ab. Gespeicherte Reihen bleiben über
    /// Speichern und Laden. Rot ohne den Eintrag <c>[50] = MigratePruefungKarte</c>.
    /// </summary>
    [Fact]
    public void Schema50_WithoutMatchLines_LoadsWithZero()
    {
        var game = TestGames.Create(3);
        foreach (var club in game.Clubs)
            club.Tactics.RecordMatchLines(1, 2);
        var saved = SaveGame.Load(SaveGame.Save(game));
        Assert.True(saved.Success, saved.Error);
        Assert.All(saved.Game!.Clubs, c => Assert.Equal((1, 2), (c.Tactics.MatchDefenseLine, c.Tactics.MatchAttackLine)));

        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        var removed = 0;
        foreach (var node in Objects(root))
            if (node.Remove("_matchDefenseLine") & node.Remove("_matchAttackLine"))
                removed++;
        Assert.True(removed >= game.Clubs.Count);
        Assert.False(SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString())).Success);

        root["schema"] = 50;
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.All(result.Game!.Clubs, c => Assert.Equal((0, 0), (c.Tactics.MatchDefenseLine, c.Tactics.MatchAttackLine)));
    }

    private static IEnumerable<System.Text.Json.Nodes.JsonObject> Objects(System.Text.Json.Nodes.JsonNode? node)
    {
        switch (node)
        {
            case System.Text.Json.Nodes.JsonObject o:
                yield return o;
                foreach (var (_, child) in o.ToList())
                    foreach (var x in Objects(child))
                        yield return x;
                break;
            case System.Text.Json.Nodes.JsonArray a:
                foreach (var child in a.ToList())
                    foreach (var x in Objects(child))
                        yield return x;
                break;
        }
    }
}
