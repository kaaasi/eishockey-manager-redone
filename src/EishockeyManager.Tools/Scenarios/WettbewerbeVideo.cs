using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur pixel-wettbewerbe (Runde 6): Wettbewerbsbildschirme mit dem Zustand der neuen Referenzbilder
/// (<c>docs/referenz-videos.md</c>). Jedes Bild heißt wie seine Vorlage; Vergleichswerte in <c>docs/wettbewerbe.md</c>.
/// </summary>
internal static class WettbewerbeVideo
{
    /// <summary>Verein, dessen Name <paramref name="part"/> enthält (Großschrift wie in der Datenbank, Umlaute vermeiden).</summary>
    internal static int Club(GameState game, string part) =>
        game.Clubs.First(c => c.Name.Contains(part, StringComparison.Ordinal)).Index;

    /// <summary>Spieltag mit dem Datum <paramref name="day"/>.<paramref name="month"/> unter den Tagen <paramref name="kind"/>.</summary>
    internal static int DayOf(DayEvents kind, int day, int month) =>
        SeasonCalendar.DaysOf(kind).First(d => SeasonCalendar.LayoutDate(d) is var date && date.Day == day && date.Month == month);

    private static readonly string[] GroupOrder = ["BB", "BZ", "BO", "ZZ", "ZO", "OO"];

    private static readonly Dictionary<string, (int, int)> GroupClasses = new()
    {
        ["BB"] = (1, 1), ["BZ"] = (1, 2), ["BO"] = (1, 4), ["ZZ"] = (2, 2), ["ZO"] = (2, 4), ["OO"] = (4, 4),
    };

    /// <summary>Ergebnis „l:r“, mit Punkt am Ende („2:3.“) nach Verlängerung.</summary>
    private static DebCupResult? Result(string text) =>
        text.Length == 0 ? null : new DebCupResult(
            int.Parse(text.Split(':')[0]), int.Parse(text.Split(':')[1].TrimEnd('.')), text.EndsWith('.') ? 1 : 0);

    /// <summary>Übersicht aus Zeilen „Gruppe|links|rechts|Ergebnis1|Ergebnis2“.</summary>
    private static DebCupOverview Overview(GameState game, int round, int played, int nextDay, string roundName, params string[] rows)
    {
        var parsed = rows.Select(r => r.Split('|')).ToList();
        var groups = GroupOrder.Select(g => new DebCupGroup(
            DebCupOverview.GroupTitle(GroupClasses[g]),
            parsed.Where(p => p[0] == g).Select(p => new DebCupRow(
                Club(game, p[1]), Club(game, p[2]), p.Skip(3).Take(played).Select(Result).ToList())).ToList())).ToList();
        return new DebCupOverview(round, played, nextDay, groups, roundName);
    }

