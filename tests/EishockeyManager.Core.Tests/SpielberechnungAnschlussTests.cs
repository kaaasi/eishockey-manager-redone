using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielberechnung, Teil 3: Anschluss der Rechnung des Originals an die Spieltage (<see cref="MatchEngine.PlayWithEvents"/>),
/// Spielerzähler und Mehrsaisonprobe. Fundstellen: <c>docs/spielberechnung.md</c> §11.
/// </summary>
public class SpielberechnungAnschlussTests(ITestOutputHelper output)
{
    private static GameState Game(int managerClub = 2, int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = managerClub });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static IEnumerable<Player> AllPlayers(GameState game) =>
        game.Clubs.SelectMany(c => c.Players).Concat(game.TransferMarket.FreeAgents);

    /// <summary>Regressionstest (vor Teil 3 rot: die Zähler wurden nie hochgezählt): ein Ligaspieltag füllt die Zähler.</summary>
    [Fact]
    public void LeagueMatchDay_FillsPlayerCounters()
    {
        var game = Game();
        Assert.All(AllPlayers(game), p => Assert.Equal(0, p.Goals + p.Assists + p.Games + p.GoalieMinutes));
        TestSeason.PlayMatchDay(game);
        var players = AllPlayers(game).ToList();
        Assert.True(players.Sum(p => p.Goals) > 0);
        Assert.True(players.Sum(p => p.GoalieMinutes) > 0);
        Assert.True(players.Sum(p => p.Penalties2 + p.Penalties5) > 0, "Managerverein ohne Strafen nach einem Spieltag");
        Assert.True(players.Sum(p => p.Assists) <= players.Sum(p => p.Goals)); // [ORIGINAL] höchstens eine Vorlage je Tor
    }

    /// <summary>
    /// Je Verein: Tore der Spieler ≤ Tore des Vereins (ein Fünftel bleibt ohne Torschützen, 0x5028); Strafen je Spieler
    /// nur beim Managerverein, Torhüterminuten des Managervereins 60 je Spiel; Gegentore der Torhüter = Gegentore.
    /// </summary>
    [Fact]
    public void RegularSeason_PlayerCountersConsistentWithResults()
    {
        // Nur Ligaspieltage, ohne Tagesablauf: keine Pokale und keine Transfers zwischen den Spielen.
        var game = Game();
        while (game.PlayNextMatchDay())
        {
        }
        var own = game.Clubs[2];
        foreach (var club in game.Clubs.Where(c => c.League is not null))
        {
            var results = game.Results.Where(r => r.Involves(club.Index)).Select(r => r.For(club.Index)).ToList();
            var goals = results.Sum(r => r.For);
            var against = results.Sum(r => r.Against);
            Assert.InRange(club.Players.Sum(p => p.Goals), goals * 6 / 10, goals);
            Assert.True(club.Players.Sum(p => p.Assists) <= club.Players.Sum(p => p.Goals));
            // [ORIGINAL] 0x4DB6: Gegentore eines Torhüters zählen nur, solange das Byte nicht über 255 steigt.
            Assert.InRange(club.Players.Sum(p => p.GoalsAgainst), Math.Min(against, 200), against);
            if (club == own)
            {
                Assert.Equal(60 * results.Count, club.Players.Sum(p => p.GoalieMinutes));
                Assert.True(club.Players.Sum(p => p.Penalties2) > 0);
            }
            else
            {
                Assert.Equal(0, club.Players.Sum(p => p.Penalties2 + p.Penalties5)); // [ORIGINAL] nur Vereinsminuten
                Assert.Equal(60 * results.Count, club.Players.Sum(p => p.GoalieMinutes)); // 0x4DB6 verteilt 60 Minuten
            }
        }
    }

    /// <summary>
    /// [ORIGINAL] #165/0x36DC5: Spielwertung 0:2 bzw. 2:0 ohne Berechnung bleibt; der nicht spielfähige Managerverein
    /// zählt nichts (keine Spiele, keine Torhüterminuten).
    /// </summary>
    [Fact]
    public void Forfeit_StaysZeroTwo_WithoutCounters()
    {
        var game = Game();
        var own = game.Clubs[2];
        foreach (var p in own.Lineup.Players.Skip(8).ToList())
            own.Lineup.Remove(p);
        foreach (var p in own.Players.Where(p => !own.Lineup.Contains(p)))
            p.InjuredGames = 50;
        while (game.Results.Count(r => r.Home == own.Index) < 2 || game.Results.Count(r => r.Away == own.Index) < 2)
            TestSeason.PlayMatchDay(game);
        Assert.All(game.Results.Where(r => r.Involves(own.Index)),
            r => Assert.Equal(r.Home == own.Index ? (0, 2) : (2, 0), (r.HomeGoals, r.AwayGoals)));
        Assert.All(own.Players, p => Assert.Equal(0, p.Games + p.GoalsAgainst + p.GoalieMinutes + p.Penalties2));
    }

    /// <summary>K.-o.-Spiele haben immer einen Sieger: Verlängerung, danach Penaltyschießen (Minutenweg und schneller Weg).</summary>
    [Theory]
    [InlineData(2, 3)] // Managerpaarung: Minutenweg mit Verlängerung
    [InlineData(4, 5)] // ohne Manager im Pokal: schneller Weg mit Penaltymarke
    public void Knockout_AlwaysHasWinner_ShootoutKept(int home, int away)
    {
        var game = Game();
        var random = new Random(7);
        int ties = 0, overtimeGoals = 0;
        for (var i = 0; i < 400; i++)
        {
            var m = KnockoutMatch.Play(game, 1, game.Clubs[home], game.Clubs[away], MatchCompetition.DebCup, random);
            Assert.True(m.Winner == home || m.Winner == away);
            if (m.HomeGoals == m.AwayGoals)
            {
                ties++;
                Assert.NotNull(m.PenaltyWinner);
            }
            else
            {
                Assert.Null(m.PenaltyWinner);
            }
            overtimeGoals += m.HomeGoals + m.AwayGoals;
        }
        Assert.True(ties > 0, "Kein Penaltyschießen – Test prüft nichts");
        Assert.True(overtimeGoals > 0);
    }

    /// <summary>Zähler gelten je Saison: der Saisonwechsel setzt sie zurück (Länderspiele bleiben).</summary>
    [Fact]
    public void NewSeason_ResetsCounters()
    {
        var game = Game();
        LifecycleRegressionTests.FinishSeason(game);
        var capped = AllPlayers(game).FirstOrDefault();
        Assert.NotNull(capped);
        capped.Caps = 3;
        Assert.True(AllPlayers(game).Sum(p => p.Goals) > 0);
        game.StartNextSeason();
        Assert.All(AllPlayers(game), p =>
            Assert.Equal(0, p.Games + p.Goals + p.Assists + p.Penalties2 + p.Penalties5 + p.GoalsAgainst + p.GoalieMinutes));
        Assert.Equal(3, capped.Caps);
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiver und aktiver Manager, Modus 4 „Liga + Weltkugel“ mit Originaldaten):
    /// Zähler je Saison gefüllt und konsistent, nach jedem Wechsel 0; Abdruck wie <see cref="RngTwinProbeTests"/>.
    /// Modus 1 („nur Weltkugel“) siehe <see cref="FourSeasonChanges_WorldOnly"/>.
    /// Spur spielstaerke-manager (#204): beide Sollwerte neu (Lader der Teamwerte 0x2EF1B beim ersten Spiel, Tagesform
    /// 0x4F527; Begründung bei <see cref="RngTwinProbeTests"/>).
    /// Sollwerte auf dem Stand dieses Branches (Spur spielberechnung, vor der Integration Runde 7) ermittelt; die Leitstelle
    /// zieht sie in der Integration nach.
    /// Spur statistiken (Runde 8, #135): beide neu, nur durch die Einzelprämien im Kontostand (Gegenprobe ohne
    /// Einzelprämien: alte Werte); Basis <c>10eb6c5</c>.
    /// Integration Runde 8 (Leitstelle): beide neu auf dem integrierten Stand (Lader, Einzelprämien, Werbebewertung).
    /// Spur transfer-vertraege-komplett (Teil 1): aktiv neu durch die Ablöse am Saisonende (0x27DF3); Gegenprobe ohne Ablöse:
    /// alter Wert.
    /// Spur transfer-vertraege-komplett (Rest von Teil 2): aktiv neu durch das Startgehalt Wert(Gehalt) (0x29EE4) und die Laufzeit
    /// 1 Jahr beim Kauf (0x42CB1); Gegenprobe mit altem Startgehalt und alter Laufzeit: alter Wert.
    /// Spur transfer-vertraege-komplett (Teil 5): beide neu durch den Listenwechsel aus dem Spielerpool (0x494EF, anderer Verbrauch
    /// der Hauptquelle; Begründung bei <see cref="RngTwinProbeTests"/>).
    /// Spur spielberechnung-komplett, Teil 1 (#204): beide neu (Wanderung der Teamwerte 0x9D1B, Begründung bei
    /// <see cref="RngTwinProbeTests"/>).
    /// Spur spielberechnung-komplett, Teil 3 (0x38B8F, 0x386D1): beide neu (Verletzungen und Schützenwahl im Spiel, Begründung bei <see cref="RngTwinProbeTests"/>).
    /// Spur spielberechnung-komplett, Teil 5 (0x3901B): beide neu (Penaltyschießen nach Verlängerung, Begründung bei <see cref="RngTwinProbeTests"/>).
    /// Integration Runde 9 (Leitstelle): beide auf dem integrierten Stand neu, einschließlich Minutenweg der Pokalspiele
    /// ohne Manager bei eingeschaltetem Anzeigeschalter (DEB-Pokal an, 0x3702E).
    /// Fix #351 (Leitstelle): neu – Play-offs und Aufstiegsrunde ohne Manager über den schnellen Weg 0x3731F (Meldungsmatrix voreingestellt aus, 0x37058), anderer Zufallsverbrauch und andere Meister.
    /// Spur kader-transfer, Teil 1 (A170, 0x29A3F): neu – der Managerverein in der Bundesliga hat 25 statt 20 Spieler (Kader nach Start-Liga), andere Kadererzeugung, Gehälter und Spielstärke.
    /// Spur kader-transfer, Teil 3 (0x058DC, 0x063BE): aktiv neu (Käufer gestärkt, Sponsorzuschuss gewürfelt, Begründung bei <see cref="RngTwinProbeTests"/>).
    /// Spur kader-transfer, Teil 4 (0x54E71, 0x072DD, 0x059B8): beide neu (Markt täglich, Begründung bei <see cref="RngTwinProbeTests"/>).
    /// </summary>
    [GameDataTheory]
    [InlineData(false, "ACB9AEB0EC55EA6F")]
    [InlineData(true, "22A42E99FA069083")]
    public void FourSeasonChanges_OriginalDatabase_CountersPerSeason(bool active, string expected)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = 2 });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 5);
        var season = game.Season;
        var seasons = new List<string>();
        var print = RngTwinProbeTests.Run(game, active,
            afterStep: g =>
            {
                if (g.Season == season)
                    return;
                season = g.Season;
                Assert.All(AllPlayers(g), p => Assert.Equal(0, p.Games + p.Goals + p.Assists + p.Penalties2 + p.GoalsAgainst));
            },
            beforeChange: g =>
            {
                var players = AllPlayers(g).ToList();
                var goals = players.Sum(p => p.Goals);
                var league = g.Results.Sum(r => r.HomeGoals + r.AwayGoals);
                Assert.True(goals > league / 2, $"{goals} Spielertore bei {league} Ligatoren");
                Assert.True(players.Sum(p => p.Assists) <= goals);
                Assert.True(players.Sum(p => p.GoalieMinutes) > 0);
                seasons.Add($"{g.Season}: Ligaspiele {g.Results.Count}, Tore/Spiel {league / (double)g.Results.Count:F2}, Spielertore {goals}");
            });
        foreach (var line in seasons)
            output.WriteLine(line);
        Assert.Equal(4, seasons.Count);
        output.WriteLine(print);
        Assert.Equal(expected, print);
    }

    /// <summary>Mehrsaisonprobe Modus 1 („nur Weltkugel“): WM-Spiele mit der neuen Berechnung, vier Saisonwechsel.</summary>
    [Fact]
    public void FourSeasonChanges_WorldOnly()
    {
        var game = GameModeTests.Create(11, false, true, 2);
        var print = GameModeTests.Fingerprint(game, 5);
        output.WriteLine(print);
        Assert.Equal(WorldOnlyReference, print);
    }

    // Spur transfer-vertraege-komplett (Teil 5): Titel und Konten gleich, nur die Hauptquelle (R…) verschoben – Pool und
    // Listenwechsel (0x494EF) beim Saisonwechsel ziehen andere Zahlen als die bisherige Startliste.
    // Spur spielstaerke-manager: Titel und Konten gleich, nur die Hauptquelle (R…) verschoben – der Lader der Teamwerte zieht
    // beim ersten (WM-)Spiel 1650 Zahlen; die WM-Nationalteams spielen ohne Streuung (RatedNation, Form aus dem Lader).
    // spielstaerke-manager-2: Spiele des Bundestrainers sind Managerpaarungen (Energie, Reihenwechsel, Seitentausch je
    // Minute, 0x87F7/0x8EF2/0x80CB) – andere WM-Ergebnisse und Pokalquelle (C…), Hauptquelle und Konten gleich.
    // Integration Runde 8, Welle 3 (R8W3I-01): Managerpaarungen des Bundestrainers mit dem Spielstand vor den Toren der
    // Minute – andere WM-Ergebnisse ab Saison 5 und Pokalquelle (C…); Hauptquelle und Konten gleich.
    // spielberechnung-komplett, Teil 1 (#204): Titel, Konten und Pokalquelle gleich, nur die Hauptquelle (R…) verschoben – die
    // Wanderung des Spielstarts 0x9D1B zieht beim ersten (WM-)Spiel weitere Zahlen (im Modus „nur Weltkugel“ keine weitere).
    // spielberechnung-komplett, Teil 2 (R8W3I-06): Hauptquelle und Konten gleich; das Nationalteam des Bundestrainers zieht
    // jetzt den Strafspieler 0x378C5 aus seinem Kader – andere WM-Ergebnisse ab Saison 3 und Pokalquelle (C…).
    // kader-transfer, Teil 1 (A170, 0x29A3F): Titel, Konten und Pokalquelle gleich, nur die Hauptquelle (R…) verschoben – der
    // Managerverein (Bundesliga) bekommt beim Spielstart 25 statt 20 Kaderplätze; die Kadererzeugung zieht dafür mehr Zahlen.
    private const string WorldOnlyReference =
        "WorldChampion=52|500000|WorldChampion=52|500000|WorldChampion=52|500000|WorldChampion=54|500000|WorldChampion=52|500000|R540662232 C5178594";

    /// <summary>
    /// Größenordnung gegen das Video „MEISTER werden in 75 Minuten“ (Reihe karriere, Bild 01019: Oberliga nach dem
    /// 36. Spieltag 1732 Tore in 216 Spielen = 8,0 je Spiel, 17 Unentschieden = 7,9 %). Originaldaten, eine Hauptrunde.
    /// </summary>
    [GameDataFact]
    public void OriginalDatabase_GoalsPerGame_InVideoRange()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = 30, StartLeague = League.Oberliga });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 7);
        while (game.PlayedMatchDays < 36)
            TestSeason.PlayMatchDay(game);
        foreach (var league in Enum.GetValues<League>())
        {
            var results = game.Results.Where(r => r.League == league).ToList();
            var perGame = results.Average(r => r.HomeGoals + r.AwayGoals);
            var draws = results.Count(r => r.HomeGoals == r.AwayGoals) / (double)results.Count;
            output.WriteLine($"{league}: {results.Count} Spiele, {perGame:F2} Tore je Spiel, Heim {results.Average(r => r.HomeGoals):F2}, Unentschieden {draws:P1}");
            // Integration Runde 8, Welle 3: Der Videobeleg gilt nur für die Oberliga (8,0). Die Bundesliga (ohne
            // Managerpaarung) streut über den gemeinsamen Zufallsstrom zwischen 5,97 und 6,69 (Seeds 1–12, R8W3I-14) – in
            // der Oberliga 6–10 wie belegt, in den übrigen Ligen nur eine Plausibilitätsspanne 5–11.
            if (league == League.Oberliga)
                Assert.InRange(perGame, 6.0, 10.0);
            else
                Assert.InRange(perGame, 5.0, 11.0);
            // Spur spielberechnung-komplett (#204): Mit der Wanderung der Teamwerte liegen die Bundesligavereine enger
            // zusammen (69 … 94); Seed 7 hat dort 15,3 % Unentschieden. Mittel über 12 Seeds × 4 Saisons 10,7 %, Video
            // (Reihe folgen, Bild folge21_01558: Bundesliga nach dem 44. Spieltag) 30 von 264 = 11,4 %.
            Assert.InRange(draws, 0.03, league == League.Oberliga ? 0.15 : 0.17);
        }
    }
}
