using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 14, Spur kader-start: Oberflächen-Befunde aus den Reviews <c>2026-11-09-integration-kader-start-r14.md</c>
/// (R14KS-05, R14KS-08) und <c>2026-11-09-integration-ui-rest-r14-nachpruefung.md</c> (R14N-01 bis R14N-03).
/// </summary>
public class KaderStartRunde14FixUiTests
{
    private static byte At(Surface s, int x, int y) => s.Pixels[y * Surface.Width + x];

    /// <summary>
    /// R14N-01: Die drei äußeren Schattenecken in Füllfarbe Index 4 auch beim Vertragsfenster (168 | 31), (4 | 97), (168 | 97)
    /// (`file46_3`, `file40_12`, `file35_3`).
    /// </summary>
    [Fact]
    public void DialogFrame_ContractWindow_OuterShadowCornersInFillColor()
    {
        var s = new Surface();
        s.Clear(200);
        TeamUi.DialogFrame(s, new Rect(2, 29, 165, 67));
        Assert.Equal(TeamUi.DialogFill, At(s, 168, 31));
        Assert.Equal(TeamUi.DialogFill, At(s, 4, 97));
        Assert.Equal(TeamUi.DialogFill, At(s, 168, 97));
        Assert.Equal(TeamUi.DialogShadow, At(s, 168, 32));
        Assert.Equal(TeamUi.DialogShadow, At(s, 5, 97));
        Assert.Equal(TeamUi.DialogShadow, At(s, 167, 97));
        Assert.Equal(TeamUi.DialogShadow, At(s, 168, 96));
        Assert.Equal(200, At(s, 3, 97));
        Assert.Equal(200, At(s, 168, 30));
    }

    /// <summary>
    /// R14KS-05 (M45): Trennlinie des Verhandlungsfensters x 6–163, Doppelpunkte x 76 (`file46_3`: Linie y 43 in 164 von 165 Pixeln
    /// gleich).
    /// </summary>
    [GameDataFact]
    public void ContractDialog_SeparatorSixTo163_ColonsAt76()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var p = game.Clubs[game.ActiveManager.ClubIndex].Players[0];
        var rect = ContractsScreen.DialogRect(24);
        var dialog = new ContractDialog(p, rect, 2, 10_000, 0, () => 0, _ => null, () => null);
        var s = new Surface();
        s.Clear(200);
        dialog.Draw(s, d.Host.Assets);