    [Scenario("pixel-debpokal")]
    private static void DebCupGames(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var back = new MainMenuScreen(d.Host);
        int[] Highlight(params string[] parts) => parts.Select(p => Club(game, p)).ToArray();

        // folge13_02545: erste Runde nach zwei Terminen (zweite Saison oder später: ohne Rundenname), nächster Tag 29.9.
        var folge13 = Overview(game, 0, 2, DayOf(DayEvents.DebCup, 29, 9), DebCupOverview.RoundTitle(0, GameState.FirstSeason + 1),
            "BB|PREUSSEN BERLIN|ECD SAUERLAND|6:2|2:3.", "BB|LNER EC|SB ROSENHEIM|4:3|7:3",
            "BZ|EC HANNOVER|SSELDORFER EG|2:3.|2:10", "BZ|SC RIESSERSEE|EV LANDSHUT|2:1|1:6",
            "BZ|SC MEMMINGEN|ESC WOLFSBURG|1:8|1:10", "BZ|MANNHEIMER ERC|EHC FREIBURG|4:5.|1:5",
            "BO|EV F|EC RATINGEN|0:10|1:9", "BO|TEV MIESBACH|KREFELDER EV|0:4|1:11", "BO|ERC SCHWENNINGEN|ES WEI|2:5|0:9",
            "ZZ|EC KASSEL|SV BAYREUTH|6:4|6:1",
            "ZO|HEC HEILBRONN|BERLINER SC|3:1|1:6", "ZO|EC BAD NAUHEIM|EHC 80 N|5:3|4:8", "ZO|TSV ERDING|EISB|0:10|1:6",
            "ZO|ESC WEDEMARK|AUGSBURGER EV|1:3|1:4",
            "OO|ESC FRANKFURT|ESSEN-WEST|3:2|3:5", "OO|ESV K|EV LANDSBERG|6:7.|3:10");
        d.Host.Show(new DebCupGamesScreen(d.Host, back, folge13, Highlight("BERLINER SC")));
        d.Move(0, 239); d.Save("folge13_02545");

        // duell_s2a_01217: erste Runde der zweiten Saison vor dem ersten Termin (22.9.), zwei Manager
        var duell2 = Overview(game, 0, 0, DayOf(DayEvents.DebCup, 22, 9), DebCupOverview.RoundTitle(0, GameState.FirstSeason + 1),
            "BB|ESV KAUFBEUREN|EV LANDSHUT", "BB|EC HEDOS M|ESC WOLFSBURG",
            "BZ|EHC 80 N|MANNHEIMER ERC", "BZ|ES WEI|SB ROSENHEIM", "BZ|EC KASSEL|KREFELDER EV", "BZ|ESC FRANKFURT|EC RATINGEN",
            "BZ|ESSEN-WEST|PREUSSEN BERLIN",
            "BO|TSV ERDING|LNER EC", "BO|SC RIESSERSEE|SSELDORFER EG", "BO|SCHALKER HAIE|ERC SCHWENNINGEN",
            "ZZ|AUGSBURGER EV|EV LANDSBERG",
            "ZO|ETC TIMMENDORF|SV BAYREUTH", "ZO|EC HARZ BRAUNLAGE|EHC FREIBURG",
            "OO|EISB|EC BAD NAUHEIM", "OO|EC BAD T|EHC STRAUBING", "OO|1.EV WEIDEN|EV F");
        d.Host.Show(new DebCupGamesScreen(d.Host, back, duell2, Highlight("SCHALKER HAIE", "EISB")));
        d.Move(0, 239); d.Save("duell_s2a_01217");

        // duell_s6a_03100: Achtelfinale vor dem ersten Termin (20.10.), drei leere Gruppen
        var duell6 = Overview(game, 1, 0, DayOf(DayEvents.DebCup, 20, 10), DebCupOverview.RoundTitle(1, GameState.FirstSeason + 5),
            "BB|EISB|ESV KAUFBEUREN",
            "BZ|SCHALKER HAIE|PREUSSEN BERLIN", "BZ|EHC 80 N|MANNHEIMER ERC", "BZ|ESC FRANKFURT|EC HEDOS M",
            "BZ|SSELDORFER EG|SB ROSENHEIM", "BZ|EHC FREIBURG|SV BAYREUTH", "BZ|SC MEMMINGEN|EC HANNOVER",
            "ZZ|ECD SAUERLAND|ESSEN-WEST");
        d.Host.Show(new DebCupGamesScreen(d.Host, back, duell6, Highlight("SCHALKER HAIE", "EISB")));
        d.Move(0, 239); d.Save("duell_s6a_03100");

        // eisbaeren_f60_02758: Endspiel (27.3.), fünf leere Gruppen
        var final = Overview(game, 4, 0, DayOf(DayEvents.DebCup, 27, 3), DebCupOverview.RoundTitle(4, GameState.FirstSeason + 3),
            "BB|AUGSBURGER EV|EISB");
        d.Host.Show(new DebCupGamesScreen(d.Host, back, final, Highlight("EISB")));
        d.Move(0, 239); d.Save("eisbaeren_f60_02758");
    }
    /// <summary>
    /// Plätze „Name(n)“ Zeile für Zeile, links vor rechts; setzt die Spielklasse jedes Vereins wie im Bild (1 Bundesliga,
    /// 2 Zweite Liga, 3 Oberliga), damit Farbe und Ziffer stimmen.
    /// </summary>
    private static List<int?> Slots(GameState game, params string[] entries) =>
        entries.Select(e =>
        {
            var club = game.Clubs[Club(game, e[..^3])];
            club.League = e[^2] switch { '1' => League.Bundesliga, '2' => League.ZweiteLiga, _ => League.Oberliga };
            return (int?)club.Index;
        }).ToList();

