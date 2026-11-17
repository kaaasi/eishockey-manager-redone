using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zusehen-rest (Runde 22): Tormeldung der Paarungen ohne Reihe (A832, Datei 0xAB47–0xAD71) und Import der Markierung
/// Vereinsfeld 0x34 Bits 6–7 und DGROUP 0x22C (A834).
/// </summary>
public sealed class ZusehenRestTests
{
    private static GameState Import(string file, int seed = 1993) => GameState.ImportOriginal(
        OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file))), ClubDatabase.Load(TestGameData.Load()), seed);

    private static GameState Game(int seed, params int[] managerClubs)
    {
        var setup = new GameSetup();
        foreach (var club in managerClubs)
            setup.Managers.Add(new ManagerSetup { Name = $"M{club}", Head = 0, ClubIndex = club });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>
    /// A834: [ORIGINAL] Vereinsfeld 0x34 Bits 6–7 je Satzplatz. FILE11 markiert neben dem Managerverein das Land in Satz 54
    /// (Schalter „WM-Spiele“ an). Die Stände selbst ergibt auch das Nachspielen (Spielstart, Schalter „WM-Spiele“); Fehler
    /// vorher zeigt <see cref="Import_ReadsChangedWatchBits"/>.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE11.HOK", true)]
    [InlineData("FILE18.HOK", true)]
    [InlineData("FILE54.HOK", true)]
    [InlineData("FILE72.HOK", false)]
    public void Import_ReadsWatchBits(string file, bool nationMarked)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var clubOf = OriginalImport.ClubIndices(save, ClubDatabase.Load(TestGameData.Load()));
        var records = save.Block("far:9657");
        var game = Import(file);
        for (var slot = 0; slot < clubOf.Length; slot++)
            Assert.Equal(records[slot * OriginalSaveGame.ClubRecordLength + 0x34] >> 6, game.Clubs[clubOf[slot]].WatchMode);
        Assert.Equal(nationMarked, game.Clubs.Any(c => c.Index >= NationalTeams.First && c.WatchMode == GameState.WatchLive));
    }

    /// <summary>
    /// A834: Der Import nimmt die Bits des Stands, nicht die Markierung des Spielstarts. Am FILE72 (Manager in Satz 3 und 8)
    /// wird Satz 3 auf Modus 0 und Satz 20 auf Modus 1 gesetzt (Logobits bleiben). Vorher rot: Satz 3 blieb Modus 2, Satz 20
    /// Modus 0 (<c>MarkStartWatches</c>, Feldkarte U).
    /// </summary>
    [GameDataFact]
    public void Import_ReadsChangedWatchBits()
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE72.HOK")));
        var database = ClubDatabase.Load(TestGameData.Load());
        var clubOf = OriginalImport.ClubIndices(save, database);
        var records = save.Block("far:9657");
        records[3 * OriginalSaveGame.ClubRecordLength + 0x34] &= 0x3F;
        records[20 * OriginalSaveGame.ClubRecordLength + 0x34] = (byte)((records[20 * OriginalSaveGame.ClubRecordLength + 0x34] & 0x3F) | 0x40);
        var game = GameState.ImportOriginal(save, database, 1993);
        Assert.Equal(GameState.WatchOff, game.Clubs[clubOf[3]].WatchMode);
        Assert.Equal(GameState.WatchGoals, game.Clubs[clubOf[20]].WatchMode);
        Assert.Equal(GameState.WatchLive, game.Clubs[clubOf[8]].WatchMode);
    }

    /// <summary>
    /// A834: [ORIGINAL] DGROUP 0x22C (Block 0x21D + 15) ist die offene Sicherung während des Amts des Bundestrainers (FILE18: 1,
    /// FILE49: 0); ohne Amt keine (FILE72). Vorher rot: Der Import ließ die Sicherung des Nachspielens stehen bzw. null.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE18.HOK", true)]
    [InlineData("FILE49.HOK", false)]
    [InlineData("FILE72.HOK", null)]
    public void Import_ReadsWorldCupOptionBackup(string file, bool? expected)
    {
        var game = Import(file);
        Assert.Equal(expected, game.WorldCupOptionBackup);
        Assert.Equal(expected is not null, game.NationalCoach is not null);
    }

    /// <summary>
    /// A832: [ORIGINAL] Datei 0xABB4–0xABF5: Eine Paarung ohne Reihe meldet ihre Tore, wenn an diesem Spieltag die Live-Anzeige
    /// läuft (eine Reihe besteht) und einer der Vereine markiert ist (Modus 1 genügt). Vorher rot: <see cref="LiveScoreboard"/>
    /// hatte keine Meldungen. Gegenprobe: ohne Markierung keine Meldung dieser Paarung.
    /// </summary>
    [Fact]
    public void GoalNotice_RecordedForMarkedPairWithoutRow()
    {
        var withMark = RunDay(mark: true);
        var without = RunDay(mark: false);
        Assert.NotEmpty(withMark.Notices);
        Assert.All(withMark.Notices, n => Assert.True(withMark.Rows.All(r => r.HomeClub != n.HomeClub && r.AwayClub != n.AwayClub)));
        Assert.Empty(without.Notices);
        // [ORIGINAL] Datei 0xABF8–0xAC10: Spielstand nach dem Tor, je Paarung fortlaufend.
        foreach (var pair in withMark.Notices.GroupBy(n => (n.HomeClub, n.AwayClub)))
        {
            var total = 0;
            foreach (var n in pair.OrderBy(n => n.Minute))
            {
                Assert.Equal(++total, n.HomeGoals + n.AwayGoals);
                Assert.InRange(n.Minute, 1, 70);
            }
        }
    }

    /// <summary>
    /// Spielt Ligatage, bis eine Reihe besteht und eine spätere Paarung mit markiertem Verein ohne Reihe Tore geschossen hat.
    /// Markiert (Modus 1) werden alle Vereine der Liga außer dem Managerverein und dessen Gegner.
    /// </summary>
    private static LiveScoreboard RunDay(bool mark)
    {
        for (var seed = 1; seed < 40; seed++)
        {
            var game = Game(seed, 0);
            var managerClub = game.Managers[0].ClubIndex;
            if (mark)
                foreach (var c in game.Clubs.Where(c => c.Index != managerClub && c.Index < NationalTeams.First
                             && c.League == game.Clubs[managerClub].League))
                    game.SetWatchMode(c, evict: false, GameState.WatchGoals);
            for (var day = 0; day < 6; day++)
            {
                TestSeason.PlayMatchDay(game);
                if (game.LiveScoreboard is { Active: true } board && (!mark || board.Notices.Count > 0))
                    return board;
            }
        }
        throw new InvalidOperationException("kein Spieltag mit Reihe und Meldung");
    }
}
