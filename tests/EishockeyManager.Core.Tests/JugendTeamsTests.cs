using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Jugendteams nach em.exe (Spur geld Teil 3, G2): Startwerte (Datei 0x29B76–0x29BC1, #327), Ligaspieltag (Datei 0x39DEC)
/// und Saisonwechsel (Datei 0x3A13B). Inventur A049, F085, F086.
/// </summary>
public class JugendTeamsTests
{
    private const int LastLeagueDay = 147;

    private static GameState Create(League startLeague, int club = 2, int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", ClubIndex = club, StartLeague = startLeague });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Regression #327: das Ligabyte der Jugend ist die Start-Liga (DGROUP 0x11A), nicht die Liga des Vereins.</summary>
    [Fact]
    public void Start_YouthLeagueIsTheSetupStartLeague()
    {
        var game = Create(League.ZweiteLiga, club: 42); // Verein ohne Liga → Oberliga
        Assert.All(game.Finance(game.ActiveManager).Youth, t => Assert.Equal((2, 6), (t.League, t.Rank)));
    }

    [Theory]
    [InlineData(League.Bundesliga, 80)]
    [InlineData(League.ZweiteLiga, 66)]
    [InlineData(League.Oberliga, 52)]
    public void Start_StrengthFollowsTheStartLeague(League startLeague, int expected)
    {
        var game = Create(startLeague);
        Assert.All(game.Finance(game.ActiveManager).Youth, t => Assert.Equal(expected, t.Strength));
    }

    /// <summary>
    /// Regression A049/F086: im Saisonwechsel steigen Platz 1–2 auf (Start als 11./12.), Platz 12 ab (Start als 2.), sonst
    /// Neustart auf Platz 6; in der 1. Liga bzw. 3. Liga bleibt die Liga. Gesetzt nach dem letzten Ligaspieltag: danach
    /// bewegt sich im Original nichts mehr bis zum Saisonwechsel.
    /// </summary>
    [Fact]
    public void SeasonChange_PromotesTopTwo_RelegatesTwelfth_RestartsOthersSixth()
    {
        var game = TestGames.Create(5);
        var me = game.Managers[0];
        for (var guard = 0; game.FinanceDay <= LastLeagueDay; guard++)
        {
            Assert.True(guard < 1000);
            me.Money = Math.Max(me.Money, 1_000_000);
            game.Continue();
        }
        var youth = game.Finance(me).Youth;
        youth[0].MoveTo(1 * YouthTeam.TeamsPerLeague + 1); // 2. Platz 2. Liga
        youth[1].MoveTo(1 * YouthTeam.TeamsPerLeague + 11); // 12. Platz 2. Liga
        youth[2].MoveTo(0 * YouthTeam.TeamsPerLeague + 3); // 4. Platz 1. Liga
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000);
            me.Money = Math.Max(me.Money, 1_000_000);
            game.Continue();
        }
        Assert.Equal((2, 2), (youth[0].League, youth[0].Rank)); // vor dem Saisonwechsel unverändert
        game.StartNextSeason();
        Assert.Equal((1, 12), (youth[0].League, youth[0].Rank));
        Assert.Equal((3, 2), (youth[1].League, youth[1].Rank));
        Assert.Equal((1, 6), (youth[2].League, youth[2].Rank));
    }

    [Theory]
    [InlineData(2, 1, 1, 11)]
    [InlineData(3, 2, 2, 12)]
    [InlineData(1, 1, 1, 6)] // 1. Liga: kein Aufstieg
    [InlineData(1, 12, 2, 2)]
    [InlineData(3, 12, 3, 6)] // 3. Liga: kein Abstieg
    [InlineData(2, 3, 2, 6)]
    [InlineData(2, 11, 2, 6)]
    public void SeasonChange_Table(int league, int rank, int expectedLeague, int expectedRank)
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var team = game.Finance(me).Youth[0];
        (team.League, team.Rank) = (league, rank);
        game.YouthSeasonChange(me);
        Assert.Equal((expectedLeague, expectedRank), (team.League, team.Rank));
    }

    /// <summary>[ORIGINAL] 0x3A1A3–0x3A207: für t = 2, 1 Stärke[t] = (2 · Stärke[t] + Stärke[t − 1]) / 3, Schüler unverändert.</summary>
    [Fact]
    public void SeasonChange_YoungerTeamMovesUp()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var youth = game.Finance(me).Youth;
        (youth[0].Strength, youth[1].Strength, youth[2].Strength) = (30, 61, 85);
        game.YouthSeasonChange(me);
        Assert.Equal((30, 50, 77), (youth[0].Strength, youth[1].Strength, youth[2].Strength));
    }

    /// <summary>
    /// Je Ligaspieltag ändert sich die Stärke um höchstens 1 (Klemme 10–85) und der Platz um höchstens T ≤ 4 (Datei 0x3A0CF);
    /// an Tagen ohne Ligaspiel bewegt sich nichts (Kalenderbit 0–2, Datei 0x55008).
    /// </summary>
    [Fact]
    public void MatchDay_StepsAreBounded_AndOnlyOnLeagueDays()
    {
        var game = TestGames.Create(5);
        var me = game.Managers[0];
        var youth = game.Finance(me).Youth;
        game.SetYouthFunding(me, YouthTeamKind.Junioren, YouthTeam.MaxFunding(YouthTeamKind.Junioren));
        var moved = 0;
        var leagueDays = SeasonCalendar.DaysOf(DayEvents.League).ToHashSet();
        for (var guard = 0; game.FinanceDay <= LastLeagueDay + 20; guard++)
        {
            Assert.True(guard < 1000);
            me.Money = Math.Max(me.Money, 1_000_000);
            var before = youth.Select(t => (t.Strength, t.Position)).ToArray();
            var from = game.FinanceDay;
            game.Continue();
            var days = Enumerable.Range(from + 1, game.FinanceDay - from).Count(leagueDays.Contains);
            for (var t = 0; t < youth.Length; t++)
            {
                Assert.InRange(youth[t].Strength, YouthTeam.MinStrength, YouthTeam.MaxStrength);
                Assert.InRange(Math.Abs(youth[t].Strength - before[t].Strength), 0, days);
                // Platz bleibt in der Liga (Klemme 0–11), höchstens 4 Plätze je Ligaspieltag
                Assert.Equal(youth[t].League, before[t].Position / YouthTeam.TeamsPerLeague + 1);
                Assert.InRange(Math.Abs(youth[t].Position - before[t].Position), 0, 4 * days);
                if (youth[t].Position != before[t].Position)
                    moved++;
            }
        }
        Assert.True(moved > 0, "kein Platz bewegt");
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel): ohne Förderung sinkt die Stärke Richtung 40, mit voller Förderung steigt sie
    /// Richtung 90 (Deckel 85); Plätze und Ligen bleiben gültig, zwei gleiche Läufe ergeben dieselben Stände.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_StrengthFollowsFunding(bool active)
    {
        string Run()
        {
            var game = Create(active ? League.Oberliga : League.Bundesliga, seed: 5);
            var me = game.Managers[0];
            var youth = game.Finance(me).Youth;
            var start = youth[2].Strength;
            if (active)
                foreach (var kind in Enum.GetValues<YouthTeamKind>())
                    game.SetYouthFunding(me, kind, YouthTeam.MaxFunding(kind));
            var log = new List<string>();
            for (var change = 0; change < 4; change++)
            {
                for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
                {
                    Assert.True(guard < 1000, "Saisonende nicht erreicht");
                    me.Money = Math.Max(me.Money, 1_000_000);
                    if (active)
                        TestSeason.KeepSquad(game, me, extendExpired: true); // R12K-03: Abdruck mit gehaltenem Kader
                    game.Continue();
                }
                game.StartNextSeason();
                foreach (var team in youth)
                {
                    Assert.InRange(team.League, 1, YouthTeam.Leagues);
                    Assert.InRange(team.Rank, 1, YouthTeam.TeamsPerLeague);
                    log.Add($"{change}:{team.Kind}:{team.League}/{team.Rank}/{team.Strength}");
                }
            }
            foreach (var team in youth)
                if (active)
                    Assert.True(team.Strength >= start + 15, $"{team.Kind}: {start} → {team.Strength}");
                else
                    Assert.True(team.Strength <= start - 15, $"{team.Kind}: {start} → {team.Strength}");
            return string.Join(";", log);
        }

        var run = Run();
        Assert.Equal(run, Run());
        // R11G-03: Sollwert des Protokolls (Seed 5, 4 Saisonwechsel) – Abdruck der Spur geld Teil 3 für Platz- und Stärkeverlauf.
        // Integration Runde 12: neu ermittelt, weil wettbewerbe-spiel (A184 Belastung, A174 Computervereine) die Ligaergebnisse und
        // damit den Zufallsverbrauch vor der Jugendrechnung ändert; Richtung (aktiv ≥ +15, passiv ≤ −15) unverändert geprüft.
        // Integration Runde 12 (+ kader-transfer): erneut neu, U4/U5/U9 ändern Ligaergebnisse und Zufallsverbrauch.
        Assert.Equal(active ? ExpectedActive : ExpectedPassive, run);
    }

    // kader-start Teil 2 (U15-a, L13): neu – der fest gewählte Verein tauscht beim Spielstart in die Start-Liga (Datei 0x29CF2, auch innerhalb der Bundesliga); Zufallsverbrauch und Teamwerte ab Spielstart anders.
    // kader-start Teil 3 (R-K5, L13): neu – Satzfolge vor der Spielvorbereitung aus dem Spielstart-Seed (Datei 0x43A17), Zufallsplatz des Starttauschs als Satzplatz.
    // Runde 14, nationalteam Teil 4 (#664): neu ab Saison 1 – der aktive Manager wird nicht Bundestrainer, die WM läuft trotzdem
    // in der WM-Schleife (Datei 0x55570–0x55740: kein Training, kein Markt außer nach bestimmten WM-Tagen (dort 224/228, falsch),
    // keine Wanderung); Saison 0 unverändert. Gegenprobe: Schleife nur mit Bundestrainer (alter Stand) → alter Wert.
    // Review #674 (R14NT4-01/-05, #675): neu ab Saison 0 – (c) der WM-Schleife nach den WM-Spielen der Tage 216, 220 und 230
    // (Zähler 0x3B7 = Tag + 1) samt Markt-Verfall 0x071BB und ohne Listenwechsel; Takttage (Wanderung, Talentsuche) und Heilung
    // des Hauptwegs mit dem Zähler (Tage 3, 6, 10, 13 … statt 0, 4, 7, 11 …). Gegenprobe: nur der Hauptweg zurück → weder alter
    // noch neuer Wert, beide Teile wirken.
    // Nachprüfung #681 (R14NT4N-01): neu – der Tageslauf von Tag 216 (Wechsel nach den Spielen des 13. April, Zähler 217) rechnet wie im Original den Takt des Hauptwegs (Datei 0x55204–0x552D3: Wanderung, Talentsuche, Training/Markt/Verträge nach den Remake-Regeln des Hauptwegs, Heilung, Aufstellungsautomatik), erst danach die Schleife; anderer Zufallsverbrauch ab dem 14. April jeder Saison. Gegenprobe: nur Core-Änderung zurück (Testdateien neu) → alter Wert. Saison 0 unverändert, Richtung (≥ +15) weiter geprüft.
    // kader-start Runde 14 Teil 2b (Herkunft berichtigt, Review R14KS-04): neu durch die neuen Spielerfelder im Abdruck (Zähler und
    // Überträge je Wettbewerb, StoredTableRank), den Spielerwert je Wettbewerb (A135) und das Tabellenfeld der Zuschauerformel
    // (A046, aktive Läufe). Taktik 375h ändert keinen Abdruck (Rückmutation im Review).
    // Merge #669 (kader-start) in #672: neu ermittelt, beide Änderungen wirken (jeweils allein → deren Wert).
    // Fixlauf #676: neu durch die Play-off-Paarung nach Tabelle DGROUP 0x144 (Halbfinale 1–8 gegen 3–6, Heimrecht beim kleineren Platz; Serien in Platzfolge des Originals, auch die Spielreihenfolge im Viertelfinale) – andere Play-off-Spiele, danach anderer Zufallsverbrauch. Gegenprobe: alter Playoffs.cs → alle alten Werte. Saison 0 unverändert, Richtung weiter geprüft.
    // Merge main (#672) in #683: neu ermittelt, beide Ursachen wirken – #672 (Tageszähler 0x3B7, WM-Schleife ohne Bundestrainer) und
    // #676 (Halbfinale nach 0x144). Gegenproben: alte Paarung (Playoffs.cs von e5fe46f) → Wert von main; alter Tagesablauf
    // (src-Änderungen von #672 zurück) → Wert von #683 am Head 84fe66c.
    // #678/#679 (Tagestakt Hauptweg): neu – 0x07169 (Training, Markt-Verfall, Gebote, Angebote, Verträge) nur an den Takttagen je Manager, Listenwechsel nur in den Managerzügen der Tage mit Ereignis, Training Mi/Sa statt Mi/So; anderer Spielverlauf und Zufallsverbrauch ab der ersten Woche. Stadionbau am Zähler ≡ 0 allein ändert keinen Abdruck (Gegenprobe: Montag zurück → gleicher Wert; alles zurück → alter Wert). Richtung (≥ +15) weiter geprüft.
    // Merge main (#683) in #688: neu ermittelt, beide Ursachen wirken – #683 (Halbfinale nach 0x144) und #688 (Tagestakt Hauptweg).
    // Gegenproben: #683 zurück (Playoffs.cs, OriginalImport*.cs, SaveGame.cs von ca47ff0) → Wert von #688 am Head ca47ff0;
    // alter Takt (GameState.Integration.cs, CupsNational.WorldCupLoop.cs, GameState.Manager.cs von 4e72f01, Training Mi/So) → Wert von main.
    // Review #688 (R688-03/-04/-05, Fixlauf Leitstelle Runde 14): neu. Gegenprobe gesamt: alter TransferMarket.StartSeason und Tageswechsel
    // nach dem Halt (SeasonFlow.RunDays ohne OpenDay) → Wert des Merge-Stands 5245767. Ursache allein R688-05 (Saisonwechsel behält die Markt-Plätze, kein Listenwechsel 0x494EF; Gegenprobe nur alter StartSeason
    // → Wert 5245767). Tendenz je Takt (A134), Aufstellung am Takt und an Tagen mit Ereignis, Heilung je Manager, Sommerpause
    // und entlassene Manager einzeln zurück → gleicher Wert.
    // Nutzer 09.11.2026 („Sommerpause wie im Original“): neu – Tagesabschluss 0x3ED01 je Tag der Sommerpause (Datei 0x56512: fünf
    // Monatsabschlüsse, Zinsen, Werbung, Börse) und Sommerpause nach dem Kaderwechsel (Datei 0x5645D vor 0x564D2), Werbeangebote des
    // Saisonbeginns danach (0x56650). Gegenprobe: ohne Tagesabschluss der Sommerpause → Wert von f98f22f.
    // Nachprüfung #691 (R688S-01, Leitstelle Runde 14): neu – Sommerpause ab Zähler 234 = 1. Mai (Datei 0x26F55 setzt 233, 0x564BC zählt
    // weiter), kein Tagesabschluss des 30. April (Werbeangebote am Monatsletzten, April-Abschluss, Börse): anderer Zufallsverbrauch ab
    // dem 1. Mai; Saison 0 unverändert, Richtung weiter geprüft. Gegenprobe: Start 233 zurück → alter Wert.
    // Runde 15, Spur tagesablauf (#699 Punkt 2, Entscheidung Nutzer R15-E1 = A): neu – Tag 0 jeder Saison wird vor dem ersten Menü
    // geöffnet (Datei 0x565A7/0x43A93 → 0x54D5C → 0x55073 → 0x55204): Tagesabschluss 0x3ED01/0x3F833 je Manager auch für den
    // 10. September (Zinskonto, Werbeangebote, Bewertung, Börse), Listenwechsel-Wurf von Manager 0 vor dessen Menü; Jugend von Tag 0
    // weiter nach dem Menü. Anderer Zufallsverbrauch ab dem ersten Tag. Rückschaltprobe: Teil-1-Quellen von 4194e9d → alle alten Werte.
    // Runde 15, Spur kader (#698, A134, R204, R15-E3 = C; Fixlauf nach Rebase auf tagesablauf #725): neu – Jugend am Saisonende
    // (0x26F20, Datei 0x2728C–0x27320: zwei Zufallszahlen je Manager und Saison, ab 1995 Junioren), Tendenz/Steuerbyte der
    // Startkader (0x4907A aus 0x42CB1), Vertragslose aus Managervereinen weder auf der Liste noch vertragslos, sondern ganz verloren
    // (Datei 0x27DE3–0x27E9D, 0x425B6; FILE44 → FILE46 KJELLBERG). Rückschaltprobe: Quellen (src/) von main 2f9cc99 → Wert von main.
    // Ursache (Review #741 P3-2): allein Teil 1 – nur JoinSeasonEndYouth zurück → Wert von main, passiv und aktiv; Teil 2, Teil 3
    // und R15-E3 wirken nur zusammen mit den Junioren.
    // Runde 15, Spur abgleich-fix (#730, R15-E2 = C): neu – das Training begrenzt die Belastung vorzeichenbehaftet (Datei 0x4BD56,
    // cbw): ab 128 → 50 statt bis 150; andere Belastung, Spielenergie und Ergebnisse ab dem ersten Training. Rückschaltprobe:
    // Squad.Training.cs von 18972c0 → alte Werte.
    // #772 (Spur abgleich-rest, Runde 16): neu – Spielvorbereitung 0x8365 wie im Original (Einsatz = Grundwert 60/40 über die Stärke 0x368B1, Angriffsbits aus dem letzten Spiel); andere Ergebnisse ab dem ersten Spieltag. Gegenprobe: ohne PrepareMatch → alter Wert.
    // Danach Spur pool (#743, Fixlauf auf main 846e515, R15-E2 = C): neu – Startkader im Spielerpool (Datei 0x5B0BB–0x5B2BE),
    // 25 Plätze weniger für Benennen, Umverteilung und Listenwechsel. Rückschaltprobe: Transfer.Pool.cs/Transfer.cs von 846e515
    // → Wert nach #772.
    // Runde 17, Spur pruefung-karte (#784, R15-E2 = C): neu – 0x8365 je Paarung zuerst für den Heimverein, dann für den Gast
    // gegen den schon geschriebenen Grundwert des Heimvereins (Datei 0x36D12/0x36D49); andere Gast-Einsätze, Ergebnisse und
    // Zufallsverbrauch. Rückschaltprobe: MatchEngine.Preparation.cs von e1476e6 → alte Werte.
    // Runde 17, Spur wettbewerbe-rest Teil 3 (A113/A142, R15-E2 = C): neu – Liga-Spielplan über die Satzfolge (Datei 0x4E7C7:
    // Buchstabe = Satzplatz, nicht Rang des Vereinsindex) und Aufstiegsrunde nach Tabelle DGROUP 0x324 an den Play-Off-
    // Terminen 0, 2, 5, 7, 10, 12 (Datei 0x31A2C); andere Paarungen ab dem ersten Spieltag. Rückschaltprobe: src/ von 8e49168
    // → Wert von Teil 2.
    private const string ExpectedActive =
        "0:Schueler:2/12/57;0:Jugend:2/11/59;0:Junioren:2/11/60;1:Schueler:1/12/69;1:Jugend:2/6/69;1:Junioren:1/11/72;2:Schueler:1/6/78;2:Jugend:1/11/79;2:Junioren:1/6/79;3:Schueler:1/6/79;3:Jugend:1/6/81;3:Junioren:1/6/81";
    // kader-start Teil 2 (U15-a, L13): neu, siehe oben.
    // kader-start Teil 3 (R-K5, L13): neu – Satzfolge vor der Spielvorbereitung aus dem Spielstart-Seed (Datei 0x43A17), Zufallsplatz des Starttauschs als Satzplatz.
    // Review #662 (R14NT-01, Leitstelle Runde 14): neu – der passive Bundesligist wird Bundestrainer; nach der Ernennung läuft die
    // WM ohne kleine Wanderung (WM-Schleife Datei 0x55570–0x55740), damit ändert sich der Zufallsverbrauch ab dem 14. April.
    // Saison 0 unverändert, Richtung (≤ −15) weiter geprüft.
    // Runde 14, nationalteam Teil 4 (#664): neu ab Saison 1 – mit Bundestrainer trainiert in der WM-Schleife nur noch Manager 0
    // nach bestimmten WM-Tagen (Datei 0x5565C, DGROUP 0xA70E = 0; dort 224/228, falsch), Markt ebenso (statt Training an jedem
    // Trainingstag und täglichem Markt). Die Gegenprobe „Schleife nur mit Bundestrainer“ ändert diesen Wert nicht (er hängt allein
    // am Weg mit Bundestrainer); Saison 0 unverändert, Richtung (≤ −15) weiter geprüft.
    // Review #674 (R14NT4-01/-05, #675): neu ab Saison 0 – (c) der WM-Schleife nach den WM-Spielen der Tage 216, 220 und 230
    // (Zähler 0x3B7 = Tag + 1) samt Markt-Verfall 0x071BB und ohne Listenwechsel; Takttage (Wanderung, Talentsuche) und Heilung
    // des Hauptwegs mit dem Zähler (Tage 3, 6, 10, 13 … statt 0, 4, 7, 11 …). Gegenprobe: nur der Hauptweg zurück → weder alter
    // noch neuer Wert, beide Teile wirken. Richtung weiter geprüft.
    // Nachprüfung #681 (R14NT4N-01): neu – der Tageslauf von Tag 216 (Wechsel nach den Spielen des 13. April, Zähler 217) rechnet wie im Original den Takt des Hauptwegs (Datei 0x55204–0x552D3: Wanderung, Talentsuche, Training/Markt/Verträge nach den Remake-Regeln des Hauptwegs, Heilung, Aufstellungsautomatik), erst danach die Schleife; anderer Zufallsverbrauch ab dem 14. April jeder Saison. Gegenprobe: nur Core-Änderung zurück (Testdateien neu) → alter Wert. Saison 0 unverändert, Richtung (≤ −15) weiter geprüft.
    // Fixlauf #676: neu durch die Play-off-Paarung nach Tabelle DGROUP 0x144 (Halbfinale 1–8 gegen 3–6, Heimrecht beim kleineren Platz; Serien in Platzfolge des Originals, auch die Spielreihenfolge im Viertelfinale) – andere Play-off-Spiele, danach anderer Zufallsverbrauch. Gegenprobe: alter Playoffs.cs → alle alten Werte. Saison 0 unverändert.
    // Merge main (#672) in #683: neu ermittelt, beide Ursachen wirken – #672 (Tageszähler 0x3B7, WM-Schleife ohne Bundestrainer) und
    // #676 (Halbfinale nach 0x144). Gegenproben: alte Paarung (Playoffs.cs von e5fe46f) → Wert von main; alter Tagesablauf
    // (src-Änderungen von #672 zurück) → Wert von #683 am Head 84fe66c.
    // #678/#679 (Tagestakt Hauptweg): neu – 0x07169 (Training, Markt-Verfall, Gebote, Angebote, Verträge) nur an den Takttagen je Manager, Listenwechsel nur in den Managerzügen der Tage mit Ereignis, Training Mi/Sa statt Mi/So; anderer Spielverlauf und Zufallsverbrauch ab der ersten Woche. Stadionbau am Zähler ≡ 0 allein ändert keinen Abdruck (Gegenprobe: Montag zurück → gleicher Wert; alles zurück → alter Wert). Richtung (≤ −15) weiter geprüft.
    // Merge main (#683) in #688: neu ermittelt, beide Ursachen wirken – #683 (Halbfinale nach 0x144) und #688 (Tagestakt Hauptweg).
    // Gegenproben: #683 zurück (Playoffs.cs, OriginalImport*.cs, SaveGame.cs von ca47ff0) → Wert von #688 am Head ca47ff0;
    // alter Takt (GameState.Integration.cs, CupsNational.WorldCupLoop.cs, GameState.Manager.cs von 4e72f01, Training Mi/So) → Wert von main.
    // Review #688 (R688-03/-04/-05, Fixlauf Leitstelle Runde 14): neu. Gegenprobe gesamt: alter TransferMarket.StartSeason und Tageswechsel
    // nach dem Halt (SeasonFlow.RunDays ohne OpenDay) → Wert des Merge-Stands 5245767. Ursache allein R688-05 (Saisonwechsel behält die Markt-Plätze, kein Listenwechsel 0x494EF; Gegenprobe nur alter StartSeason
    // → Wert 5245767). Tendenz je Takt (A134), Aufstellung am Takt und an Tagen mit Ereignis, Heilung je Manager, Sommerpause
    // und entlassene Manager einzeln zurück → gleicher Wert.
    // Nutzer 09.11.2026 („Sommerpause wie im Original“): neu – Tagesabschluss 0x3ED01 je Tag der Sommerpause (Datei 0x56512: fünf
    // Monatsabschlüsse, Zinsen, Werbung, Börse) und Sommerpause nach dem Kaderwechsel (Datei 0x5645D vor 0x564D2), Werbeangebote des
    // Saisonbeginns danach (0x56650). Gegenprobe: ohne Tagesabschluss der Sommerpause → Wert von f98f22f.
    // Nachprüfung #691 (R688S-01, Leitstelle Runde 14): neu – Sommerpause ab Zähler 234 = 1. Mai (Datei 0x26F55 setzt 233, 0x564BC zählt
    // weiter), kein Tagesabschluss des 30. April (Werbeangebote am Monatsletzten, April-Abschluss, Börse): anderer Zufallsverbrauch ab
    // dem 1. Mai; Saison 0 unverändert, Richtung weiter geprüft. Gegenprobe: Start 233 zurück → alter Wert.
    // Runde 15, Spur tagesablauf (#699 Punkt 2, Entscheidung Nutzer R15-E1 = A): neu – Tag 0 jeder Saison wird vor dem ersten Menü
    // geöffnet (Datei 0x565A7/0x43A93 → 0x54D5C → 0x55073 → 0x55204): Tagesabschluss 0x3ED01/0x3F833 je Manager auch für den
    // 10. September (Zinskonto, Werbeangebote, Bewertung, Börse), Listenwechsel-Wurf von Manager 0 vor dessen Menü; Jugend von Tag 0
    // weiter nach dem Menü. Anderer Zufallsverbrauch ab dem ersten Tag. Rückschaltprobe: Teil-1-Quellen von 4194e9d → alle alten Werte.
    // Runde 15, Spur kader (#698, A134, R204, R15-E3 = C; Fixlauf nach Rebase auf tagesablauf #725): neu – Jugend am Saisonende
    // (0x26F20, Datei 0x2728C–0x27320: zwei Zufallszahlen je Manager und Saison, ab 1995 Junioren), Tendenz/Steuerbyte der
    // Startkader (0x4907A aus 0x42CB1), Vertragslose aus Managervereinen weder auf der Liste noch vertragslos, sondern ganz verloren
    // (Datei 0x27DE3–0x27E9D, 0x425B6; FILE44 → FILE46 KJELLBERG). Rückschaltprobe: Quellen (src/) von main 2f9cc99 → Wert von main.
    // Ursache (Review #741 P3-2): allein Teil 1 – nur JoinSeasonEndYouth zurück → Wert von main, passiv und aktiv; Teil 2, Teil 3
    // und R15-E3 wirken nur zusammen mit den Junioren.
    // Runde 15, Spur abgleich-fix (#730, R15-E2 = C): neu – das Training begrenzt die Belastung vorzeichenbehaftet (Datei 0x4BD56,
    // cbw): ab 128 → 50 statt bis 150; andere Belastung, Spielenergie und Ergebnisse ab dem ersten Training. Rückschaltprobe:
    // Squad.Training.cs von 18972c0 → alte Werte.
    // #772 (Spur abgleich-rest, Runde 16): neu – Spielvorbereitung 0x8365 wie im Original; danach Spur pool (#743, Fixlauf auf main
    // 846e515, R15-E2 = C): neu – Startkader im Spielerpool (Datei 0x5B0BB–0x5B2BE), 25 Plätze weniger für Benennen, Umverteilung
    // und Listenwechsel. Rückschaltprobe: Transfer.Pool.cs/Transfer.cs von 846e515 → Wert nach #772.
    // Runde 17, Spur pruefung-karte (#784, R15-E2 = C): neu – 0x8365 je Paarung zuerst für den Heimverein, dann für den Gast
    // gegen den schon geschriebenen Grundwert des Heimvereins (Datei 0x36D12/0x36D49); andere Gast-Einsätze, Ergebnisse und
    // Zufallsverbrauch. Rückschaltprobe: MatchEngine.Preparation.cs von e1476e6 → alte Werte.
    // Runde 17, Spur wettbewerbe-rest Teil 3 (A113/A142, R15-E2 = C): neu – Liga-Spielplan über die Satzfolge (Datei 0x4E7C7:
    // Buchstabe = Satzplatz, nicht Rang des Vereinsindex) und Aufstiegsrunde nach Tabelle DGROUP 0x324 an den Play-Off-
    // Terminen 0, 2, 5, 7, 10, 12 (Datei 0x31A2C); andere Paarungen ab dem ersten Spieltag. Rückschaltprobe: src/ von 8e49168
    // → Wert von Teil 2.
    private const string ExpectedPassive =
        "0:Schueler:2/2/67;0:Jugend:2/2/69;0:Junioren:2/2/72;1:Schueler:3/2/52;1:Jugend:2/6/58;1:Junioren:2/6/61;2:Schueler:3/6/47;2:Jugend:2/6/46;2:Junioren:3/2/47;3:Schueler:3/6/42;3:Jugend:3/2/43;3:Junioren:3/6/42";
}

public class JugendTeamsMigrationTests
{
    /// <summary>[APPROX] Schema 24 kennt keine Jugendstärke: Startstärke der Liga, in der das Team spielt.</summary>
    [Fact]
    public void Schema24_GetsStartStrengthOfTheCurrentLeague()
    {
        var game = TestGames.Create();
        var youth = game.Finance(game.ActiveManager).Youth;
        youth[1].MoveTo(2 * YouthTeam.TeamsPerLeague + 4); // 3. Liga
        youth[1].Strength = YouthTeam.StartStrength(League.Oberliga);
        var json = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        json["schema"] = 24;
        var removed = 0;
        foreach (var manager in json["manager"]!.AsArray())
        {
            var teams = manager!["FinanceData"]!["Youth"]!;
            var items = teams is System.Text.Json.Nodes.JsonArray plain ? plain : teams["items"]!.AsArray();
            foreach (var team in items)
                if (team!.AsObject().Remove("Strength"))
                    removed++;
        }
        Assert.Equal(3, removed);

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game!));
    }
}
