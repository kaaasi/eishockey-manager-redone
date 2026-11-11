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
/// (<see cref="Pair_OtherSeed_SameExpectedList"/>, alle Paare mit fünf weiteren Saaten).
/// </remarks>
public class AbgleichOriginalPaareTests
{
    private const int Strain = 730, PoolSeasonChange = 731, WorldOnlyManagerSlot = 732, AutoLineupFlag = 733, MoneyFile13 = 734,
        DayZero = 699, FormFile44 = 738, ManagerTeamValues = 748;

    /// <summary>Zufall, kein Befund: Begründung am Eintrag (nur optionale Muster).</summary>
    private const int Chance = 0;

    /// <summary>
    /// Erwartete Abweichung: Pfadmuster (<c>*</c> = beliebig) mit Issue. <paramref name="Delta"/>: jede Abweichung des Musters muss
    /// genau Remake − Original = Delta haben (sonst neu). <paramref name="Optional"/>: tritt nur bei manchen Saaten auf, kein Fehler,
    /// wenn das Muster nicht trifft. Muster nennen das belegte Feld, nicht den ganzen Kader (Review P3-1).
    /// </summary>
    public sealed record Known(string Pattern, int Issue, int? Delta = null, bool Optional = false);

    /// <summary>Saisonwechsel: Pool nach dem Saisonwechsel (#731). Tag 0 (#699 Punkt 2) behoben durch #725.</summary>
    private static readonly Known[] SeasonStart = [new("Invariante.PoolMittel.*", PoolSeasonChange)];

    /// <summary>Belastung #730: Pflicht für die Spieler, die über alle sechs Saaten abweichen (Original 50), sonst optional.</summary>
    private static Known[] StrainOf(string club, params string[] players) =>
        [.. players.Select(p => new Known($"Clubs[{club}].Players[{p}].Strain", Strain))];

    private static Known[] StrainOptional(string club, params string[] players) =>
        [.. players.Select(p => new Known($"Clubs[{club}].Players[{p}].Strain", Strain, Optional: true))];

    private const string Landshut = "Clubs[7 EV LANDSHUT]";

