using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #730 (Spur abgleich-dosbox, Teil 1): Reihenwahl der Managerpaarung ohne Mausklick gegen die DOSBox-Aufnahme `runde-17`
/// (FILE02 → FILE50, 1. Spieltag KÖLNER EC – SC PREUSSEN BERLIN). Bericht: <c>docs/analyse/abgleich-dosbox.md</c> §1.
/// </summary>
/// <remarks>
/// [ORIGINAL] Bilder <c>file50_02…85</c> (Referenz-Repo, je Spielminute die Anzeigetafel 0x52BD6): Lampe k = Reihe auf dem Eis
/// (Vereinsfeld 0x2E + 2 · Teil) bei (x0 + 8 + 8t | 15 + 7k − 1), x0 = 13 Heim, 240 Gast. Minute = Spieluhr unten links; je
/// Minute das letzte vollständige Bild, fehlende Minuten (17, 23, 37, 43, 60) vom Vorgänger übernommen. Ziffer = Reihe 0…2 je
/// Spielminute 1…60. Das Original ist ein einziges Spiel, das Remake wird deshalb als Verteilung über Saaten geprüft.
/// </remarks>
public class AbgleichDosboxLinesTests(ITestOutputHelper output)
{
    /// <summary>[ORIGINAL] KÖLNER EC (Heim, Managerverein) Abwehr, Bilder file50_02…85.</summary>
    public const string HomeDefense = "000111121222200002222111100011111222200002222221110000011111";

    /// <summary>[ORIGINAL] KÖLNER EC Sturm.</summary>
    public const string HomeAttack = "000111121222200002222111100011111222200001111100002222111122";

    /// <summary>[ORIGINAL] SC PREUSSEN BERLIN (Gast, Computerverein) Abwehr.</summary>
    public const string AwayDefense = "000111112222000001222221110000221122200001111000012222111100";

    /// <summary>[ORIGINAL] SC PREUSSEN BERLIN Sturm.</summary>
    public const string AwayAttack = "000111112222000002222221110000111122200001122211111000011111";

    private const int Seeds = 200;

    /// <summary>
    /// Kennzahlen eines Spiels: Eisminuten je Seite, Teil und Reihe 0…2 (12 Werte), Minuten mit Abwehrreihe ≠ Sturmreihe je
    /// Seite (2) und Zahl der Reihenwechsel je Seite (2).
    /// </summary>
    public static int[] Figures(IReadOnlyList<string> sides)
    {
        var figures = new List<int>();
        foreach (var part in sides)
            for (var line = 0; line < 3; line++)
                figures.Add(part.Count(c => c - '0' == line));
        for (var side = 0; side < 2; side++)
        {
            var defense = sides[2 * side];
            var attack = sides[2 * side + 1];
            figures.Add(Enumerable.Range(0, 60).Count(m => defense[m] != attack[m]));
            figures.Add(Enumerable.Range(1, 59).Count(m => defense[m] != defense[m - 1] || attack[m] != attack[m - 1]));
        }
        return [.. figures];
    }

    private static readonly string[] FigureNames =
    [
        "Heim Abwehr R1", "Heim Abwehr R2", "Heim Abwehr R3", "Heim Sturm R1", "Heim Sturm R2", "Heim Sturm R3",
        "Gast Abwehr R1", "Gast Abwehr R2", "Gast Abwehr R3", "Gast Sturm R1", "Gast Sturm R2", "Gast Sturm R3",
        "Heim getrennt", "Heim Wechsel", "Gast getrennt", "Gast Wechsel",
    ];

    /// <summary>
    /// Beleg #730: Ohne Mausklick liegt jede Kennzahl des Originals in der Verteilung des Remakes über 200 Saaten (mindestens
    /// zwei Saaten, also 1 %, auf jeder Seite des Originalwerts). Eine Reihenwahl, die eine Reihe deutlich mehr spielen lässt
    /// (Verdacht aus FILE02→03: Reihen 1/2 kippen über 127), oder ein anderer Wechselrhythmus macht den Fall rot.
    /// </summary>
    [GameDataFact]
    public void File50_LinesWithoutClick_WithinRemakeSpread()
    {
        var original = Figures([HomeDefense, HomeAttack, AwayDefense, AwayAttack]);
        var save = OriginalImportTests.OriginalSave("FILE02.HOK");
        var database = ClubDatabase.Load(TestGameData.Load());
        var runs = new List<int[]>();
        for (var seed = 1; seed <= Seeds; seed++)
            runs.Add(Figures(PlayFirstMatch(HokCompare.Import(save, database, seed), seed)));

        var failures = new List<string>();
        for (var i = 0; i < original.Length; i++)
        {
            var below = runs.Count(r => r[i] <= original[i]);
            var above = runs.Count(r => r[i] >= original[i]);
            var values = runs.Select(r => r[i]).Order().ToList();
            output.WriteLine($"{FigureNames[i],-16} Original {original[i],2}  Remake {values[0]}…{values[^1]} " +
                $"Median {values[values.Count / 2]}  ≤ {below}, ≥ {above}");
            if (below < 2 || above < 2)
                failures.Add($"{FigureNames[i]}: Original {original[i]}, Remake {values[0]}…{values[^1]}");
        }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// Das erste Managerspiel aus FILE02 (KÖLNER EC – SC PREUSSEN BERLIN) Minute für Minute wie <see cref="OriginalMinutes.Play"/>,
    /// Reihen je Minute nach dem Reihenwechsel (Stand der Anzeigetafel am Minutenende).
    /// </summary>
    private static List<string> PlayFirstMatch(GameState game, int seed)
    {
        var home = game.Clubs[game.Managers[0].ClubIndex];
        var away = game.Clubs.Single(c => c.Name == "SC PREUSSEN BERLIN");
        Assert.Equal("KÖLNER EC", home.Name);
        var homeTeam = MatchEngine.LiveTeamOf(game, home, away, managed: true, managerMatch: true);
        var awayTeam = MatchEngine.LiveTeamOf(game, away, home, managed: false, managerMatch: true);
        MatchEngine.PrepareMatch(game, home, away, homeTeam, awayTeam);
        var rules = new MatchRules(ManagerMatch: true);
        var report = new MatchReport();
        var random = new Random(seed);
        var lines = new System.Text.StringBuilder[4];
        for (var i = 0; i < 4; i++)
            lines[i] = new System.Text.StringBuilder();
        for (var minute = 1; minute <= 60; minute++)
        {
            if (minute is 21 or 41)
            {
                OriginalMinutes.RecoverEnergy(homeTeam);
                OriginalMinutes.RecoverEnergy(awayTeam);
            }
            OriginalMinutes.PlayMinute(homeTeam, awayTeam, rules, minute, (minute + 19) / 20 * 20, report, random);
            lines[0].Append(homeTeam.DefenseLine);
            lines[1].Append(homeTeam.AttackLine);
            lines[2].Append(awayTeam.DefenseLine);
            lines[3].Append(awayTeam.AttackLine);
        }
        return [.. lines.Select(l => l.ToString())];
    }
}