        var y = rect.Y + 14;
        Assert.All(Enumerable.Range(6, 158), x => Assert.Equal(Colors.White, At(s, x, y)));
        Assert.NotEqual(Colors.White, At(s, 5, y));
        Assert.NotEqual(Colors.White, At(s, 164, y));
        for (var i = 0; i < 3; i++)
        {
            var rows = Enumerable.Range(rect.Y + 27 + 8 * i, 6).ToArray();
            Assert.Contains(rows, r => At(s, 76, r) == Colors.White);
            Assert.DoesNotContain(rows, r => At(s, 77, r) == Colors.White || At(s, 75, r) == Colors.White);
        }
    }

    /// <summary>
    /// R14N-03: Der gedrückte Knopf (kb\2 ab (17 | 191)) trägt die Beschriftung an derselben Stelle wie der ungedrückte.
    /// [ORIGINAL] Die Warteschleife 0x120:0x6B (Datei 0x4BEBD) zeichnet nur den Knopf mit Zustand 1 (Datei 0x4BFF2); die
    /// Beschriftung bleibt gleich. Kein DOSBox-Bild des gedrückten Knopfs.
    /// </summary>
    [GameDataFact]
    public void ContractDialog_PressedButton_LabelAtSamePlace()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var p = game.Clubs[game.ActiveManager.ClubIndex].Players[0];
        var dialog = new ContractDialog(p, ContractsScreen.DialogRect(24), 2, 10_000, 0, () => 0, _ => null, () => null);
        var r = dialog.AcceptButton;
        var up = new Surface();
        var down = new Surface();
        dialog.Draw(up, d.Host.Assets);
        dialog.Update(new InputState { MouseX = r.X + 3, MouseY = r.Y + 3, LeftDown = true });
        Assert.Equal(r, dialog.PressedButton);
        dialog.Draw(down, d.Host.Assets);
        bool InButton(int i) => r.Contains(i % Surface.Width, i / Surface.Width);
        var label = Enumerable.Range(0, Surface.Width * Surface.Height)
            .Where(i => InButton(i) && up.Pixels[i] == TeamUi.DialogDark).ToArray();
        Assert.NotEmpty(label);
        Assert.All(label, i => Assert.Equal(TeamUi.DialogDark, down.Pixels[i])); // Schrift an derselben Stelle
        Assert.NotEqual(up.Pixels, down.Pixels); // anderer Knopfzustand
    }

    /// <summary>
    /// R14KS-08: Gebotsfenster wie das Kauffenster bei x 2, Breite 165 (Aufruf 0xB155 mit x 2 … 0xA8), Linie (6 | 0x2B)–(0xA3 | 0x2B)
    /// (Datei 0x4917B–0x4919A).
    /// </summary>
    [GameDataFact]
    public void OfferWindow_AtXTwo_Width165_LineSixTo163()
    {
        Assert.Equal(new Rect(2, 29, 165, 71), TransferDialog.OfferBox);
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var player = club.Players.OrderByDescending(p => p.Condition + p.Technique)
            .First(p => p.Role == PlayerRole.Forward && TransferMarket.CanLeave(club, p));
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, player));
        for (var day = 0; day < 300 && game.TransferMarket.Bids.Count == 0; day++)
            game.TransferMarket.AdvanceDay(game, game.Random);
        var bid = Assert.Single(game.TransferMarket.Bids);
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        var i = game.TransferMarket.Listings.ToList().IndexOf(bid.Listing);
        var buy = TransferScreen.EntryBuy(i);
        d.Click(buy.X + 3, buy.Y + 3);
        Assert.Equal(TransferDialog.DialogKind.Bid, screen.Dialog?.Kind);
        d.Frame();
        var s = d.Surface;

        Assert.Equal(TeamUi.DialogLight, At(s, 2, 50)); // linke Kante x 2
        Assert.Equal(TeamUi.DialogDark, At(s, 166, 50)); // rechte Kante x 166
        Assert.Equal(TeamUi.DialogShadow, At(s, 168, 50));
        Assert.All(Enumerable.Range(6, 158), x => Assert.Equal(Colors.White, At(s, x, 0x2B)));
        Assert.NotEqual(Colors.White, At(s, 5, 0x2B));
        Assert.NotEqual(Colors.White, At(s, 164, 0x2B));
    }

    /// <summary>
    /// R14N-02: Kontoleiste der Transferliste wie ein Dialogfenster (0 | 223)–(317 | 237), Schatten Index 6 in y 238–239,
    /// „Saldo:“ und Betrag Index 240 (`file40_03`, `file40_04`).
    /// </summary>
    [GameDataFact]
    public void TransferScreen_AccountBar_LikeDosBox()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Transfer");
        Assert.IsType<TransferScreen>(d.Host.Current);
        d.Frame(_ => { });
        var s = d.Surface;

        Assert.Equal(new Rect(0, 223, 318, 15), TransferScreen.AccountBar);
        Assert.Equal(TeamUi.DialogLight, At(s, 150, 223));
        Assert.Equal(TeamUi.DialogFill, At(s, 150, 224));
        Assert.Equal(TeamUi.DialogDark, At(s, 150, 237));
        Assert.Equal(TeamUi.DialogShadow, At(s, 150, 238));
        Assert.Equal(TeamUi.DialogShadow, At(s, 150, 239));
        Assert.Equal(TeamUi.DialogLight, At(s, 0, 230));
        Assert.Equal(TeamUi.DialogDark, At(s, 317, 225)); // darunter Schatten der Listenfenster (8 | 8 | 8) wie im Bild
        Assert.Equal(TeamUi.DialogShadow, At(s, 318, 230));
        Assert.Equal(0, At(s, 317, 237));
        var saldo = Enumerable.Range(198, 50).SelectMany(x => Enumerable.Range(225, 12).Select(y => At(s, x, y))).ToArray();
        Assert.Contains(TransferScreen.SaldoColor, saldo);
        Assert.DoesNotContain(TeamUi.DialogDark, saldo);
        Assert.Equal(((byte)6, (byte)9, (byte)18), Dac(s.Palette, TransferScreen.SaldoColor));
    }

    /// <summary>
    /// R14KS-05 (M43): In der Kaderliste der Transferliste steht kein „*“ mehr – auch nicht bei einem Altstand vor A380, in dem der
    /// Spieler auf der Liste noch im Kader steht (Migration lässt ihn dort, <c>OwnListingsOutsideSquad</c> zählt ihn nicht).
    /// </summary>
    [GameDataFact]
    public void TransferScreen_SquadList_NoStarForListedPlayer_EvenInLegacyState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var p = club.Players.First(q => q.Role == PlayerRole.Forward && !q.OnLoan && TransferMarket.CanLeave(club, q));
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));

        // Altstand nachbauen: der gelistete Spieler steht wieder im Kader (hinten).
        var root = JsonNode.Parse(SaveGame.Save(game), documentOptions: new System.Text.Json.JsonDocumentOptions { MaxDepth = 1024 })!
            .AsObject();
        root["vereine"]![club.Index]!["Players"]!["items"]!.AsArray().Add(new JsonObject { ["$spieler"] = p.Id });
        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(loaded.Success, loaded.Error);
        var legacy = loaded.Game!;
        var legacyClub = legacy.Clubs[club.Index];
        var lp = legacyClub.Players.Single(q => q.Id == p.Id);
        Assert.NotNull(legacy.TransferMarket.ListingOf(lp));

        d.Host.Game = legacy;
        d.OpenTile("Transfer");
        Assert.IsType<TransferScreen>(d.Host.Current);
        d.Frame(_ => { });
        var row = TeamUi.Rows(legacyClub).ToList().IndexOf(lp);
        Assert.True(row >= 0);
        var y = TransferScreen.FirstRowY + row * TransferScreen.RowPitch;
        var mini = d.Host.Assets.Mini;
        var from = 25 + mini.Measure(lp.Name);
        var to = 25 + mini.Measure(lp.Name + " *");
        var color = TeamUi.RoleColor(lp.Role);
        for (var x = from; x < to; x++)
            for (var yy = y; yy < y + TransferScreen.RowPitch - 1; yy++)
                Assert.NotEqual(color, At(d.Surface, x, yy));
    }

    private static (byte, byte, byte) Dac(Palette palette, byte index)
    {
        var (r, g, b) = palette[index];
        return ((byte)(r >> 2), (byte)(g >> 2), (byte)(b >> 2));
    }
}
