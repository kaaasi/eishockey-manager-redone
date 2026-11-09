using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur kader-start, Teil 4 (Runde 14), A362: Liste hinter den Saisonende-Verhandlungen als Abbild (em.exe Datei 0x27C9F, 0x27F31),
/// „kein V.“ (Datei 0x423A2) und Lage von Fenster und Spaltenköpfen gegen die DOSBox-Bilder <c>file46_3</c> und <c>file40_11</c>.
/// </summary>
public class KaderStartTeil4ScreenTests
{
    /// <summary>
    /// Drei auslaufende Verträge A, B, C: Die Liste zeigt bei allen „kein V.“ und das alte Gehalt (KJELLBERG: 18.800 DM in der Liste,
    /// 19.152 DM im Fenster). A bleibt („Mach' ich!“) – die Liste wird nicht neu gezeichnet; B geht („Abbruch“) – danach neu: A mit
    /// neuem Vertrag, B fehlt, C noch „kein V.“ mit altem Gehalt.
    /// </summary>
    [GameDataFact]
    public void SeasonEnd_ListIsPictureBeforeNegotiation_RedrawnAfterDeparture()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var guard = 0; game.Phase != SeasonPhase.SeasonOver; guard++)
        {
            Assert.True(guard < 40000, "Saisonende nicht erreicht");
            game.ActiveManager.Money = Math.Max(game.ActiveManager.Money, 5_000_000);
            d.Advance();
        }
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        foreach (var p in club.Players)
        {
            p.ContractYears = 3;
            p.Notice = ContractNotice.None;
        }
        var expiring = club.Players.Where(p => p.Role == PlayerRole.Forward && !p.OnLoan).TakeLast(3).ToArray();
        var old = new[] { 18_800, 17_100, 12_700 };
        for (var i = 0; i < 3; i++)
            (expiring[i].ContractYears, expiring[i].MonthlySalary) = (1, old[i]);
        var (a, b, c) = (expiring[0], expiring[1], expiring[2]);

        ContractsScreen? contracts = null;
        for (var guard = 0; contracts is null; guard++)
        {
            Assert.True(guard < 200, "Verhandlung am Saisonende nicht erreicht");
            d.Press(Key.Enter);
            contracts = d.Host.Current as ContractsScreen;
        }
        Assert.Equal(a, contracts.Negotiating);
        Assert.NotEqual(old[0], a.MonthlySalary); // das neue Gehalt steht schon im Kern (Fenster)
        AssertRow(contracts, a, "kein V.", old[0]);
        AssertRow(contracts, b, "kein V.", old[1]);
        AssertRow(contracts, c, "kein V.", old[2]);
        var save = SaveGame.Save(game);
        d.Frame(_ => { });
        Assert.Equal(save, SaveGame.Save(game)); // Zeichnen ändert nichts
        AssertDrawn(d, contracts, c, "kein V.", $"{old[2]} DM");

        // A bleibt: „Mach' ich!“ ohne Eingaben nimmt das Angebot „1 Jahr“ an (Datei 0x4AB66); kein Neuzeichnen.
        var accept = contracts.Dialog!.AcceptButton;
        d.Click(accept.X + 5, accept.Y + 3);
        Assert.Equal([a.Name, "bleibt Ihnen auch", "die nächste Saison erhalten"], contracts.Message.Lines);
        d.Click(10, 10);
        Assert.Equal(b, contracts.Negotiating);
        AssertRow(contracts, a, "kein V.", old[0]);

        // B geht: nach den Meldungen neu gezeichnet (Datei 0x27F31).
        var cancel = contracts.Dialog!.CancelButton;
        d.Click(cancel.X + 5, cancel.Y + 3);
        d.Click(10, 10);
        d.Click(10, 10);
        Assert.Equal(c, contracts.Negotiating);
        Assert.DoesNotContain(contracts.SeasonEndRows!, r => r.Player == b);
        AssertRow(contracts, a, "1 Jahr", a.MonthlySalary);
        AssertRow(contracts, c, "kein V.", old[2]);
        Assert.Equal(contracts.SeasonEndRows!.Select(r => r.Player), TeamUi.Rows(club));
    }

    /// <summary>
    /// [ORIGINAL] Lage aus den DOSBox-Bildern: Verhandlungsfenster in <c>file46_3</c> mit Rahmen (2 | 29)–(166 | 95) (Zeile unten →
    /// oben); Spaltenköpfe y 19–23 ab x 7 und Linien y 25/209 von x 7 bis 251 in <c>file40_11</c> und <c>file46_3</c>. Die Spalten
    /// der Kopfzeile (Bildpunkte mit Schrift) stimmen mit den Bildern überein.
    /// </summary>
    [GameDataFact]
    public void ContractsScreen_HeaderAndDialogFrame_MatchDosBoxPictures()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Verträge");
        Assert.IsType<ContractsScreen>(d.Host.Current);
        d.Frame(_ => { });
        var px = d.Surface.Pixels;
        byte At(int x, int y) => px[y * Surface.Width + x];

        // Spalten mit Schrift in y 19–23 (gemessen in file40_11 und file46_3, gleich in beiden).
        (int, int)[] expected =
        [
            (7, 10), (12, 15), (17, 19), (27, 30), (32, 35), (37, 41), (43, 45), (83, 86), (88, 91), (97, 100), (102, 104),
            (110, 112), (114, 117), (123, 126), (128, 130), (132, 135), (137, 139), (141, 144), (146, 149), (163, 165), (171, 175),
            (177, 177), (179, 182), (184, 187), (189, 192), (194, 196), (198, 201), (217, 221), (223, 225), (227, 230), (232, 234),
        ];
        var header = TeamUi.HeaderColor;
        var columns = Enumerable.Range(3, 259).Where(x => Enumerable.Range(18, 7).Any(y => At(x, y) == header)).ToArray();
        Assert.Equal(expected, Runs(columns));
        Assert.DoesNotContain(Enumerable.Range(3, 259), x => At(x, 24) == header || At(x, 18) == header);
        foreach (var y in new[] { 25, 209 })
        {
            Assert.All(Enumerable.Range(7, 245), x => Assert.Equal(TeamUi.LineColor, At(x, y)));
            Assert.NotEqual(TeamUi.LineColor, At(6, y));
            Assert.NotEqual(TeamUi.LineColor, At(252, y));
        }
        Assert.DoesNotContain(Enumerable.Range(7, 245), x => At(x, 210) == TeamUi.LineColor);

        var frame = ContractsScreen.DialogRect(24); // KJELLBERG, Zeile 25 in file46_3
        Assert.Equal((2, 29, 166, 95), (frame.X, frame.Y, frame.X + frame.W - 1, frame.Y + frame.H - 1));
        Assert.Equal(109, ContractsScreen.DialogRect(0).Y);
    }

    private static void AssertRow(ContractsScreen screen, Player p, string contract, int salary)
    {
        var row = Assert.Single(screen.SeasonEndRows!, r => r.Player == p);
        Assert.Equal((contract, salary), (row.Contract, row.Salary));
    }

    /// <summary>Die Zeile des Spielers trägt genau die Bildpunkte von V.DAUER und WERT, wie sie die Liste zeichnet.</summary>
    private static void AssertDrawn(ScreenDriver d, ContractsScreen screen, Player p, string contract, string salary)
    {
        var row = screen.SeasonEndRows!.Select(r => r.Player).ToList().IndexOf(p);
        var y = ContractsScreen.FirstRowY + row * ContractsScreen.RowPitch;
        var expected = new Surface();
        var c = TeamUi.RoleColor(p.Role);
        expected.Text(d.Host.Assets.Mini, 177, y, contract, c);
        expected.TextRight(d.Host.Assets.Mini, 259, y, salary, c);
        for (var yy = y; yy < y + 5; yy++)
            for (var x = 170; x < 260; x++)
                Assert.Equal(expected.Pixels[yy * Surface.Width + x] == c, d.Surface.Pixels[yy * Surface.Width + x] == c);
    }

    private static List<(int, int)> Runs(IReadOnlyList<int> xs)
    {
        var runs = new List<(int, int)>();
        for (var i = 0; i < xs.Count; i++)
        {
            var start = xs[i];
            while (i + 1 < xs.Count && xs[i + 1] == xs[i] + 1)
                i++;
            runs.Add((start, xs[i]));
        }
        return runs;
    }
}
