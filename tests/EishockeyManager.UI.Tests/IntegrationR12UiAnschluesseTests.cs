using System.Text;
using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 12: Anschlüsse der Spur ui-meldungen in fremden Dateien (#483, #492, #511, #513, #515). Jeder Test war vor
/// dem Anschluss rot.
/// </summary>
public class IntegrationR12UiAnschluesseTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-r12-ui-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static GameState Synthetic()
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    // ---- #511 (UM-2): Rekorde der Vereine aus Ligaspielen -----------------------------------------------------------------

    /// <summary>
    /// [ORIGINAL] 0x50283 bei Code 7: Jedes Ligaspiel trägt die Rekorde beider Vereine ein (<see cref="TeamRecords.Enter"/>). Vorher
    /// blieben alle Vereinsrekorde leer, das Vereinsfenster zeigte „Noch keine Eintragung.“.
    /// </summary>
    [Fact]
    public void LeagueMatches_EnterClubRecords()
    {
        var game = Synthetic();
        while (game.PlayedMatchDays < 3) // Zeitleiste wie TestSeason.PlayMatchDay (Core-Tests)
            game.Continue();

        var words = new int[TeamRecords.Teams * TeamRecords.Kinds];
        var opponents = new int[TeamRecords.Teams * TeamRecords.Kinds];
        foreach (var r in game.Results)
            TeamRecords.Enter(words, opponents, r.Home, r.Away, r.HomeGoals, r.AwayGoals);

        Assert.Contains(words, w => w != 0);
        for (var club = 0; club < NationalTeams.First; club++)
            foreach (var kind in Enum.GetValues<TeamRecordKind>())
            {
                var i = TeamRecords.Index(club, kind);
                Assert.Equal((words[i], opponents[i]), game.TeamRecord(club, kind));
            }
    }

    // ---- #513 (UM-4): Kurzmeldungen der Aufrufer ---------------------------------------------------------------------------

    /// <summary>[ORIGINAL] Datei 0x46DEF: Linksklick auf einen eigenen Spieler mit angekündigtem Ruhestand → nur die Kurzmeldung.</summary>
    [GameDataFact]
    public void Transfer_ClickOnRetiringPlayer_ShowsShortMessageOnly()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var rows = TeamUi.Rows(club);
        var row = Enumerable.Range(0, rows.Count).First(i => !rows[i].OnLoan);
        rows[row].Notice = ContractNotice.Retiring;
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        var before = StateFingerprint.Of(game);

        d.Click(TransferScreen.SquadWindow.X + 30, TransferScreen.FirstRowY + row * TransferScreen.RowPitch + 1);

        var message = Assert.IsType<ShortMessage>(screen.Message.Short);
        Assert.Equal(("Dieser Spieler hört", "am Saisonende auf."), (message.Line1, message.Line2));
        Assert.Null(screen.Dialog);
        var after = StateFingerprint.Of(game);
        Assert.True(before == after, StateFingerprint.FirstDifference(before, after));
    }

    /// <summary>Kennung stimmt, Version nicht: unterscheidbare Ergebnisart (Gegenstück zu 0x5C3E3 → 0x5C559).</summary>
    [Theory]
    [InlineData("{\"kennung\":\"ID\",\"schema\":NEWER}", true)]
    [InlineData("{\"kennung\":\"ID\",\"schema\":0}", true)]
    [InlineData("{\"kennung\":\"ID\"}", true)]
    [InlineData("{\"kennung\":\"FREMD\",\"schema\":1}", false)]
    [InlineData("kein json", false)]
    public void SaveGameLoad_FlagsWrongVersion(string template, bool wrongVersion)
    {
        var text = template.Replace("ID", SaveGame.Identifier).Replace("NEWER", (SaveGame.SchemaVersion + 1).ToString());
        var result = SaveGame.Load(Encoding.UTF8.GetBytes(text));
        Assert.False(result.Success);
        Assert.Equal(wrongVersion, result.WrongVersion);
    }

    /// <summary>[ORIGINAL] Datei 0x5C559: Laden einer fremden Version → Kurzmeldung „Spielstand aus / falscher Version !“, zurück zur Auswahl.</summary>
    [GameDataFact]
    public void Load_WrongVersion_ShowsShortMessageAndStays()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = false;
        d.StartNewGame();
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "ALT.ems"),
            $"{{\"kennung\":\"{SaveGame.Identifier}\",\"schema\":{SaveGame.SchemaVersion + 1}}}");
        var game = d.Host.Game;

        d.OpenTile("Laden");
        var index = SaveFiles.List(_directory).ToList().FindIndex(e => e.FileName == "ALT.ems");
        var rect = SaveLoadScreen.EntryRect(index / SaveLoadScreen.RowsPerColumn, index % SaveLoadScreen.RowsPerColumn);
        d.Click(rect.X + 3, rect.Y + 3);
        d.Click(rect.X + 3, rect.Y + 3);

        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        Assert.Equal(["Spielstand aus", "falscher Version !"], screen.MessageLines);
        Assert.Same(game, d.Host.Game);
        d.Frames(ShortMessage.FramesFor(ShortMessage.Duration("Spielstand aus")));
        Assert.Empty(screen.MessageLines);
        Assert.IsType<SaveLoadScreen>(d.Host.Current);
    }

    // ---- #515 (E4): „gelesen“ beim Öffnen des Posteingangs -----------------------------------------------------------------

    /// <summary>Öffnen des Posteingangs markiert die erste Seite, Blättern die nächste; Zeichnen ändert nichts.</summary>
    [Fact]
    public void Inbox_MarksReadOnOpenAndPaging_NotOnDraw()
    {
        var game = Synthetic();
        var host = new ScreenHost(null!) { Game = game };
        var me = game.ActiveManager;
        var messages = Enumerable.Range(0, InboxScreen.PerPage + 1).Select(i => game.Notify(me, $"Nachricht {i}")).ToList();
        Assert.All(messages, m => Assert.False(m.Read));

        var inbox = new InboxScreen(host, new Blank(host));
        host.Show(inbox);
        Assert.False(messages[0].Read); // älteste steht auf Seite 2
        Assert.All(messages.Skip(1), m => Assert.True(m.Read));

        inbox.Update(new InputState { Keys = { Key.PageDown } });
        Assert.True(messages[0].Read);
    }

    private sealed class Blank(ScreenHost host) : Screen(host)
    {
        public override void Update(InputState input) { }
        public override void Draw(Surface surface) { }
    }

    // ---- #492 (Teil 5): Zwischenbild im Desktop-Spiel ----------------------------------------------------------------------

    /// <summary>[ORIGINAL] DGROUP 0x22A: Seitenwechsel nach dem Spielstart, ohne Seitenwechsel nach dem Laden (Datei 0x5687A).</summary>
    [Fact]
    public void DesktopTransition_PageFlipFollowsStartOrLoad()
    {
        var host = new ScreenHost(null!);
        SaveContext.For(host).Directory = _directory;
        var t = ScreenTransition.ForDesktop();
        var ms = TimeSpan.FromMilliseconds(1);

        host.Show(new Blank(host));
        t.Observe(host, ms * 0);
        host.Game = Synthetic(); // neues Spiel (nicht aus Laden)
        host.Show(new Blank(host));
        t.Observe(host, ms * 1);
        Assert.True(t.PageFlip);
        Assert.True(t.IsActive(ms * 1));

        host.Show(new SaveLoadScreen(host, host.Current!, SaveLoadMode.Load));
        t.Observe(host, ms * 500);
        host.Game = Synthetic(); // geladen
        host.Show(new Blank(host));
        t.Observe(host, ms * 1000);
        Assert.False(t.PageFlip);
    }
}