    /// <summary>
    /// Erwartete Listen, erneuert gegen <c>main</c> 62e4927 (nach #725, #726, #727): Teamwerte #729 behoben durch #727, Tag 0
    /// #699 (Punkt 2) behoben durch #725 außer im Modus „nur Weltkugel“ (FILE20→21).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Known[]> Expected =
        new Dictionary<string, Known[]>
        {
            ["FILE02→FILE03"] = [.. StrainOf("5 KÖLNER EC", "HOCK", "MENDE", "SIKORA")],
            ["FILE13→FILE14"] = [.. SeasonStart, new("Managers[0].Money", MoneyFile13),
                new("Clubs[6 KREFELDER EV]._autoLineup", AutoLineupFlag), new("Clubs[6 KREFELDER EV].TeamValues.*Form", AutoLineupFlag),
                // Andere Aufstellung (#733) → andere Einsätze → verdeckte Werte einzelner Spieler (Saaten 2, 3, 4711).
                new("Clubs[6 KREFELDER EV].Players[*].HiddenSkill*", AutoLineupFlag, Optional: true),
                // Kondition/Technik des Managervereins aus der Aufstellung (#748): 16–43 tiefer, über ±35 nur bei manchen Saaten.
                new("Clubs[6 KREFELDER EV].TeamValues.*Condition", ManagerTeamValues, Optional: true),
                new("Clubs[6 KREFELDER EV].TeamValues.*Technique", ManagerTeamValues, Optional: true),
                // Zufall: Endrunde KREFELD mit 9 statt 3 Spielen (Saaten 1993, 77: ClubGamesCarry 9), Belastung 25–40 höher.
                new("Clubs[6 KREFELDER EV].Players[*].Strain", Chance, Optional: true)],
            // Zufall (#737, abgleich-fix): 200.000 DM Einzelprämien im Original (file16_9), im Remake bei keiner Saat; mit ihnen
            // im Bereich (AbgleichFixMoneyTests.File16A_MoneyDeviation_ComesFromSinglePremiums).
            ["FILE16A→FILE17"] = [.. SeasonStart, new("Managers[0].Money", Chance, Optional: true),
                .. StrainOf("14 SV BAYREUTH", "A.RAUBAL", "BLAZYCEK", "DOLL", "HADRASCHEK", "HORSTMANN", "M.SCHMIDT", "NOACK",
                    "TRAVNICEK", "ZIMMERMANN"),
                // Saat 1993 seit #725 im Bereich ±20, die übrigen fünf Saaten nicht.
                .. StrainOptional("14 SV BAYREUTH", "LANGLOIS", "MEIXNER", "MORRISON", "PESKA", "PROKES", "PYLE"),
                // Zufall: LANDSHUT steigt im Original in die Oberliga ab (Teamwerte auf deren Obergrenze, alle −33), im Remake
                // nicht (Liga über den Saisonwechsel zufallsabhängig); Abstand 21–51 je Saat.
                new($"{Landshut}.TeamValues.*", Chance, Optional: true)],
            // „nur Weltkugel“ (#732): Managerkader altert im Remake ein Jahr, Verträge ein Jahr kürzer; das Original bewertet Gehalt
            // und Preis neu (neue Tendenz, Alter und Vertrag unverändert) – sichtbar seit dem Import der Tendenz (#726/#727).
            ["FILE20→FILE21"] = [new("DayOpened", DayZero), new("Invariante.PoolMittel.*", PoolSeasonChange),
                new($"{Landshut}.Players[*].Age", WorldOnlyManagerSlot, Delta: 1),
                new($"{Landshut}.Players[*].ContractYears", WorldOnlyManagerSlot, Delta: -1),
                new($"{Landshut}.Players[*].MonthlySalary", WorldOnlyManagerSlot),
                new($"{Landshut}.Players[*].PurchasePrice", WorldOnlyManagerSlot),
                // Form und Belastung einzelner Spieler (je nach Saat andere, Remake Belastung 70).
                new($"{Landshut}.Players[*].Form", WorldOnlyManagerSlot, Optional: true),
                new($"{Landshut}.Players[*].Strain", WorldOnlyManagerSlot, Optional: true),
                // Teamwerte des Managervereins (Original leer) seit #747 (abgleich-fix) auch im Remake leer.
                new("Managers[0].Money", WorldOnlyManagerSlot)],
            ["FILE22→FILE23"] = [.. SeasonStart],
            ["FILE24→FILE25"] = [.. StrainOf("5 KÖLNER EC", "BRANDL", "KWASIGROCH"),
                new("Clubs[5 KÖLNER EC]._autoLineup", AutoLineupFlag)],
            ["FILE30→FILE31"] = [],
            ["FILE38→FILE39"] = [.. StrainOf("5 KÖLNER EC", "DOROCHIN", "LIEBSCH")],
            ["FILE39→FILE40"] = [],
            // Formwerte KÖLN im Remake bei allen Saaten 10–17 höher (#738), bei 1993 knapp im Bereich.
            // Platz 196 KJELLBERG (#749) behoben durch abgleich-fix (Import: Besitzer Manager ohne Kader = vergeben).
            ["FILE44→FILE46"] = [.. SeasonStart, new("Clubs[5 KÖLNER EC].TeamValues.*Form", FormFile44, Optional: true)],
            // „nur Weltkugel“: Teamwerte nicht mehr neu gewürfelt (#747 behoben durch abgleich-fix).
            ["FILE18→FILE48"] = [],
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

    /// <summary>Alle Paare mit den Saaten der Bereichsprobe (Review P3-2): 4711, 1, 2, 3, 77.</summary>
    public static TheoryData<string, string, int> PairsOtherSeeds()
    {
        var data = new TheoryData<string, string, int>();
        foreach (var pair in Expected.Keys)
        {
            var files = pair.Split('→');
            foreach (var seed in OtherSeeds)
                data.Add(files[0], files[1], seed);
        }
        return data;
    }

    private static readonly int[] OtherSeeds = [4711, 1, 2, 3, 77];

    [GameDataTheory]
    [MemberData(nameof(Pairs))]
    public void Pair_OnlyKnownDeviations(string a, string b) => AssertExpected(Compare(a, b, HokCompare.DefaultSeed));

    /// <summary>
    /// Bereiche gegen Zufall: jedes Paar mit fünf weiteren Saaten ergibt dieselbe Liste (Muster mit <see cref="Known.Optional"/>
    /// ausgenommen). Kippt das, ist ein Bereich zu eng gewählt (oder ein Feld gar nicht deterministisch).
    /// </summary>
    [GameDataTheory]
    [MemberData(nameof(PairsOtherSeeds))]
    public void Pair_OtherSeed_SameExpectedList(string a, string b, int seed) => AssertExpected(Compare(a, b, seed));

    /// <summary>
    /// Gegenprobe Geld (Review P2-1): FILE22→23 nachspielen und die in der Zeit gebuchten Gehälter wieder herausrechnen (wie
    /// „<c>GameState.Book</c> bucht keine Gehälter“). Der Kontostand muss abweichen; ohne die Herausrechnung nicht.
    /// </summary>
    [GameDataFact]
    public void Money_WithoutSalaries_IsDeviation()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var start = HokCompare.Import(OriginalImportTests.OriginalSave("FILE22.HOK"), database, HokCompare.DefaultSeed);
        var target = HokCompare.Import(OriginalImportTests.OriginalSave("FILE23.HOK"), database, HokCompare.DefaultSeed);
        Assert.NotEqual(start.Season, target.Season);
        var manager = start.Managers[0];
        var bookedBefore = start.Finance(manager).Book.Current(FinanceCategory.Gehaelter);
        var before = HokCompare.Values(start);
        Assert.Null(HokCompare.PlayTo(start, target));

        var money = Row(HokCompare.Rows(target, start, crossesSeason: true, [], before));
        Assert.False(money.Deviation, money.ToString());

        // Genau ein Saisonwechsel: Gehälter der alten Saison stehen im Vorjahr des Finanzbuchs, die der neuen im laufenden Jahr.
        var book = start.Finance(manager).Book;
        var salaries = book.Previous(FinanceCategory.Gehaelter) - bookedBefore + book.Current(FinanceCategory.Gehaelter);
        Assert.True(salaries < -500_000, $"Gehälter FILE22→23: {salaries}");
        manager.Money -= salaries;
        money = Row(HokCompare.Rows(target, start, crossesSeason: true, [], before));
        Assert.True(money.Deviation, money.ToString());

        static HokCompareRow Row(IEnumerable<HokCompareRow> rows) => rows.Single(r => r.Path == "Managers[0].Money");
    }

