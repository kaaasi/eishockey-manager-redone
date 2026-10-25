using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>Abläufe der Mannschafts-Bildschirme mit den Originaldateien (ohne gamedata übersprungen).</summary>
public class TeamScreenTests
{
    [Fact]
    public void Lineup_ClickPlayerThenSlot_ChangesGoalkeeper()
    {
        var d = FlowTests.CreateDriver();
        if (d is null)
            return;
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        Assert.IsType<LineupScreen>(d.Host.Current);

        var club = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex];
        var rows = club.SortedPlayers.ToList();
        var reserveGoalie = rows.Take(2).Single(p => club.Lineup.Goalkeeper != p);
        var row = rows.IndexOf(reserveGoalie);
        d.Click(60, LineupScreen.FirstRowY + row * LineupScreen.RowPitch + 2);
        var gk = LineupScreen.SlotButton(LineupSlot.Goalkeeper);
        d.Click(gk.X + 5, gk.Y + 5);
        Assert.Same(reserveGoalie, club.Lineup.Goalkeeper);

        // Stürmer auf einen Verteidigerplatz: Meldung, Aufstellung unverändert
        var forward = rows.First(p => p.Role == PlayerRole.Forward);
        var defenderSlot = LineupScreen.SlotButton(LineupSlot.Defender(0, 0));
        var before = club.Lineup[LineupSlot.Defender(0, 0)];
        d.Click(60, LineupScreen.FirstRowY + rows.IndexOf(forward) * LineupScreen.RowPitch + 2);
        d.Click(defenderSlot.X + 5, defenderSlot.Y + 5);
        Assert.Same(before, club.Lineup[LineupSlot.Defender(0, 0)]);
        d.Click(10, 10); // Meldung schließen

        // Rechtsklick leert, Esc geht zurück ins Hauptmenü
        d.RightClick(defenderSlot.X + 5, defenderSlot.Y + 5);
        Assert.Null(club.Lineup[LineupSlot.Defender(0, 0)]);
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    [Fact]
    public void Contracts_ExtendContract_WithInsurance()
    {
        var d = FlowTests.CreateDriver();
        if (d is null)
            return;
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
}
