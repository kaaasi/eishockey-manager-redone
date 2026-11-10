using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-original, Teil 2: Remake gegen die Spielstandpaare des Originals (<c>emtool abgleich</c>, R15-D2 = A). Je Paar ein
/// Fall; bekannte Abweichungen stehen als Pfadmuster mit Issue-Nummer in <see cref="Expected"/>. Jede neue Abweichung macht den
/// Fall rot, ebenso ein Muster, das nicht mehr trifft (behoben → Eintrag entfernen). Bericht: <c>docs/analyse/abgleich-original.md</c>.
/// </summary>
/// <remarks>
/// Paare ohne Eingriff: FILE02→03, FILE13→14, FILE16A→17, FILE20→21, FILE22→23, FILE24→25, FILE30→31, FILE38→39→40; mit bekanntem
/// Eingriff (nur übrige Felder, <see cref="HokCompareClasses.Interventions"/>): FILE44→46, FILE18→48. Saat 1993 (Werkzeugvorgabe);
/// zufallsabhängige Felder sind mit Bereichen und Invarianten geprüft, ein anderer Zufall darf das Ergebnis nicht kippen
/// (<see cref="Pair_OtherSeed_SameExpectedList"/>).
/// </remarks>
public class AbgleichOriginalPaareTests
{
    private const int TeamValuesImport = 729, Strain = 730, PoolSeasonChange = 731, WorldOnlyManagerSlot = 732, AutoLineupFlag = 733,
        MoneyFile13 = 734, DayZero = 699;

    /// <summary>Gemeinsame Muster: Teamwerte (Kondition/Technik) nach dem Import (#729), Tag 0 am Saisonanfang (#699 Punkt 2).</summary>
    private static readonly (string Pattern, int Issue)[] TeamValues =
    [
        ("Clubs[*].TeamValues.*Condition", TeamValuesImport), ("Clubs[*].TeamValues.*Technique", TeamValuesImport),
    ];

    private static readonly (string Pattern, int Issue)[] SeasonStart =
    [
        ("DayOpened", DayZero), ("FinanceDay", DayZero), ("TrainedUntil.*", DayZero), ("RankSlotsRecorded", DayZero),
        ("Invariante.PoolMittel.*", PoolSeasonChange),
    ];

    public static readonly IReadOnlyDictionary<string, (string Pattern, int Issue)[]> Expected =
        new Dictionary<string, (string, int)[]>
        {
            ["FILE02→FILE03"] = [.. TeamValues, ("Clubs[*].Players[*].Strain", Strain)],
            ["FILE13→FILE14"] = [.. TeamValues, .. SeasonStart, ("Managers[0].Money", MoneyFile13),
                ("Clubs[6 KREFELDER EV]._autoLineup", AutoLineupFlag), ("Clubs[6 KREFELDER EV].TeamValues.*Form", AutoLineupFlag)],
            ["FILE16A→FILE17"] = [.. TeamValues, .. SeasonStart, ("Clubs[*].Players[*].Strain", Strain)],
            ["FILE20→FILE21"] = [.. TeamValues, ("DayOpened", DayZero), ("Invariante.PoolMittel.*", PoolSeasonChange),
                ("Clubs[7 EV LANDSHUT].Players*", WorldOnlyManagerSlot), ("Clubs[7 EV LANDSHUT].TeamValues", WorldOnlyManagerSlot),
                ("Managers[0].Money", WorldOnlyManagerSlot)],
            ["FILE22→FILE23"] = [.. TeamValues, .. SeasonStart],
            ["FILE24→FILE25"] = [.. TeamValues, ("Clubs[*].Players[*].Strain", Strain), ("Clubs[5 KÖLNER EC]._autoLineup", AutoLineupFlag)],
            ["FILE30→FILE31"] = [.. TeamValues],
            ["FILE38→FILE39"] = [.. TeamValues, ("Clubs[*].Players[*].Strain", Strain)],
            ["FILE39→FILE40"] = [.. TeamValues],
            ["FILE44→FILE46"] = [.. TeamValues, .. SeasonStart],
            ["FILE18→FILE48"] = [.. TeamValues],
        };

    public static TheoryData<string, string> Pairs()
    {
        var data = new TheoryData<string, string>();
        foreach (var pair in Expected.Keys)
        {
            var files = pair.Split('→');
            data.Add(files[0], files[1]);
        }
        return data;
    }

    [GameDataTheory]
    [MemberData(nameof(Pairs))]
    public void Pair_OnlyKnownDeviations(string a, string b) => AssertExpected(Compare(a, b, HokCompare.DefaultSeed));

    /// <summary>
    /// Bereiche gegen Zufall: Zwei Paare (eines innerhalb der Saison, eines über den Saisonwechsel) mit anderer Saat ergeben dieselbe
    /// Liste. Kippt das, ist ein Bereich zu eng gewählt (oder ein Feld gar nicht deterministisch).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE02", "FILE03", 4711)]
    [InlineData("FILE22", "FILE23", 4711)]
    public void Pair_OtherSeed_SameExpectedList(string a, string b, int seed) => AssertExpected(Compare(a, b, seed));

    private static HokCompareReport Compare(string a, string b, int seed) =>
        HokCompare.Compare(OriginalImportTests.OriginalSave(a + ".HOK"), OriginalImportTests.OriginalSave(b + ".HOK"),
            ClubDatabase.Load(TestGameData.Load()), seed);

    private static void AssertExpected(HokCompareReport report)
    {
        Assert.True(report.PlayError is null, report.PlayError);
        var expected = Expected[report.Pair];
        var deviations = report.Deviations.ToList();
        var unexpected = deviations.Where(d => !expected.Any(e => HokCompareClasses.Matches(d.Path, [e.Pattern]))).ToList();
        Assert.True(unexpected.Count == 0,
            $"{report.Pair}: {unexpected.Count} neue Abweichung(en):\n" + string.Join("\n", unexpected.Take(20)));
        var stale = expected.Where(e => !deviations.Any(d => HokCompareClasses.Matches(d.Path, [e.Pattern]))).ToList();
        Assert.True(stale.Count == 0,
            $"{report.Pair}: erwartete Abweichung trifft nicht mehr (behoben? Eintrag entfernen): " +
            string.Join(", ", stale.Select(s => $"{s.Pattern} (#{s.Issue})")));
    }
}
