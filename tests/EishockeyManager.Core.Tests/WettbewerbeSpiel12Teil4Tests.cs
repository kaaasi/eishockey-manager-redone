using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Runde 12, Teil 4 (U8, A013; <c>docs/analyse/re-nachtrag.md</c> §3.4): Die Auto-Aufstellung des
/// Nationalteams („A“) folgt derselben Regel wie beim Verein. [ORIGINAL] Satz 5 ruft Datei 0x426BC/0x42838 wie ein Verein
/// (Einstellung DGROUP 0x37F; Aufrufe Datei 0x3B4C6, 0x54D84). Befund vorher (Base 270d4a0): <see cref="NationalCoach.AutoFormation"/>
/// stellte nach dem Spielwert mit Energie auf (<see cref="Lineup.AutoFill"/>) und setzte den Rest nach Kondition + Technik +
/// Form ein – andere Auswahl als 0x42838 (Wert (K + T + F) / 3, Gleichstand späterer Platz, Ausweichrollen, Ersatz reihum).
/// </summary>
public class WettbewerbeSpiel12Teil4Tests
{
    private static NationalCoach Nominated(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        return game.NationalCoach!;
    }

    private static string Print(Lineup lineup) =>
        string.Join(",", LineupSlot.All.Select(s => lineup[s]?.Name ?? "-"));

    /// <summary>Testidee U8: gleiche Auswahl wie beim Verein (U7) bei gleichem Kader – Plätze in der Reihenfolge der Liste.</summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    [InlineData(17)]
    public void AutoFormation_SameSelectionAsClubRule(int seed)
    {
        var coach = Nominated(seed);
        // Unterschiedliche Belastung, damit der alte Weg (Spielwert mit Energie) sichtbar abweicht.
        var rows = coach.Rows;
        for (var i = 0; i < rows.Count; i++)
            rows[i].Strain = 50 + i * 37 % 101;
        var expected = new Lineup();
        var dressed = AutoLineupRule.Apply(expected, rows);

        coach.AutoFormation();

        Assert.Equal(Print(expected), Print(coach.Formation));
        Assert.Equal(dressed.Select(p => p.Name).Order(StringComparer.Ordinal),
            rows.Where(coach.IsDressed).Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.True(coach.DressedCount <= NationalCoach.MaxDressed);
    }

    /// <summary>[ORIGINAL] 0x42838: Verletzte und Gesperrte werden weder aufgestellt noch eingesetzt.</summary>
    [Fact]
    public void AutoFormation_SkipsUnavailable()
    {
        var coach = Nominated(3);
        var injured = coach.Rows.First(p => p.Role == PlayerRole.Forward);
        injured.InjuredGames = 2;
        coach.AutoFormation();
        Assert.False(coach.Formation.Contains(injured));
        Assert.False(coach.IsDressed(injured));
    }
}
