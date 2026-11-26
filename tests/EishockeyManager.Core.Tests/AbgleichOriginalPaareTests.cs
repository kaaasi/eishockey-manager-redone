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
/// Paare ohne Eingriff: FILE02→03, FILE02→50 und FILE02→51 (DOSBox `runde-17`, FILE51 mit Torwartwechsel im Spiel), FILE50→61→63
/// und FILE50→66…70 (DOSBox `runde-19`), FILE16A→17,
/// FILE20→21, FILE22→23, FILE24→25, FILE30→31, FILE38→39→40; mit bekanntem
/// Eingriff (nur übrige Felder, <see cref="HokCompareClasses.Interventions"/>): FILE13→14 (Sitzungszustand, Aufstellung von Hand;
/// #733, #734), FILE44→46, FILE18→48. Saat 1993 (Werkzeugvorgabe);
/// zufallsabhängige Felder sind mit Bereichen und Invarianten geprüft, ein anderer Zufall darf das Ergebnis nicht kippen
/// (<see cref="Pair_OtherSeed_SameExpectedList"/>, alle Paare mit fünf weiteren Saaten).
/// </remarks>
public class AbgleichOriginalPaareTests
{
    private const int Strain = 730;

    /// <summary>
    /// Kein Befund (#732, abgleich-rest Teil 3): Im Modus „nur Weltkugel“ steht ein Spieler im Kader von Satz 0 und im Nationalkader
    /// (Satz 5) mit je eigenen Werten; das Remake führt einen Wertesatz je Spieler und nimmt im Modus 1 die des Satzes 5
    /// (<c>OriginalImport.National.cs</c>, [APPROX]). Begründung am Eintrag.
    /// </summary>
    private const int NationalValueSet = 0;

