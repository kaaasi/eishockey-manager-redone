namespace EishockeyManager.Core;

/// <summary>
/// WM-Schleife des Originals (Spur nationalteam Runde 14 Teil 4, #664/R14NT-08; Review #662 R14NT-01/02): die Tage nach dem
/// 13. April in den Spielarten mit Vereinssaison, mit und ohne Bundestrainer.
/// </summary>
/// <remarks>
/// [ORIGINAL] Tagesfunktion 0x54C9F, selbst nachgelesen (<c>tools/reverse-engineering/disasm.py gamedata/em.exe 0x55374 0x55760</c>):
/// <list type="bullet">
/// <item>Am Tag mit Markierung 255 geht es nach den Managermenüs über 0x553A2 nach 0x553FF: Ernennung nur bei Modus ≠ 2 und einem
///   Manager unter den ersten fünf (Datei 0x55436–0x554BF, DGROUP 0x9EDA := 0x6AC bei 0x55485). Ohne Ernennung würfelt das
///   Original die Werte Deutschlands (Datei 0x554D9–0x5556D, <see cref="RerollNationalRatings"/>) und läuft danach
///   <b>in dieselbe Schleife</b> Datei 0x55570–0x55740 – im Modus 2 („nur Fahne“) genauso wie im Modus 4 ohne Ernennung.
///   Der Modus 1 („nur Weltkugel“) springt schon am Saisonanfang dorthin (Datei 0x54CEE–0x54D38: Zähler bis zum ersten WM-Tag
///   weiter, Datei 0x54CF7, dann 0x553FF) und läuft ab dem ersten WM-Tag ebenso durch die Schleife (Review #674, R14NT4-04;
///   <see cref="OnDayWorldOnlyLoop"/>).</item>
/// <item>Ein Tag der Schleife: (a) nur mit Bundestrainer und Tagesbyte ≠ 0 sein Menü, 0x37F/0x380 und 0x952C(5, 0) (Datei
///   0x55587–0x555D6, <see cref="NationalCoach"/>); (b) Modus ≠ 2 und Tagesbyte ≠ 0: WM-Spiele 0x30E7B(1) (Datei 0x555DD–0x555F3);
///   (c) Tageszähler DGROUP 0x3B7 ≡ 0 oder 4 (mod 7), geprüft vor dem Weiterzählen: Tagesablauf 0x07169 (Datei 0x5561A–0x5565C,
///   siehe <see cref="WorldCupLoopDailyRun"/>). Der Zähler steht an den Spielen von Tag d auf d + 1 (<see cref="DayCounter"/>),
///   (c) trifft also die WM-Tage 216, 220 und 230 (Zähler 217, 221, 231; Review #674, R14NT4-01);
///   (d) nächster Tag (Datei 0x55670 zählt weiter), je Manager 0x3ED01 und 0x3F833 (Datei 0x5569A–0x556B1), am Zähler ≡ 0
///   (mod 7) 0x3D316 (Datei 0x556B5–0x556CD), je Kaderplatz des Managers 0x07C12 (Datei 0x556D2–0x55712); Tagesbyte 0x10 oder
///   0 → nächster Schleifentag (Datei 0x55724–0x55740), 255 → Saisonwechsel.</item>
/// <item>In der Schleife gibt es keine kleine Wanderung 0x9D1B (nur bei Datei 0x56448, Saisonwechsel), keine Talentsuche 0x06FA5
///   (nur Datei 0x5528A im Hauptweg), keine Jugendteams 0x39DEC (Datei 0x5501C), keinen Listenwechsel 0x494EF des Markts
///   (Datei 0x54E71–0x54E7C, Hauptweg) und kein Training außer in 0x07169.</item>
/// </list>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Läuft der Tag in der WM-Schleife des Originals? [ORIGINAL] Alle Tage nach dem Tag mit Markierung 255 (13. April) bis zum
    /// Saisonwechsel, in jeder Spielart mit Vereinssaison und unabhängig von der Ernennung (siehe Klassenkommentar; #664) –
    /// ohne den ersten: Dessen Tageslauf (Wechsel nach den Spielen des 13. April) rechnet noch der Hauptweg
    /// (<see cref="IsWorldCupEntryDay"/>, R14NT4N-01). Der Modus 1 hat seinen eigenen Anschluss (<see cref="OnDayWorldOnlyLoop"/>).
    /// </summary>
    private bool InWorldCupLoop(int day) => !WorldOnly && day > SeasonCalendar.ClubSeasonEnd + 1;

    /// <summary>
    /// Erster Tag nach dem 13. April (Tag <see cref="SeasonCalendar.ClubSeasonEnd"/> + 1) in den Spielarten mit Vereinssaison:
    /// Sein Tageslauf ist im Remake der Tageswechsel nach den Spielen des Tages mit Markierung 255, Zähler
    /// <see cref="DayCounter.Of"/>(day) = 217 (Nachprüfung #681, R14NT4N-01).
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] Selbst nachgelesen (<c>disasm.py gamedata/em.exe 0x54D41 0x55300</c>): Markierung 255 springt bei Datei 0x54D65
    /// nach 0x55073 und läuft ohne Sprung bis 0x55204; dort zählt 0x3B7 auf 217, je Manager folgen 0x3ED01/0x3F833 (Datei
    /// 0x5522B/0x55230), bei 217 ≡ 0 (mod 7) Talentsuche 0x06FA5 (0x5528A), kleine Wanderung 0x9D1B (0x55292), 0x07169 je
    /// Manager (0x552A6) und Stadionbau 0x3D316 (0x552C3). Erst Datei 0x552D5 prüft die Markierung des gespielten Tags (255) und
    /// geht über die Ernennung in die Schleife. FILE22/FILE44 (Modus 4, 14.04.) haben 0x3B7 = 217: kein weiterer Zählschritt.
    /// Im Remake läuft dieser Tag also über den Hauptweg (<c>OnDayClubSeason</c>) mit Tagesabschluss und Talentsuche
    /// (<see cref="AdvanceFinance"/> mit <c>clubDay: true</c>; Jugendteams nur an Ligaspieltagen), Wanderung, Heilung und
    /// Aufstellungsautomatik; an seiner Stelle von 0x952C des Hauptwegs Schritt (a) der Schleife (<see cref="OnDayWorldCupEntry"/>).
    /// [ORIGINAL] 0x07169 je Manager am Takt 217 ≡ 0 (<see cref="MainPathDailyRun"/>, #678); kein Listenwechsel 0x494EF, den der
    /// Tag mit Markierung 255 überspringt (Datei 0x54E71 liegt in 0x54D68–0x55072; <see cref="IsListChangeDay"/>).
    /// </remarks>
    private bool IsWorldCupEntryDay(int day) => !WorldOnly && day == SeasonCalendar.ClubSeasonEnd + 1;

    /// <summary>
    /// Am Eintrittstag der Schleife statt 0x952C(5, 1) des Hauptwegs: [ORIGINAL] Schritt (a) der Schleife, an einem WM-Tag mit
    /// Bundestrainer dessen Menü und 0x952C(5, 0) (Datei 0x555D6), nach dem Takt des Hauptwegs (Wanderung Datei 0x55292 würfelt
    /// die Form, 0x952C rechnet danach die Werte aus der Aufstellung). Keine Neuaufstellung „Tag mit Ereignis“ (Datei
    /// 0x54D41–0x54DAA liegt im übersprungenen Teil).
    /// </summary>
    private void OnDayWorldCupEntry(int day)
    {
        if (NationalCoachPlaysOn(day))
            UpdateNationalTeamValues();
    }

    /// <summary>
    /// Ein Tag der WM-Schleife (Datei 0x55570–0x55740) im Tageslauf des Remakes, vor den Spielen des Tages <paramref name="day"/>:
    /// <list type="number">
    /// <item>[ORIGINAL] Schritt (c) des Vortags: Er läuft im Original nach den WM-Spielen des Vortags (Datei 0x5565C hinter 0x555F3),
    ///   im Remake also hier (<see cref="WorldCupLoopStepC"/>).</item>
    /// <item>[ORIGINAL] Schritt (d) zum Tag <paramref name="day"/>: Tagesabschluss ohne Talentsuche und Jugendteams
    ///   (<see cref="AdvanceFinance"/> mit <c>clubDay: false</c>), Heilung (<see cref="HealInjuries"/>; 0x07C12 nach dem
    ///   Weiterzählen, Zähler <see cref="DayCounter.Of"/>(day) ≡ 0 mod 7). [APPROX] Wie <see cref="HealInjuries"/>: auch die
    ///   Spieler ohne Manager und die freien Marktspieler, das Original in der Schleife nur die Kaderplätze der Manager.</item>
    /// <item>[ORIGINAL] Schritt (a): an WM-Tagen mit Bundestrainer nach dessen Menü 0x952C(5, 0) (Datei 0x555D6,
    ///   <see cref="UpdateNationalTeamValues"/>). Keine Neuaufstellung „Tag mit Ereignis“ (0x54D41, R14NT-02) und kein 0x952C(5, 1)
    ///   des Hauptwegs: Es spielt die Aufstellung, die der Bundestrainer zuletzt gesehen hat. Schritt (b) sind die Wettbewerbe
    ///   des Tages nach diesem Tageslauf.</item>
    /// </list>
    /// Kein Training und kein Markt an den übrigen Tagen, keine Talentsuche, keine Jugendteams, keine Wanderung, keine
    /// Aufstellungsautomatik außer in Schritt (c) und nach einer Heilung.
    /// </summary>
    private void OnDayWorldCupLoop(int day)
    {
        TrainedUntil = (Season, day); // #678: Training nur in Schritt (c)
        WorldCupLoopStepC(day);
        AdvanceFinance(day, clubDay: false);
        HealInjuries(day);
        if (NationalCoachPlaysOn(day))
            UpdateNationalTeamValues();
    }

    /// <summary>
    /// Modus 1 („nur Weltkugel“) ab dem ersten WM-Tag: dieselbe Schleife (Datei 0x54D38 → 0x553FF → 0x55570; Review #674,
    /// R14NT4-04), vor den Spielen des Tages <paramref name="day"/>.
    /// <list type="bullet">
    /// <item>[ORIGINAL] Schritt (c) des Vortags mit dem Markt-Verfall wie in den übrigen Modi (<see cref="WorldCupLoopStepC"/>);
    ///   Beleg FILE18 → FILE48 und FILE19 → FILE20 (siehe dort).</item>
    /// <item>[ORIGINAL] Schritt (d): Heilung 0x07C12 (Datei 0x556D2–0x55712; [APPROX] wie <see cref="HealInjuries"/> für alle
    ///   Spieler, das Original für die Kaderplätze von Manager 0 – im Modus 1 die Spieler Deutschlands, FILE18 „Verein 7
    ///   DEUTSCHLAND“). [APPROX] Ohne Tagesabschluss 0x3ED01/0x3F833 und Stadionbau 0x3D316: Das Remake hat in diesem Modus keine
    ///   Vereinsfinanzen (Konto im Original FILE19 → FILE20 unverändert, 500.000 DM).</item>
    /// <item>[ORIGINAL] Schritt (a): Länderwerte aus der Aufstellung des Bundestrainers (0x952C(5, 0), Datei 0x555D6).</item>
    /// <item>[ORIGINAL] Am ersten WM-Tag nur (a): Der Sprung Datei 0x54CF7–0x54D38 zählt ohne Tageslauf bis zum ersten WM-Tag, die
    ///   Schleife beginnt mit dessen Spielen; (c) und (d) gibt es erst danach.</item>
    /// </list>
    /// </summary>
    private void OnDayWorldOnlyLoop(int day)
    {
        if (day > FirstWorldCupDay)
        {
            WorldCupLoopStepC(day);
            HealInjuries(day);
        }
        UpdateNationalTeamValues();
    }

    /// <summary>
    /// Schritt (c) der WM-Schleife für die Spiele des Vortags (<paramref name="day"/> − 1): Datei 0x5561A–0x5565C prüft den Zähler
    /// dieser Spiele vor dem Weiterzählen (Datei 0x55670), also <see cref="DayCounter.Of"/>(day − 1) ≡ 0/4 (mod 7), und ruft
    /// 0x07169 (<see cref="WorldCupLoopDailyRun"/>); wirksam nur nach WM-Spielen (<see cref="WorldCupPlayedOn"/>).
    /// </summary>
    private void WorldCupLoopStepC(int day)
    {
        var previous = day - 1;
        if (previous > SeasonCalendar.ClubSeasonEnd && MatchEngine.IsDriftDay(DayCounter.Of(previous))
            && WorldCupPlayedOn(previous))
            WorldCupLoopDailyRun(previous);
    }

    /// <summary>
    /// Hatte der Schleifentag WM-Spiele? [ORIGINAL] Datei 0x555DD–0x555ED: Modus ≠ 2 (DGROUP 0xD0B9) und Tagesbyte ≠ 0; in der
    /// Schleife sind die Tagesbytes ≠ 0 die WM-Tage (0x10).
    /// </summary>
    private bool WorldCupPlayedOn(int day) =>
        Mode != GameMode.ClubOnly && WorldCup is not null && SeasonCalendar.Events(day).HasFlag(DayEvents.WorldCup);

    /// <summary>Grenze und Abzug des Markt-Verfalls in 0x07169. [ORIGINAL] Datei 0x071BB–0x071C8: +0x13 &gt; 0x38 → +0x13 − 10.</summary>
    public const int MarketDecayAbove = 0x38, MarketDecay = 10;

    /// <summary>
    /// Schritt (c) der WM-Schleife: Tagesablauf 0x07169 (Datei 0x5565C) für den Manager DGROUP 0xA70E nach den WM-Spielen des
    /// Tages <paramref name="day"/> (Zähler <see cref="DayCounter.Of"/>(day)).
    /// <list type="bullet">
    /// <item>[ORIGINAL] 0xA70E ist vor den Spielen 5 (Datei 0x55581/0x555BC); 0x30E7B setzt es über 0x36B5D auf 0 (Datei 0x36B7A),
    ///   und die Strafen der Paarungen ohne Manager (0x35818 aus 0x37A62) schreiben Feld +0x16 des Landes, bei allen Ländern ohne
    ///   Bundestrainer 0 (Datei 0x35AB8–0x35AC3). 0x07169 endet bei 0xA70E ≥ Zahl der Manager sofort (Datei 0x07172–0x0717B →
    ///   0x07C0C). Also wirkt (c) nur nach WM-Spielen und nur für Manager 0; an den übrigen Tagen mit Zähler ≡ 0/4 (Tage 223 und
    ///   227, ohne WM-Spiele) nichts.</item>
    /// <item>[APPROX] Mit Bundestrainer: Traf die letzte Strafe des Spieltags Deutschland (Feld +0x16 = 6 nach Datei 0x55494),
    ///   bliebe 0xA70E = 5 und (c) wirkungslos; das Remake rechnet immer Manager 0.</item>
    /// <item>[ORIGINAL] Markt (nur Manager 0, Datei 0x0718F–0x071CC): Belastung +0x13 der Marktspieler über 0x38 sinkt um 10
    ///   (<see cref="MarketDecayAbove"/>). Belege (Modus 1, <c>emtool hok dump</c>): FILE18 (Zähler 217) → FILE48 (219) alle
    ///   Marktspieler E −6/−7 (eine Ausführung), FILE19 (217) → FILE20 (231) E −13/−14 (zwei), Liste gleich. Kein Listenwechsel
    ///   (0x494EF hat nur die Aufrufer 0x437D5 und 0x54E7C im Hauptweg). [APPROX] Untergrenze 50 (<see cref="TrainingRules.MinStrain"/>):
    ///   Das Original zieht ohne Grenze ab (57–59 → 47–49); im Remake ist der Marktspieler zugleich Spieler seines Vereins, dessen
    ///   Belastung in 50 … 150 bleibt (<see cref="PlayerFactory.Validate"/>, Dateien kader-start).</item>
    /// <item>Inhalt für Manager 0 (nicht im Modus 1): Heilung seiner Kaderplätze (Datei 0x07B4E → 0x07C12 mit dem Zähler der Spiele,
    ///   <see cref="PlayerInjuries.IsHealingDay"/>), danach je Platz das Training (Datei 0x07B5E → 0x0659C,
    ///   <see cref="TrainingRules.Apply"/>, Kosten wie <see cref="TrainOneDay"/>), Aufstellungsautomatik (Datei 0x07C07).
    ///   [ORIGINAL] Verträge (Ankündigungen, Verlängerungen, Sperren; Datei 0x073E3–0x07AEF) über <see cref="AnnounceContractEnds"/>
    ///   nur für Manager 0 (Kaderplätze 0x5DC3 + 0xA70E · 0x618, Datei 0x07402), Verfall offener Gebote (Datei 0x07215–0x0726A)
    ///   und Angebotswurf auf seine Markt-Plätze (Datei 0x072DD) über <see cref="TransferMarket.CatchUpManagerDay"/> (#678), in der
    ///   Reihenfolge des Originals: Markt, Verträge, Heilung und Training, Aufstellung.</item>
    /// <item>[APPROX] Modus 1: nur der Markt. Die Kaderplätze von Manager 0 sind dort die Spieler Deutschlands (FILE18 „Verein 7
    ///   DEUTSCHLAND“), das Remake führt keinen eigenen Kader des Managers in diesem Modus; Konto FILE19 → FILE20 unverändert.</item>
    /// </list>
    /// </summary>
    private void WorldCupLoopDailyRun(int day)
    {
        if (Managers.Count == 0 || Managers[0] is not { Dismissed: false } manager)
            return;
        DailyRunMarket(manager); // #678: Verfall, Gebotsverfall und Angebotswurf von Manager 0 (Datei 0x0718F–0x073CE)
        if (WorldOnly)
            return;
        AnnounceContractEnds(day, manager); // #678: nur die Kaderplätze von Manager 0 (Datei 0x07402: Satz 0x5DC3 + 0xA70E · 0x618)
        var club = Clubs[manager.ClubIndex];
        if (PlayerInjuries.IsHealingDay(DayCounter.Of(day)))
        {
            var healed = false;
            foreach (var p in club.Players)
                healed |= PlayerInjuries.Heal(p);
            if (healed)
                PlayerInjuries.RefillLineup(club);
        }
        TrainManager(manager);
        club.ApplyAutoLineup();
    }
}
