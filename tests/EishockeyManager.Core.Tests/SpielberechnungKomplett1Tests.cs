using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielberechnung-komplett, Teil 1 (R8W3I-15, #204): Spielstärke der Managervereine gegen das Original.
/// Befund: Im Original spielt ein Managerverein mit seinen Spielern (0x98F8), deren Startwerte nur von Start-Liga und
/// Start-Stärke abhängen (DGROUP 0x4B42, Datei 0x29DA4), nicht von der Datenbankstärke des Vereins. Wer Düsseldorf (92)
/// mit „Mittelfeld“ übernimmt, bekommt einen Mittelfeldkader – der Abstand zu <c>PlayAtDatabaseStrength</c> ist kein Fehler.
/// Einzelheiten: docs/spielberechnung.md §14.
/// </summary>
public sealed class SpielberechnungKomplett1Tests(ITestOutputHelper output)
{
    private const int Seeds = 12;

    private static double MeanPlace(int clubIndex, StartStrength strength, out double goalDiff)
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        double place = 0, diff = 0;
        for (var seed = 1; seed <= Seeds; seed++)
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = clubIndex, StartStrength = strength });
            var game = NewGame.Create(database, setup, seed);
            TestSeason.PlayRegularSeason(game);
            var table = LeagueTable.Compute(game, League.Bundesliga, TableView.Gesamt, game.PlayedMatchDays);
            var row = table.Single(r => r.Club.Index == clubIndex);
            place += row.Rank;
            diff += row.GoalsFor - row.GoalsAgainst;
        }
        goalDiff = diff / Seeds;
        return place / Seeds;
    }

    /// <summary>
    /// [ORIGINAL] 0x29DA4/DGROUP 0x4B42: Die Start-Stärke bestimmt die Spielstärke des Managervereins, die Datenbankstärke
    /// nicht. Düsseldorf (92) und Rosenheim (70) landen mit gleicher Start-Stärke im Mittel nah beieinander; „Oberes
    /// Drittel“ (87) vor „Mittelfeld“ (81) vor „Abstiegskampf“ (70).
    /// </summary>
    [GameDataFact]
    public void ManagedClub_PlaceFollowsStartStrength_NotDatabase()
    {
        var places = new Dictionary<(int Club, StartStrength Strength), double>();
        foreach (var club in new[] { 2, 11 })
            foreach (var strength in Enum.GetValues<StartStrength>())
            {
                places[(club, strength)] = MeanPlace(club, strength, out var diff);
                output.WriteLine($"Verein {club}, {strength}: Platz {places[(club, strength)]:F2}, TD {diff:F1}");
            }
        foreach (var club in new[] { 2, 11 })
        {
            Assert.True(places[(club, StartStrength.OberesDrittel)] < places[(club, StartStrength.Mittelfeld)]);
            Assert.True(places[(club, StartStrength.Mittelfeld)] < places[(club, StartStrength.Abstiegskampf)]);
        }
        foreach (var strength in Enum.GetValues<StartStrength>())
            Assert.InRange(Math.Abs(places[(2, strength)] - places[(11, strength)]), 0, 2.0);
    }
}
