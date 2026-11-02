using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>transfer-original-vertrag</c> über die echten Ansichten: Verhandlungsdialog nach 0x4A1DB (Meldungen, Forderung,
/// Sperre), Hinweiszeilen der Listen nach 0x41683 und Gebotssperre der Transferliste (0x47140). Öffnen und Überfahren
/// verändern den Spielstand nicht.
/// </summary>
public class TransferOriginalVertragScreenTests
{
    private static (ScreenDriver D, GameState Game, Club Club) Start(string tile)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        d.OpenTile(tile);
        return (d, game, game.Clubs[game.ActiveManager.ClubIndex]);
    }

    private static void EnterYears(ScreenDriver d, string years)
    {
        var field = ContractsScreen.YearsField(ContractsScreen.DialogRect(0));
        d.Click(field.X + 5, field.Y + 2);
        d.Type(years);
        d.Press(Key.Enter);
    }

    private static void Accept(ScreenDriver d)
    {
        var accept = ContractsScreen.AcceptButton(ContractsScreen.DialogRect(0));
        d.Click(accept.X + 5, accept.Y + 5);
    }

    /// <summary>
    /// 5 Jahre: „SOOOO lange verpflichtet sich kein Eishockey-Spieler !“, der Dialog schließt, der Spieler ist gesperrt; beim
    /// nächsten Versuch meldet er „ist im Moment nicht verhandlungsbereit.“ Vorher rot: 5 Jahre wurden angenommen.
    /// </summary>
    [GameDataFact]
    public void FiveYears_TooLong_ThenNotWilling()
    {
        var (d, _, club) = Start("Verträge");
        var screen = Assert.IsType<ContractsScreen>(d.Host.Current);
        var p = TeamUi.Rows(club)[0];
        var years = p.ContractYears;

        d.Click(60, ContractsScreen.FirstRowY + 2);
        EnterYears(d, "5");
        Accept(d);

        Assert.Null(screen.Negotiating);
        Assert.Equal(Contracts.Message(NegotiationResult.TooLong, p), screen.Message.Lines);
        Assert.InRange(p.NegotiationLock, 10, 18);
        Assert.Equal(years, p.ContractYears);
        d.Click(10, 10); // Meldung schließen

        d.Click(60, ContractsScreen.FirstRowY + 2);
        var field = ContractsScreen.YearsField(ContractsScreen.DialogRect(0));
        d.Click(field.X + 5, field.Y + 2);
        Assert.Equal(Contracts.Message(NegotiationResult.NotWilling, p), screen.Message.Lines);
    }

    /// <summary>
    /// Laufzeit eingeben füllt das Gehalt mit der Forderung; „Mach' ich!“ übernimmt beides ohne Meldung (Level 3).
    /// </summary>
    [GameDataFact]
    public void Years_FillDemand_AcceptedDirectly()
    {
        var (d, game, club) = Start("Verträge");
        var screen = Assert.IsType<ContractsScreen>(d.Host.Current);
        var p = TeamUi.Rows(club)[0];
        var demand = Contracts.DemandAfterYears(game, game.ActiveManager, p, 3);
        Assert.NotNull(demand);

        d.Click(60, ContractsScreen.FirstRowY + 2);
        EnterYears(d, "3");
        Accept(d);

        Assert.Null(screen.Negotiating);
        Assert.False(screen.Message.Visible);
        Assert.Equal((3, demand!.Value), (p.ContractYears, p.MonthlySalary));
    }

    /// <summary>
    /// Hinweiszeile der Liste „Verträge“ nach 0x41683 mit Maske 0x3D146 (Aufruf 0x400E0): Vertragsdauer, Gehalt, Tendenz.
    /// Überfahren ändert nichts am Spielstand.
    /// </summary>
    [GameDataFact]
    public void ContractsList_HintLine_FromOriginalColumns()
    {
        var (d, game, club) = Start("Verträge");
        var screen = Assert.IsType<ContractsScreen>(d.Host.Current);
        var before = SaveGame.Save(game);
        var p = TeamUi.Rows(club)[0];
        var layout = ContractsScreen.Layout(TeamUi.Rows(club).Count);

        foreach (var column in new[] { 15, 16, 17, 2 })
        {
            var x = Enumerable.Range(0, 256).First(x => PlayerHintLine.Column(layout, x) == column);
            d.Move(x, ContractsScreen.FirstRowY + 2);
            Assert.Equal(PlayerHintLine.Text(p, column, new PlayerHintInfo(club.IsDressed(p), 0), false)?[..10], screen.Status?[..10]);
        }
        Assert.Equal("Dauer des Vertrages mit dem Spieler (in Saisons)", PlayerHintLine.Text(p, 16, default, false));
        Assert.Equal(before, SaveGame.Save(game));
    }

    /// <summary>
    /// Transferliste: abgelehntes Gebot sperrt den Spieler; ein Klick auf (K) bzw. (L) meldet „Angebot wurde bereits
    /// abgelehnt.“ statt den Gebotsdialog zu öffnen (0x47140). Kader-Hinweiszeile nach 0x41683 (Maske 0x90C6).
    /// </summary>
    [GameDataFact]
    public void LockedListing_ShowsAlreadyRejected()
    {
        var (d, game, club) = Start("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        var manager = game.ActiveManager;
        var market = game.TransferMarket;
        var index = market.Listings.ToList().FindIndex(l => l.Seller is null && !l.IsFreeAgent);
        var listing = market.Listings[index];
        manager.Money = 50_000_000;
        Assert.Equal(TransferResult.Rejected, market.Buy(game, manager, listing, listing.Price / 2, false));

        var buy = TransferScreen.EntryBuy(index);
        d.Click(buy.X + 3, buy.Y + 2);
        Assert.Null(screen.Dialog);
        Assert.Equal(TransferMarket.Message(TransferResult.AlreadyRejected), screen.Message.Lines);
        d.Frames(100); // Integration Runde 11 (ui-meldungen, K2): Kurzmeldung 0x0C0B8 ohne Knopf, schließt von selbst (Klick wirkt nicht)

        var layout = PlayerListLayout.TransferList(TeamUi.Rows(club).Count);
        var x = Enumerable.Range(0, 160).First(x => PlayerHintLine.Column(layout, x) == 7);
        var before = SaveGame.Save(game);
        d.Move(x, TransferScreen.FirstRowY + 2);
        Assert.StartsWith("Kondition, Technik, Form ", screen.Status);
        var name = TransferScreen.EntryNameRow(index);
        d.Click(name.X + 20, name.Y + 2); // Spielerdaten-Fenster mit Sichtbarkeit nach Verein
        Assert.Same(listing.Player, screen.Info.Player);
        Assert.Equal(before, SaveGame.Save(game));
    }
}
