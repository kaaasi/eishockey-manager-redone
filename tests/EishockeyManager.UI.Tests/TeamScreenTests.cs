using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>Abläufe der Mannschafts-Bildschirme mit den Originaldateien (ohne gamedata übersprungen).</summary>
public class TeamScreenTests
{
    [GameDataFact]
    public void Lineup_ClickPlayerThenSlot_ChangesGoalkeeper()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        Assert.IsType<LineupScreen>(d.Host.Current);

        var club = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex];
        var rows = club.SortedPlayers.ToList();
        var reserveGoalie = rows.Take(2).Single(p => club.Lineup.Goalkeeper != p);
        var row = rows.IndexOf(reserveGoalie);
        // Erst den Platz, dann den Spieler: Linksklick auf einen eingesetzten Reservisten allein nimmt ihn heraus
        // (Datei 0x40F45, Spur taktik-verein).
        var gk = LineupScreen.SlotButton(LineupSlot.Goalkeeper);
        d.Click(gk.X + 5, gk.Y + 5);
        d.Click(60, LineupScreen.FirstRowY + row * LineupScreen.RowPitch + 2);
        Assert.Same(reserveGoalie, club.Lineup.Goalkeeper);
        d.Frames(100); // Integration Runde 11 (ui-meldungen, K2): Kurzmeldung 0x0C0B8 ohne Knopf, schließt von selbst (Klick wirkt nicht)

        // Stürmer auf einen Verteidigerplatz: Meldung, Aufstellung unverändert
        var forward = rows.First(p => p.Role == PlayerRole.Forward);
        var defenderSlot = LineupScreen.SlotButton(LineupSlot.Defender(0, 0));
        var before = club.Lineup[LineupSlot.Defender(0, 0)];
        d.Click(defenderSlot.X + 5, defenderSlot.Y + 5);
        d.Click(60, LineupScreen.FirstRowY + rows.IndexOf(forward) * LineupScreen.RowPitch + 2);
        Assert.Same(before, club.Lineup[LineupSlot.Defender(0, 0)]);
        d.Frames(100); // Integration Runde 11 (ui-meldungen, K2): Kurzmeldung 0x0C0B8 ohne Knopf, schließt von selbst (Klick wirkt nicht)

        // Rechtsklick leert, Esc geht zurück ins Hauptmenü
        d.RightClick(defenderSlot.X + 5, defenderSlot.Y + 5);
        Assert.Null(club.Lineup[LineupSlot.Defender(0, 0)]);
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    [GameDataFact]
    public void Contracts_ExtendContract_WithInsurance()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Verträge");
        var screen = Assert.IsType<ContractsScreen>(d.Host.Current);
        var club = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex];
        var first = club.SortedPlayers.First();

        d.Click(60, ContractsScreen.FirstRowY + 2);
        Assert.Same(first, screen.Negotiating);
        var dialog = ContractsScreen.DialogRect(0);
        var years = ContractsScreen.YearsField(dialog);
        d.Click(years.X + 5, years.Y + 2);
        d.Type("4");
        d.Press(Key.Enter);
        var insurance = ContractsScreen.InsuranceField(dialog);
        d.Click(insurance.X + 5, insurance.Y + 2);
        var accept = ContractsScreen.AcceptButton(dialog);
        d.Click(accept.X + 5, accept.Y + 5);

        Assert.Null(screen.Negotiating);
        Assert.Equal(4, first.ContractYears);
        Assert.True(first.Insured);
        d.Click(10, 10); // Meldung schließen
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    [GameDataFact]
    public void Tactics_ModesBlocksAndSwitchToLineup()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Taktik");
        var screen = Assert.IsType<TacticsScreen>(d.Host.Current);
        var t = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex].Tactics;

        // Feld bei AUTOM. gesperrt (Startwert 0, A180 kader-start Runde 14; vorher 60)
        var grund = TacticsScreen.SituationArrows(ScoreSituation.Base, attack: false);
        d.Click(grund.X + 4, grund.Y + 2);
        Assert.Equal(0, t.Effort(0, ScoreSituation.Base));

        var mode = TacticsScreen.EffortArrows;
        d.Click(mode.X + 4, mode.Y + 2);
        Assert.Equal(TacticMode.Semi, t.EffortMode);

        var tab = TacticsScreen.BlockTab(1);
        d.Click(tab.X + 5, tab.Y + 5);
        Assert.Equal(1, screen.Block);
        d.Click(grund.X + 4, grund.Y + 2);
        d.Click(grund.X + 4, grund.Y + 2);
        Assert.Equal(2, t.Effort(1, ScoreSituation.Base));
        d.Click(grund.X + 4, grund.Y + grund.H - 2);
        Assert.Equal(1, t.Effort(1, ScoreSituation.Base));
        Assert.Equal(0, t.Effort(0, ScoreSituation.Base));

        // Minuten nur bei „Minuten-Vorgabe (s.u.)“
        var line = TacticsScreen.LineChangeArrows;
        while (t.LineChange != LineChangeMode.MinutesPreset)
            d.Click(line.X + 4, line.Y + 2);
        var minutes = TacticsScreen.MinutesArrows(0, forwards: true);
        d.Click(minutes.X + 4, minutes.Y + 2);
        Assert.Equal(1, t.Minutes(0, forwards: true));

        d.Click(TeamUi.SwitchButton.X + 5, TeamUi.SwitchButton.Y + 5);
        Assert.IsType<LineupScreen>(d.Host.Current);
        d.Click(TeamUi.SwitchButton.X + 5, TeamUi.SwitchButton.Y + 5);
        Assert.IsType<TacticsScreen>(d.Host.Current);
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    [GameDataFact]
    public void Transfer_InfoBuySellAndTalent()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var listing = game.TransferMarket.Listings[0];

        // Spielerinfo
        var name = TransferScreen.EntryNameRow(0);
        d.Click(name.X + 20, name.Y + 2);
        Assert.Same(listing.Player, screen.Info.Player);
        d.Click(PlayerInfoPopup.AhaButton.X + 5, PlayerInfoPopup.AhaButton.Y + 5);
        Assert.False(screen.Info.Visible);

        // Kaufen: Betrag eingeben. Über 130 % des Preises nimmt der Computermarkt immer an (em.exe 0x4991D, Spur
        // transfer-original; zum geforderten Preis nur mit Wahrscheinlichkeit).
        manager.Money = 2_000_000;
        var buy = TransferScreen.EntryBuy(0);
        d.Click(buy.X + 5, buy.Y + 2);
        Assert.Equal(TransferDialog.DialogKind.Buy, screen.Dialog!.Kind);
        Assert.Equal(listing.Price, screen.Dialog.Amount);
        var offer = listing.Price * 135 / 100;
        d.Click(TransferDialog.AmountField.X + 5, TransferDialog.AmountField.Y + 2);
        d.Type(offer.ToString());
        d.Press(Key.Enter);
        Assert.Equal(offer, screen.Dialog.Amount);
        d.Press(Key.Enter);
        Assert.Null(screen.Dialog);
        // Spur transfer-vertraege-komplett: nach der Annahme die Vertragsverhandlung (0x4786A); erst „Mach' ich!“ schließt ab.
        Assert.NotNull(screen.Contract);
        Assert.DoesNotContain(listing.Player, club.Players);
        Assert.Equal(2_000_000, manager.Money);
        d.Press(Key.Enter);
        Assert.Null(screen.Contract);
        Assert.Contains(listing.Player, club.Players);
        Assert.Equal(2_000_000 - offer, manager.Money);
        d.Click(10, 10); // Meldung schließen

        // Eigenen Stürmer auf den Markt setzen
        var rows = TeamUi.Rows(club);
        var row = rows.ToList().FindIndex(p => p.Role == PlayerRole.Forward);
        d.Click(60, TransferScreen.FirstRowY + TransferScreen.RowPitch * row + 2);
        Assert.Equal(TransferDialog.DialogKind.Sale, screen.Dialog!.Kind);
        d.Press(Key.Enter);
        Assert.NotNull(game.TransferMarket.ListingOf(rows[row]));

        // Talentsuche ablehnen
        d.Click(TransferScreen.TalentButton.X + 5, TransferScreen.TalentButton.Y + 5);
        Assert.Equal(TransferDialog.DialogKind.Talent, screen.Dialog!.Kind);
        var cancel = screen.Dialog.CancelButton;
        d.Click(cancel.X + 5, cancel.Y + 5);
        Assert.Null(screen.Dialog);

        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    [GameDataFact]
    public void Training_SelectKindPaintUnitsAndPay()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Training");
        var screen = Assert.IsType<TrainingScreen>(d.Host.Current);
        var plan = screen.Plan;
        Assert.Equal(TrainingKind.Special, screen.Selected);
        Assert.Equal(0, plan.Count(TrainingKind.Special)); // Standardplan

        // Frei wählen und den Montag überstreichen
        var free = TrainingScreen.KindIcon(TrainingKind.Free);
        d.Click(free.X + 5, free.Y + 5);
        Assert.Equal(TrainingKind.Free, screen.Selected);
        var points = Enumerable.Range(0, TrainingPlan.UnitsPerDay)
            .Select(u => (TrainingScreen.UnitHit(0, u).X + 3, TrainingScreen.UnitHit(0, u).Y + 3)).ToArray();
        d.Drag(points);
        Assert.All(Enumerable.Range(0, 4), u => Assert.Equal(TrainingKind.Free, plan[0, u]));

        // Bewegen ohne Taste ändert nichts
        var special = TrainingScreen.KindIcon(TrainingKind.Special);
        d.Click(special.X + 5, special.Y + 5);
        var unit = TrainingScreen.UnitHit(2, 2);
        d.Move(unit.X + 3, unit.Y + 3);
        Assert.Equal(TrainingKind.Free, plan[2, 2]);
        d.Click(unit.X + 3, unit.Y + 3);
        Assert.Equal(TrainingKind.Special, plan[2, 2]);
        Assert.Equal(12_400, plan.WeeklyCost); // Oberliga wie im Video

        // Zurück, zwei Spieltage: Kosten werden je Trainingstag abgebucht
        var game = d.Host.Game!;
        long Training() => game.Finance(game.ActiveManager).Book.Current(FinanceCategory.Training);
        var before = Training();
        d.RightClick(160, 120);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        d.PlayMatchDay();
        d.PlayMatchDay();
        // Verarbeitet sind alle Tage vor dem angezeigten Datum; der angezeigte Tag selbst noch nicht (R2-02)
        var days = Enumerable.Range(1, game.Day - 1).Count(GameState.IsTrainingDay);
        Assert.Equal(before - days * plan.CostPerTrainingDay, Training());
    }
}
