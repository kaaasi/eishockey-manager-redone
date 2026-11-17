using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kaderplatz-werte (Runde 22), Teil 2: eingespielte Reihe am Kaderplatz +0x02 (A093). Fundstellen: Leser Datei 0x09A65–0x09A8A
/// (<see cref="MatchEngine.ManagedLineValues"/>), Wurf Datei 0x548A8–0x548E7 (<see cref="SquadSlotRules.RollPlayedLines"/>), Kauf vom
/// Manager Datei 0x478C4, Stand <c>a093</c> (FILE71–FILE74).
/// </summary>
public class KaderplatzWerteTeil2Tests
{
    private static Player P(PlayerRole role, int technique, int playedLine, int number) =>
        new() { Name = $"P{number}", Role = role, Number = number, Condition = 60, Technique = technique, Form = 50, PlayedLine = playedLine };

    private static LivePlayer L(Player p, int position) => new() { Player = p, Position = position, Dressed = true, Energy = 100 };

    /// <summary>Sturm der Reihe 0 (Position 0x81) mit drei Stürmern, Technik 60/60/12, eingespielte Reihen 1, 0 und 2.</summary>
    private static LiveSquad Attack(int difficulty)
    {
        var s = new LiveSquad { Difficulty = difficulty };
        s.Players.Add(L(P(PlayerRole.Forward, 60, 1, 1), 0x81)); // eigene Reihe: kein Abzug
        s.Players.Add(L(P(PlayerRole.Forward, 60, 0, 2), 0x81)); // +0x02 = 0: (0 − 1) ≠ 0 → − 10 (#899 P3-1)
        s.Players.Add(L(P(PlayerRole.Forward, 12, 2, 3), 0x81)); // fremde Reihe, aber Technik 12 (nicht &gt; 12): kein Abzug
        return s;
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x09A65–0x09A8A: Im Spiel ([bp+0xC] ≠ 0) bei DGROUP 0xED8 &lt; 2 (Level 4/5) verliert ein Spieler, dessen
    /// eingespielte Reihe (+0x02) − 1 nicht die Reihe ist, 10 Technik, wenn sie nach dem Energiefaktor über 12 liegt – auch bei +0x02 = 0.
    /// Vorher kein Abzug (rot: 60 + 60 + 12 = 132 / 3 = 44 statt 122 / 3 = 40).
    /// </summary>
    [Fact]
    public void ManagedLineValues_Level5_OutsidePlayedLine_TechniqueMinus10()
    {
        Assert.Equal((60 + 50 + 12) / 3, MatchEngine.ManagedLineValues(Attack(0), 0, 1, 0, 100, 1, inMatch: true).Technique);
        Assert.Equal((60 + 50 + 12) / 3, MatchEngine.ManagedLineValues(Attack(1), 0, 1, 0, 100, 1, inMatch: true).Technique);
        // Level 3 (0xED8 = 2), ohne Spiel und beim Bundestrainer ([APPROX], Satz 5) kein Abzug.
        Assert.Equal((60 + 60 + 12) / 3, MatchEngine.ManagedLineValues(Attack(2), 0, 1, 0, 100, 1, inMatch: true).Technique);
        Assert.Equal((60 + 60 + 12) / 3, MatchEngine.ManagedLineValues(Attack(0), 0, 1, 0, 100, 1, inMatch: false).Technique);
        var national = new LiveSquad { Difficulty = 0, NationalCoach = true };
        national.Players.AddRange(Attack(0).Players);
        Assert.Equal((60 + 60 + 12) / 3, MatchEngine.ManagedLineValues(national, 0, 1, 0, 100, 1, inMatch: true).Technique);
    }

    /// <summary>
    /// [ORIGINAL] Reihenfolge 0x09A54–0x09AC2: erst Energiefaktor, dann − 10 (Grenze 12), dann − 15 auf fremder Position (Grenze 15).
    /// Verteidiger auf einem Sturmplatz mit Technik 26: 26 − 10 = 16 &gt; 15 → 1.
    /// </summary>
    [Fact]
    public void ManagedLineValues_Minus10BeforeWrongPositionMinus15()
    {
        var s = new LiveSquad { Difficulty = 0 };
        s.Players.Add(L(P(PlayerRole.Defender, 26, 0, 1), 0x82));
        Assert.Equal(1 / 3, MatchEngine.ManagedLineValues(s, 1, 1, 0, 100, 1, inMatch: true).Technique);
        s.Players[0].Player.Technique = 90;
        Assert.Equal((90 - 10 - 15) / 3, MatchEngine.ManagedLineValues(s, 1, 1, 0, 100, 1, inMatch: true).Technique);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x548A8–0x548E7: je Kaderplatz in Kaderfolge random(0, 4); bei 0 wird +0x02 = +0x15 &amp; 0x7F (Torhüter 1,
    /// Stürmer ohne Bit 7, nicht Aufgestellte 0). Jeder Platz würfelt, auch der nicht eingesetzte. Vorher kein Feld und kein Zug.
    /// </summary>
    [Fact]
    public void RollPlayedLines_EachSlotDrawsZeroToFour_SetsLineupLine()
    {
        var game = TestGames.Create(5);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        foreach (var p in club.Players)
            p.PlayedLine = 9;
        var twin = GameRandom.FromState(game.Random.Capture());
        var expected = club.Players.Select(p =>
        {
            if (TransferRules.Random16(twin, 0, 4) != 0)
                return 9;
            return club.Lineup.FindSlot(p) is { } s ? LiveSquad.PositionOf(s) & 0x7F : 0;
        }).ToList();

        SquadSlotRules.RollPlayedLines(club, game.Random);

        Assert.Equal(expected, club.Players.Select(p => p.PlayedLine).ToList());
        Assert.Equal(twin.Capture(), game.Random.Capture());
        Assert.Contains(expected, v => v != 9);
        Assert.Contains(club.Players, p => p.PlayedLine == 9);
    }

    /// <summary>
    /// [ORIGINAL] Die Spielabrechnung 0x544C0 würfelt vor den Minuten (Datei 0x548A8, nach den Zuschauern 0x5464A derselben
    /// Managerrunde): nach einem Spieltag hat ein Teil der Kaderplätze die Reihe der Aufstellung vor dem Spiel (Stand a093: rund ein
    /// Fünftel), die übrigen behalten ihren Wert.
    /// </summary>
    [Fact]
    public void MatchDay_RollsPlayedLinesOfManagerSquad()
    {
        var game = TestGames.Create(6);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        club.AutoLineupEnabled = false;
        foreach (var p in club.Players)
            p.PlayedLine = 9;
        var before = new Dictionary<Player, int>(ReferenceEqualityComparer.Instance);
        foreach (var p in club.Players)
            before[p] = club.Lineup.FindSlot(p) is { } s ? LiveSquad.PositionOf(s) & 0x7F : 0;

        TestSeason.PlayMatchDay(game);

        var rolled = club.Players.Where(p => p.PlayedLine != 9).ToList();
        Assert.NotEmpty(rolled);
        Assert.True(rolled.Count < club.Players.Count);
        Assert.All(rolled, p => Assert.Equal(before[p], p.PlayedLine));
    }

    /// <summary>
    /// [ORIGINAL] Neuer Kaderplatz: +0x02 = 0 (memset 0x42E3A, Kauf vom Computermarkt); der Kauf von einem Manager übernimmt +0x02
    /// (Datei 0x4787D–0x478CB, 0x478C4).
    /// </summary>
    [Fact]
    public void ApplyNewSlot_ResetsPlayedLine_ExceptFromManager()
    {
        var start = new SlotStart(100, 50, 1);
        var p = new Player { Name = "A", Role = PlayerRole.Forward, PlayedLine = 3 };
        p.ApplyNewSlot(start, fromManager: false);
        Assert.Equal(0, p.PlayedLine);
        p.PlayedLine = 2;
        p.ApplyNewSlot(start, fromManager: true);
        Assert.Equal(2, p.PlayedLine);
    }

    /// <summary>
    /// Stand a093: FILE71/FILE72 alle +0x02 = 0, FILE73 fünf Plätze (Berlin 1, 4, 6, 19; München 14), FILE74 drei (München 2, 14, 18),
    /// je 1 … 3. Import liest +0x02 in <see cref="Player.PlayedLine"/>, der Export schreibt ihn für jeden Managerkader unverändert zurück.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE71.HOK", 0)]
    [InlineData("FILE72.HOK", 0)]
    [InlineData("FILE73.HOK", 5)]
    [InlineData("FILE74.HOK", 3)]
    public void OriginalStand_A093_ImportAndExportPlayedLine(string file, int nonZero)
    {
        var raw = OriginalExportTests.RawDatabase();
        var original = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var game = GameState.ImportOriginal(original, ClubDatabase.Parse(raw), 4711);
        var exported = OriginalSaveGame.Read(OriginalExport.Export(game, raw));
        var a = original.Block("5DC3");
        var b = exported.Block("5DC3");
        var count = 0;
        for (var m = 0; m < game.Managers.Count; m++)
        {
            var players = game.Clubs[game.Managers[m].ClubIndex].Players.ToList();
            for (var i = 0; i < OriginalSaveGame.SquadSlotsPerManager; i++)
            {
                var at = m * 0x618 + i * OriginalSaveGame.SquadSlotLength;
                if (a[at + 0x0F] == 0)
                    continue;
                Assert.Equal(a[at + 0x02], players[i].PlayedLine);
                Assert.Equal(a[at + 0x02], b[at + 0x02]);
                if (a[at + 0x02] != 0)
                {
                    count++;
                    Assert.InRange((int)a[at + 0x02], 1, 3);
                }
            }
        }
        Assert.Equal(nonZero, count);
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiver und aktiver Manager, Level 5): Die eingespielte Reihe bleibt über den Saisonwechsel
    /// stehen ([ORIGINAL] Datei 0x27A0C–0x27AA8 setzt +0x02 nicht zurück, A088), neu verpflichtete Vertragslose beginnen mit 0
    /// (neuer Kaderplatz, memset 0x42E3A), jeder Wert ist eine Reihe 0 … 4, und der Spielstand trägt sie (Schema 55).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_PlayedLineKeptOverSeasonChange(bool active)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", Head = 0, ClubIndex = 2, Level = 5 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed: 7);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        Dictionary<Player, int>? before = null;
        var changes = 0;
        var nonZero = 0;
        RngTwinProbeTests.Run(game, active, afterStep: g =>
        {
            Assert.All(club.Players, p => Assert.InRange(p.PlayedLine, 0, 4));
            nonZero = Math.Max(nonZero, club.Players.Count(p => p.PlayedLine != 0));
            if (before is null)
                return;
            foreach (var p in club.Players)
            {
                if (before.TryGetValue(p, out var line))
                    Assert.Equal(line, p.PlayedLine);
                else
                    Assert.Equal(0, p.PlayedLine); // neu verpflichtet (nur unter 20 Spielern, RngTwinProbeTests.SignFreeAgents)
            }
            before = null;
        }, beforeChange: _ =>
        {
            changes++;
            before = new Dictionary<Player, int>(ReferenceEqualityComparer.Instance);
            foreach (var p in club.Players)
                before[p] = p.PlayedLine;
        });
        Assert.Equal(4, changes);
        Assert.True(nonZero > 3, $"eingespielte Reihen: höchstens {nonZero}");
        var loaded = SaveGameTests.AssertRoundTrip(game);
        Assert.Equal(club.Players.Select(p => p.PlayedLine),
            loaded.Clubs[club.Index].Players.Select(p => p.PlayedLine));
    }
}
