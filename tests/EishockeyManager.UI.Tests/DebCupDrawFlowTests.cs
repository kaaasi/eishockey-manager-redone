using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;
using Xunit.Abstractions;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur debpokal-auslosung (#116, Befund H2-03): die DEB-Pokal-Auslosung im Spielablauf – Spielstart, Saisonwechsel und nach
/// dem dritten Termin jeder Runde, mit der Frage „Sind Ihre Nerven stark genug …“.
/// </summary>
public class DebCupDrawFlowTests(ITestOutputHelper output)
{
    internal static ScreenDriver StartStoppingAtDraw(int seed = 1993)
    {
        var d = FlowTests.CreateDriver(seed);
        d.StopAtDebCupDraw = true;
        d.StartNewGame();
        return d;
    }

    /// <summary>Antwort im Fragefenster: Escape = „Kein Gedanke“, sonst weiter (Enter = „Aber klar“; im Hauptmenü Klick aufs Vereinslogo).</summary>
    private static void Step(ScreenDriver d, bool live)
    {
        if (!live && d.Host.Current is DebCupDrawLiveScreen { Current: DebCupDrawLiveScreen.Phase.Ask })
            d.Press(Key.Escape);
        else
            d.Advance();
    }

    /// <summary>[ORIGINAL] Datei 0x43A8C, Video 3:40: nach „Los jetzt!“ erst die Auslosung der ersten Runde mit Frage.</summary>
    [GameDataFact]
    public void GameStart_ShowsFirstRoundDraw_WithQuestionToAll()
    {
        var d = StartStoppingAtDraw();
        var screen = Assert.IsType<DebCupDrawLiveScreen>(d.Host.Current);
        Assert.Equal(0, screen.Round);
        Assert.Equal(DebCupDrawLiveScreen.Phase.Ask, screen.Current);
        Assert.Equal(DebCup.Teams, screen.Slots.Count);
        Assert.All(screen.Slots, s => Assert.Null(s)); // leere Streifen hinter der Frage (Video 3:40–3:46)

        d.Click(DebCupDrawLiveScreen.NoButton.X + 5, DebCupDrawLiveScreen.NoButton.Y + 3);
        Assert.IsType<MainMenuScreen>(d.Host.Current); // „Kein Gedanke“: keine Übersicht (0x33DB1 → 0x340C7)
    }

    /// <summary>„Aber klar“: Plätze einzeln aufdecken, „TASTE DRÜCKEN“, Übersicht „DEB-Pokalspiele“, Hauptmenü.</summary>
    [GameDataFact]
    public void AnswerYes_RevealsSlotByslot_ThenGamesOverview_WithoutChangingState()
    {
        var d = StartStoppingAtDraw();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);
        var screen = Assert.IsType<DebCupDrawLiveScreen>(d.Host.Current);

        d.Click(DebCupDrawLiveScreen.YesButton.X + 5, DebCupDrawLiveScreen.YesButton.Y + 3);
        Assert.Equal(DebCupDrawLiveScreen.Phase.Reveal, screen.Current);
        for (var i = 0; i < DebCupDrawLiveScreen.RevealFrames; i++)
            d.Frame();
        Assert.Equal(1, screen.Revealed);
        Assert.Single(screen.Slots, s => s is not null);

        d.Advance(); // deckt alle übrigen auf
        Assert.Equal(DebCupDrawLiveScreen.Phase.Done, screen.Current);
        Assert.Equal(game.DebCup!.Draw.Select(c => (int?)c), screen.Slots);

        d.Advance();
        Assert.IsType<DebCupGamesScreen>(d.Host.Current);
        d.Advance();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>
    /// Abnahme (3): nach dem dritten Termin jeder Runde genau eine Auslosung (Runden 2–4 mit Frage); vor dem Endspiel
    /// (zwei Plätze) keine Frage, nur die Übersicht (0x33BC3). Die Anzeige zeigt die Ziehung zum Zeitpunkt der Auslosung.
    /// </summary>
    [GameDataFact]
    public void DrawAppearsOncePerRound_AfterThirdDate()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var cup = game.DebCup!;
        var seen = new List<(int Round, IReadOnlyList<int?> Slots)>();
        Screen? last = null;
        var finalOverview = false;
        for (var guard = 0; cup.Winner is null || d.Host.Current is not MainMenuScreen; guard++)
        {
            Assert.True(guard < 20000, "Pokalsieger nicht erreicht");
            var current = d.Host.Current;
            if (!ReferenceEquals(current, last))
            {
                if (current is DebCupDrawLiveScreen live)
                {
                    Assert.Equal(DebCupDrawLiveScreen.Phase.Ask, live.Current);
                    // Vorrunde entschieden, neue Runde ausgelost
                    Assert.All(cup.Rounds[live.Round - 1], t => Assert.NotNull(t.Winner));
                    Assert.Equal(cup.Rounds[live.Round - 1].Select(t => t.Winner!.Value).Order(),
                        cup.Rounds[live.Round].SelectMany(t => new[] { t.First, t.Second }).Order());
                    seen.Add((live.Round, live.Slots));
                }
                if (current is DebCupGamesScreen && cup.CurrentRound == DebCup.RoundCount - 1 && last is not DebCupDrawLiveScreen)
                    finalOverview = true;
                last = current;
            }
            if (current is DebCupDrawLiveScreen { Current: DebCupDrawLiveScreen.Phase.Done } done)
                Assert.Equal(cup.Rounds[done.Round].SelectMany(t => new[] { t.First, t.Second }).Order(),
                    done.Slots.Select(s => s!.Value).Order());
            d.Advance();
        }

        Assert.Equal([1, 2, 3], seen.Select(s => s.Round));
        Assert.Equal([16, 8, 4], seen.Select(s => s.Slots.Count));
        Assert.True(finalOverview, "Vor dem Endspiel fehlt die Übersicht „DEB-Pokalspiele“");
    }

    /// <summary>
    /// Abnahme (2)/(3): „Kein Gedanke“ überspringt nur die Anzeige – Paarungen und alle Ergebnisse der Saison sind mit und ohne
    /// Live-Auslosung gleich (bis zum Hauptmenü der zweiten Saison samt deren Auslosung).
    /// </summary>
    [GameDataFact]
    public void PairingsAndResults_SameWithAndWithoutDisplay()
    {
        string Play(bool live)
        {
            var d = FlowTests.CreateDriver();
            d.StopAtDebCupDraw = true;
            d.StartNewGame();
            var game = d.Host.Game!;
            for (var guard = 0; game.History.Count < 1 || d.Host.Current is not MainMenuScreen; guard++)
            {
                Assert.True(guard < 20000, "Saisonwechsel nicht erreicht");
                Step(d, live);
            }
            return StateFingerprint.Of(game);
        }

        var withDisplay = Play(live: true);
        var without = Play(live: false);
        Assert.True(withDisplay == without, StateFingerprint.FirstDifference(withDisplay, without));
    }

    /// <summary>Saisonwechsel: die Auslosung der ersten Runde erscheint vor dem Hauptmenü der neuen Saison (0x56609).</summary>
    [GameDataFact]
    public void SeasonChange_ShowsFirstRoundDraw()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        DebCupDrawLiveScreen? atChange = null;
        for (var guard = 0; game.History.Count < 1 || d.Host.Current is not MainMenuScreen; guard++)
        {
            Assert.True(guard < 20000, "Saisonwechsel nicht erreicht");
            if (game.History.Count == 1 && d.Host.Current is DebCupDrawLiveScreen live)
                atChange ??= live;
            d.Advance();
        }
        Assert.NotNull(atChange);
        Assert.Equal(0, atChange!.Round);
        Assert.Equal(1994, game.Season);
        Assert.Equal(game.DebCup!.Draw.Select(c => (int?)c), atChange.Slots);
    }

    /// <summary>
    /// Mehrsaisonprobe (Abnahme 4): vier Saisonwechsel über die Oberfläche, einmal mit Live-Auslosung, einmal ohne. Pokalsieger,
    /// Meister und Abschlusstabellen je Saison sind gleich und gleich den Referenzwerten von <c>main</c> vor dieser Spur
    /// (a09c81e, Auslosung im Spiel noch nicht sichtbar).
    /// Spur heimspiel-statistik (#136): Der Manager hält seinen Kader (<see cref="KeepSquadHelper"/>), sonst wird er nach
    /// „Weniger als neun Spieler“ (200.000 DM je Spiel) in Saison 1995 entlassen und das Spiel endet. 1993 und 1994 bleiben
    /// gleich; ab 1995 spielt sein Verein mit dem verlängerten statt mit leerem Kader, daher neue Werte für 1995 und 1996.
    /// Spur kader-start (#168, em.exe 0x29DA4): Der Kader des Berliner SC bekommt Kondition/Technik random(29, 33)
    /// (Oberliga, Mittelfeld) statt Teamwert ± 2. Vergleichslauf gegen 15d0733 nach jedem Schritt: Gehälter +200 DM,
    /// Konto ab Tag 7, Spielstärke ab Tag 17 der ersten Saison um einen Punkt verschieden; Haupt-, Pokal- und
    /// Vertragsquelle bleiben bis 1994 Tag 122 gleich, 1993 daher unverändert, ab 1994 neue Meister und Tabellen.
    /// Integration Runde 7: Sollwerte auf dem integrierten Stand neu ermittelt (kader-start, boerse-start, weltkugel-dosbox,
    /// spielberechnung, Freundschaftsspiele über die neue Spielberechnung). Die Änderungen der einzelnen Spuren sind oben
    /// begründet; der Integrationswert ist ihr Zusammenwirken (siehe docs/arbeitspakete.md, „Integration Runde 7“).
    /// </summary>
    [GameDataFact]
    public void MultiSeason_CupResultsUnchanged_WithAndWithoutDisplay()
    {
        string Play(bool live)
        {
            var d = FlowTests.CreateDriver();
            d.StopAtDebCupDraw = true;
            d.StartNewGame();
            var game = d.Host.Game!;
            for (var guard = 0; game.History.Count < 4 || d.Host.Current is not MainMenuScreen; guard++)
            {
                Assert.True(guard < 80000, "Vier Saisonwechsel nicht erreicht");
                KeepSquadHelper.KeepSquads(game); // Spur heimspiel-statistik (#136): sonst ab Saison 3 entlassen
                Step(d, live);
            }
            return Summary(game);
        }

        var withDisplay = Play(live: true);
        var without = Play(live: false);
        output.WriteLine(withDisplay);
        Assert.Equal(withDisplay, without);
        Assert.Equal(Reference, withDisplay);
    }

    /// <summary>Je Saison: Pokalsieger, Meister und eine Prüfsumme der Abschlusstabellen.</summary>
    internal static string Summary(GameState game) => string.Join(" | ", game.History.Select(r =>
        $"{r.Season}: DEB {r.Titles.GetValueOrDefault(Title.CupWinner, -1)}, DM {r.Titles.GetValueOrDefault(Title.GermanChampion, -1)}, " +
        $"Tab {r.FinalTables.Values.SelectMany(t => t).Select((c, i) => c * (i + 1)).Sum()}"));

    /// <summary>Referenz: <see cref="Summary"/> auf <c>main</c> a09c81e (nur Leertaste, Seed 1993), siehe PR der Spur;
    /// ab 1994 neu mit Spur kader-start (siehe oben). Spur spielstaerke-manager (#204): ab 1993 neu – gestreute Teamwerte
    /// des Laders 0x2EF1B und Tagesform 0x4F527 ändern alle Ergebnisse; mit und ohne Anzeige weiter gleich.
    /// Spur spielstaerke-manager-2: Managerpaarungen mit Spielerwerten, Energie und Reihenwechsel (0x98F8, 0x87F7, 0x8EF2)
    /// – andere Ergebnisse ab dem ersten Spiel des Managers; mit und ohne Anzeige weiter gleich. Integration Runde 8, Welle 3
    /// (R8W3I-01): Spielstand vor den Toren der Minute für 0x80CB/0x939A – neue Werte, mit und ohne Anzeige gleich.
    /// Spur transfer-vertraege-komplett (Teil 1): ab 1995 neu – nach den Prämien kommt die Verhandlung ausgelaufener Verträge
    /// (0x27D05); Enter ist „Mach' ich!“, die Spieler bleiben ein Jahr statt zu gehen. Mit und ohne Anzeige weiter gleich.
    /// Spur transfer-vertraege-komplett (Teil 5): ab 1993 neu – Pool und Listenwechsel 0x494EF (random(0, Managerzahl + 2) je
    /// Ligaspieltag) verbrauchen die Hauptquelle anders als die bisherige Liste aus Computerkadern; die Datenbankspieler des
    /// Pools fehlen den Computerkadern (jeder Name nur einmal), damit andere Ergebnisse;
    /// mit und ohne Anzeige weiter gleich.
    /// Spur spielberechnung-komplett, Teil 1 (#204): Wanderung der Teamwerte 0x9D1B – neue Werte ab 1993, mit und ohne
    /// Anzeige weiter gleich. Spur spielberechnung-komplett, Teil 3 (0x38B8F, 0x386D1): neue Werte ab 1993 (Verletzungen und Schützenwahl in den Spielen des
    /// Managers), mit und ohne Anzeige weiter gleich. Spur spielberechnung-komplett, Teil 5 (0x3901B): 1993 gleich, ab 1994 neuer Meister
    /// (Penaltyschießen des Originals in den Play-offs mit Manager statt NextDouble), mit und ohne Anzeige weiter gleich.
    /// Integration Runde 9 (Leitstelle): Werte auf dem integrierten Stand neu, einschließlich Minutenweg der DEB-Pokalspiele
    /// ohne Manager (Anzeigeschalter an, 0x3702E); mit und ohne Anzeige weiter gleich.</summary>
    private const string Reference =
        "1993: DEB 8, DM 5, Tab 15314 | 1994: DEB 6, DM 5, Tab 15917 | 1995: DEB 9, DM 9, Tab 15930 | 1996: DEB 10, DM 6, Tab 16140";
}
