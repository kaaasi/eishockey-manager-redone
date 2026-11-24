using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur aufstellung Teil 1, B31 (A892, A893, A956, A959, A960; B31 = A, Nutzer 24.11.2026): Verlassen von Aufstellung und Taktik
/// schreibt wie im Original ohne Bedingung – 0x952C (Datei 0x41513–0x4151A) bzw. 0x48E62 (Aufruf Datei 0x48E19). Das bloße Öffnen
/// ändert nichts (AGENTS.md §4). Vorher (rot): nur nach einer Änderung, die Taktik des Bundestrainers gar nicht.
/// </summary>
public sealed class AufstellungB31Tests
{
    /// <summary>A956: Aufstellung des Bundestrainers ohne Änderung verlassen → Länderwerte aus der Aufstellung.</summary>
    [GameDataFact]
    public void NationalLineup_LeftUnchanged_WritesNationalValues()
    {
        var (d, game) = NationalLineupScreenTests.StartWorldOnly();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.AutoFormation();
        game.OnNationalLineupLeft();
        var before = game.NationalRatings(NationalTeams.Germany);
        foreach (var slot in LineupSlot.All)
            if (coach.Formation[slot] is { } p)
                p.Technique = Math.Max(1, p.Technique - 30);

        d.OpenTile("Aufstellung");
        Assert.IsType<NationalLineupScreen>(d.Host.Current);
        d.Frames(2);
        Assert.Equal(before, game.NationalRatings(NationalTeams.Germany)); // Öffnen ändert nichts
        d.Press(Key.Escape);

        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.NotEqual(before, game.NationalRatings(NationalTeams.Germany));
    }

    /// <summary>
    /// A893: [ORIGINAL] Der Taktikbildschirm des Bundestrainers ist derselbe 0x47F2B; 0x48E62 schreibt beim Verlassen 0x26 im Satz
    /// Deutschlands (Teambyte DGROUP 0x9EDA = 0x3F) = Grundwert + Zuschlag „Unentschieden“ (Datei 0x4902B–0x4903E), Angriff bleibt.
    /// </summary>
    [GameDataFact]
    public void NationalTactics_Left_WritesMatchEffortOfGermany()
    {
        var (d, game) = NationalLineupScreenTests.StartWorldOnly();
        var t = game.NationalTactics;
        Assert.Same(t, game.NationFieldTactics(NationalTeams.Germany));
        t.EffortMode = TacticMode.Semi;
        t.SetEffort(0, ScoreSituation.Base, 50);
        t.SetEffort(0, ScoreSituation.Tied, 10);
        t.RecordMatchFields(33, 2);

        d.OpenTile("Taktik");
        Assert.IsType<TacticsScreen>(d.Host.Current);
        d.Frames(2);
        Assert.Equal((33, 2), (t.MatchEffort, t.MatchAttack)); // Öffnen ändert nichts
        d.Press(Key.Escape);

        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal((60, 2), (t.MatchEffort, t.MatchAttack));
    }

    /// <summary>
    /// A960: [ORIGINAL] 0x48E62 schreibt erst 0x26 (Datei 0x4902B–0x4903E), dann die Neuaufstellung bei eingeschalteter Einstellung
    /// (Datei 0x49071 → 0x426BC). Nach dem Einschalten im Bildschirm stehen beide Ergebnisse: Aufstellung neu, 0x26 geschrieben.
    /// </summary>
    [GameDataFact]
    public void ClubTactics_SwitchOnAuto_Left_WritesEffortAndRelines()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        club.SetAutoLineup(false);
        club.ClearLineup();
        var t = club.Tactics;
        t.EffortMode = TacticMode.Semi;
        t.SetEffort(0, ScoreSituation.Base, 50);
        t.SetEffort(0, ScoreSituation.Tied, 10);
        t.RecordMatchFields(33, 2);

        d.OpenTile("Taktik");
        Assert.IsType<TacticsScreen>(d.Host.Current);
        var r = TacticsScreen.GoalieArrows;
        d.Click(r.X + r.W / 2, r.Y + 2); // Pfeil oben an Knopf 0: „automatisch aufstellen“ ein (Datei 0x47CA0)
        Assert.True(club.AutoLineupEnabled);
        Assert.Empty(club.Lineup.Players);
        d.Press(Key.Escape);

        Assert.Equal((60, 2), (t.MatchEffort, t.MatchAttack));
        Assert.NotEmpty(club.Lineup.Players);
    }
}
