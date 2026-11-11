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
    /// R688N-03 (Nachprüfung #688): Die Gegenprobe (Takt nach 0x952C desselben Tages) wird nur sichtbar, wenn das Training des
    /// Takts die Länderwerte ändert – mit Saat 3 nicht. Gegenprobe am Head mit den Saaten 3, 5, 7, 9, 11, 13, 17, 19, 23, 29, 31:
    /// rot für 7, 9, 11, 13, 17, 19, 23; daher vier rot werdende Saaten neben 3.
    /// Spur tagesablauf (#699 Punkt 3, Entscheidung Nutzer R15-E1 = A): An Tagen mit Ereignis stellt der Tageswechsel vor den
    /// Menüs Satz 5 neu auf und ruft 0x952C(5, 0) (Datei 0x54D76–0x54DAA, nach der Wanderung Datei 0x5525A–0x55292 des
    /// Tageswechsels; 0x952C schreibt 0x18–0x1D mit beiden Merkern, Datei 0x09672 → 0x096E6 → 0x09749/0x09771). Ein Halttag ist ein
    /// Spieltag des Managers und damit ein Tag mit Ereignis: Dort gilt jetzt die Form der Aufstellung, auch am Wandertag (vorher:
    /// gewürfelte Form, weil die Neuaufstellung erst nach den Menüs lag). Rückschaltprobe: Teil 3 zurück (alte Reihenfolge) → der
    /// alte Test grün, dieser rot. #725 R725-03: Halttage ohne Ereignis gibt es im Hauptweg nicht (Halt = Spieltag des Managers);
    /// die gewürfelte Form der Wanderung zwischen zwei Halten sichert <see cref="Day_MainPath_SmallDrift_RollsFormOfAllSlotsBetweenHalts"/>.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(17)]
    public void Day_MainPathOnly_NationalValuesBeforeDrift_EventDayHaltShowsLineupForm(int seed)
    {
        var game = GameModeTests.Create(seed, true, true, 2);
        Assert.Equal(GameMode.Full, game.Mode);
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var coach = game.NationalCoach!;
        Assert.Equal(NationalTeams.Germany, coach.Nation);
        coach.AutoFormation();
        var germany = game.Clubs[NationalTeams.Germany];

        bool eventDriftSeen = false, plainSeen = false;
        for (var guard = 0; guard < 400 && !(eventDriftSeen && plainSeen); guard++)
        {
            // Form der Aufgestellten weit unter 45: die Form der Aufstellung kann nicht mit einer gewürfelten zusammenfallen.
            foreach (var slot in LineupSlot.All)
                if (coach.Formation[slot] is { } p)
                    p.Form = 1;
            game.Continue();
            Assert.Equal(SeasonPhase.Running, game.Phase);
            // RunDays hält vor dem Spieltag h des Managers. #688 (R688-04/-08): Der Tageswechsel zu h (Takt 0x07169, Wanderung) ist
            // wie im Original schon gerechnet, 0x952C(5, 1) (Datei 0x54F9C, nach den Menüs) zuletzt am Vortag.
            var halt = game.CurrentDay;
            var last = halt - 1;
            Assert.True(last <= SeasonCalendar.ClubSeasonEnd); // nur Hauptweg (Phase Running endet mit dem 13. April)
            if (SeasonCalendar.Events(last).HasFlag(DayEvents.WorldCup) || last < 1)
                continue;

            var (defense, attack, _) = MatchEngine.LineupTeamValues(coach.Formation);
            var values = germany.TeamValues!.Value;
            if (game.IsListChangeDay(halt))
            {
                // #699 Punkt 3: Neuaufstellung und 0x952C(5, 0) nach Takt und Wanderung, vor dem Menü – Werte der Aufstellung des
                // Halttags (Form 1 der gestörten Spieler ist durch die Neuaufstellung ersetzt).
                Assert.Equal((defense.Condition, defense.Technique, attack.Condition, attack.Technique),
                    game.NationalRatings(NationalTeams.Germany));
                Assert.Equal((defense.Form, attack.Form), (values.DefenseForm, values.AttackForm));
                if (GameState.IsDailyRunDay(halt))
                    eventDriftSeen = true;
                else
                    plainSeen = true;
                continue;
            }

            // #725 R725-03: Ein Halttag ist ein Spieltag des Managers und damit immer ein Tag mit Ereignis (Tagesbyte ∉ {0, 9, 255},
            // Datei 0x54D4C–0x54D65); der frühere Zweig „Halttag ohne Ereignis“ war tot (Assert.Fail dort → grün). Die kleine
            // Wanderung des Hauptwegs prüft Day_MainPath_SmallDrift_RollsFormOfAllSlotsBetweenHalts (Mutante N33).
            Assert.Fail($"Halttag {halt} ohne Ereignis – im Hauptweg nicht erwartet");
        }
        Assert.True(eventDriftSeen && plainSeen, $"Wander-Halttag mit Ereignis {eventDriftSeen}, Halttag mit Ereignis ohne Wanderung {plainSeen}");
    }

    /// <summary>
    /// #725 R725-03, Mutante N33 (kleine Wanderung im Hauptweg auslassen): [ORIGINAL] Der Tageswechsel des Hauptwegs würfelt an Tagen
    /// mit Zähler ≡ 0 oder 4 (mod 7) die Form 0x1C/0x1D aller Plätze einschließlich der Nationalteams neu (0x9D1B(1), Datei
    /// 0x5525A–0x55292; MatchEngine.DriftTeamValues). Ein Nationalteam ohne Bundestrainer (Index 52) und ein Computerverein
    /// bekommen vor jedem Weiterschalten die Form 1; nach dem Halt ist sie genau dann gewürfelt (45 … 55), wenn zwischen den
    /// beiden Halten ein Wandertag lag, sonst unverändert 1. Gegenprobe N33 (Aufruf in OpenClubSeasonDay ausgelassen) → rot.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Day_MainPath_SmallDrift_RollsFormOfAllSlotsBetweenHalts(int seed)
    {
        var game = GameModeTests.Create(seed, true, true, 2);
        Assert.Equal(GameMode.Full, game.Mode);
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var nation = game.Clubs[NationalTeams.First];
        Assert.NotEqual(NationalTeams.Germany, nation.Index);
        var computer = game.Clubs.First(c => c.Index < NationalTeams.First && !managed.Contains(c.Index));

        bool driftSeen = false, plainSeen = false;
        for (var guard = 0; guard < 400 && game.Phase == SeasonPhase.Running && !(driftSeen && plainSeen); guard++)
        {
            var previous = game.CurrentDay;
            var sentinels = new[] { nation, computer }.Where(c => c.TeamValues is not null).ToList();
            foreach (var club in sentinels)
                club.TeamValues = club.TeamValues!.Value with { DefenseForm = 1, AttackForm = 1 };
            game.Continue();
            var halt = game.CurrentDay;
            if (game.Phase != SeasonPhase.Running || halt > SeasonCalendar.ClubSeasonEnd || sentinels.Count < 2)
                continue;
            // Tageswechsel previous + 1 … halt laufen vor dem Halt (OpenDay, #688 R688-04).
            var drift = Enumerable.Range(previous + 1, halt - previous).Any(GameState.IsDailyRunDay);
            foreach (var club in sentinels)
            {
                var v = club.TeamValues!.Value;
                if (drift)
                {
                    Assert.InRange(v.DefenseForm, 45, 55);
                    Assert.InRange(v.AttackForm, 45, 55);
                }
                else
                    Assert.Equal((1, 1), (v.DefenseForm, v.AttackForm));
            }
            if (drift)
                driftSeen = true;
            else
                plainSeen = true;
        }
        Assert.True(driftSeen && plainSeen, $"Halt nach Wandertag {driftSeen}, Halt ohne Wandertag {plainSeen}");
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
    /// R14NT-01 (P1): Regressionstest (vorher rot: Form Deutschlands an Wandertagen der WM gewürfelt, 45 … 55).
    /// [ORIGINAL] Nach der Ernennung (Datei 0x55482/0x55485) läuft die WM in der Schleife Datei 0x55570–0x55740: je WM-Tag
    /// Menü des Bundestrainers, 0x37F/0x380 und 0x426BC (Datei 0x55599–0x555CA), 0x952C(5, 0) (Datei 0x555D6), dann die
    /// WM-Spiele 0x30E7B(1) (Datei 0x555F3). Die kleine Wanderung 0x9D1B ruft die Schleife nicht (einziger Aufruf hinter
    /// 0x555D6 bei Datei 0x56448, Saisonwechsel). Also spielt Deutschland auch an den WM-Tagen, deren Tageslauf im Hauptweg
    /// gewandert hätte, mit der Form der Aufstellung: Zähler 0x3B7 = Tag + 1 ≡ 0/4 (mod 7), Tage 216, 220, 230 (#675, Review #674;
    /// vorher mit dem Remake-Tag 224/228).
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
        var driftDates = probe.Seen.Where(s => MatchEngine.IsDriftDay(DayCounter.Of(s.Day))).ToList();
        Assert.Equal([216, 220, 230], driftDates.Select(s => s.Day));
        Assert.All(probe.Seen, s =>
        {
            Assert.InRange(s.DefenseForm, 0, 44);
            Assert.InRange(s.AttackForm, 0, 44);
        });
    }

    /// <summary>
    /// Mehrsaisonprobe zu R14NT-01/02 (AGENTS.md §5: 4 Saisonwechsel, passiver und aktiver Manager), Modus 4: Verein 2 je Saison
    /// mit Höchstwerten (Vorbedingung Ernennung). Je Saison echte Ernennung am 13. April, WM vollständig (Stufe 7, Weltmeister),
    /// keine Wanderung in der WM-Schleife: passiv (WM-Kader leer, nichts schreibt die Länderwerte) bleibt die Form Deutschlands über
    /// alle WM-Tage einer Saison gleich; aktiv (Kader „auto“, Aufstellung in Saison 0 geleert, Form 1) liegt sie an jedem WM-Tag
    /// unter 45. Speichern/Laden bleibt stabil.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mode4_FourSeasonChanges_WorldCupLoopEverySeason(bool active)
    {
        var game = GameModeTests.Create(3, true, true, 2);
        var probe = new FormProbe(NationalTeams.Germany);
        game.Competitions.Insert(0, probe);
        for (var season = 0; season < 5; season++)
        {
            probe.Seen.Clear();
            bool nominated = false, appointed = false;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                appointed |= game.NationalCoach?.Manager == 0 && game.CurrentDay > SeasonCalendar.ClubSeasonEnd;
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = Math.Max(m.Money, 10_000_000);
                if (game.Phase == SeasonPhase.Running)
                {
                    TestSeason.KeepSquad(game, game.Managers[0], extendExpired: true); // Vorbedingung Ernennung über die Saisons
                    ManagedSquads.PlayAtTopStrength(game, 0);
                }
                if (active && game.NationalTurn && !nominated)
                {
                    game.AutoNominate();
                    if (season == 0)
                        game.NationalCoach!.ClearFormation();
                    nominated = true;
                }
                if (active && game.NationalCoach is { } c)
                    LowerSquadForm(c);
                game.Continue();
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            Assert.NotNull(game.WorldCup!.Winner);
            Assert.Equal(GameState.FinalWorldCupStage, game.WorldCupStage);
            Assert.True(appointed, $"Saison {season}: keine Ernennung"); // echte Ernennung in jeder Saison
            Assert.Equal(SeasonCalendar.DaysOf(DayEvents.WorldCup).Count(), probe.Seen.Count);
            if (active)
                Assert.All(probe.Seen, v => Assert.InRange(Math.Max(v.DefenseForm, v.AttackForm), 0, 44));
            else
                Assert.Single(probe.Seen.Select(v => (v.DefenseForm, v.AttackForm)).Distinct());

            game.Competitions.Remove(probe);
            var saved = SaveGame.Save(game);
            Assert.Equal(saved, SaveGame.Save(SaveGame.Load(saved).Game!));
            game.Competitions.Insert(0, probe);
            if (season < 4)
                game.StartNextSeason();
        }
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

    /// <summary>
    /// #716 R14G4-06 (Gegenprobe N25): Spätere WM-Termine laufen über Schritt (a) der WM-Schleife (<c>WorldCupLoopStepA</c>,
    /// 0x952C(5, 1) nach dem Menü, Datei 0x555D6), nicht über den Eintrittstag. Am zweiten Zug des Bundestrainers nach dem Menü
    /// die Form der Aufgestellten auf 100 setzen: Nach dem Weiterschalten trägt Deutschland die Form dieser Aufstellung.
    /// Gegenprobe: <c>UpdateNationalTeamValues()</c> nur in <c>WorldCupLoopStepA</c> ausgelassen → rot.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Mode4_LaterWorldCupDay_StepA_UsesLineupForm(int seed)
    {
        var (game, coach) = AppointedAfterClubSeason(seed);
        var germany = game.Clubs[NationalTeams.Germany];
        game.Continue(); // erster Zug des Bundestrainers (Tag 216, Eintrittstag der Schleife)
        Assert.True(game.NationalTurn);
        var first = game.CurrentDay;
        game.Continue();
        for (var guard = 0; !(game.NationalTurn && game.CurrentDay > first); guard++)
        {
            Assert.True(guard < 50 && game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver, "kein späterer Zug des Bundestrainers");
            game.Continue();
        }
        var day = game.CurrentDay;
        Assert.True(SeasonCalendar.Events(day).HasFlag(DayEvents.WorldCup));
        foreach (var p in LineupSlot.All.Select(s => coach.Formation[s]).OfType<Player>())
            p.Form = 100;
        game.Continue();
        Assert.True(game.CurrentDay > day);
        var (defense, attack, _) = MatchEngine.LineupTeamValues(coach.Formation);
        Assert.Equal((100, 100), (defense.Form, attack.Form));
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