    [Scenario("pixel-auslosung")]
    private static void DebCupDraw(ScreenDriver d)
    {
        // folge01_00217: Frage beim Spielstart über 32 leeren Plätzen
        d.StopAtDebCupDraw = true;
        d.StartNewGame();
        var game = d.Host.Game!;
        var back = new MainMenuScreen(d.Host);
        d.Move(0, 239); d.Save("folge01_00217");

        // duell_s2a_02651: Frage zur Auslosung des Achtelfinales (16 Plätze)
        d.Host.Show(new DebCupDrawLiveScreen(d.Host, 1, game.DebCup!.Draw.Take(16).ToList(), back));
        d.Move(0, 239); d.Save("duell_s2a_02651");

        game.Managers[0].ClubIndex = Club(game, "EISB");

        // eisbaeren_f30_01719: Achtelfinale aufgedeckt, „TASTE DRÜCKEN“
        var f30 = Slots(game,
            "EC KASSEL(1)", "SB ROSENHEIM(1)", "LNER EC(2)", "EISB(1)", "KREFELDER EV(2)", "EC HEDOS M(2)",
            "EV LANDSBERG(2)", "TSV ERDING(1)", "ESV KAUFBEUREN(2)", "EV LANDSHUT(1)", "AUGSBURGER EV(1)", "GERETSRIED(3)",
            "PREUSSEN BERLIN(1)", "SSELDORFER EG(1)", "MANNHEIMER ERC(2)", "ECD SAUERLAND(2)");
        d.Host.Show(new DebCupDrawScreen(d.Host, back, f30));
        d.Move(0, 239); d.Save("eisbaeren_f30_01719");

        // eisbaeren_f52_03358: erste Runde aufgedeckt
        var f52 = Slots(game,
            "ESC WOLFSBURG(2)", "EC BAD T(3)", "ERC SCHWENNINGEN(2)", "EC BAD NAUHEIM(3)", "ESV KAUFBEUREN(1)", "ESC FRANKFURT(1)",
            "TEV MIESBACH(1)", "TSV ERDING(2)", "PREUSSEN BERLIN(1)", "SB ROSENHEIM(1)", "EC RATINGEN(3)", "SC MEMMINGEN(3)",
            "EA KEMPTEN(3)", "SSELDORFER EG(2)", "ESV K(2)", "ECD SAUERLAND(1)", "EC HEDOS M(1)", "EISB(1)",
            "ETC TIMMENDORF(2)", "ES WEI(3)", "SV BAYREUTH(3)", "HEC HEILBRONN(3)", "BREMERHAVEN(3)", "KREFELDER EV(2)",
            "EC KASSEL(1)", "MANNHEIMER ERC(2)", "EV LANDSBERG(2)", "AUGSBURGER EV(1)", "EC HANNOVER(3)", "SC RIESSERSEE(1)",
            "SCHALKER HAIE(2)", "EV LANDSHUT(2)");
        d.Host.Show(new DebCupDrawScreen(d.Host, back, f52));
        d.Move(0, 239); d.Save("eisbaeren_f52_03358");
    }
    /// <summary>Seite „Paarungen“ aus Zeilen „Heim|Platz|Stärke|Gast|Platz|Stärke|g:g“.</summary>
    private static PairingsPage Page(GameState game, string header, params string[] rows) =>
        new(header, rows.Select(r => r.Split('|')).Select(p => new PairingRow(
            Club(game, p[0]), Club(game, p[3]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[4]), int.Parse(p[5]),
            (int.Parse(p[6].Split(':')[0]), int.Parse(p[6].Split(':')[1])))).ToList());

