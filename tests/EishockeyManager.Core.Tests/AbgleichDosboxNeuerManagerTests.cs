using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #847 Gegenprobe (Spur abgleich-dosbox-19, Teil 3): Neuer Manager mitten in der Saison gegen die DOSBox-Aufnahme `runde-19`
/// Sitzung 3. FILE64 (20.09.1993, ein Manager „Joerg“ mit KÖLNER EC) → „Neuer“: „Spieler2“ mit EC HANNOVER (vorher 2. Liga),
/// Start-Liga Bundesliga → FILE65 (zwei Manager). Laut Bildern <c>file65_1/2</c> → <c>file65_5/6</c> tauschte Hannover mit
/// EISBÄREN BERLIN (Bundesliga Platz 12 ↔ 2. Liga Platz 3). Bericht: <c>docs/analyse/abgleich-dosbox.md</c> §6.
/// </summary>
public class AbgleichDosboxNeuerManagerTests(ITestOutputHelper output)
{
    private const string Newcomer = "EC HANNOVER";
    private const string Partner = "EISBÄREN BERLIN";

    /// <summary>
    /// Remake ab FILE64 mit dem Platz, den das Original gezogen hat (erste Saat, deren Zufallsplatz EISBÄREN BERLIN ist), gegen
    /// FILE65 je Verein (Name): Liga, Tabellensatz, Teamwerte; dazu Verein und Kadergröße beider Manager, Name, Konto und
    /// Eintritt des Neuen. [ORIGINAL] 0x36169(v, r, 0x80): Kennung und Spieler wandern, der Tabellensatz bleibt am Platz;
    /// danach schreibt 0x952C(m, 0) (Datei 0x2A073) die Teamwerte des Managerplatzes aus der Aufstellung (Regressionstest #847).
    /// </summary>
    [GameDataFact]
    public void File64ToFile65_SwapLikeOriginal()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var before = OriginalImportTests.OriginalSave("FILE64.HOK");
        var after = HokCompare.Import(OriginalImportTests.OriginalSave("FILE65.HOK"), database, 1);
        var probe = HokCompare.Import(before, database, 1);
        var hannover = probe.Clubs.Single(c => c.Name == Newcomer).Index;
        var eisbaeren = probe.Clubs.Single(c => c.Name == Partner).Index;
        Assert.Equal(League.ZweiteLiga, probe.Clubs[hannover].League);
        Assert.Equal(League.Bundesliga, probe.Clubs[eisbaeren].League);

        GameState? game = null;
        for (var seed = 1; seed <= 400 && game is null; seed++)
        {
            var g = HokCompare.Import(before, database, seed);
            if (g.AddManager(new ManagerSetup { Name = "Spieler2", Head = 1, ClubIndex = hannover }).ClubIndex == eisbaeren)
                game = g;
        }
        Assert.NotNull(game);

        var diffs = new List<string>();
        foreach (var club in after.Clubs.Where(c => c.League is League.Bundesliga or League.ZweiteLiga))
        {
            var r = Place(game, game.Clubs.Single(c => c.Name == club.Name));
            var o = Place(after, club);
            if (r != o)
                diffs.Add($"{club.Name}: Remake {r}\n         Original {o}");
        }
        foreach (var m in new[] { 0, 1 })
        {
            Check(diffs, $"Verein Manager {m}", after.Clubs[after.Managers[m].ClubIndex].Name, game.Clubs[game.Managers[m].ClubIndex].Name);
            Check(diffs, $"Kadergröße Manager {m}", after.Clubs[after.Managers[m].ClubIndex].Players.Count,
                game.Clubs[game.Managers[m].ClubIndex].Players.Count);
        }
        var manager = game.Managers[1];
        var original = after.Managers[1];
        Check(diffs, "Manager 1 Name", original.Name, manager.Name);
        Check(diffs, "Manager 1 Konto", original.Money, manager.Money);
        Check(diffs, "Manager 1 Eintritt", after.Finance(original).Stadium.TicketPrice, game.Finance(manager).Stadium.TicketPrice);
        Check(diffs, "Manager 0 Konto", after.Managers[0].Money, game.Managers[0].Money);
        foreach (var d in diffs)
            output.WriteLine(d);
        Assert.True(diffs.Count == 0, string.Join("\n", diffs));
    }

    private static void Check<T>(List<string> diffs, string what, T original, T remake)
    {
        if (!EqualityComparer<T>.Default.Equals(original, remake))
            diffs.Add($"{what}: Original {original}, Remake {remake}");
    }

    /// <summary>
    /// Liga, Tabellensatz (Gesamt) und Teamwerte eines Vereins (Spieler nicht: den Kader des Neuen zieht der Zufall, die
    /// Computerkader zieht der Import neu). Teamwerte eines Managervereins als „Aufstellung“, wenn sie genau den Werten aus
    /// seiner Aufstellung entsprechen ([ORIGINAL] 0x952C(m, 0), Datei 0x2A073), sonst als Zahlen.
    /// </summary>
    private static string Place(GameState game, Club club)
    {
        var row = club.League is { } league
            ? LeagueTable.Compute(game, league, TableView.Gesamt, game.PlayedMatchDays).Single(r => r.Club == club)
            : null;
        var v = MatchEngine.TeamValuesOf(club);
        var b = MatchEngine.ComputerTeamValue(club);
        var values = $"{b + v.DefenseCondition},{b + v.AttackCondition},{b + v.DefenseTechnique},{b + v.AttackTechnique}," +
            $"{v.DefenseForm},{v.AttackForm}";
        if (game.Managers.Any(m => m.ClubIndex == club.Index))
        {
            var (d, a, _) = MatchEngine.ClubLineupTeamValues(club);
            var lineup = $"{d.Condition},{a.Condition},{d.Technique},{a.Technique},{d.Form},{a.Form}";
            values = values == lineup ? "Aufstellung" : $"{values} (Aufstellung {lineup})";
        }
        return string.Join("|", club.League, row is null ? "-" : $"{row.Rank}:{row.Games}:{row.PointsPlus}:{row.GoalsFor}:{row.GoalsAgainst}",
            $"Teamwerte {values}", v.DayForm);
    }

}
