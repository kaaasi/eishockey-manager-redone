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
/// Paare ohne Eingriff: FILE02→03, FILE13→14 (bis auf die Aufstellung von Hand, <see cref="HandLineupFile13"/>), FILE16A→17, FILE20→21, FILE22→23, FILE24→25, FILE30→31, FILE38→39→40; mit bekanntem
/// Eingriff (nur übrige Felder, <see cref="HokCompareClasses.Interventions"/>): FILE44→46, FILE18→48. Saat 1993 (Werkzeugvorgabe);
/// zufallsabhängige Felder sind mit Bereichen und Invarianten geprüft, ein anderer Zufall darf das Ergebnis nicht kippen
/// (<see cref="Pair_OtherSeed_SameExpectedList"/>, alle Paare mit fünf weiteren Saaten).
/// </remarks>
public class AbgleichOriginalPaareTests
{
    private const int MoneyFile13 = 734, Strain = 730;

    /// <summary>
    /// Kein Befund (#732, abgleich-rest Teil 3): Im Modus „nur Weltkugel“ steht ein Spieler im Kader von Satz 0 und im Nationalkader
    /// (Satz 5) mit je eigenen Werten; das Remake führt einen Wertesatz je Spieler und nimmt im Modus 1 die des Satzes 5
    /// (<c>OriginalImport.National.cs</c>, [APPROX]). Begründung am Eintrag.
    /// </summary>
    private const int NationalValueSet = 0;

    /// <summary>Zufall, kein Befund: Begründung am Eintrag (nur optionale Muster).</summary>
    private const int Chance = 0;

    /// <summary>
    /// Eingriff, kein Befund (#733, abgleich-fix): FILE13→14 schaltet „automatisch aufstellen“ (DGROUP 0x375/0x376 `01 01` →
    /// `00 00`) ab. [ORIGINAL] Ohne Nutzerhandlung schreibt em.exe dort keine 0: Schreiber sind nur die Aufstellung bei einer
    /// Änderung von Hand (Datei 0x40ADE, 0x40DE4, Meldung „… ist deaktiviert“), der Taktikknopf (0x47F4E/0x49058) und der
    /// Tageslauf, der 0x376 aus 0x375 sichert (0x54FDC) und 0x375 nur für die Spiele auf 0 setzt (0x54FE8, vor dem nächsten
    /// Zug zurück, 0x54D9A/0x55307); Laden und Sichern kopieren den Block (0x5C3E3/0x5B9DD). Nach dem Laden zeigte die Aufstellung
    /// 5 Eingesetzte (`file13_2`), im DEB-Pokal standen weniger als 9 Spieler (Angabe Nutzer) – mit der Einstellung hätte
    /// 0x426BC voll aufgestellt. Folgen der Handaufstellung: Formwerte, verdeckte Werte einzelner Spieler.
    /// </summary>
    private const int HandLineupFile13 = 0;

    /// <summary>
    /// Erwartete Abweichung: Pfadmuster (<c>*</c> = beliebig) mit Issue. <paramref name="Delta"/>: jede Abweichung des Musters muss
    /// genau Remake − Original = Delta haben (sonst neu). <paramref name="Optional"/>: tritt nur bei manchen Saaten auf, kein Fehler,
    /// wenn das Muster nicht trifft. Muster nennen das belegte Feld, nicht den ganzen Kader (Review P3-1).
    /// <paramref name="RemakeMin"/>/<paramref name="RemakeMax"/>: der Remake-Wert der Abweichung muss im Bereich liegen (sonst neu).
    /// </summary>
    public sealed record Known(string Pattern, int Issue, int? Delta = null, bool Optional = false, int? RemakeMin = null,
        int? RemakeMax = null);

    /// <summary>
    /// Kein Originalfeld, kein Befund: Die Belastung steht im Original nur am Kaderplatz (+0x13), der Pool hat keine; der Import
    /// setzt für Poolspieler die Vorgabe 150, das Remake zieht beim Saisonwechsel 80–120 (Begründung am Eintrag).
    /// </summary>
    private const int NoOriginalField = 0;

    /// <summary>
    /// Saisonwechsel: Pool-Mittel Kondition/Technik (#731) behoben durch abgleich-fix (Werte bleiben beim Umbenennen am Platz);
    /// Belastung ohne Originalfeld. Tag 0 (#699 Punkt 2) behoben durch #725.
    /// </summary>
    private static readonly Known[] SeasonStart = [new("Invariante.PoolMittel.Strain", NoOriginalField)];