    private static HokCompareReport Compare(string a, string b, int seed) =>
        HokCompare.Compare(OriginalImportTests.OriginalSave(a + ".HOK"), OriginalImportTests.OriginalSave(b + ".HOK"),
            ClubDatabase.Load(TestGameData.Load()), seed);

    /// <summary>Passt die Abweichung zum Muster (und zum festen Abstand)?</summary>
    public static bool Covers(Known known, HokCompareRow row)
    {
        if (!HokCompareClasses.Matches(row.Path, [known.Pattern]))
            return false;
        if (known.Delta is not { } delta)
            return true;
        return int.TryParse(row.Original, out var o) && int.TryParse(row.Remake, out var r) && r - o == delta;
    }

    private static void AssertExpected(HokCompareReport report)
    {
        Assert.True(report.PlayError is null, report.PlayError);
        var expected = Expected[report.Pair];
        var deviations = report.Deviations.ToList();
        var unexpected = deviations.Where(d => !expected.Any(e => Covers(e, d))).ToList();
        Assert.True(unexpected.Count == 0,
            $"{report.Pair}: {unexpected.Count} neue Abweichung(en):\n" + string.Join("\n", unexpected.Take(20)));
        var stale = expected.Where(e => !e.Optional && !deviations.Any(d => Covers(e, d))).ToList();
        Assert.True(stale.Count == 0,
            $"{report.Pair}: erwartete Abweichung trifft nicht mehr (behoben? Eintrag entfernen): " +
            string.Join(", ", stale.Select(s => $"{s.Pattern} (#{s.Issue})")));
    }
}
