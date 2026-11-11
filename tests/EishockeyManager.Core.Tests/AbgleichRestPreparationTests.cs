using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #772 (Spur abgleich-rest, Fortsetzung 2): Spielvorbereitung 0x8365 wie im Original. [ORIGINAL] Datei 0x839D–0x83D6 vergleicht
/// die doppelten Stärken 0x368B1(Verein, 1, 0, 0, neu) beider Vereine; Datei 0x8400–0x8411 |Differenz| &lt; 25 → Grundwert 60,
/// sonst 40; Datei 0x8473–0x847D Vereinsfeld 0x26 = 0x2D = Grundwert (ohne Zuschlag „Unentschieden“); die Angriffsbits 0x2C
/// (4–6) schreibt 0x8365 nicht (Datei 0x8481–0x84BD), sie bleiben aus dem letzten Managerspiel stehen. Beleg FILE03 (Vereinsfelder
/// der Computervereine, <c>docs/analyse/belastung-staerke/clubs.py</c>): (40, 40, 0) × 23, (60, 60, 0) × 9, (0, 0, 0) × 16, die
/// drei Kölner Gegner mit Angriff 5/4/6.
/// </summary>
public class AbgleichRestPreparationTests(ITestOutputHelper output)
{
    /// <summary>
    /// Regressionstest (rot vor dem Fix: Computervereine spielen mit Einsatz 80/55 = Grundwert + „Unentschieden“ und mit
    /// Angriff 4/6 aus der Automatik): FILE02 → FILE03 (drei Spieltage ohne Eingriff). Jeder Computerverein, der im Original
    /// nicht gegen einen Managerverein gespielt hat, steht danach wie im Original auf Einsatz 0 (kein Spiel), 40 oder 60 und
    /// Angriff 0; die Gegner des Managervereins behalten einen Angriff aus dessen Spiel (Original 4–6).
    /// </summary>
    [GameDataTheory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void File02To03_ComputerClubs_EffortAndAttackLikeOriginal(int seed)
    {
        var game = Import("FILE02", seed);
        var target = Import("FILE03", seed);
        Assert.Null(HokCompare.PlayTo(game, target));

        var records = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE03.HOK"))).Block("far:9657");
        int same = 0, compared = 0, managerOpponents = 0;
        for (var slot = 0; slot < 52; slot++)
        {
            var r = records.AsSpan(slot * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength);
            if (r[0x16] != 0)
                continue;
            var name = OriginalImport.ClubName(records, slot);
            var club = game.Clubs.Single(c => c.Name == name);
            var (effort, attack) = (club.Tactics.MatchEffort, club.Tactics.MatchAttack);
            var originalAttack = (r[0x2C] >> 4) & 7;
            if (originalAttack != 0)
            {
                // Gegner des Managervereins: Einsatz und Angriff aus 0x939A am Spielende (auch 0 möglich, z. B. „hoher Rückstand“).
                managerOpponents++;
                Assert.InRange(attack, 0, Tactics.MaxAttack);
                continue;
            }
            Assert.Equal(r[0x26] == 0, effort == 0);
            Assert.Contains(effort, new[] { 0, 40, 60 });
            Assert.Equal(0, attack);
            compared++;
            if (effort == r[0x26])
                same++;
        }
        output.WriteLine($"Saat {seed}: Grundwert wie im Original bei {same} von {compared}, Gegner des Managervereins {managerOpponents}");
        Assert.Equal(3, managerOpponents);
        Assert.Equal(48, compared);
    }

    /// <summary>
    /// Vergleich über die Stärke 0x368B1 mit dem Einsatz des letzten Spiels (Datei 0x9C66), nicht über den Teamwert: Zwei
    /// Computervereine, Grundwert 60 genau dann, wenn die doppelten Stärken weniger als 25 auseinanderliegen; der Angriff kommt
    /// aus dem letzten Spiel und bleibt im Spiel ohne Manager stehen.
    /// </summary>
    [Fact]
    public void Prepare_ComputerClubs_BaseEffortFromStrengthAndStoredAttack()
    {
        var game = TestGames.Create(7);
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var clubs = game.Clubs.Where(c => c.Index < 52 && !managed.Contains(c.Index) && c.Players.Count > 0).Take(12).ToList();
        var seen = new HashSet<int>();
        foreach (var home in clubs)
        {
            foreach (var away in clubs.Where(c => c != home))
            {
                home.Tactics.RecordMatchFields(70, 6);
                away.Tactics.RecordMatchFields(30, 0);
                var h = MatchEngine.LiveTeamOf(game, home, away, managed: false, managerMatch: false);
                var a = MatchEngine.LiveTeamOf(game, away, home, managed: false, managerMatch: false);
                h.Effort = 99; // darf die Stärke nicht beeinflussen: 0x368B1 liest 0x26 des letzten Spiels
                var hs = OriginalMatch.TeamStrength(
                    OriginalMatch.LineValues(h.Defense, 0, false, 0, h.LineEnergy(0, 0), 70, inMatch: true),
                    OriginalMatch.LineValues(h.Attack, 0, true, 0, h.LineEnergy(1, 0), 70, inMatch: true), 1, 0, h.ClubForm);
                var @as = OriginalMatch.TeamStrength(
                    OriginalMatch.LineValues(a.Defense, 0, false, 0, a.LineEnergy(0, 0), 30, inMatch: true),
                    OriginalMatch.LineValues(a.Attack, 0, true, 0, a.LineEnergy(1, 0), 30, inMatch: true), 1, 0, a.ClubForm);

                MatchEngine.PrepareMatch(game, home, away, h, a);

                var expected = Math.Abs(2 * hs - 2 * @as) < Tactics.EvenMatchLimit ? 60 : 40;
                seen.Add(expected);
                Assert.Equal((expected, expected), (h.Effort, a.Effort));
                Assert.Equal((6, 0), (h.AttackLevel, a.AttackLevel));
                Assert.Equal((hs, @as), (h.TacticsStrength, h.TacticsOpponentStrength));
                Assert.Equal((@as, hs), (a.TacticsStrength, a.TacticsOpponentStrength));
            }
        }
        Assert.Equal(new HashSet<int> { 40, 60 }, seen);
    }

    /// <summary>
    /// Ein Spiel ohne Manager hält Einsatz und Angriff fest (Vereinsfelder 0x26/0x2C bleiben stehen): Grundwert 40/60, Angriff
    /// unverändert aus dem letzten Spiel; Speichern und Laden behält beide Werte.
    /// </summary>
    [Fact]
    public void ComputerMatch_KeepsFieldsAcrossSaveAndLoad()
    {
        var game = TestGames.Create(5);
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var (home, away) = game.Clubs.Where(c => c.Index < 52 && !managed.Contains(c.Index) && c.Players.Count > 0)
            .Take(2).ToArray() switch { var p => (p[0], p[1]) };
        home.Tactics.RecordMatchFields(0, 5);
        MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.League, knockOut: false, new Random(3));
        Assert.Contains(home.Tactics.MatchEffort, new[] { 40, 60 });
        Assert.Equal(home.Tactics.MatchEffort, away.Tactics.MatchEffort);
        Assert.Equal((5, 0), (home.Tactics.MatchAttack, away.Tactics.MatchAttack));

        var loaded = SaveGame.Load(SaveGame.Save(game)).Game!;
        Assert.Equal((home.Tactics.MatchEffort, 5), (loaded.Clubs[home.Index].Tactics.MatchEffort, loaded.Clubs[home.Index].Tactics.MatchAttack));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel, passiver und aktiver Manager (eigener Einsatz/Angriff „SEMI“). Nach jeder
    /// Saison stehen die Computervereine der Ligen auf Grundwert 40/60 (bis auf die Gegner im letzten Managerspiel), die Werte der Managervereine bleiben in den Grenzen.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_PreparedFieldsStayInRange(bool active)
    {
        var game = TestGames.Create(21);
        var manager = game.ActiveManager;
        var managedClub = game.Clubs[manager.ClubIndex];
        if (active)
        {
            managedClub.Tactics.EffortMode = TacticMode.Semi;
            managedClub.Tactics.AttackMode = TacticMode.Semi;
            managedClub.Tactics.SetEffort(0, ScoreSituation.Base, 70);
        }
        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, manager);
                if (manager.Money < 2_000_000)
                    manager.Money = 2_000_000; // keine Entlassung im Testlauf
                game.Continue();
            }
            // Nur die Gegner im letzten Managerspiel stehen auf dem Wert aus 0x939A (höchstens einer je Managerverein).
            var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
            var computer = game.Clubs.Where(c => c.League is not null && !managed.Contains(c.Index)).ToList();
            Assert.All(computer, c => Assert.InRange(c.Tactics.MatchEffort, 0, Tactics.MaxEffort));
            Assert.InRange(computer.Count(c => c.Tactics.MatchEffort is not (40 or 60)), 0, managed.Count);
            Assert.InRange(managedClub.Tactics.MatchEffort, 0, Tactics.MaxEffort);
            Assert.InRange(managedClub.Tactics.MatchAttack, 0, Tactics.MaxAttack);
            game.StartNextSeason();
        }
    }

    private static GameState Import(string file, int seed) =>
        HokCompare.Import(OriginalImportTests.OriginalSave(file + ".HOK"), ClubDatabase.Load(TestGameData.Load()), seed);
}