    /// <summary>
    /// Obergrenze der Belastung am Spielstand: Seit #730 (abgleich-fix, Training begrenzt vorzeichenbehaftet, 0x4BD56 `cbw`) steigt
    /// sie mit den Einsätzen bis über 127 und fällt beim nächsten Training auf 50; an allen Paaren und Prüfsaaten liegt das Remake
    /// bei höchstens 127. Ein Remake, in dem die Belastung nicht mehr auf 50 fällt, läuft bis 150 und wird rot (Review B1).
    /// </summary>
    private const int StrainSawtoothMax = 127;

    /// <summary>
    /// Zufall, kein Befund: Belastung der übrigen Spieler des Managervereins (Sägezahn, Phase je Spieler vom Zufall der Spiele
    /// abhängig, nur bei manchen Saaten außerhalb ±20). Spieler, die bei allen Prüfsaaten abweichen, stehen als Pflichteinträge in
    /// <see cref="StrainOf"/>. Zusätzlich geprüft: das Vereinsmittel (<c>AbgleichFixStrainTests.Pairs_ManagerClubStrainMean_LikeOriginal</c>).
    /// </summary>
    private static Known StrainSawtooth(string club) =>
        new($"Clubs[{club}].Players[*].Strain", Chance, Optional: true, RemakeMax: StrainSawtoothMax);

    /// <summary>
    /// Befund #730 (Restursache offen, Review B1): Pflicht für die Spieler, deren Belastung bei allen sechs Prüfsaaten (1993, 4711,
    /// 1, 2, 3, 77) außerhalb ±20 liegt. Im Original kippen die Vielspieler der Reihen 1/2 vor dem Training über 127 und stehen
    /// danach bei 50, im Remake fast nie (Verdacht: Belastungszuwachs je Reihe/Einsatz); <paramref name="min"/>–<paramref name="max"/>
    /// ist der Remake-Bereich über die Prüfsaaten (mit Rand).
    /// </summary>
    private static Known[] StrainOf(string club, int min, int max, params string[] players) =>
        [.. players.Select(p => new Known($"Clubs[{club}].Players[{p}].Strain", Strain, RemakeMin: min, RemakeMax: max))];

    private const string Landshut = "Clubs[7 EV LANDSHUT]";

