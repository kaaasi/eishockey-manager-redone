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
    /// Review #662 (R14NT-01): Die Reihenfolge ist belegt, mit Bundestrainer aber im Original nicht erreichbar (vor dem 13. April
    /// gibt es keinen, danach rechnet die WM-Schleife). Der Test ernennt deshalb künstlich zu Saisonbeginn und prüft nur den
    /// Hauptweg der Vereinssaison bis zum 13. April; die WM nach einer echten Ernennung prüft
    /// <see cref="Mode4_AfterRealAppointment_DriftDayWorldCupDate_UsesLineupForm"/>.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Day_MainPathOnly_NationalValuesBeforeDrift_DriftDayShowsRolledForm(int seed)
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
            Assert.True(last <= SeasonCalendar.ClubSeasonEnd); // nur Hauptweg (Phase Running endet mit dem 13. April)
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

    // ---- R14NT-01/02: WM-Schleife des Originals nach der Ernennung ------------------------------------------------

    /// <summary>
    /// Modus 4 mit echter Ernennung: Verein 2 spielt mit Höchstwerten (Vorbedingung Tabellenplatz wie
    /// <c>NationalCoachMode4Tests</c>) bis nach dem 13. April; dann Kader und Aufstellung automatisch.
    /// </summary>
    private static (GameState Game, NationalCoach Coach) AppointedAfterClubSeason(int seed)
    {
        var game = GameModeTests.Create(seed, true, true, 2);
        Assert.Equal(GameMode.Full, game.Mode);
        ManagedSquads.PlayAtTopStrength(game, 0);
        NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        var coach = Assert.IsType<NationalCoach>(game.NationalCoach); // echte Ernennung am 13. April (Datei 0x553A2 ff.)
        Assert.Equal(0, coach.Manager);
        Assert.Equal(SeasonCalendar.ClubSeasonEnd + 1, game.CurrentDay);
        game.AutoNominate();
        coach.AutoFormation();
        return (game, coach);
    }

    /// <summary>Form 1 für den ganzen WM-Kader: jede Aufstellung liegt dann weit unter der gewürfelten Form 45 … 55.</summary>
    private static void LowerSquadForm(NationalCoach coach)
    {
        foreach (var member in coach.Squad)
            member.Player.Form = 1;
    }

    /// <summary>
    /// R14NT-01 (P1): Regressionstest (vorher rot: Form Deutschlands an den Wandertagen 224/228 gewürfelt, 45 … 55).
    /// [ORIGINAL] Nach der Ernennung (Datei 0x55482/0x55485) läuft die WM in der Schleife Datei 0x55570–0x55740: je WM-Tag
    /// Menü des Bundestrainers, 0x37F/0x380 und 0x426BC (Datei 0x55599–0x555CA), 0x952C(5, 0) (Datei 0x555D6), dann die
    /// WM-Spiele 0x30E7B(1) (Datei 0x555F3). Die kleine Wanderung 0x9D1B ruft die Schleife nicht (einziger Aufruf hinter
    /// 0x555D6 bei Datei 0x56448, Saisonwechsel). Also spielt Deutschland auch am 5. Gruppentermin (Tag 224 ≡ 0 mod 7) und
    /// am Halbfinale (Tag 228 ≡ 4 mod 7) mit der Form der Aufstellung.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Mode4_AfterRealAppointment_DriftDayWorldCupDate_UsesLineupForm(int seed)
    {
        var (game, coach) = AppointedAfterClubSeason(seed);
        var probe = new FormProbe(NationalTeams.Germany);
        game.Competitions.Insert(0, probe); // vor der WM: misst die Form Deutschlands unmittelbar vor deren Spielen
        for (var guard = 0; game.Phase == SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 100, "Saisonende nicht erreicht");
            LowerSquadForm(coach);
            game.Continue();
        }
        var driftDates = probe.Seen.Where(s => MatchEngine.IsDriftDay(s.Day)).ToList();
        Assert.Equal([224, 228], driftDates.Select(s => s.Day));
        Assert.All(probe.Seen, s =>
        {
            Assert.InRange(s.DefenseForm, 0, 44);
            Assert.InRange(s.AttackForm, 0, 44);
        });
    }

    /// <summary>Misst an jedem WM-Tag die Form eines Landes vor den WM-Spielen (steht in <c>Competitions</c> vor der WM).</summary>
    private sealed class FormProbe(int nation) : ISeasonCompetition
    {
        public List<(int Day, int DefenseForm, int AttackForm)> Seen { get; } = [];
        public Competition Kind => Competition.WorldCup;
        public DayEvents Days => DayEvents.WorldCup;
        public Title? Title => null;
        public int? Winner => null;
        public void StartSeason(GameState game) { }
        public (int Opponent, bool Home)? MatchOn(GameState game, int day, int club) => null;

        public void PlayDay(GameState game, int day, List<SeasonStep> steps)
        {
            var (defense, attack, _) = game.NationalTeamValues(nation);
            Seen.Add((day, defense.Form, attack.Form));
        }
    }

    /// <summary>
    /// R14NT-02 (P2): Regressionstest (vorher rot: der Tageslauf stellte am WM-Tag nach dem Zug des Bundestrainers neu auf,
    /// Datei 0x54D41–0x54DAA des Hauptwegs; am Tag 216 spielte Deutschland mit Abwehrform 20 statt 1). [ORIGINAL] In der
    /// WM-Schleife stellt nach dem Menü nur 0x426BC bei Datei 0x555CA auf, mit 0x37F = 0 also wirkungslos: Es spielt die
    /// Aufstellung, die der Bundestrainer zuletzt gesehen hat, auch wenn sich Spielerwerte danach ändern.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Mode4_WorldCupDay_PlaysLineupSeenInCoachTurn(int seed)
    {
        var (game, coach) = AppointedAfterClubSeason(seed);
        var germany = game.Clubs[NationalTeams.Germany];
        game.Continue(); // Vereinsmanager fertig → Zug des Bundestrainers am Tag 216 (Menü stellt automatisch auf)
        Assert.True(game.NationalTurn);
        var day = game.CurrentDay;
        Assert.True(SeasonCalendar.Events(day).HasFlag(DayEvents.WorldCup));
        var seen = LineupSlot.All.Select(s => coach.Formation[s]).ToList();
        foreach (var p in seen.OfType<Player>())
            p.Form = 1; // nach dem Menü: eine neue Automatik nähme jetzt andere Spieler
        game.Continue();
        Assert.True(game.CurrentDay > day);
        Assert.Equal(seen, LineupSlot.All.Select(s => coach.Formation[s]).ToList());
        var (defense, attack, _) = MatchEngine.LineupTeamValues(coach.Formation);
        Assert.Equal((defense.Form, attack.Form), (germany.TeamValues!.Value.DefenseForm, germany.TeamValues.Value.AttackForm));
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
        game.Continue(); // Sprung zum ersten WM-Termin und Ernennung; der Tag ist noch nicht gerechnet
        Assert.NotNull(game.NationalCoach);
        Assert.Empty(game.NationalCoach!.Squad);
        // R14NT-06: den ersten WM-Tag mit Bundestrainer und leerem Kader wirklich rechnen (vorher hielt der Test vorher an).
        var firstDay = game.CurrentDay;
        Assert.Equal(GameState.FirstWorldCupDay, firstDay);
        game.Continue();
        Assert.True(game.CurrentDay > firstDay, "erster WM-Tag nicht gerechnet");
        Assert.Empty(game.NationalCoach!.Squad);
        Assert.Equal((0, 0, 9, 10), game.NationalRatings(NationalTeams.Germany));
        Assert.Equal(form, (germany.TeamValues.Value.DefenseForm, germany.TeamValues.Value.AttackForm));
    }
    // ---- Teil 2: Nebenbefunde #580 -------------------------------------------------------------------------------

    /// <summary>
    /// #580/#623: Regressionstest (vorher rot: Handaufnahme hängte hinten an). [ORIGINAL] Klick in „Spieler in WM-Kader“
    /// (Datei 0x3C97A) → 0x3CABF → 0x42CB1 (Datei 0x3CB09, Grenze 0x1D = 29, Manager 5): erster Platz, dessen Spielerart größer
    /// als die des Neuen ist (wie <see cref="PlayerFactory.InsertSlot"/>). Stürmer, Verteidiger, Torhüter, Verteidiger → T, V, V, S.
    /// </summary>
    [Fact]
    public void HandNomination_InsertsByRole()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        var coach = game.NationalCoach!;
        Assert.Empty(coach.Squad);
        var candidates = game.NationalCandidates().Select(c => c.Player).Where(p => p.InjuredGames == 0).ToList();
        Player Pick(PlayerRole role, int skip = 0) => candidates.Where(p => p.Role == role).Skip(skip).First();
        var (f, d1, g, d2) = (Pick(PlayerRole.Forward), Pick(PlayerRole.Defender), Pick(PlayerRole.Goalkeeper), Pick(PlayerRole.Defender, 1));
        foreach (var p in new[] { f, d1, g, d2 })
            Assert.Equal(NominationResult.Added, game.ToggleNomination(p));
        Assert.Equal([g, d1, d2, f], coach.Squad.Select(m => m.Player));

        // Entlassen und wieder aufnehmen: wieder hinter dem letzten seiner Art.
        Assert.Equal(NominationResult.Removed, game.ToggleNomination(d1));
        Assert.Equal(NominationResult.Added, game.ToggleNomination(d1));
        Assert.Equal([g, d2, d1, f], coach.Squad.Select(m => m.Player));
    }

    /// <summary>
    /// #580: [ORIGINAL] Aufbau des Hauptmenüs (Datei 0x4509E–0x450A4): im Zug des Bundestrainers Sperrbytes DGROUP 0x518C/0x518D
    /// (Tabelle 0x5159 + 42, Reihe Mannschaft, Plätze 4 und 5 = „Transfer“, „Training“) = WM-Stufe DGROUP 0x7AE (gespielte
    /// WM-Termine; 0 bei der Anmeldung, Datei 0x30E89, + 1 je Termin außer dem Finale, Datei 0x311C8/0x31152). DOSBox `file19_26` (20.04.1994).
    /// </summary>
    [Fact]
    public void WorldCupStage_LocksNationalSquadFromFirstDate()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        Assert.Equal(0, game.WorldCupStage);
        Assert.False(game.NationalSquadLocked);
        game.AutoNominate();
        game.Continue();
        Assert.Equal(1, game.WorldCupStage);
        Assert.True(game.NationalSquadLocked);
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 50);
            game.Continue();
        }
        // R14NT-04: [ORIGINAL] Der Finaltermin zählt nicht (Datei 0x31152 überspringt 0x311C8): nach dem Finale 7, nicht 8.
        Assert.NotNull(game.WorldCup!.Winner);
        Assert.Equal(7, game.WorldCupStage);
        game.StartNextSeason();
        Assert.Equal(0, game.WorldCupStage);
        Assert.False(game.NationalSquadLocked);
    }
}
