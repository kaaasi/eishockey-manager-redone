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

    private void MainPathDailyRun(int day) => MainPathDailyRunAndBuild(day, stadiumWeek: false);

    /// <summary>
    /// Hauptweg: am Takttag 0x07169 je Manager in Managerreihenfolge (Datei 0x55298–0x552D3), auch für entlassene Manager (#688,
    /// R688-06): Markt-Teil (bei Platz 0 Verfall, Lösen der Sperre und Gebotsverfall) wie im Original. [APPROX] Kaderplätze und
    /// Training hat ein entlassener Manager im Remake nicht mehr (er hat keinen Verein).
    /// <para>
    /// <paramref name="stadiumWeek"/>: Stadionbau 0x3D316 fällig (<see cref="AdvanceMainPathFinance"/>). [ORIGINAL] je Manager direkt
    /// nach seinem 0x07169 (Datei 0x552A6–0x552C3, tagesabschluss/hauptweg.py Teil 1; Spur tagesabschluss Teil 2).
    /// </para>
    /// </summary>
    private void MainPathDailyRunAndBuild(int day, bool stadiumWeek)
    {
        TrainedUntil = (Season, day);
        if (!IsDailyRunDay(day))
        {
            // Nur mit Ladeversatz möglich (Finanztag = Tag + 1); dann ohne Tageslauf wie vor Teil 2.
            if (stadiumWeek)
                WeeklyFinances();
            return;
        }
        foreach (var manager in Managers.ToList())
        {
            ManagerDailyRun(day, manager);
            if (stadiumWeek)
                WeeklyManagerFinances(manager);
        }
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
    /// nur Manager 0, Wurf nur auf seine Plätze). [ORIGINAL] Spur zufallsstrom (A521, N3 = B): Das Lösen der Sperre nach
    /// abgelehntem Gebot steht hier, im Lauf von Manager 0 direkt nach dem Verfall je Markt-Platz, und zieht random(0, 2) nur für
    /// gesperrte Plätze aus dem gemeinsamen Strom <see cref="Random"/> (Datei 0x071CC–0x07202, Aufruf 0x071EC). [ORIGINAL] T03/A829
    /// (Spur saison-geld; docs/analyse/re-abweichungen.md T03): Verfall, Sperre und Gebotsverfall stehen je Markt-Platz in einer
    /// Schleife (Datei 0x0719F–0x0727C, nur Manager 0, Datei 0x0718F); <c>random(0, 3)</c> (Datei 0x07215) fällt für jeden Platz,
    /// erst danach prüft das Original das offene Gebot (Datei 0x07227–0x07247) und löscht es bei 0. Bis Runde 25 erst Verfall und
    /// Sperren aller Plätze, dann ein Wurf je Gebot. Danach je Manager Heilung und Angebotswurf seiner Plätze (zweite Schleife ab
    /// Datei 0x0727C, <see cref="TransferMarket.CatchUpManagerDay"/>). [ORIGINAL] A499 (Spur national Teil 2): Der Verfall hat keine Untergrenze
    /// (Datei 0x071BB–0x071C8: nur <c>cmp +0x13, 0x38</c>, dann −10), also 47 … 56 nach einem Abzug (vorher als Näherung Untergrenze 50,
    /// R14NT4N-03); Beleg FILE18 → FILE48 (57–59 → 47–49).
    /// </summary>
    internal void DailyRunMarket(int day, Manager manager)
    {
        if (Managers.Count > 0 && Managers[0] == manager)
            foreach (var listing in TransferMarket.Listings.ToList())
            {
                if (listing.Player.Strain > MarketDecayAbove)
                    listing.Player.Strain -= MarketDecay;
                TransferMarket.ReleaseBidLock(listing, Random);
                if (TransferRules.Random16(Random, 0, 3) == 0)
                    foreach (var bid in TransferMarket.Bids.Where(b => b.Listing == listing).ToList())
                        TransferMarket.Decline(bid);
            }
        // #688 (R688-07): [ORIGINAL] Heilung je Markt-Platz mit Pool +0x1A = Manager vor dessen Angebotswurf (Datei 0x072A0–0x072CB).
        if (!manager.Dismissed && PlayerInjuries.IsHealingDay(Counter(day)))
            foreach (var listing in TransferMarket.OwnListingsOutsideSquad(Clubs[manager.ClubIndex]))
                PlayerInjuries.Heal(listing.Player);
        TransferMarket.CatchUpManagerDay(this, manager);
    }

    /// <summary>
    /// Kaderteil von 0x07169 (Datei 0x07AF2–0x07C07) für einen Manager im Verein: Heilung je Kaderplatz (0x07C12 an Tagen mit Zähler
    /// ≡ 0 mod 7, danach die Neuaufstellung, Datei 0x07C77), Training mit Kosten ([ORIGINAL] 46E:0C89, je Kaderplatz 0x0659C), Tendenz
    /// (<see cref="AdvanceTrend"/>), am Ende die Aufstellungsautomatik (Datei 0x07C07). [ORIGINAL] Spur zufallsstrom (A524, N3 = B):
    /// je Kaderplatz verschränkt Heilung (Datei 0x07B4E), Training (Datei 0x07B5E → 0x0659C) und Tendenz (Datei 0x07B64–0x07BFC,
    /// Wurf 0x07B77), Training und Tendenz aus dem gemeinsamen Strom <see cref="Random"/>; Kosten des Trainings einmal je Lauf
    /// (<see cref="FinanceCategory.Training"/>). Die Neuaufstellung nach einer Heilung folgt wie bisher nach der Schleife.
    /// </summary>
    internal void ManagerSquadRun(int day, Manager manager)
    {
        var club = Clubs[manager.ClubIndex];
        var healing = PlayerInjuries.IsHealingDay(Counter(day));
        var plan = PrepareTraining(manager);
        Book(manager, FinanceCategory.Training, -plan.CostPerTrainingDay);
        var healed = false;
        foreach (var p in club.Players)
        {
            if (healing)
                healed |= HealSlot(p); // A497: Satz 5 heilt nicht (CupsNational.WorldCupLoop.cs)
            TrainingRules.Apply(p, plan, manager.Level, Random);
            AdvanceTrend(p, Random);
        }
        if (healed)
            PlayerInjuries.RefillLineup(club);
        club.ApplyAutoLineup();
    }

    private void HealSquad(Club club)
    {
        var healed = false;
        foreach (var p in club.Players)
            healed |= HealSlot(p); // A497: Satz 5 heilt nicht (CupsNational.WorldCupLoop.cs)
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
                HealSlot(p); // A497: Satz 5 heilt nicht (CupsNational.WorldCupLoop.cs)
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
    /// Zufall im Tageslauf aus dem gemeinsamen Strom (Spur zufallsstrom A524, <see cref="ManagerSquadRun"/>). [APPROX] Plätze ohne
    /// bekannten Wert oder ohne Steuerbyte (0: ältere Spielstände) behalten ihre Stufe und ziehen nicht – im Original hat jeder
    /// Kaderplatz beide Werte (0x4907A).
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
    /// mit „automatisch aufstellen“, <see cref="Club.ApplyAutoLineup"/>). [ORIGINAL] Satz 5 (Bundestrainer, nur bei 0x9EDA &lt; 0x3F,
    /// Datei 0x54D76–0x54D82): Neuaufstellung mit der gesicherten Einstellung (Datei 0x54D8C–0x54D9E) und 0x952C(5, 0) (Datei 0x54DAA,
    /// <see cref="UpdateNationalTeamValues"/>), ebenfalls vor den Menüs (#699 Punkt 3); nach den Menüs folgt nur 0x952C(5, 1)
    /// (<c>OnDayNationalTeam</c>). Einen Bundestrainer gibt es im Hauptweg nur in Tests (Ernennung erst am 13. April, danach
    /// WM-Schleife). [ORIGINAL] #748: 0x952C(Satz, 0) der Managersätze (Datei 0x54DAA in derselben Schleife) schreibt die Felder
    /// 0x18–0x1D der Managervereine aus der Aufstellung (<see cref="UpdateManagerTeamValues"/>).
    /// </summary>
    private void MainPathEventDayLineups(int day)
    {
        if (!IsListChangeDay(day))
            return;
        ApplyAutoLineups();
        UpdateManagerTeamValues();
        if (NationalCoach is { } coach)
        {
            coach.OnEventDay();
            UpdateNationalTeamValues();
        }
    }

    /// <summary>
    /// Teamwerte der Managervereine aus ihrer Aufstellung (#748). [ORIGINAL] 0x952C(m, Modus) für m &lt; [0x386] (Managersatz mit
    /// Verein, Vereinsbyte ≤ 0x95, Datei 0x0954C) schreibt die Vereinsfelder 0x18–0x1D ohne weitere Bedingung aus Reihe 0–2 der
    /// Aufstellung, leere und nicht eingesetzte Plätze zählen 0 (<see cref="MatchEngine.ClubLineupTeamValues"/>); der Modus wirkt nur
    /// auf den Managerwert, den
    /// das Remake im Spiel rechnet. Aufrufe im Hauptweg an Tagen mit Ereignis: vor den Zügen nach der Neuaufstellung (Datei
    /// 0x54DAA) und nach den Zügen vor den Spielen (Datei 0x54FBF, <see cref="OnDayNationalTeam"/>). Beleg: In allen Ständen mit
    /// „automatisch aufstellen“ stehen die Felder genau auf diesen Werten (FILE02/03/05/13/14/16A/17/24/25/36/37,
    /// <c>AbgleichFixLineupTests</c>). Die Wanderung (Datei 0x55292) bewegt sie bis zum nächsten Tag mit Ereignis wie bei allen Vereinen.
    /// Dazu das Verlassen der Vereinsaufstellung (Datei 0x41506–0x4151A, <see cref="OnLineupLeft"/>, A858). Nicht im Modus „nur Weltkugel“ (#747: dort trägt
    /// der Satz des Managervereins das Land). Je Manager <see cref="WriteLineupTeamValues"/> (dort der Hinweis auf noch nicht
    /// gezogene Teamwerte).
    /// </summary>
    internal void UpdateManagerTeamValues()
    {
        if (WorldOnly)
            return;
        foreach (var manager in ActiveManagers)
        {
            if (manager.ClubIndex is >= 0 and < NationalTeams.First)
                WriteLineupTeamValues(Clubs[manager.ClubIndex]); // eine Rechnung mit dem Hinzufügen (Review #870 P3-3)
        }
    }

    /// <summary>
    /// Ereignis „Vereinsaufstellung verlassen“ (A858, Spur zusehen-rest Teil 3). [ORIGINAL] Am Ende der Aufstellung 0x40364
    /// (Datei 0x41513–0x4151A: <c>push 0</c>, <c>push [0xA70E]</c>, <c>call 0x28:0x146C</c>) ruft das Original 0x952C für den
    /// Manager am Zug ohne Bedingung – derselbe Aufruf wie beim Bundestrainer (<see cref="OnNationalLineupLeft"/>). Die Felder
    /// 0x18–0x1D des Managervereins stehen danach auf den Werten der neuen Aufstellung (<see cref="UpdateManagerTeamValues"/>).
    /// Beleg DOSBox `a093`: FILE71 → FILE72 nur Reihentausch bei EC HEDOS MÜNCHEN ohne Spiel, Felder 68 70 69 69 55 48 →
    /// 69 70 69 70 55 48 (Kondition A/S, Technik A/S, Form A/S) – genau die Rechnung aus der getauschten Aufstellung
    /// (<c>ZusehenRestAufstellungTests</c>). Der Bildschirm meldet es bei jedem Verlassen, auch ohne Änderung (B31 = A, Nutzer
    /// 24.11.2026); hat die Wanderung 0x9D1B die Felder seit dem letzten Tag mit Ereignis bewegt, setzt das Verlassen sie zurück.
    /// Das bloße Öffnen ändert nichts (AGENTS.md §4).
    /// </summary>
    public void OnLineupLeft(Club club)
    {
        ArgumentNullException.ThrowIfNull(club);
        if (WorldOnly || club.Index >= NationalTeams.First || !IsClubActivelyManaged(club.Index))
            return;
        WriteLineupTeamValues(club);
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
    /// [ORIGINAL] A526 (docs/analyse/re-abweichungen.md F08, f08_sommerpause.py): je Kalendertag (0x367B1) und je Manager verschränkt
    /// Bau, Tagesabschluss und Heilung, dann der nächste Manager; die Bücher führen die Beträge in der neuen Saison (Datei 0x56476
    /// zählt die Saison vor der Schleife weiter, 3 → 4 vor der ersten Buchung). [APPROX] Die Börse rechnet wie im Hauptweg einmal
    /// je Tag nach dem letzten Manager (Entscheidung #188).
    /// [ORIGINAL] Auch im Modus „nur Weltkugel“ (Schleife ohne Modusprüfung, Aufruf 0x56512; #732, abgleich-rest Teil 3):
    /// Satz 0 bucht Gehälter, Unterhalt, Zinsen usw. (FILE20 → FILE21: 500.000 → −1.530.327 DM).
    /// </summary>
    private void RunSummerBreak()
    {
        foreach (var counter in SummerBreakCounters())
            RunSummerDay(counter);
    }

    /// <summary>Ein Kalendertag der Sommerpause (Zähler <paramref name="counter"/>), je Manager verschränkt (A526, siehe <see cref="RunSummerBreak"/>).</summary>
    private void RunSummerDay(int counter)
    {
        var date = SeasonCalendar.LayoutDate(counter - 1);
        for (var i = 0; i < Managers.Count; i++)
        {
            var manager = Managers[i];
            if (counter % 7 == 0)
                WeeklyManagerFinances(manager);
            DayClose(date, i);
            if (PlayerInjuries.IsHealingDay(counter) && !manager.Dismissed)
                HealSquad(Clubs[manager.ClubIndex]);
        }
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
