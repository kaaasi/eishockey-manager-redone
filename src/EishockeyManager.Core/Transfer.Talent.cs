namespace EishockeyManager.Core;

/// <summary>Ergebnis des Auftrags an den „Spion“ (Knopf „Talent“ in der Transferliste).</summary>
public enum TalentSearchStart
{
    /// <summary>Bezahlt, der „Spion“ ist unterwegs; das Ergebnis kommt nach einigen Tagen als Nachricht.</summary>
    Started,

    /// <summary>
    /// Es läuft schon eine Suche. [ORIGINAL] Im Original gibt es diesen Fall nicht: Die Transferliste zeichnet den Knopf
    /// „Talent“ nur bei Zähler 0 (Datei 0x46704) und wertet den Klick nur dann aus (Datei 0x468C1).
    /// </summary>
    AlreadyRunning,

    /// <summary>„Sie haben nicht / genug Geld !“ (DGROUP 0x1211, Datei 0x46A3D).</summary>
    NotEnoughMoney,
}

/// <summary>
/// Die Talentsuche des Originals: Knopf „Talent“ in der Transferliste (em.exe Datei 0x465F1), Rückfrage „Möchten Sie, daß
/// Ihr 'Spion' / für Sie auf Talentsuche geht?“ mit „Preis: … DM“ und den Knöpfen „Aber immer!“ / „Lieber nicht.“
/// (DGROUP 0x1339/0x133D, 0x1341, 0x1311/0x1315, Datei 0x468E7–0x4696E). Ablauf und Ergebnis in <c>FinanceYouth.cs</c>
/// (Datei 0x06FA5, 0x06D7F), Preis im Tagesabschluss (Datei 0x3EEA2–0x3EF4E).
/// </summary>
/// <remarks>
/// Belege: <c>docs/analyse/re-rest.md</c> „Basis 0x46E – Talentsuche“ und <c>docs/finanzen-rekonstruktion.md</c>
/// „Talentsuche (Spur geld, Teil 6)“. Bis Teil 6 gab es Rabatt nach Jugendförderung, 4 Wochen, Chance nach Jugendplatz und
/// Grenze 30 – nichts davon steht im Original; der „Spion“ im Fenster „Jugendteams“ entfällt (A328: die Texte nutzt nur
/// 0x465F1).
/// </remarks>
public static class TalentScout
{
    /// <summary>
    /// Preis beim Spielstart. [ORIGINAL] DGROUP 0x20D + 4 · Manager, Startwert 0x41EB0 = 270.000 Drittel-DM = 90.000 DM
    /// (Datenbereich der em.exe); Video („LetsShow“, ca. 11:15): „Preis: 90.000 DM“.
    /// </summary>
    public const long StartPrice = 90_000;

    /// <summary>
    /// Neuer Preis: 60.000 + 10.000 · random(0, 10) DM. [ORIGINAL] Datei 0x3EEC0–0x3EEF3; danach · 3 / 1 (DGROUP 0x79D/0x7A1,
    /// DM → Drittel) und auf 3.000 Drittel abgerundet (Datei 0x3EEF7–0x3EF4E) – bei Vielfachen von 10.000 DM ohne Wirkung.
    /// </summary>
    public const long PriceBase = 60_000, PriceStep = 10_000;

    /// <summary>Höchstwert des Würfels für den Preis (random(0, 10)).</summary>
    public const int PriceSteps = 10;

    /// <summary>
    /// Zähler beim Start: random(3, 10), mit random(0, 3) = 0 zusätzlich + 6. [ORIGINAL] Datei 0x469D2–0x46A10 (Managersatz
    /// +0xEC, DGROUP 0x9747 + 0x1AF · Manager). Der Zähler sinkt zweimal je Woche (<see cref="IsTick"/>).
    /// </summary>
    public const int MinTicks = 3, MaxTicks = 10, ExtraTicks = 6;

    /// <summary>
    /// Höchstzahl Spieler im Besitz des Managers, bis zu der ein Talent noch kommt. [ORIGINAL] Datei 0x06D8D–0x06D97: Zahl aus
    /// 0x3CCEB ≥ 28 → kein Talent und keine Nachricht (<see cref="GameState.OwnedPlayers"/>).
    /// </summary>
    public const int MaxOwned = 28;

    /// <summary>
    /// Stärkenmitte s = random(40, 90) des gefundenen Talents. [ORIGINAL] Datei 0x07020–0x0703D (Aufruf 0x06D7F(m, s, 80)).
    /// </summary>
    public const int MinStrength = 40, MaxStrength = 90;

    /// <summary>Gehalt des Talents in Prozent von Wert(Gehalt). [ORIGINAL] drittes Argument 80 an 0x06D7F (Datei 0x07020), Datei 0x06F27–0x06F5A.</summary>
    public const int SalaryPercent = 80;

    /// <summary>
    /// Tage, an denen der Zähler sinkt: Tageszähler 0x3B7 ≡ 0 oder 4 (mod 7). [ORIGINAL] Tageslauf 0x54C9F ruft 0x06FA5 an Datei
    /// 0x5528A unter derselben Bedingung wie die kleine Wanderung der Teamwerte (Datei 0x55253–0x5528A,
    /// <see cref="MatchEngine.IsDriftDay"/>), nach dem Weiterzählen bei Datei 0x55204: im Tageslauf des Remake-Tags
    /// <paramref name="day"/> also Zähler <see cref="DayCounter.Of"/>(day) = day + 1 (#675, Review #674 R14NT4-03).
    /// </summary>
    public static bool IsTick(int day) => MatchEngine.IsDriftDay(DayCounter.Of(day));

    /// <summary>
    /// Höchstwert des Erfolgswürfels: Erfolg, wenn random(0, 5 − ⌊(5 − Level) / 2⌋) = 0. [ORIGINAL] Datei 0x07003–0x0701E
    /// (DGROUP 0xED8 = 5 − Level): Chance 1/4 bei Level 0–1, 1/5 bei Level 2–3, 1/6 bei Level 4–5.
    /// </summary>
    public static int SuccessDie(int level) => 5 - (5 - level) / 2;

    /// <summary>Meldung zum Start.</summary>
    public static string[] Message(TalentSearchStart result) => result switch
    {
        // [ORIGINAL] Nach dem Start keine Meldung: Datei 0x46A14–0x46A38 übermalt nur die Stelle des Knopfs (0x30:0x800) und
        // zeichnet die Kontozeile neu (0x463CC); Spur kader-rest, Teil 2 (A244). Bis Runde 17 [APPROX] „Ihr 'Spion' geht auf
        // Talentsuche.“ Eine leere Meldung öffnet kein Fenster (MessageBox.Visible).
        TalentSearchStart.Started => [],
        // [APPROX] Nur für den Anschluss im Remake; das Original zeigt den Knopf bei laufender Suche nicht (siehe Enum).
        TalentSearchStart.AlreadyRunning => ["Ihr 'Spion' ist schon", "auf Talentsuche."],
        _ => TransferMarket.Message(TransferResult.NotEnoughMoney),
    };

    /// <summary>Knopf „Talent“ in der Transferliste (Datei 0x465F1).</summary>
    public static TalentSearchStart Search(GameState game, Manager manager) => game.StartTalentSearch(manager);
}
