using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;
using static EishockeyManager.Tools.Scenarios.WettbewerbeVideo;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur ui-rest (Runde 17), Teil 1: Seiten „Play-Off Spiele“ und „Aufstieg“ der Ligapaarungen mit dem Zustand der
/// Referenzbilder. Jedes Bild heißt wie seine Vorlage; Vergleichswerte im PR und in <c>docs/wettbewerbe.md</c>.
/// </summary>
internal static class UiRestVideo
{
    /// <summary>Ergebnis „l:r“, je Punkt am Ende eine Marke („7:5..“).</summary>
    private static DebCupResult Result(string text)
    {
        var dots = text.Length - text.TrimEnd('.').Length;
        var parts = text.TrimEnd('.').Split(':');
        return new DebCupResult(int.Parse(parts[0]), int.Parse(parts[1]), dots);
    }

    /// <summary>Abschnitt aus Zeilen „links|rechts|Ergebnis …“.</summary>
    private static PlayoffSection Section(GameState game, int round, params string[] rows) =>
        new(PlayoffOverview.SectionTitles[round], rows.Select(r => r.Split('|')).Select(p =>
            new PlayoffRow(Club(game, p[0]), Club(game, p[1]), p.Skip(2).Select(Result).ToList())).ToList());

    [Scenario("pixel-playoff")]
    private static void Playoffs(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var back = new MainMenuScreen(d.Host);
        void Show(string name, params PlayoffSection[] sections)
        {
            d.Host.Show(new PlayoffGamesScreen(d.Host, back, sections, [Club(game, "BERLINER SC")]));
            d.Move(0, 239); d.Save(name);
        }

        Show("folge21_02150",
            Section(game, 0, "LNER EC|EC HEDOS M|6:1|3:2.|3:0", "BERLINER SC|ES WEI|3:2|0:4|4:2|4:0",
                "EHC FREIBURG|EHC 80 N|7:5..|4:6|4:3.|5:3", "EISB|PREUSSEN BERLIN|3:8|2:6|1:5"),
            Section(game, 1, "LNER EC|EHC FREIBURG|6:5.|1:4|2:1|5:0", "BERLINER SC|PREUSSEN BERLIN|5:6|1:3|3:4"),
            Section(game, 2, "LNER EC|PREUSSEN BERLIN|3:1|3:2|3:1"));
        Show("folge23_02017",
            Section(game, 0, "EHC FREIBURG|EC HEDOS M|8:2|4:1|6:3", "BERLINER SC|KREFELDER EV|4:0|4:3.|3:1",
                "ES WEI|PREUSSEN BERLIN|1:2|2:4|7:2|5:2.|4:2", "EHC 80 N|LNER EC|5:3|3:6|4:3|2:1"),
            Section(game, 1, "EHC FREIBURG|ES WEI|1:5|3:5|3:1|2:6", "BERLINER SC|EHC 80 N|6:1|1:6|7:2|1:2|5:6"),
            Section(game, 2, "EHC 80 N|ES WEI|6:3|3:2|5:3"));
    }

    /// <summary>Hin-/Rückspiel: „g:g“ Ergebnis, „t.m.“ Termin, leer nichts.</summary>
    private static PromotionLeg? Leg(string text)
    {
        if (text.Length == 0)
            return null;
        if (text.Contains(':'))
        {
            var p = text.Split(':');
            return new PromotionLeg(int.Parse(p[0]), int.Parse(p[1]), false);
        }
        var q = text.TrimEnd('.').Split('.');
        return new PromotionLeg(int.Parse(q[0]), int.Parse(q[1]), true);
    }

    /// <summary>Tabellenzeile „Verein|Siege|Unentschieden|Niederlagen|Tore|Gegentore“.</summary>
    private static TableRow Row(GameState game, int rank, string text)
    {
        var p = text.Split('|');
        return new TableRow
        {
            Club = game.Clubs[Club(game, p[0])], Rank = rank, Wins = int.Parse(p[1]), Draws = int.Parse(p[2]), Losses = int.Parse(p[3]),
            GoalsFor = int.Parse(p[4]), GoalsAgainst = int.Parse(p[5]),
        };
    }

    private static PromotionRoundPage Page(GameState game, string[] pairings, params string[] table) =>
        new(pairings.Select(r => r.Split('|')).Select(p => new PromotionPairing(Club(game, p[0]), Club(game, p[1]), Leg(p[2]), Leg(p[3])))
            .ToList(), table.Select((t, i) => Row(game, i + 1, t)).ToList());

    [Scenario("pixel-aufstieg")]
    private static void Promotion(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var back = new MainMenuScreen(d.Host);
        void Show(string name, PromotionRoundPage? page)
        {
            d.Host.Show(new PromotionRoundScreen(d.Host, back, League.Oberliga, page?.Pairings ?? [], page?.Table ?? [], [Club(game, "BERLINER SC")]));
            d.Move(0, 239); d.Save(name);
        }

        Show("folge06_01257", Page(game,
            ["EC BAD NAUHEIM|ES WEI|6:3|3:2", "TSV ERDING|BERLINER SC|2:2|4:8", "BERLINER SC|EC BAD NAUHEIM|4:4|22.3.",
             "ES WEI|TSV ERDING|4:2|", "EC BAD NAUHEIM|TSV ERDING|8:4|29.3.", "ES WEI|BERLINER SC|1:6|"],
            "EC BAD NAUHEIM|3|1|0|21|13", "BERLINER SC|2|2|0|20|11", "ES WEI|1|0|3|10|17", "TSV ERDING|0|1|3|12|22"));
        Show("folge07_03307", Page(game,
            ["BERLINER SC|EA KEMPTEN|6.2.|25.2.", "ECD SAUERLAND|SC RIESSERSEE||", "SC RIESSERSEE|BERLINER SC|10.2.|22.3.",
             "EA KEMPTEN|ECD SAUERLAND||", "BERLINER SC|ECD SAUERLAND|20.2.|29.3.", "EA KEMPTEN|SC RIESSERSEE||"],
            "BERLINER SC|0|0|0|0|0", "EA KEMPTEN|0|0|0|0|0", "ECD SAUERLAND|0|0|0|0|0", "SC RIESSERSEE|0|0|0|0|0"));
        Show("aufstieg-noch-nicht-fest", null);
    }
}
