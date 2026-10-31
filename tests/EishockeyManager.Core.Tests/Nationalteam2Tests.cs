using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nationalteam-2 (Runde 8, Welle 3): Restbefunde der Nationalteam-Taktik aus dem Integrationsreview Runde 8
/// (`docs/reviews/2026-10-31-integration-runde-8.md`, R8-B03 und R8-B04).
/// </summary>
public class Nationalteam2Tests
{
    private static readonly ScoreSituation[] AllSituations = Enum.GetValues<ScoreSituation>();

    private static int[] ShownEffort(Tactics t) => AllSituations.Select(s => t.ShownEffort(0, s)).ToArray();

    private static int[] ShownAttack(Tactics t) => AllSituations.Select(s => t.ShownAttack(0, s)).ToArray();

    private static int[] EffortLine(int own, int opponent) => AllSituations.Select(s => Tactics.AutoEffort(own, opponent, s)).ToArray();

    private static int[] AttackLine(int own, int opponent) => AllSituations.Select(s => Tactics.AutoAttack(own, opponent, s)).ToArray();

    private static List<KnockoutMatch> GermanGroupMatches(GameState game) =>
        game.WorldCup is { } cup
            ? cup.Groups.SelectMany(g => g.Results)
                .Where(m => m.Home == NationalTeams.Germany || m.Away == NationalTeams.Germany).ToList()
            : [];

    /// <summary>
    /// R8-B03: Heimspiel-Zweig der Spielvorbereitung (0x8365 je Paarung, Datei 0x36F27/0x36F5A). Bei jedem WM-Gruppenspiel
    /// Deutschlands steht danach unter „AUTOM.“ die Zeile aus Sicht Deutschlands (eigene Stärke zuerst) – auch bei
    /// Heimspielen. Geprüft wird nur, wo sich die erwartete Zeile von der vorherigen unterscheidet, damit ein fehlender
    /// Heimzweig auffällt. Mutation „Heimzweig aus“ bzw. „Stärken vertauscht“ macht den Test rot.
    /// </summary>
    [Fact]
    public void WorldCup_HomeMatch_RecordsLineFromGermanView()
    {
        var checkedHome = 0;
        var checkedAway = 0;
        foreach (var seed in new[] { 7, 3, 11, 19, 23, 29 })
        {
            var game = GameModeTests.Create(seed, false, true, 2);
            game.Continue(); // Modus 1: Sprung zum ersten WM-Termin, Ernennung
            game.AutoNominate();
            var tactics = game.NationalTactics;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                var strengths = Enumerable.Range(NationalTeams.First, NationalTeams.Last - NationalTeams.First + 1)
                    .ToDictionary(c => c, game.NationalMatchStrength);
                var played = GermanGroupMatches(game).Count;
                var (effortBefore, attackBefore) = (ShownEffort(tactics), ShownAttack(tactics));
                game.Continue();
                var matches = GermanGroupMatches(game);
                if (matches.Count == played)
                    continue;
                var match = matches[^1];
                var home = match.Home == NationalTeams.Germany;
                var opponent = home ? match.Away : match.Home;
                var (effort, attack) = (EffortLine(strengths[NationalTeams.Germany], strengths[opponent]),
                    AttackLine(strengths[NationalTeams.Germany], strengths[opponent]));
                if (effort.SequenceEqual(effortBefore) && attack.SequenceEqual(attackBefore))
                    continue;
                Assert.Equal(effort, ShownEffort(tactics));
                Assert.Equal(attack, ShownAttack(tactics));
                if (home)
                    checkedHome++;
                else
                    checkedAway++;
            }
            if (checkedHome > 0 && checkedAway > 0)
                break;
        }
        Assert.True(checkedHome > 0, "kein unterscheidbares Heimspiel Deutschlands gefunden");
        Assert.True(checkedAway > 0, "kein unterscheidbares Auswärtsspiel Deutschlands gefunden");
    }

    /// <summary>
    /// R8-B04: Ohne Bundestrainer schreibt die Automatik immer, gleich welcher Modus in der Taktik steht.
    /// [ORIGINAL] Datei 0x8421–0x842F: Vereinsfeld 0x16 = 0 (kein Manager) → Einsatz- und Angriffszeile werden ohne
    /// Blick auf die Modi geschrieben (0x8443, 0x8496). Für Deutschland setzt die Tagesfunktion das Feld am Tagesende auf 0
    /// (Datei 0x5540E, Satz 0x3D Bytes ab DGROUP 0x9657, Land DGROUP 0x6AC) und nur bei der Ernennung auf 6 (Manager 5,
    /// Datei 0x55494); 0x55752 setzt es wieder auf 0. Vorher blieb ein „SEMI“ des Vorgängers stehen und die Anzeige alt.
    /// </summary>
    [Fact]
    public void VacantNationalTeam_RecordsAutoLine_RegardlessOfModes()
    {
        foreach (var (seed, club) in new[] { (3, 11), (7, 11), (11, 10), (19, 9), (23, 11), (29, 10) })
        {
            var game = GameModeTests.Create(seed, true, true, club); // Modus 4, passiver Manager eines schwachen Vereins
            var tactics = game.NationalTactics;
            tactics.EffortMode = TacticMode.Semi; // z. B. vom Vorgänger stehen geblieben
            tactics.AttackMode = TacticMode.Semi;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = Math.Max(m.Money, 10_000_000);
                game.Continue();
            }
            if (game.NationalCoach is not null || GermanGroupMatches(game).Count == 0)
                continue; // Ernennung am 13. April – Spielstand ohne freies Nationalteam suchen
            Assert.Same(tactics, game.NationalTactics);
            tactics.EffortMode = TacticMode.Auto;
            tactics.AttackMode = TacticMode.Auto;
            var effort = ShownEffort(tactics);
            var attack = ShownAttack(tactics);
            Assert.True(new[] { EffortLine(0, 0), EffortLine(0, 50) }.Any(l => l.SequenceEqual(effort)), "Einsatz: " + string.Join("/", effort));
            Assert.True(new[] { AttackLine(0, 1), AttackLine(1, 0) }.Any(l => l.SequenceEqual(attack)), "Angriff: " + string.Join("/", attack));
            return;
        }
        Assert.Fail("kein Spielstand ohne Bundestrainer zur WM gefunden");
    }

    /// <summary>Gegenprobe zu R8-B04: Mit Bundestrainer bleibt „SEMI“ unberührt (0x8437–0x8441, Modus des Managers).</summary>
    [Fact]
    public void Coach_Semi_KeepsShownLine()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        var tactics = game.NationalCoach!.Tactics;
        tactics.EffortMode = TacticMode.Semi;
        tactics.AttackMode = TacticMode.Semi;
        var played = GermanGroupMatches(game).Count;
        for (var guard = 0; GermanGroupMatches(game).Count == played; guard++)
        {
            Assert.True(guard < 3000, "kein WM-Spiel Deutschlands");
            game.Continue();
        }
        tactics.EffortMode = TacticMode.Auto;
        tactics.AttackMode = TacticMode.Auto;
        Assert.All(ShownEffort(tactics), v => Assert.Equal(0, v));
        Assert.All(ShownAttack(tactics), v => Assert.Equal(0, v));
    }
}