/// <summary>Migration 47 → 48 (<c>SaveGame.Migration.AbgleichRest.cs</c>, #772).</summary>
public class AbgleichRestMigrationTests
{
    /// <summary>
    /// Ein Stand ohne <c>_matchEffort</c>/<c>_matchAttack</c> lädt mit 0 (wie FILE02); das aktuelle Schema ohne die Felder lehnt
    /// der strenge Leser ab. Rot ohne den Eintrag <c>[47] = MigrateAbgleichRest</c>.
    /// </summary>
    [Fact]
    public void Schema47_WithoutMatchFields_LoadsWithZero()
    {
        var game = TestGames.Create(3);
        foreach (var club in game.Clubs)
            club.Tactics.RecordMatchFields(60, 4);
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        var removed = 0;
        foreach (var node in Objects(root))
            if (node.Remove("_matchEffort") & node.Remove("_matchAttack"))
                removed++;
        Assert.True(removed >= game.Clubs.Count);

        var current = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.False(current.Success);

        root["schema"] = 47;
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.All(result.Game!.Clubs, c => Assert.Equal((0, 0), (c.Tactics.MatchEffort, c.Tactics.MatchAttack)));
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
                foreach (var child in a)
                    foreach (var x in Objects(child))
                        yield return x;
                break;
        }
    }
}
