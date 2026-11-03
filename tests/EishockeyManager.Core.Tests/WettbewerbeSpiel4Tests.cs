using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Teil 4 (Inventur P1 Nr. 5, A200, A201): Die Stärketabelle 0x51206 liest die Vereinsfelder
/// 0x18 + k (Kondition, Datei 0x5149A), 0x1A + k (Technik, 0x514BD), 0x1C + k (Form, 0x514E3) und für den Trendbalken
/// 0x17 (Tagesform, 0x51900) – nicht die Spielerwerte. Fundstellen: <c>docs/spielberechnung.md</c> §21.
/// Vorher rot: Ligavereine zeigten das Mittel der Spielerwerte, die Form das Mittel der Spielerform und der Trend einen
/// aus der Form abgeleiteten Wert.
/// </summary>
public class WettbewerbeSpiel4Tests
{
    private static GameState Create()
    {
        var game = TestGames.Create(seed: 4);
        MatchEngine.EnsureTeamValues(game);
        return game;
    }

    private static Club ComputerClub(GameState game) =>
        game.Clubs.First(c => c.League == League.Bundesliga && !game.Managers.Any(m => m.ClubIndex == c.Index) && c.Players.Count > 0);

    /// <summary>[ORIGINAL] Computerverein: Teamwert + Streuung des Laders, Form 0x1C/0x1D, Trend 0x17.</summary>
    [Fact]
    public void ComputerClub_ShowsClubFields()
    {
        var game = Create();
        var club = ComputerClub(game);
        club.TeamValues = new ClubTeamValues(3, -2, 1, -4, 46, 54, 52);
        var value = MatchEngine.TeamValue(game, club);
        var defense = StrengthTable.Compute(game, [club], StrengthView.Abwehr).Single();
        var attack = StrengthTable.Compute(game, [club], StrengthView.Sturm).Single();
        Assert.Equal((Math.Min(99, value + 3), Math.Min(99, value + 1), 46, 52), (defense.Condition, defense.Technique, defense.Form, defense.Trend));
        Assert.Equal((Math.Min(99, value - 2), Math.Min(99, value - 4), 54, 52), (attack.Condition, attack.Technique, attack.Form, attack.Trend));
        Assert.Equal((value + 3 + value + 1 + 46) / 3, defense.Average);
    }

    /// <summary>[ORIGINAL] Spielerwerte wirken beim Computerverein nicht; ein Vereinsfeld ändern → Tabelle folgt (Abnahme P1 Nr. 5).</summary>
    [Fact]
    public void ComputerClub_FollowsFieldsNotPlayers()
    {
        var game = Create();
        var club = ComputerClub(game);
        var before = StrengthTable.Compute(game, [club], StrengthView.Gesamt).Single();
        foreach (var p in club.Players)
            p.Form = Math.Min(99, p.Form + 30);
        Assert.Equal(before, StrengthTable.Compute(game, [club], StrengthView.Gesamt).Single());
        var v = club.TeamValues!.Value;
        club.TeamValues = v with { DefenseForm = v.DefenseForm + 4, AttackForm = v.AttackForm + 4, DayForm = 47 };
        var after = StrengthTable.Compute(game, [club], StrengthView.Gesamt).Single();
        Assert.Equal(before.Form + 4, after.Form);
        Assert.Equal(47, after.Trend);
    }

    /// <summary>[ORIGINAL] Managerverein: 0x952C schreibt die Felder 0x18–0x1D täglich aus der Aufstellung (Tageslauf 0x54F9C).</summary>
    [Fact]
    public void ManagerClub_ShowsLineupValues()
    {
        var game = Create();
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        club.TeamValues = club.TeamValues!.Value with { DayForm = 48 };
        var (d, a, _) = MatchEngine.LineupTeamValues(club.Lineup);
        var defense = StrengthTable.Compute(game, [club], StrengthView.Abwehr).Single();
        var attack = StrengthTable.Compute(game, [club], StrengthView.Sturm).Single();
        Assert.Equal((Math.Min(99, d.Condition), Math.Min(99, d.Technique), Math.Min(99, d.Form), 48),
            (defense.Condition, defense.Technique, defense.Form, defense.Trend));
        Assert.Equal((Math.Min(99, a.Condition), Math.Min(99, a.Technique), Math.Min(99, a.Form)),
            (attack.Condition, attack.Technique, attack.Form));
    }

    /// <summary>[ORIGINAL] Länderteam ohne Bundestrainer: Kondition/Technik aus 0x4C4E, Form 0x1C/0x1D und Trend 0x17 des Vereinsplatzes.</summary>
    [Fact]
    public void NationalTeam_FormAndTrendFromClubFields()
    {
        var game = Create();
        var nation = game.Clubs[52];
        nation.TeamValues = new ClubTeamValues(0, 0, 0, 0, 45, 55, 53);
        var (cd, td, ca, ta) = game.NationalRatings(52);
        var defense = StrengthTable.Compute(game, [nation], StrengthView.Abwehr).Single();
        var attack = StrengthTable.Compute(game, [nation], StrengthView.Sturm).Single();
        Assert.Equal((Math.Min(99, cd), Math.Min(99, td), 45, 53), (defense.Condition, defense.Technique, defense.Form, defense.Trend));
        Assert.Equal((Math.Min(99, ca), Math.Min(99, ta), 55), (attack.Condition, attack.Technique, attack.Form));
    }

    /// <summary>Die Monatseinnahme und die Zuschauer (Spur geld) lesen weiter <see cref="StrengthTable.TeamValues"/>; bis zur Integration unverändert.</summary>
    [Fact]
    public void FinanceTeamValues_Unchanged()
    {
        var game = Create();
        var club = ComputerClub(game);
        var before = StrengthTable.TeamValues(game, club);
        club.TeamValues = club.TeamValues!.Value with { DefenseCondition = 9, DefenseForm = 45 };
        Assert.Equal(before, StrengthTable.TeamValues(game, club));
    }
}