    [Scenario("pixel-paarungen")]
    private static void LeaguePairings(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var back = new MainMenuScreen(d.Host);
        void Show(string name, League league, PairingsPage page, params string[] highlight)
        {
            d.Host.Show(new LeaguePairingsScreen(d.Host, back, league, page, highlight.Select(h => Club(game, h)).ToArray()));
            d.Move(0, 239); d.Save(name);
        }

        // Seiten mit Knöpfen (Hauptmenü → Paarungen), jeweils nach dem ersten Spieltag (10.9.)
        Show("folge06_01256", League.Oberliga, Page(game, "Oberliga   1.Spieltag  10.9.",
            "SC RIESSERSEE|8|41|EC HARZ BRAUNLAGE|12|28|2:1", "EC BAD T|11|37|TSV ERDING|1|45|5:9",
            "TEV MIESBACH|9|34|SCHALKER HAIE|4|36|10:7", "BERLINER SC|2|54|ESC WOLFSBURG|7|44|9:0",
            "ECD SAUERLAND|5|44|EHC 80 N|6|32|3:6", "EV F|10|33|ESV K|3|50|1:6"), "BERLINER SC");
        Show("folge17_02406", League.ZweiteLiga, Page(game, "Zweite Liga   1.Spieltag  10.9.",
            "SV BAYREUTH|3|63|ESV KAUFBEUREN|5|60|6:2", "SB ROSENHEIM|6|61|MANNHEIMER ERC|2|63|6:2",
            "ECD SAUERLAND|7|58|SC MEMMINGEN|11|59|2:6", "EA KEMPTEN|12|54|AUGSBURGER EV|8|52|5:4",
            "BERLINER SC|1|77|EC HANNOVER|4|61|6:3", "HEC HEILBRONN|9|55|EC KASSEL|10|43|7:4"), "BERLINER SC");
        Show("folge21_02149", League.Bundesliga, Page(game, "Bundesliga   1.Spieltag  10.9.",
            "EC HEDOS M|8|60|HEC HEILBRONN|11|57|2:1", "PREUSSEN BERLIN|5|77|EV LANDSHUT|10|68|7:2",
            "KREFELDER EV|9|64|EISB|4|64|2:4", "LNER EC|1|74|SSELDORFER EG|12|65|9:1",
            "EHC 80 N|6|77|BERLINER SC|2|82|8:3", "ES WEI|7|74|EHC FREIBURG|3|75|5:0"), "BERLINER SC");

        // Gleicher Fensteraufbau ohne Knöpfe im Spielablauf („60.MINUTE“, gibt es im Remake noch nicht): Vergleich nur im Fenster
        Show("duell_s2a_01138", League.Oberliga, Page(game, "",
            "SCHALKER HAIE|1|47|ETC TIMMENDORF|12|38|5:1", "EC BAD T|10|31|TSV ERDING|4|39|2:4",
            "1.EV WEIDEN|2|29|EHC STRAUBING|11|29|5:2", "EC HARZ BRAUNLAGE|3|34|EV F|9|44|4:2",
            "EA KEMPTEN|7|41|EC BAD NAUHEIM|6|38|3:3", "EISB|5|52|SC RIESSERSEE|8|46|3:2"), "SCHALKER HAIE", "EISB");
        Show("eisbaeren_f14_01518", League.ZweiteLiga, Page(game, "",
            "EISB|2|60|SC MEMMINGEN|8|53|5:4", "HEC HEILBRONN|10|58|SV BAYREUTH|7|56|3:3",
            "SC RIESSERSEE|12|43|EHC FREIBURG|1|62|0:7", "EC HEDOS M|6|50|TSV ERDING|11|52|5:3",
            "AUGSBURGER EV|3|59|SB ROSENHEIM|4|61|3:5", "EC KASSEL|9|51|ESV KAUFBEUREN|5|55|3:3"), "EISB");
        Show("eisbaeren_f40_01833", League.Bundesliga, Page(game, "",
            "EC KASSEL|5|73|SC RIESSERSEE|4|73|3:2", "SB ROSENHEIM|6|72|TEV MIESBACH|1|81|3:7",
            "KREFELDER EV|2|77|AUGSBURGER EV|8|70|2:2", "ESC FRANKFURT|10|63|PREUSSEN BERLIN|7|70|5:4",
            "ECD SAUERLAND|11|65|TSV ERDING|9|68|5:3", "ETC TIMMENDORF|12|65|EISB|3|83|1:2"), "EISB");
    }
    /// <summary>
    /// Gruppe a–d mit Ergebnissen je Runde „a-b|c-d“ als „l:r/l:r“ (Hin-/Rückspiel aus Sicht der Paarung, leer = nicht gespielt);
    /// die Reihenfolge der Vereine ergibt sich aus der ersten Runde („abcd“).
    /// </summary>
    private static CupGroup EuropeanGroup(GameState game, int number, int firstDate, string[] teams, params string[] results)
    {
        var group = new CupGroup(number, teams.Select(t => Club(game, t)).ToList());
        var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);
        for (var round = 0; round < 3; round++)
        {
            var pairs = group.Pairings(round);
            var legs = results[round].Split('|');
            for (var p = 0; p < 2; p++)
            {
                var (home, away) = pairs[p];
                var scores = legs[p].Split('/');
                for (var leg = 0; leg < scores.Length; leg++)
                {
                    var g = scores[leg].Split(':').Select(int.Parse).ToArray();
                    group.Results.Add(leg == 0
                        ? new KnockoutMatch(dates[firstDate + round], home, away, g[0], g[1])
                        : new KnockoutMatch(dates[firstDate + round + 3], away, home, g[1], g[0]));
                }
            }
        }
        return group;
    }

    [Scenario("pixel-europapokal")]
    private static void European(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var back = new MainMenuScreen(d.Host);
        void Show(string name, int view, CupGroup group)
        {
            d.Host.Show(new EuropeanCupScreen(d.Host, back, view, group));
            d.Move(0, 239); d.Save(name);
        }
        const int final = EuropeanCup.GroupDates;

        // folge04: alle Spiele gespielt (Manager Berliner SC, kein Teilnehmer)
        Show("folge04_01241", 0, EuropeanGroup(game, 1, 0, ["CHAMONIX", "DYNAMO MOSKAU", "AESSAT PORI", "ZSKA MOSKAU"],
            "5:6/2:6|3:2/3:2", "5:3/5:4|4:3/6:3", "3:4/3:6|5:3/4:3"));
        Show("folge04_01245", 1, EuropeanGroup(game, 2, 0, ["METALLURG PERMIK", "PREUSSEN BERLIN", "SPARTAK MOSKAU", "HC ROUEN"],
            "1:3/1:8|4:2/5:4", "6:2/3:0|4:2/2:1", "0:6/1:5|5:3/4:1"));
        var endrunde = EuropeanGroup(game, 3, final, ["DYNAMO MOSKAU", "AESSAT PORI", "PREUSSEN BERLIN", "SPARTAK MOSKAU"],
            "5:2/3:0|4:7/6:4", "4:2/5:0|0:4/2:4", "1:2/2:5|0:5/3:4");
        Show("folge04_01243", 2, endrunde);
        Show("folge04_01247", 2, endrunde);

        // Eisbären Berlin als Manager-Verein (gelb)
        game.Managers[0].ClubIndex = Club(game, "EISB");
        Show("eisbaeren_f51_03346", 1, EuropeanGroup(game, 2, 0, ["JOKERIT HELSINKI", "AMBRI-PIOTTA", "EISB", "IFK HELSINKI"],
            "4:3|6:2", "1:1|1:3", "3:3|2:2"));
        Show("eisbaeren_f58_01323", 2, EuropeanGroup(game, 3, final, ["EISB", "VITKOVICE", "KOSICE", "DUKLA TRENCIN"],
            "3:2/2:0|3:4/0:5", "3:2|4:3", "3:0|6:3"));
    }
}