    /// <summary>
    /// Erwartete Listen, erneuert gegen <c>main</c> 62e4927 (nach #725, #726, #727): Teamwerte #729 behoben durch #727, Tag 0
    /// #699 (Punkt 2) behoben durch #725, im Modus „nur Weltkugel“ (FILE20→21) durch abgleich-rest Teil 4.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Known[]> Expected =
        new Dictionary<string, Known[]>
        {
            // #730: HOCK Original 50, Remake 119–126 (alle Prüfsaaten); SIKORA (4 von 6, nicht 1993 und 3) und BRANDL im Sägezahn.
            ["FILE02→FILE03"] = [.. StrainOf("5 KÖLNER EC", 100, StrainSawtoothMax, "HOCK"), StrainSawtooth("5 KÖLNER EC"),
                // Zufall: Pool-Mittel der Belastung über zehn Saaten 143–146, nur Saat 1 bei 136,7 (seit #748 anderer Zufallsverlauf).
                new("Invariante.PoolMittel.Strain", Chance, Optional: true)],
            ["FILE13→FILE14"] = [.. SeasonStart, new("Managers[0].Money", MoneyFile13),
                new("Clubs[6 KREFELDER EV]._autoLineup", HandLineupFile13),
                new("Clubs[6 KREFELDER EV].TeamValues.*Form", HandLineupFile13),
                // Andere Aufstellung (Eingriff, #733) → andere Einsätze → verdeckte Werte einzelner Spieler (Saaten 2, 3, 4711).
                new("Clubs[6 KREFELDER EV].Players[*].HiddenSkill*", HandLineupFile13, Optional: true),
                // Kondition/Technik des Managervereins aus der Aufstellung (0x952C, #748 behoben durch abgleich-fix): Das Remake
                // rechnet sie aus seiner automatischen Aufstellung, das Original aus der von Hand (Eingriff, Abwehr-Technik 37 tiefer).
                new("Clubs[6 KREFELDER EV].TeamValues.*Condition", HandLineupFile13, Optional: true),
                new("Clubs[6 KREFELDER EV].TeamValues.*Technique", HandLineupFile13, Optional: true),
                // #730 Gegenrichtung: J.MEYER (Original 118, Remake 50–88) und KUHNEKATH (96, Remake 50–53) bei allen Prüfsaaten;
                // Anteil der Handaufstellung (Eingriff) nicht getrennt.
                .. StrainOf("6 KREFELDER EV", 50, 95, "J.MEYER", "KUHNEKATH"),
                StrainSawtooth("6 KREFELDER EV"),
                // Zufall: Wanderung über den Saisonwechsel, Sturm-Kondition WEIßWASSER über zehn Saaten −8 … 14, nur Saat 1 bei −19.
                new("Clubs[23 ES WEIßWASSER].TeamValues.AttackCondition", Chance, Optional: true)],
            // Zufall (#737, abgleich-fix): 200.000 DM Einzelprämien im Original (file16_9), im Remake bei keiner Saat; mit ihnen
            // im Bereich (AbgleichFixMoneyTests.File16A_MoneyDeviation_ComesFromSinglePremiums).
            ["FILE16A→FILE17"] = [.. SeasonStart, new("Managers[0].Money", Chance, Optional: true),
                // #730: A.RAUBAL, ZIMMERMANN (Original 50, Remake 96–122) bei allen Prüfsaaten, Gegenrichtung PYLE (Original 120,
                // Remake 50–88); TRAVNICEK (5 von 6, nicht 1993), NOACK (5 von 6, nicht 3), BLAZYCEK, DOLL, HORSTMANN, M.SCHMIDT
                // (4 von 6) im Sägezahn.
                .. StrainOf("14 SV BAYREUTH", 90, StrainSawtoothMax, "A.RAUBAL", "ZIMMERMANN"),
                .. StrainOf("14 SV BAYREUTH", 50, 95, "PYLE"),
                StrainSawtooth("14 SV BAYREUTH"),
                // Zufall: Die Trainingswirkung auf die verdeckten Werte sinkt mit der Belastung über 125 (Sägezahn, #730), einzelne
                // Spieler liegen über die Saison außerhalb des Bereichs (Saaten 1993, 3); Mittel wie im Original (H1 5,3–5,9 zu 5,7,
                // H2 4,7–5,3 zu 5,5).
                new("Clubs[14 SV BAYREUTH].Players[*].HiddenSkill*", Chance, Optional: true),
                // Zufall: LANDSHUT steigt im Original in die Oberliga ab (Teamwerte auf deren Obergrenze, alle −33), im Remake
                // nicht (Liga über den Saisonwechsel zufallsabhängig); Abstand 21–51 je Saat.
                new($"{Landshut}.TeamValues.*", Chance, Optional: true)],
            // „nur Weltkugel“ (#732): Alter und Laufzeit des Managerkaders behoben durch abgleich-fix (0x26F20 läuft im Modus 1 nicht,
            // AbgleichFixWorldOnlyTests); Gehalt, Preis und Konto durch abgleich-rest Teil 3 (Import des Kaders von Satz 0,
            // Tagesabschluss für Satz 0 in Sommerpause und WM-Schleife; AbgleichRestWorldOnlyTests). Teamwerte des Managervereins
            // (Original leer) seit #747 (abgleich-fix) auch im Remake leer.
            ["FILE20→FILE21"] = [.. SeasonStart,
                // Form und Belastung der Spieler, die zugleich im Nationalkader stehen (je nach Saat andere, Remake Belastung 70).
                new($"{Landshut}.Players[*].Form", NationalValueSet, Optional: true),
                new($"{Landshut}.Players[*].Strain", NationalValueSet, Optional: true)],
            ["FILE22→FILE23"] = [.. SeasonStart],
            // „automatisch aufstellen“ (#733) behoben durch abgleich-fix: Der Import liest das gesicherte Byte 0x376 (FILE24 `00 01`).
            ["FILE24→FILE25"] = [StrainSawtooth("5 KÖLNER EC")],
            ["FILE30→FILE31"] = [],
            ["FILE38→FILE39"] = [StrainSawtooth("5 KÖLNER EC")],
            ["FILE39→FILE40"] = [StrainSawtooth("5 KÖLNER EC")],
            // Formwerte KÖLN (#738) behoben durch abgleich-fix (Handbetrieb ohne Nachrücken, Import ohne Reihenspieler ohne
            // „eingesetzt“). Platz 196 KJELLBERG (#749) behoben durch abgleich-fix (Import: Besitzer Manager ohne Kader = vergeben).
            ["FILE44→FILE46"] = [.. SeasonStart],
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
        if (known.Delta is null && known.RemakeMin is null && known.RemakeMax is null)
            return true;
        if (!int.TryParse(row.Original, out var o) || !int.TryParse(row.Remake, out var r))
            return false;
        return (known.Delta is not { } delta || r - o == delta) && r >= (known.RemakeMin ?? int.MinValue)
            && r <= (known.RemakeMax ?? int.MaxValue);
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