    /// <summary>Zufall, kein Befund: Begründung am Eintrag (nur optionale Muster).</summary>
    private const int Chance = 0;

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
    /// Zufall, kein Befund (Spur tagesabschluss Teil 2, R26-E1 = B): wöchentliche Stadionschäden 0x3CFA0 (Datei 0x3D330; je Woche und
    /// Manager etwa 1 % bei Level 3) – über eine Sommerpause bei manchen Saaten eine Stufe oder Plätze weniger als im Original.
    /// </summary>
    private static readonly Known[] StadiumDamage =
    [
        new("Managers[*].FinanceData.Stadium._levels[*]", Chance, Optional: true),
        new("Managers[*].FinanceData.Stadium.Seats", Chance, Optional: true),
        new("Managers[*].FinanceData.Stadium.Standing", Chance, Optional: true),
    ];

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
    /// Ehemals Befund #730, im Bereich geschlossen R19 (Review #870 P3-2): Remake-Bereich einzelner Spieler, deren Belastung bei den
    /// meisten der sechs Prüfsaaten (1993, 4711, 1, 2, 3, 77) außerhalb ±20 liegt; nur noch im Drift-Wächter
    /// <see cref="Strain_DeviatesAtMostSeeds_DriftGuard"/>, nicht als Pflichteintrag in <see cref="Expected"/>. Im Original kippen die Vielspieler der Reihen 1/2 vor dem Training über 127 und stehen
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
            // #730: HOCK Original 50, Remake 116–127, seit #784 (Gast der Spielvorbereitung nach dem Heimverein, Runde 17) nur bei
            // 5 von 6 Saaten (nicht 2) → Sägezahn wie SIKORA und BRANDL; Pflicht über die Saaten in Strain_DeviatesAtMostSeeds_DriftGuard.
            ["FILE02→FILE03"] = [StrainSawtooth("5 KÖLNER EC"),
                // Zufall: Pool-Mittel der Belastung über zehn Saaten 143–146, nur Saat 1 bei 136,7 (seit #748 anderer Zufallsverlauf).
                new("Invariante.PoolMittel.Strain", Chance, Optional: true),
                // Zufall: Wanderung der Teamwerte des Leerplatzes, seit #784 nur Saat 3 (12 → −4).
                new("Clubs[51 LEER4].TeamValues.AttackCondition", Chance, Optional: true),
                // Runde 21, Spur zufallsstrom Teil 4 (R21-D2 = A, Erstreview #909 P3-1: als einziges Muster nötig, Base-Liste sonst
                // grün): Mit Pokalen und Finanzen im gemeinsamen Strom fallen bis zur Wanderung andere Würfe; Leerplatz LEER4 liegt
                // bei den Saaten 3 und 77 in der Abwehr-Kondition außerhalb (Original 10, Remake −6) – dasselbe Zufallsmuster wie
                // LEER4.AttackCondition oben, kein neu eingetragener Originalwert.
                new("Clubs[51 LEER4].TeamValues.DefenseCondition", Chance, Optional: true),
                // Runde 22, Spur wettbewerbe-regeln Teil 2 (R21-D2 = A, R15-E2 = C): Mit der Jugend nach den Menüs (A384/A670, Datei
                // 0x55003–0x5502C) fallen bis zur Wanderung andere Würfe; SC PREUSSEN BERLIN liegt bei Saat 4711 in der Sturm-Kondition
                // außerhalb (Original 1, Remake −15) – Wanderung wie bei LEER4 oben, kein neu eingetragener Originalwert.
                new("Clubs[1 SC PREUSSEN BERLIN].TeamValues.AttackCondition", Chance, Optional: true),
                // Spur wettbewerbe-regeln Fixlauf Rebase auf main f7f0410 (Runde 22, R21-D2 = A; vorläufig, Entscheidung Leitstelle –
                // `spur-wartet`): Mit den Strömen von kader-regeln, transfer-pool und einzelpunkte fallen bis zur Wanderung andere Würfe;
                // Leerplatz LEER3 liegt bei Saat 4711 in der Abwehr-Kondition außerhalb (Original 18, Remake 1) – Wanderung wie LEER4 oben,
                // kein neu eingetragener Originalwert. f7f0410 und 3ca753a je allein grün.
                new("Clubs[50 LEER3].TeamValues.DefenseCondition", Chance, Optional: true),
                // Runde 25, Spur saison-geld Teil 2 (F08 A512, R21-D2 = A, R15-E2 = C): Die Baufirmen ziehen im Tagesabschluss 11 Würfe je
                // Manager (Datei 0x3ED19–0x3ED48), andere Lage der Würfe bis zur Wanderung; ESC FRANKFURT liegt bei der Standardsaat in der
                // Abwehr-Kondition außerhalb (Original −9, Remake 9) – Wanderung wie LEER4 oben, kein neu eingetragener Originalwert.
                new("Clubs[13 ESC FRANKFURT].TeamValues.DefenseCondition", Chance, Optional: true)],
            // Konto, Automatik, Teamwerte und verdeckte Werte KREFELD: Eingriff/Sitzungszustand (#734, HokCompareClasses.Interventions).
            ["FILE13→FILE14"] = [.. SeasonStart,
                // #730 Gegenrichtung: KUHNEKATH (Original 96, Remake 50–51) seit #784 nur bei 5 von 6 Saaten (nicht 1; Pflicht über
                // die Saaten in Strain_DeviatesAtMostSeeds_DriftGuard), J.MEYER
                // (118, Remake 50–77) seit #772 nicht bei allen → beide Sägezahn; Anteil der Handaufstellung (Eingriff) nicht getrennt.
                // kader-rest (A130, Merge mit main 12.11.2026), vermutete Ursache bei KUHNEKATH (nicht getrennt): Der Import behält die
                // Bezugsstärke (Club.ReferenceRating) des verworfenen erzeugten Managerkaders, dessen Startbelastung A130 ändert.
                StrainSawtooth("6 KREFELDER EV"),
                // Zufall: Wanderung über den Saisonwechsel, Sturm-Kondition WEIßWASSER über zehn Saaten −8 … 14, nur Saat 1 bei −19.
                new("Clubs[23 ES WEIßWASSER].TeamValues.AttackCondition", Chance, Optional: true),
                // Zufall (Runde 17, wettbewerbe-rest Teil 2, A004): Die Auslosung des Europapokals am Saisonwechsel zieht mit 0x4D316
                // andere Gegner (andere Spiele, anderer Zufallsverlauf); danach Sturm-Kondition RATINGEN nur bei der Standardsaat außerhalb
                // (Original 10, Remake −26), die fünf Prüfsaaten treffen nicht.
                new("Clubs[10 EC RATINGEN].TeamValues.AttackCondition", Chance, Optional: true),
                // Zufall (Runde 17, wettbewerbe-rest Teil 3, A113): Liga-Spielplan über die Satzfolge (Datei 0x4E7C7) – andere
                // Paarungen, anderer Zufallsverlauf; danach Abwehr-Kondition WEIßWASSER bei der Standardsaat außerhalb (Original 20,
                // Remake −22), wie die Sturm-Kondition oben ein Wert der Wanderung.
                new("Clubs[23 ES WEIßWASSER].TeamValues.DefenseCondition", Chance, Optional: true)],
            // Zufall (#737, abgleich-fix): 200.000 DM Einzelprämien im Original (file16_9), im Remake bei keiner Saat; mit ihnen
            // im Bereich (AbgleichFixMoneyTests.File16A_MoneyDeviation_ComesFromSinglePremiums).
            ["FILE16A→FILE17"] = [.. SeasonStart, new("Managers[0].Money", Chance, Optional: true),
                // Ehemals #730 (im Bereich geschlossen R19, Review #870 P3-2): ZIMMERMANN (Original 50, Remake 97–122, 39 von 40 Saaten)
                // nur Sägezahn, der Bereich über die Saaten im Drift-Wächter Strain_DeviatesAtMostSeeds_DriftGuard; seit #772 A.RAUBAL (5 von 6, nicht 2) und
                // Gegenrichtung PYLE (Original 120, Remake 87–115, 3 von 6) im Sägezahn, ebenso TRAVNICEK, NOACK, BLAZYCEK, DOLL,
                // HORSTMANN, M.SCHMIDT (4–5 von 6).
                StrainSawtooth("14 SV BAYREUTH"),
                // Zufall: Die Trainingswirkung auf die verdeckten Werte sinkt mit der Belastung über 125 (Sägezahn, #730), einzelne
                // Spieler liegen über die Saison außerhalb des Bereichs (Saaten 1993, 3); Mittel wie im Original (H1 5,3–5,9 zu 5,7,
                // H2 4,7–5,3 zu 5,5).
                new("Clubs[14 SV BAYREUTH].Players[*].HiddenSkill*", Chance, Optional: true),
                // Zufall: LANDSHUT steigt im Original in die Oberliga ab (Teamwerte auf deren Obergrenze, alle −33), im Remake
                // nicht (Liga über den Saisonwechsel zufallsabhängig); Abstand 21–51 je Saat.
                new($"{Landshut}.TeamValues.*", Chance, Optional: true),
                // Zufall (Runde 25, Spur saison-geld Teil 1, T03/A829, R21-D2 = A): Mit dem Markt-Takt random(0, 3) je Markt-Platz
                // (Datei 0x07215) geht bei Saat 4711 im Remake EC HEDOS MÜNCHEN als Partner des Zwangsabsteigers in die Oberliga
                // (Doppelwechsel, Teamwerte an deren Grenze, alle −43), im Original bleibt er in der Bundesliga; wie LANDSHUT ein
                // Zufallsfeld der Aufstiegsrunde, kein fester Befund.
                new("Clubs[9 EC HEDOS MÜNCHEN].TeamValues.*", Chance, Optional: true),
                // Zufall (Runde 25, Spur saison-geld Teil 3, K11, R21-D2 = A): Mit dem Rollenwert aus dem Stand (Pool +0x18, Torwürfe
                // Datei 0x5077–0x52CB) verläuft der Strom anders; bei Prüfsaat 77 geht EC RATINGEN in die Oberliga (Teamwerte an deren
                // Grenze, alle −35), im Original nicht; wie LANDSHUT und HEDOS ein Zufallsfeld der Aufstiegsrunde, kein fester Befund.
                new("Clubs[10 EC RATINGEN].TeamValues.*", Chance, Optional: true),
                // Zufall (Runde 22, Spur transfer-pool, A864/A391, R21-D2 = A): Nur mit der Saat 1993 weicht ein Teamwert von MEMMINGEN
                // ab (Angriff Kondition −11 im Original, 27 im Remake); mit den fünf weiteren Saaten (Pair_OtherSeed_SameExpectedList)
                // nicht. Die Wanderung der Teamwerte zieht aus dem gemeinsamen Strom, den die Verletzung außerhalb des Spiels
                // (Datei 0x07462–0x07548) verschiebt; wie LANDSHUT ein Zufallsfeld, kein fester Befund.
                new("Clubs[18 SC MEMMINGEN].TeamValues.AttackCondition", Chance, Optional: true)],
            // „nur Weltkugel“ (#732): Alter und Laufzeit des Managerkaders behoben durch abgleich-fix (0x26F20 läuft im Modus 1 nicht,
            // AbgleichFixWorldOnlyTests); Gehalt, Preis und Konto durch abgleich-rest Teil 3 (Import des Kaders von Satz 0,
            // Tagesabschluss für Satz 0 in Sommerpause und WM-Schleife; AbgleichRestWorldOnlyTests). Teamwerte des Managervereins
            // (Original leer) seit #747 (abgleich-fix) auch im Remake leer.
            ["FILE20→FILE21"] = [.. SeasonStart, .. StadiumDamage,
                // Form und Belastung der Spieler, die zugleich im Nationalkader stehen (je nach Saat andere, Remake Belastung 70).
                new($"{Landshut}.Players[*].Form", NationalValueSet, Optional: true),
                new($"{Landshut}.Players[*].Strain", NationalValueSet, Optional: true)],
            ["FILE22→FILE23"] = [.. SeasonStart],
            // „automatisch aufstellen“ (#733) behoben durch abgleich-fix: Der Import liest das gesicherte Byte 0x376 (FILE24 `00 01`).
            ["FILE24→FILE25"] = [StrainSawtooth("5 KÖLNER EC")],
            ["FILE30→FILE31"] = [],
            // Spur wettbewerbe-regeln Fixlauf Rebase auf main f7f0410 (Runde 22, R21-D2 = A; vorläufig, Entscheidung Leitstelle –
            // `spur-wartet`): Zufall – bei Saat 2 bekommt KJELLBERG (KÖLN) im Tageslauf 0x07169 ein Verlängerungsangebot (Würfe
            // random(0, OfferLimit) und random(0, 3) = 0, Datei 0x076FA–0x07951; Transfer.Squads.cs), das Original nicht; andere Lage der
            // Würfe durch den gemeinsamen Strom. Nur Saat 2, f7f0410 und 3ca753a je allein grün. Kein neu eingetragener Originalwert.
            ["FILE38→FILE39"] = [StrainSawtooth("5 KÖLNER EC"),
                new("Clubs[5 KÖLNER EC].Players[KJELLBERG].Notice", Chance, Optional: true),
                new("Clubs[5 KÖLNER EC].Players[KJELLBERG].NoticeYears", Chance, Optional: true),
                // Runde 25, Spur saison-geld Teil 2 (F08 A512, R21-D2 = A, R15-E2 = C): Mit 11 Würfen der Baufirmen je Manager und Tag
                // (Datei 0x3ED19–0x3ED48) fallen die Würfe der Tagesläufe anders; bei der Standardsaat liegen Sturm-Kondition und -Technik
                // von KÖLN außerhalb (Original −11/−10, Remake −29/−28) – Zufall der Aufstellung, kein neu eingetragener Originalwert.
                new("Clubs[5 KÖLNER EC].TeamValues.AttackCondition", Chance, Optional: true),
                new("Clubs[5 KÖLNER EC].TeamValues.AttackTechnique", Chance, Optional: true)],
            ["FILE39→FILE40"] = [StrainSawtooth("5 KÖLNER EC")],
            // Formwerte KÖLN (#738) behoben durch abgleich-fix (Handbetrieb ohne Nachrücken, Import ohne Reihenspieler ohne
            // „eingesetzt“). Platz 196 KJELLBERG (#749) behoben durch abgleich-fix (Import: Besitzer Manager ohne Kader = vergeben).
            ["FILE44→FILE46"] = [.. SeasonStart, .. StadiumDamage],
            // „nur Weltkugel“: Teamwerte nicht mehr neu gewürfelt (#747 behoben durch abgleich-fix).
            ["FILE18→FILE48"] = [],
            // DOSBox-Aufnahme `runde-17` (#730, Spur abgleich-dosbox Teil 1): 1. Spieltag aus FILE02 im Live-Spiel ohne Mausklick
            // (FILE50) bzw. mit Klicks auf die Reihenknöpfe und Torwartwechsel CISSEWSKI → HEISS in Minute 26 (FILE51, Klicks laut
            // Nutzer ohne Wirkung). Nach einem Spieltag keine Abweichung, auch nicht in der Belastung (alle Kölner ±7 bei allen
            // Prüfsaaten); Reihen je Minute: AbgleichDosboxLinesTests.
            ["FILE02→FILE50"] = [],
            ["FILE02→FILE51"] = [],
            // DOSBox `runde-19` (#730, Spur abgleich-dosbox-19 Teil 1): Spieltag 2 (FILE50→61, Auswärtsspiel EC RATINGEN) und 3
            // (FILE61→63, Heimspiel ESV KAUFBEUREN) desselben Laufs ohne Mausklick. Nur der Sägezahn weicht ab (MENDE 123→127 im
            // Original, im Remake bei 3 von 6 Saaten über 127 → 50; SIKORA, BRANDL je einmal); Reihen je Minute:
            // AbgleichDosboxLinesTests, Treffer je Spiel: docs/analyse/abgleich-dosbox.md §4.
            ["FILE50→FILE61"] = [StrainSawtooth("5 KÖLNER EC")],
            ["FILE61→FILE63"] = [StrainSawtooth("5 KÖLNER EC")],
            // DOSBox `runde-19` Sitzung 4 (#730, Fortsetzung 1, R19-E4 = A): Spieltag 2 aus FILE50 in fünf weiteren Läufen ohne
            // Mausklick (FILE66–70). Nur der Sägezahn weicht ab (MENDE kippt in FILE66–68 auf 50, SIKORA im Remake je Saat);
            // Treffer je Lauf: AbgleichDosboxStrainHitsTests.
            ["FILE50→FILE66"] = [StrainSawtooth("5 KÖLNER EC")],
            ["FILE50→FILE67"] = [StrainSawtooth("5 KÖLNER EC")],
            ["FILE50→FILE68"] = [StrainSawtooth("5 KÖLNER EC")],
            ["FILE50→FILE69"] = [StrainSawtooth("5 KÖLNER EC")],
            ["FILE50→FILE70"] = [StrainSawtooth("5 KÖLNER EC")],
            // DOSBox `runde-17` (#773, Teil 2): erster Spieltag eines neuen Spiels (ES WEIßWASSER). #730 auch hier: REZNICEK
            // (Abwehr 2, FILE52 Belastung 119) kippt im Original über 127 (→ 50), im Remake bei 5 von 6 Prüfsaaten 120–125;
            // Einsatz und Strafminuten wie im Original (AbgleichDosboxPenaltyTests).
            // Mit den 5-Minuten-Strafen nach dem lrandom-Zyklus (#773) kippt REZNICEK bei Saat 2 wie im Original → Sägezahn, Pflicht über
            // die Saaten in Strain_DeviatesAtMostSeeds_DriftGuard.
            ["FILE52→FILE53"] = [StrainSawtooth("23 ES WEIßWASSER")],
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
    /// #730 (Review R17 P3-1, <c>docs/reviews/2026-11-12-pruefung-karte-r17.md</c>): Seit #784 weichen HOCK (FILE02→03, Original 50)
    /// und KUHNEKATH (FILE13→14, Original 96) nicht mehr bei allen sechs Prüfsaaten ab und stehen deshalb in
    /// <see cref="Expected"/> nur im Sägezahn (optional). Pflicht bleibt der Bereich über die Saaten: bei mindestens
    /// <paramref name="minSeeds"/> der sechs Saaten (1993, 4711, 1, 2, 3, 77) liegt die Abweichung im Remake-Bereich. Ein weiteres
    /// Abdriften (andere Richtung, anderer Bereich) oder ein Beheben von #730 macht den Fall rot. Drift-Wächter, ehemals #730, im
    /// Bereich geschlossen R19 (Review #870 P3-2); ZIMMERMANN (FILE16A→17) seit Runde 20 nur noch hier statt als Pflichteintrag.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE02", "FILE03", "5 KÖLNER EC", "HOCK", 90, StrainSawtoothMax, 5)]
    // Runde 18, Spur abgleich-dosbox Teil 2 (#773, R15-E2 = C): KUHNEKATH nur noch bei 3 von 6 Saaten (1993, 3, 77) – mit den
    // 5-Minuten-Strafen nach dem lrandom-Zyklus anderer Spielverlauf über den Saisonwechsel; Richtung und Bereich unverändert.
    [InlineData("FILE13", "FILE14", "6 KREFELDER EV", "KUHNEKATH", 40, 75, 3)]
    // DOSBox `runde-17` (abgleich-dosbox Teil 2): REZNICEK (Original 50) im Remake 120–125 bei 5 von 6 Saaten (nicht 2).
    [InlineData("FILE52", "FILE53", "23 ES WEIßWASSER", "REZNICEK", 110, StrainSawtoothMax, 5)]
    // Ehemals Pflichteintrag in Expected (Review #870 P3-2): ZIMMERMANN (Original 50) im Remake 97–122 bei 39 von 40 Saaten; seit
    // #872 (Reihen aus dem Vorspiel, Runde 20 Teil 2) bei 5 von 6 Prüfsaaten (nicht 77), wie HOCK und REZNICEK. Bereich (Review #881
    // P3-1, #882: auf 90–110 eng genug, dass eine Drift bis zum Sägezahn-Maximum auffällt): Zum Review lagen die Abweichungen bei
    // 96–99, mit dem gemeinsamen Zufallsstrom (Runde 21, zufallsstrom) bei den Prüfsaaten 1993, 4711, 1, 2, 3, 77 jetzt bei 99, 97, 102,
    // 122, 108, 115; 90–110 träfe nur 4 von 6. Bereich daher 90–125 (Maximum der Messung 122 mit Rand 3); die Untergrenze 100 macht
    // den Wächter rot. Eine Drift über 125 (Sägezahn-Maximum 127) fällt auf.
    [InlineData("FILE16A", "FILE17", "14 SV BAYREUTH", "ZIMMERMANN", 90, 125, 5)]
    public void Strain_DeviatesAtMostSeeds_DriftGuard(string a, string b, string club, string player, int min, int max, int minSeeds)
    {
        var known = StrainOf(club, min, max, player).Single();
        var hits = new List<int>();
        foreach (var seed in OtherSeeds.Prepend(HokCompare.DefaultSeed))
        {
            var report = Compare(a, b, seed);
            Assert.True(report.PlayError is null, report.PlayError);
            if (report.Deviations.Any(d => Covers(known, d)))
                hits.Add(seed);
        }
        Assert.True(hits.Count >= minSeeds,
            $"{a}→{b} {player}: Abweichung (ehemals #730) nur bei {hits.Count} von 6 Saaten ({string.Join(", ", hits)})");
    }

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
