using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nationalteam, Runde 14, Teil 1: Reihenfolge von 0x952C (Länderwerte des Bundestrainers) und der kleinen Wanderung im
/// Tageslauf 0x54C9F (#594, R13IN-01) und Länderwerte über den Saisonwechsel (#595, R13IN-02 zu R13RD-04).
/// </summary>
public class NationalteamRunde14Tests
{
    // ---- #594 ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// #594: Regressionstest (vorher rot: die Form Deutschlands nach einem Wandertag war die der Aufstellung).
    /// [ORIGINAL] Tagesfunktion 0x54C9F: 0x952C(Manager 5, 1) bei Datei 0x54F9C–0x54FBF, danach ohne Sprung aus dem Bereich die
    /// kleine Wanderung 0x9D1B(1) bei Datei 0x5525A–0x55292 (Tageszähler ≡ 0 oder 4 mod 7). Die Wanderung würfelt die Form
    /// 0x1C/0x1D aller Plätze einschließlich der Nationalteams neu, Kondition und Technik der Nationalteams (0x18–0x1B) bleiben.
    /// Modus 4 (Fahne und Weltkugel), Wandertag ohne WM-Spiel: Form gewürfelt (45 … 55), Kondition/Technik aus der Aufstellung.
    /// An einem Tag ohne Wanderung bleibt die Form der Aufstellung.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Day_NationalValuesBeforeDrift_DriftDayShowsRolledForm(int seed)
    {
        var game = GameModeTests.Create(seed, true, true, 2);
        Assert.Equal(GameMode.Full, game.Mode);
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var coach = game.NationalCoach!;
        Assert.Equal(NationalTeams.Germany, coach.Nation);
        coach.AutoFormation();
        var germany = game.Clubs[NationalTeams.Germany];

        bool driftSeen = false, plainSeen = false;
        for (var guard = 0; guard < 400 && !(driftSeen && plainSeen); guard++)
        {
            // Form der Aufgestellten weit unter 45: die Form der Aufstellung kann nicht mit einer gewürfelten zusammenfallen.
            foreach (var slot in LineupSlot.All)
                if (coach.Formation[slot] is { } p)
                    p.Form = 1;
            game.Continue();
            Assert.Equal(SeasonPhase.Running, game.Phase);
            var last = game.CurrentDay - 1; // RunDays hält vor dem Spieltag des Managers: zuletzt gerechnet ist der Vortag
            if (SeasonCalendar.Events(last).HasFlag(DayEvents.WorldCup) || last < 1)
                continue;

            var (defense, attack, _) = MatchEngine.LineupTeamValues(coach.Formation);
            Assert.InRange(defense.Form, 0, 44);
            Assert.InRange(attack.Form, 0, 44);
            Assert.Equal((defense.Condition, defense.Technique, attack.Condition, attack.Technique),
                game.NationalRatings(NationalTeams.Germany));
            var values = germany.TeamValues!.Value;
            if (MatchEngine.IsDriftDay(last))
            {
                driftSeen = true;
                Assert.InRange(values.DefenseForm, 45, 55);
                Assert.InRange(values.AttackForm, 45, 55);
            }
            else
            {
                plainSeen = true;
                Assert.Equal((defense.Form, attack.Form), (values.DefenseForm, values.AttackForm));
            }
        }
        Assert.True(driftSeen && plainSeen, $"Wandertag {driftSeen}, Tag ohne Wanderung {plainSeen}");
    }

    // ---- #595 ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// #595 (R13RD-04): [ORIGINAL] DOSBox FILE20 → FILE21 (Saisonwechsel): Felder 0x18–0x1B Deutschlands bleiben, die Form
    /// 0x1C/0x1D wird wie bei allen Plätzen neu gewürfelt (große Wanderung 0x56448, Modus 10). Ablauf aus dem Review
    /// (docs/reviews/2026-11-04-integration-nationalteam-r13.md, Abschnitt 3): „nur Weltkugel“, nur ein Stürmer 88/96/57 →
    /// (0, 0, 9, 10); nach <c>StartNextSeason</c> weiter (0, 0, 9, 10), Form 45 … 55. Der erste Tag nach der neuen Ernennung
    /// (Kader leer) schreibt nichts ([APPROX] in <c>UpdateNationalTeamValues</c>).
    /// </summary>
    [Fact]
    public void SeasonChange_GermanyConditionTechniqueKept_FormRolled()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.ClearFormation();
        var jooris = coach.Rows.First(p => p.Role == PlayerRole.Forward && p.Available);
        (jooris.Condition, jooris.Technique, jooris.Form) = (88, 96, 57);
        Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Forward(0, 0), jooris));
        game.OnNationalLineupLeft();
        Assert.Equal((0, 0, 9, 10), game.NationalRatings(NationalTeams.Germany));

        // Bis zum Saisonende nur noch der Bundestrainer selbst: Spieler aus der Aufstellung nehmen hieße neu rechnen.
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 3000, "Saisonende nicht erreicht");
            game.Continue();
        }
        var germany = game.Clubs[NationalTeams.Germany];
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);

        game.StartNextSeason();
        Assert.Equal((0, 0, 9, 10), game.NationalRatings(NationalTeams.Germany));
        Assert.InRange(germany.TeamValues!.Value.DefenseForm, 45, 55);
        Assert.InRange(germany.TeamValues.Value.AttackForm, 45, 55);
        var form = (germany.TeamValues.Value.DefenseForm, germany.TeamValues.Value.AttackForm);

        // Erster Tag der neuen Saison: neue Ernennung, Kader leer → 0x952C schreibt im Remake nichts.
        Assert.Null(game.NationalCoach);
        game.Continue();
        Assert.NotNull(game.NationalCoach);
        Assert.Empty(game.NationalCoach!.Squad);
        Assert.Equal((0, 0, 9, 10), game.NationalRatings(NationalTeams.Germany));
        Assert.Equal(form, (germany.TeamValues.Value.DefenseForm, germany.TeamValues.Value.AttackForm));
    }
}
