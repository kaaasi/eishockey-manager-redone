namespace EishockeyManager.Core;

/// <summary>
/// Tagesablauf 0x07169 des Originals (Training, Markt, Verträge eines Managers), Listenwechsel 0x494EF im Hauptweg (#678) und die
/// Sommerpause des Saisonwechsels (#688, R688-03).
/// </summary>
/// <remarks>
/// [ORIGINAL] Selbst nachgelesen (<c>tools/reverse-engineering/disasm.py gamedata/em.exe 0x551F0 0x552E0</c>, <c>… 0x07169 0x07C12</c>,
/// <c>… 0x54D38 0x54EF4</c>, <c>… 0x56460 0x565B0</c>):
/// <list type="bullet">
/// <item>Hauptweg nach den Spielen von Tag e − 1: Datei 0x55204 zählt DGROUP 0x3B7 weiter (neuer Stand e + 1 =
///   <see cref="DayCounter.Of"/>(e)), je Manager Tagesabschluss 0x3ED01/0x3F833 (Datei 0x5522B/0x55230), dann prüft Datei
///   0x55253–0x55288 den Stand ≡ 0 oder ≡ 4 (mod 7) (Zähler + 3 ≡ 0); nur dann Talentsuche 0x06FA5, Wanderung 0x9D1B und je Manager
///   (DGROUP 0xA70E = 0 … Zahl − 1) 0x07169 (Datei 0x552A6), am Stand ≡ 0 danach der Stadionbau 0x3D316 (Datei 0x552C3). Erst
///   danach folgen am Tag e die Neuaufstellungen der Tage mit Ereignis und die Managerzüge mit den Menüs. Im Remake ist das der
///   Tageswechsel <c>OnDayOpen(e)</c>, den die Zeitleiste vor dem Halt rechnet (#688, R688-04): Takt an den Tagen e ≡ 3, 6 (mod 7),
///   Mittwoch und Samstag (Tag 0 = Sonntag), jeweils vor deren Menüs.</item>
/// <item>0x07169 für Manager i: nur bei i = 0 (Datei 0x0718F–0x07197) je Markt-Platz Markt-Verfall (0x071BB), Lösen der Sperre
///   (0x071CC) und Gebotsverfall (0x07215–0x0726A); für jeden Manager je Markt-Platz mit Pool +0x1A = i die Heilung 0x07C12 (0x072CB)
///   und der Angebotswurf (0x072DD); bis Zähler 233 die Verträge seiner Kaderplätze (0x073E3–0x07AEF), dann die Trainingskosten
///   (0x07AF2) und je Kaderplatz Heilung 0x07C12 (0x07B4E), Training 0x0659C (0x07B5E) und Tendenz (0x07B64–0x07BFC),
///   Aufstellungsautomatik 0x426BC (0x07C07). Die Schleife läuft für jeden Platz 0 … [0x386] − 1, auch für entlassene Manager.</item>
/// <item>Tage mit Ereignis (Tagesbyte ∉ {0, 9, 255}): je Satz 0 … 5 (Manager und Bundestrainer) 0x426BC (Datei 0x54D9E), dann je
///   Managerzug der Listenwechsel 0x494EF (Schleife Datei 0x54E3F–0x54EF4, Wurf 0x54E65–0x54E7C random(0, Zahl + 2) = 0) vor dem
///   Hauptmenü 0x44E22 (0x54EB2). Tage mit Tagesbyte 0 (9 gilt als 0) oder 255 springen bei Datei 0x54D5C/0x54D65 nach 0x55073;
///   die WM-Schleife (Datei 0x55570–0x55740) und Modus 1 (Datei 0x54D38) rufen den Listenwechsel nicht. Weiterer Aufrufer von
///   0x494EF: nur der Spielstart (Datei 0x437D5, Aufruf 0x43A9F); der Saisonwechsel behält die Liste (R688-05).</item>
/// <item>Sommerpause (Datei 0x56476–0x56598): nach dem Saisonende zählt Datei 0x564BC den Zähler weiter, dann je Kalendertag (Datum
///   + 1, Datei 0x564DB) je Manager der Stadionbau bei Zähler ≡ 0 (Datei 0x564F5–0x5650D), der Tagesabschluss 0x3ED01 (0x56512) und
///   die Heilung 0x07C12 je Kaderplatz (0x5653C), danach Zähler + 1 (0x56566) – bis das Datum der 9. September ist (Tag DGROUP
///   0x5A20 = 9, Monat 0x5A1C = 8, Datei 0x56570–0x56593).</item>
/// </list>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Takttag des Hauptwegs für den Tageswechsel <c>OnDayOpen(day)</c>: Zähler <see cref="DayCounter.Of"/>(day) ≡ 0 oder 4 (mod 7).
    /// [ORIGINAL] Datei 0x55253–0x55288 (siehe Klassenkommentar).
    /// </summary>
    public static bool IsDailyRunDay(int day) => MatchEngine.IsDriftDay(DayCounter.Of(day));

    /// <summary>
    /// Tag mit Ereignis im Hauptweg (Neuaufstellung je Satz, Listenwechsel-Wurf in den Managerzügen)? [ORIGINAL] nicht Modus 1,
    /// nicht Eintrittstag oder Tage der WM-Schleife, Tagesbyte ∉ {0, 9, 255} (Datei 0x54D4C–0x54D65).
    /// </summary>
    public bool IsListChangeDay(int day)
    {
        if (WorldOnly || day >= SeasonCalendar.ClubSeasonEnd)
            return false;
        var events = SeasonCalendar.Events(day);
        return events is not (DayEvents.None or DayEvents.SeasonEnd) && (byte)events != 9;
    }

    /// <summary>
    /// Hauptweg: am Takttag 0x07169 je Manager in Managerreihenfolge (Datei 0x55298–0x552D3), auch für entlassene Manager (#688,
    /// R688-06): Markt-Teil (bei Platz 0 Verfall, Lösen der Sperre und Gebotsverfall) wie im Original. [APPROX] Kaderplätze und
    /// Training hat ein entlassener Manager im Remake nicht mehr (er hat keinen Verein).
    /// </summary>
    private void MainPathDailyRun(int day)
    {
        TrainedUntil = (Season, day);
        if (!IsDailyRunDay(day))
            return;
        foreach (var manager in Managers.ToList())
            ManagerDailyRun(day, manager);
    }

    /// <summary>
    /// 0x07169 im Hauptweg für einen Manager: Markt (<see cref="DailyRunMarket"/>), Verträge (<see cref="AnnounceContractEnds"/> nur
    /// für ihn; bei einem entlassenen Manager nur das Lösen der Sperre von Platz 0), Heilung, Training, Tendenz und Aufstellung seiner
    /// Kaderplätze (<see cref="ManagerSquadRun"/>).
    /// </summary>
    private void ManagerDailyRun(int day, Manager manager)
    {
        DailyRunMarket(day, manager);
        AnnounceContractEnds(day, manager);
        if (!manager.Dismissed)
            ManagerSquadRun(day, manager);
    }

    /// <summary>
    /// Markt-Teil von 0x07169: für Manager 0 der Markt-Verfall (Datei 0x071BB, <see cref="MarketDecayAbove"/>), dann Heilung der
    /// eigenen Markt-Plätze (Datei 0x072CB), Gebotsverfall und Angebotswurf (<see cref="TransferMarket.CatchUpManagerDay"/>: Verfall
    /// nur Manager 0, Wurf nur auf seine Plätze). [APPROX] Das Lösen der Sperre (0x071CC) liegt im Remake bei den Verträgen
    /// (<see cref="AnnounceContractEnds"/>, eigene Zufallsquelle). Untergrenze 50 des Verfalls wie in der WM-Schleife (R14NT4N-03).
    /// </summary>
    private void DailyRunMarket(int day, Manager manager)
    {
        if (Managers.Count > 0 && Managers[0] == manager)
            foreach (var listing in TransferMarket.Listings)
                if (listing.Player.Strain > MarketDecayAbove)
                    listing.Player.Strain = Math.Max(listing.Player.Strain - MarketDecay, TrainingRules.MinStrain);
        // #688 (R688-07): [ORIGINAL] Heilung je Markt-Platz mit Pool +0x1A = Manager vor dessen Angebotswurf (Datei 0x072A0–0x072CB).
        if (!manager.Dismissed && PlayerInjuries.IsHealingDay(Counter(day)))
            foreach (var listing in TransferMarket.OwnListingsOutsideSquad(Clubs[manager.ClubIndex]))
                PlayerInjuries.Heal(listing.Player);
        TransferMarket.CatchUpManagerDay(this, manager);
    }

    /// <summary>
    /// Kaderteil von 0x07169 (Datei 0x07AF2–0x07C07) für einen Manager im Verein: Heilung je Kaderplatz (0x07C12 an Tagen mit Zähler
    /// ≡ 0 mod 7, danach die Neuaufstellung, Datei 0x07C77), Training mit Kosten (<see cref="TrainManager"/>), Tendenz
    /// (<see cref="AdvanceTrend"/>), am Ende die Aufstellungsautomatik (Datei 0x07C07). [APPROX] Heilung, Training und Tendenz je Platz
    /// nacheinander statt verschränkt: Sie wirken nur auf den eigenen Platz, das Training zieht aus der Hauptquelle, die Tendenz aus
    /// <see cref="TrendRandom"/>.
    /// </summary>
    private void ManagerSquadRun(int day, Manager manager)
    {
        var club = Clubs[manager.ClubIndex];
        if (PlayerInjuries.IsHealingDay(Counter(day)))
            HealSquad(club);
        TrainManager(manager);
        foreach (var p in club.Players)
            AdvanceTrend(p, TrendRandom);
        club.ApplyAutoLineup();
    }

    private static void HealSquad(Club club)
    {
        var healed = false;
        foreach (var p in club.Players)
            healed |= PlayerInjuries.Heal(p);
        if (healed)
            PlayerInjuries.RefillLineup(club);
    }

    /// <summary>
    /// Heilung der Kaderplätze aller Manager am Heiltag (Zähler <see cref="DayCounter.Of"/>(day) ≡ 0 mod 7). [ORIGINAL] WM-Schleife
    /// Schritt (d), Datei 0x556D2–0x55712 (je Manager, je Kaderplatz 0x07C12).
    /// </summary>
    private void HealManagerSquads(int day)
    {
        if (!PlayerInjuries.IsHealingDay(Counter(day)))
            return;
        foreach (var club in ManagedClubs())
            HealSquad(club);
    }

    /// <summary>
    /// Heilung der Spieler ohne Kaderplatz eines Managers: Computervereine und Vertragslose. [APPROX] Das Original führt Verletzungen
    /// nur in den Kaderplätzen der Manager und des Markts (0x07C12 hat nur die Aufrufer 0x07169 und 0x54C9F); im Remake behalten
    /// Spieler, die verletzt einen Managerkader verlassen, ihre Verletzung und heilen am Heiltag wie bisher.
    /// </summary>
    private void HealUnmanaged(int day)
    {
        if (!PlayerInjuries.IsHealingDay(Counter(day)))
            return;
        var managed = ManagedClubs().ToHashSet();
        foreach (var club in Clubs.Where(c => !managed.Contains(c)))
        {
            foreach (var p in club.Players)
                PlayerInjuries.Heal(p);
            foreach (var listing in TransferMarket.OwnListingsOutsideSquad(club))
                PlayerInjuries.Heal(listing.Player);
        }
        foreach (var p in TransferMarket.FreeAgents)
            PlayerInjuries.Heal(p);
    }

    private IEnumerable<Club> ManagedClubs() =>
        Managers.Where(m => !m.Dismissed).Select(m => Clubs[m.ClubIndex]).Distinct();

    /// <summary>
    /// Tendenz eines Kaderplatzes im Tagesablauf (#688, R688-07, Inventur A134). [ORIGINAL] Datei 0x07B64–0x07BFC: t = +0x0E; mit
    /// random(0, 3) ≠ 0 (0x07B77) wird t um (+0x14 &gt;&gt; 4) &amp; 7 erhöht (Bit 7 gesetzt) bzw. erniedrigt; das untere Halbbyte von
    /// +0x14 zählt um eins herunter (Bytearithmetik, Datei 0x07BB1–0x07BC8). Erreicht es 0, zieht 0x4907A Wert und Steuerbyte neu
    /// (Datei 0x07BD4, <see cref="Player.RollTrend"/>); sonst wird t auf 30 … 68 begrenzt (0x4BD35, Datei 0x07BEA) und geschrieben.
    /// [APPROX] Zufall aus <see cref="TrendRandom"/> wie beim Ziehen (Spur kader-transfer Teil 4); Plätze ohne bekannten Wert oder ohne
    /// Steuerbyte (0: Startkader, ältere Spielstände) behalten ihre Stufe – im Original hat jeder Kaderplatz beide Werte (0x4907A).
    /// </summary>
    public static void AdvanceTrend(Player p, Random random)
    {
        if (p.TrendScore == 0 || p.TrendControl == 0)
            return;
        var t = p.TrendScore;
        var control = p.TrendControl;
        if (TransferRules.Random16(random, 0, 3) != 0)
        {
            var step = (control >> 4) & 7;
            t += (control & 0x80) != 0 ? step : -step;
        }
        var days = (control & 0x0F) - 1;
        control = ((control & 0xF0) + days) & 0xFF;
        if (days == 0)
        {
            p.RollTrend(random);
            return;
        }
        p.SetTrendScore(Math.Clamp(t, 30, 68), control);
    }

    /// <summary>
    /// Hauptweg, Tage mit Ereignis vor den Managerzügen: je Satz die Neuaufstellung (Datei 0x54D68–0x54DBA; 0x426BC bei 0x54D9E, nur
    /// mit „automatisch aufstellen“, <see cref="Club.ApplyAutoLineup"/>). [APPROX] Satz 5 (Bundestrainer, nur bei 0x9EDA &lt; 0x3F:
    /// Neuaufstellung mit der gesicherten Einstellung und 0x952C(5, 0) bei 0x54DAA) rechnet das Remake nach den Menüs vor 0x952C(5, 1)
    /// (<c>OnDayManager</c>, A009 Runde 14 Teil 3): Einen Bundestrainer gibt es im Hauptweg nur in Tests (Ernennung erst am 13. April,
    /// danach WM-Schleife).
    /// </summary>
    private void MainPathEventDayLineups(int day)
    {
        if (IsListChangeDay(day))
            ApplyAutoLineups();
    }

    /// <summary>
    /// Hauptweg: an Tagen mit Ereignis je Managerzug der Wurf des Listenwechsels (<see cref="IsListChangeDay"/>), hier im
    /// Tageswechsel die Würfe bis zum Zug des ersten aktiven Managers. [ORIGINAL] Datei 0x54E3F–0x54EF4: je Platz 0 … [0x386] − 1
    /// der Wurf (Datei 0x54E65–0x54E7C), dann dessen Menü (Datei 0x54EB2); der Wurf von Manager i fällt also nach dem Menü von
    /// Manager i − 1 (#699 Punkt 1). Die übrigen Würfe: beim Weitergeben des Zugs (<see cref="RollListChangesThrough"/>, aus
    /// <c>PassTurn</c>), nach dem letzten Zug bzw. an Tagen ohne Halt vor den Spielen (<see cref="MainPathRemainingListChanges"/>).
    /// [APPROX] Entlassene Manager (nur ältere Spielstände) würfeln mit dem Zug des nächsten aktiven Managers bzw. vor den Spielen.
    /// </summary>
    private void MainPathListChanges(int day) => RollListChanges(day, 0, FirstActiveSlot);

    /// <summary>
    /// Zug geht von Platz <paramref name="from"/> an Platz <paramref name="to"/>: Würfe der Plätze dazwischen und des neuen Platzes
    /// (Datei 0x54E3F–0x54EF4, siehe <see cref="MainPathListChanges"/>).
    /// </summary>
    private void RollListChangesThrough(int from, int to) => RollListChanges(CurrentDay, from + 1, to);

    /// <summary>
    /// Vor den Spielen eines Tages die Würfe, die noch fehlen: nach dem letzten Managerzug die Plätze dahinter
    /// (<paramref name="turnsTaken"/>), an Tagen ohne Halt alle nach dem ersten aktiven Manager. Je Tag mit Ereignis würfelt so jeder
    /// Platz genau einmal.
    /// </summary>
    private void MainPathRemainingListChanges(int day, bool turnsTaken) =>
        RollListChanges(day, (turnsTaken ? LastActiveSlot : FirstActiveSlot) + 1, Managers.Count - 1);

    private void RollListChanges(int day, int first, int last)
    {
        if (WorldOnly || InWorldCupLoop(day) || !IsListChangeDay(day))
            return;
        for (var i = first; i <= last; i++)
            TransferMarket.ListChangeTurn(this);
    }

    private int FirstActiveSlot
    {
        get
        {
            for (var i = 0; i < Managers.Count; i++)
                if (!Managers[i].Dismissed)
                    return i;
            return 0;
        }
    }

    private int LastActiveSlot
    {
        get
        {
            for (var i = Managers.Count - 1; i >= 0; i--)
                if (!Managers[i].Dismissed)
                    return i;
            return Managers.Count - 1;
        }
    }

    /// <summary>Datum, an dem die Sommerpause endet. [ORIGINAL] Datei 0x56570–0x56593: Tag DGROUP 0x5A20 = 9, Monat 0x5A1C = 8.</summary>
    public const int SummerBreakEndMonth = 9, SummerBreakEndDay = 9;

    /// <summary>
    /// Tageszähler, den das Saisonende 0x26F20 setzt (#688, R688S-01). [ORIGINAL] Datei 0x26F55: <c>mov word [0x3B7], 0xE9</c> = 233
    /// (= 30. April über 0x367B1; Fenster „Saisonende“ zeigt „Montag, 30. April“, DOSBox-Bild <c>file46_2</c>). Die WM-Schleife endet
    /// davor mit dem Tagesabschluss zum Zähler <see cref="DayCounter.Of"/>(<see cref="SeasonCalendar.SeasonEnd"/>) = 232 (29. April,
    /// Datei 0x55670–0x5569F; danach steht der Spielplan auf Markierung 255 und Datei 0x55724–0x55743 geht ohne weiteren Tag zum
    /// Saisonwechsel). Für den 30. April gibt es im Original keinen Tagesabschluss und keinen April-Monatsabschluss.
    /// </summary>
    public const int SeasonEndCounter = 233;

    /// <summary>
    /// Tageszähler der Sommerpause, je Kalendertag vom 1. Mai bis zum 9. September (#688, R688-03/R688S-01). [ORIGINAL] Nach dem
    /// Saisonende (<see cref="SeasonEndCounter"/> = 233) zählt Datei 0x564BC einmal weiter, die Schleife Datei 0x564D2–0x56593
    /// rechnet dann je Datum mit dem Zähler (0x367B1 bei 0x564DB) und zählt danach weiter (Datei 0x56566), bis der Tag 9. September
    /// gerechnet ist (Datei 0x5657A–0x56593). Ergebnis: 234 … 365 für den 1. Mai bis 9. September, 132 Tage mit vier
    /// Monatsabschlüssen (Ende Mai bis August) und 19 Bauwochen (Zähler 238 … 364).
    /// </summary>
    public static IEnumerable<int> SummerBreakCounters()
    {
        var counter = SeasonEndCounter + 1;
        var date = SeasonCalendar.LayoutDate(counter - 1);
        while (true)
        {
            yield return counter;
            if (date.Month == SummerBreakEndMonth && date.Day == SummerBreakEndDay)
                yield break;
            counter++;
            date = date.AddDays(1);
        }
    }

    /// <summary>
    /// Sommerpause des Saisonwechsels (#688, R688-03; Nutzer 09.11.2026: wie im Original). [ORIGINAL] Datei 0x564D2–0x56593 je
    /// Kalendertag vom 1. Mai bis 9. September (<see cref="SummerBreakCounters"/>), je Manager:
    /// <list type="number">
    /// <item>bei Zähler ≡ 0 (mod 7) der Stadionbau 0x3D316 (Datei 0x564F5–0x5650D; <see cref="WeeklyFinances"/>),</item>
    /// <item>der Tagesabschluss 0x3ED01 (Datei 0x56512) wie im Hauptweg: Baufirmen (0x3ED19), Zinskonto (0x3ED4F), Werbeangebote zur
    ///   Monatsmitte und am Monatsletzten (0x3EE6B), am 1. die Restlaufzeiten der Werbeverträge (0x3EF52), am Monatsletzten der
    ///   Monatsabschluss (0x3F02C: Gehälter, Versicherung, Unterhalt, Grundeinnahmen, Werbung, Jugend, Zinsen; Ende Mai bis
    ///   August), Börsentag (0x3ED4A),</item>
    /// <item>die Heilung 0x07C12 je Kaderplatz (Datei 0x5653C; prüft selbst Zähler ≡ 0).</item>
    /// </list>
    /// Keine Werbebewertung 0x3F833 (nur Hauptweg und WM-Schleife), keine Talentsuche, keine Jugendteams, keine Spiele, kein Training.
    /// [ORIGINAL] Kein Tagesabschluss für den 30. April (R688S-01, Review #691 berichtigt): Die WM-Schleife schließt zuletzt den
    /// 29. April ab (Zähler 232, Datei 0x55670–0x5569F; im Remake <c>OpenDay(231)</c> der Zeitleiste), das Saisonende 0x26F20 setzt
    /// den Zähler auf 233 (<see cref="SeasonEndCounter"/>) und 0x564BC zählt vor der Schleife auf 234 = 1. Mai. Der
    /// April-Monatsabschluss (0x3F02C prüft Tag = Monatslänge) fällt damit im Original jedes Jahr aus: elf Monatsabschlüsse je
    /// Jahr (sieben der Spielzeit, vier der Sommerpause). Der DOSBox-Stand <c>file46_2</c> (17.523.876 DM am 30. April, also
    /// 603.307 DM unter FILE44 + Prämie) bucht den April nur, weil FILE44 im Menü des Markierungstags gesichert ist (Datei 0x55350,
    /// nach dem Tageswechsel 0x55204): Nach dem Laden läuft dieser Tageswechsel erneut (Datei 0x54D41 → 0x55073 → 0x55204, Zähler
    /// 218 = 15. April, Bild <c>file46_1</c>), die WM-Schleife steht dann einen Tag später und schließt zuletzt den 30. April ab.
    /// Der HOK-Import spielt diese Eigenheit des Ladens nach (Entscheidung Nutzer 09.11.2026, C; <c>GameState.LoadQuirk.cs</c>).
    /// [APPROX] Ohne eigenen Kalendertag des Remakes (die Bücher führen die Beträge in der neuen Saison, Datei 0x56476 zählt die
    /// Saison vor der Schleife weiter); Bau, Tagesabschluss und Heilung je Tag für alle Manager nacheinander statt je Manager
    /// verschränkt (sie wirken je Manager getrennt; die Börse rechnet wie im Hauptweg einmal je Tag, Entscheidung #188). Im Modus
    /// „nur Weltkugel“ ohne Tagesabschluss: Das Remake führt dort keine Vereinsfinanzen (wie in der WM-Schleife).
    /// </summary>
    private void RunSummerBreak()
    {
        foreach (var counter in SummerBreakCounters())
        {
            var date = SeasonCalendar.LayoutDate(counter - 1);
            if (counter % 7 == 0)
                WeeklyFinances();
            if (!WorldOnly)
                SummerDayClose(date);
            if (PlayerInjuries.IsHealingDay(counter))
                foreach (var club in ManagedClubs())
                    HealSquad(club);
        }
    }

    /// <summary>Tagesabschluss 0x3ED01 eines Tages der Sommerpause (siehe <see cref="RunSummerBreak"/>; Teile wie <see cref="AdvanceFinance"/>).</summary>
    private void SummerDayClose(DateOnly date)
    {
        DailyStadium();
        AccrueDailyInterest(date);
        DailySponsorOffers(date);
        if (date.Day == 1)
            foreach (var manager in Managers)
                CountDownSponsorContracts(manager);
        if (date.Day == DateTime.DaysInMonth(date.Year, date.Month))
            MonthlyFinances();
        AdvanceStocks();
    }
}

public sealed partial class TransferMarket
{
    /// <summary>
    /// Wurf des Listenwechsels im Zug eines Managers. [ORIGINAL] Datei 0x54E65–0x54E7C: random(0, Zahl der Manager + 2) = 0 →
    /// 0x494EF (<see cref="ChangeList"/>).
    /// </summary>
    public void ListChangeTurn(GameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        UpdatedDay = game.PlayedMatchDays;
        if (ListChangeRoll(game.Managers.Count, game.Random))
            ChangeList(game, game.Random);
    }
}
