using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Weitere Manager im laufenden Spiel (Spur nutzer-befunde, #841): Kachel „Neuer“ → Spielvorbereitung im Hinzufügen-Modus.
/// </summary>
/// <remarks>
/// [ORIGINAL] Hauptmenü Datei 0x4574B–0x4576D: Kachel „Neuer“ ruft die Spielvorbereitung 0x28165 (Argument 0), stellt danach
/// den Manager am Zug (DGROUP 0xA70E/0xA70D) wieder her und baut das Menü neu auf (Sprung Datei 0x44E6D). In 0x28165 ist
/// <c>[bp−0x64]</c> = Managerzahl DGROUP 0x386 (Datei 0x2819C–0x281A1); bei einem laufenden Spiel (&gt; 0):
/// <list type="bullet">
/// <item>Die rechte Seite (Spielmodus, Regler, Laden) nimmt keinen Klick an (Datei 0x286DA–0x286E0 springt nach 0x28A6D);
///   Level, Start-Liga und Start-Stärke bleiben die gemeinsamen Werte des Spiels (DGROUP 0x118/0x11A/0x11B).</item>
/// <item>Klicks auf die vorhandenen Manager bleiben ohne Wirkung (Datei 0x286AD–0x286D7: x &lt; 0xBF und
///   y &lt; 0x26 + 0x33 · Managerzahl → 0x297D0, dort mit <c>[bp−0x2F]</c> = 0 zurück in die Schleife).</item>
/// <item>„Los jetzt!“ prüft wie beim Spielstart jede Zeile (Datei 0x29617–0x29693: Name nur mit Kopf und Verein), zählt die
///   Manager neu (Datei 0x29696–0x297BA) und richtet nur die neuen ein: Datei 0x298BF–0x29B54 (Startkader aus dem Pool,
///   Markt) und 0x29B57–0x2A0A3 je Manager ab dem alten Zähler (Konto, Start-Liga, Startwerte, Gehälter, Stadion, Werbung,
///   Aufstellung). Spielmodus, Spielende und Pool (Datei 0x29838–0x298BC, 0x5AF50) bleiben unberührt.</item>
/// </list>
/// Das Remake bildet das mit <see cref="AddManager"/> nach; die Bildschirmseite steht in <c>SetupScreen</c>.
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Kann ein weiterer Manager hinzukommen? [ORIGINAL] Kachel „Neuer“ gesperrt bei DGROUP 0x386 = 4 (Datei 0x45057). Im Modus
    /// „nur Weltkugel“ gibt es die Kachel nicht (Menü des Bundestrainers); die Prüfung sichert den Kern zusätzlich ab.
    /// </summary>
    public bool CanAddManager => !WorldOnly && Managers.Count < GameSetup.MaxManagers;

    /// <summary>Wird der Verein schon von einem Manager geführt?</summary>
    public bool IsClubManaged(int clubIndex) => Managers.Any(m => m.ClubIndex == clubIndex);

    /// <summary>
    /// Vereine in Satzfolge für die Vereinsauswahl im laufenden Spiel: Bundesliga, 2. Liga, Oberliga (je
    /// <see cref="LeagueSlots"/>), dann die Plätze ohne Liga (<see cref="OutsidePlaces"/>). [ORIGINAL] Die Auswahl zeigt auf Seite p
    /// die Vereinssätze 12 · p … 12 · p + 11 (Datei 0x28030); die Sätze stehen im laufenden Spiel in der Folge der letzten Mischung
    /// (S-K6, Datei 0x5644E). Reine Abfrage. Fehlen Vereine (synthetische Aufbauten), folgen sie nach Index.
    /// </summary>
    public IReadOnlyList<int> ClubRecordOrder()
    {
        var order = new List<int>();
        foreach (var league in Enum.GetValues<League>())
            order.AddRange(LeagueSlots(league));
        order.AddRange(OutsidePlaces().Where(c => !order.Contains(c)));
        order.AddRange(Enumerable.Range(0, Math.Min(NewGame.SelectableClubs, Clubs.Count)).Where(c => !order.Contains(c)));
        return order.Take(NewGame.SelectableClubs).ToList();
    }

    /// <summary>
    /// Nimmt einen Manager in das laufende Spiel auf (Kachel „Neuer“, „Los jetzt!“) und richtet ihn wie beim Spielstart ein.
    /// <list type="number">
    /// <item>Gemeinsame Werte: Level, Start-Liga und Start-Stärke des Spiels ([ORIGINAL] ein Wert für alle,
    ///   DGROUP 0x118/0x11A/0x11B; im Hinzufügen-Modus nicht verstellbar, Datei 0x286DA; A462): <see cref="GameSetup.Level"/>,
    ///   <see cref="GameSetup.StartLeague"/>, <see cref="GameSetup.StartStrength"/>.</item>
    /// <item>Verein: der gewählte, ohne Wahl ein zufälliger der Start-Liga ohne Manager (<see cref="NewGame.PickRandomClub"/>).</item>
    /// <item>Startkader aus dem Pool (<see cref="SeatAddedSquad"/>) und Markt, dann [ORIGINAL] Platztausch des gewählten Vereins in
    ///   die Start-Liga (Datei 0x29C3B–0x29CF7, <see cref="DrawStartLeaguePlace"/>, <see cref="SwapClubPlaces"/>): Der Manager
    ///   übernimmt den gezogenen Platz mit Liga, Tabellenstand, Spielplan und Teamwerten, der Verdrängte seinen alten Platz.
    ///   [APPROX] Ein zufällig zugeteilter Verein (ohne Wahl) tauscht nicht, wie beim Spielstart: Die Zufallszuteilung ist eine
    ///   Erweiterung des Remakes, im Original wählt jeder Manager einen Verein (Datei 0x2964C; A463, Beleg
    ///   <c>docs/analyse/re-abweichungen.md</c> K13).</item>
    /// <item>Startwerte, Gehälter ([ORIGINAL] nach dem Tausch, Datei 0x29DA4–0x29EF7), Aufstellung, danach die Teamwerte des Platzes
    ///   aus der Aufstellung ([ORIGINAL] 0x952C(m, 0), Datei 0x2A073; <see cref="WriteLineupTeamValues"/>).</item>
    /// <item>Konto <see cref="FinanceRules.StartMoney"/> ([ORIGINAL] Datei 0x29BD5), Werbeangebote ([ORIGINAL] 0x3F833/0x3FB47,
    ///   Datei 0x2A03B–0x2A061), Trainingsplan; Stadion und Jugend legt <see cref="Finance"/> beim ersten Zugriff an.</item>
    /// </list>
    /// Der neue Manager ist am selben Tag noch am Zug (Hot-Seat: nicht bereit); der Manager am Zug bleibt ([ORIGINAL] Datei
    /// 0x4575B–0x45761). Alle Prüfungen laufen vor der ersten Änderung; <c>Setup.Managers</c> und <see cref="Managers"/>
    /// bleiben gleich lang (Review P1-1).
    /// <para>Zufall (Spur zufallsstrom, A464, N3 = B): Vereinswahl, Kader, Platztausch und Startwerte ziehen aus dem gemeinsamen
    /// Strom <see cref="Random"/>, in der Reihenfolge der Spielvorbereitung im Hinzufügen-Modus ([ORIGINAL] Datei 0x2993A–0x29EF7:
    /// je Kaderplatz Poolindex, 0x42CB1 mit Energie und Tendenz, +0x19, Alter; dann Platztausch; dann je Platz Kondition, Technik,
    /// dritter Wurf, Form). Wie im Original verschiebt das Hinzufügen damit alle späteren Würfe. Teil 4 (A464 Rest): Die
    /// Werbeangebote des Neuen (<see cref="RenewOffers"/>; [ORIGINAL] Bewertung 0x3F833 Datei 0x2A03B, Angebote 0x3FB47 Datei
    /// 0x2A061, nach Preis und Gehalt und vor den Teamwerten Datei 0x2A073) ziehen aus demselben Strom.</para>
    /// </summary>
    /// <exception cref="InvalidOperationException">Kein Platz für einen weiteren Manager oder Modus „nur Weltkugel“.</exception>
    /// <exception cref="ArgumentException">Name leer, Verein nicht wählbar oder schon vergeben.</exception>
    public Manager AddManager(ManagerSetup setup) => AddManagers([setup])[0];

    /// <summary>
    /// Mehrere Manager auf einmal („Los jetzt!“ mit mehreren neuen Zeilen, [ORIGINAL] Datei 0x29696–0x297BA zählt alle neu):
    /// erst alle Zeilen prüfen (Platz, Name, Verein, kein Verein doppelt), dann alle anlegen wie <see cref="AddManager"/>.
    /// Scheitert eine Prüfung, bleibt das Spiel unverändert.
    /// </summary>
    /// <exception cref="InvalidOperationException">Kein Platz für alle oder Modus „nur Weltkugel“.</exception>
    /// <exception cref="ArgumentException">Name leer, Verein nicht wählbar, vergeben oder doppelt gewählt.</exception>
    public IReadOnlyList<Manager> AddManagers(IReadOnlyList<ManagerSetup> setups)
    {
        ArgumentNullException.ThrowIfNull(setups);
        var managers = Managers as List<Manager>
            ?? throw new InvalidOperationException("Die Managerliste dieses Spielstands ist nicht erweiterbar.");
        if (WorldOnly || Managers.Count + setups.Count > GameSetup.MaxManagers)
            throw new InvalidOperationException("Kein weiterer Manager möglich.");
        if (Setup.Managers.Count > Managers.Count)
            throw new InvalidOperationException("Spielvorbereitung und Managerliste passen nicht zusammen.");
        if (!ClubsSwappable)
            throw new InvalidOperationException("Die Vereinsliste dieses Spielstands ist nicht veränderbar.");
        var chosen = new HashSet<int>(Managers.Select(m => m.ClubIndex));
        foreach (var setup in setups)
        {
            ArgumentNullException.ThrowIfNull(setup, nameof(setups));
            if (setup.Name.Length == 0)
                throw new ArgumentException("Der Manager braucht einen Namen.", nameof(setups));
            if (setup.ClubIndex is { } club && (club < 0 || club >= NewGame.SelectableClubs || !chosen.Add(club)))
                throw new ArgumentException($"Verein {club} ist nicht wählbar.", nameof(setups));
        }

        // Ab hier keine Prüfung mehr: Die Spielvorbereitung des Spiels führt die Einträge parallel zu Managers
        // (StartLeagueOf, Kadergröße, Export); ältere Spielstände können dort weniger Einträge haben.
        while (Setup.Managers.Count < Managers.Count)
            Setup.Managers.Add(new ManagerSetup { Name = Managers[Setup.Managers.Count].Name, StartLeague = Setup.StartLeague });
        var added = new List<Manager>(setups.Count);
        for (var i = 0; i < setups.Count; i++)
        {
            // Gewählte Vereine noch nicht angelegter Zeilen bleiben für die Zufallswahl gesperrt.
            var pending = setups.Skip(i + 1).ToList();
            var reserved = pending.Where(s => s.ClubIndex is not null).Select(s => s.ClubIndex!.Value);
            added.Add(Seat(managers, setups[i], reserved, pending));
        }
        return added;
    }

    private Manager Seat(List<Manager> managers, ManagerSetup setup, IEnumerable<int> reserved, IReadOnlyList<ManagerSetup> pending)
    {
        setup.Level = Setup.Level;
        setup.StartStrength = Setup.StartStrength;
        var random = Random; // Spur zufallsstrom (A464): gemeinsamer Strom, siehe AddManager
        var randomPick = setup.ClubIndex is null;
        var clubIndex = setup.ClubIndex ?? NewGame.PickRandomClub(Clubs, Setup.StartLeague, setup.StartStrength,
            Managers.Select(m => m.ClubIndex).Concat(reserved).ToHashSet(), random);
        setup.ClubIndex = clubIndex;
        Setup.AddManager(setup);

        var manager = new Manager
        {
            Name = setup.Name, Head = Math.Max(0, setup.Head), ClubIndex = clubIndex, Level = setup.Level,
            Money = FinanceRules.StartMoney(setup.Level), // [ORIGINAL] Datei 0x29BD5
        };
        managers.Add(manager);

        SeatAddedSquad(Clubs[clubIndex], setup, random);
        SetWatchMode(Clubs[clubIndex], evict: true, WatchLive); // Spur zusehen: [ORIGINAL] Datei 0x29C1C vor dem Platztausch
        TransferMarket.RemoveListingsOfAddedClub(Clubs[clubIndex]);

        // [ORIGINAL] Datei 0x29C3B–0x29CF7 nach Kader und Markt, vor den Startwerten. Ausgeschlossen sind nur die Vereine der
        // Manager mit kleinerer Nummer (Datei 0x29C9B–0x29CC3); ein gewählter Verein einer späteren Zeile kann getroffen werden,
        // dessen Kennung wandert dann mit (Managerschleife Datei 0x361E4 über alle gezählten Manager).
        if (!randomPick)
        {
            var earlier = managers.Take(managers.Count - 1).Select(m => m.ClubIndex).ToHashSet();
            if (DrawStartLeaguePlace(clubIndex, earlier, random) is { } partner)
                SwapClubPlaces(clubIndex, partner, pending);
        }

        var club = Clubs[manager.ClubIndex];
        RollAddedStartValues(manager, club, setup, random);

        RenewOffers(manager);
        PrepareTraining(manager);
        BindLineupOwners(); // Spur aufstellung, A936 (Squad.AutoLineup.Manager.cs)
        club.ApplyAutoLineup();
        club.ReferenceRating = TeamStrength.Rating(club.Lineup);
        WriteLineupTeamValues(club);
        return manager;
    }

    /// <summary>
    /// Teamwerte des neuen Managervereins aus seiner Aufstellung. [ORIGINAL] Datei 0x2A067–0x2A078: nach der Aufstellung
    /// 0x426BC ruft die Managerschleife 0x952C([0xA70E], 0) – dieselbe Rechnung wie <see cref="UpdateManagerTeamValues"/>, nur
    /// für diesen Manager. Mitten in der Saison stehen am Platz sonst die Teamwerte des verdrängten Vereins (#847, Gegenprobe
    /// DOSBox `runde-19` FILE64 → FILE65: EC HANNOVER auf Satz 4 mit 81/81/79/80/56/51 aus der Aufstellung statt 75/71/71/75/54/47
    /// der EISBÄREN BERLIN; <c>docs/analyse/abgleich-dosbox.md</c> §6). Auch der Weg der Tage mit Ereignis
    /// (<see cref="UpdateManagerTeamValues"/>) rechnet hierüber (Review #870 P3-3).
    /// Seit Spur start-lader (A519) stehen die Teamwerte ab dem Spielstart (Lader Datei 0x439BB), die Rechnung läuft also wie im
    /// Original (Datei 0x2A073, 0x54DAA) auch vor dem ersten Spiel; beim Spielstart ruft sie <c>MatchEngine.StartGame</c>.
    /// [APPROX] Ohne Teamwerte (nur Spielstände vor Schema 12, <c>MatchEngine.EnsureTeamValues</c>) nichts, um den Zug der
    /// Teamwerte nicht vorzuziehen.
    /// </summary>
    private void WriteLineupTeamValues(Club club)
    {
        if (club.TeamValues is null || club.Index >= NationalTeams.First)
            return;
        var (defense, attack, _) = MatchEngine.ClubLineupTeamValues(club);
        var b = MatchEngine.ComputerTeamValue(club);
        club.TeamValues = MatchEngine.TeamValuesOf(club) with
        {
            DefenseCondition = defense.Condition - b, AttackCondition = attack.Condition - b,
            DefenseTechnique = defense.Technique - b, AttackTechnique = attack.Technique - b,
            DefenseForm = defense.Form, AttackForm = attack.Form,
        };
    }

    /// <summary>
    /// Startkader eines hinzukommenden Managers.
    /// [ORIGINAL] Datei 0x2993A–0x29A5E: je Art (Torhüter, Verteidiger, Stürmer) so viele Plätze wie die Kadertabelle DGROUP 0x169
    /// nach Start-Liga (<see cref="SquadSetup.ManagedSquadSize"/>); je Platz Poolindex random(DGROUP 0x381[a], 0x382[a] − 1)
    /// (1–20, 21–96, 97–199), neu gezogen, solange er über 179 (Nationalspieler, Datei 0x29962) oder nicht frei (Besitzer 6,
    /// Datei 0x29974) ist, höchstens 1000 Versuche (Datei 0x2997B); dann Kaderplatz 0x42CB1(i, 1, 0, 15000, 0x1D) mit Laufzeit 1
    /// (+0x0B, Datei 0x2998F → 0x42E75) und Belastung random(80, 120) (Datei 0x42E4C), Besitzer = Manager (Datei 0x299B3),
    /// +0x19 = random(3, 12) · 16 + random(3, 12) (Datei 0x299B7–0x299F4), Verein +0x1D = Managerverein (Datei 0x29A17),
    /// Alter random(19, 31) (Datei 0x29A1B–0x29A3A).
    /// [ORIGINAL] Startwerte Datei 0x29DA4–0x29EF7 mit Basis DGROUP 0x4B42 [Start-Liga · 3 + Start-Stärke]
    /// (<see cref="SquadSetup.StartBase"/>): je Platz Kondition, Technik = random(Basis − 2, Basis + 2), ein dritter Wurf derselben
    /// Spanne (verworfen, Datei 0x29DF2 zieht vor der Unterscheidung), Form random(40, 60); dann +0x2C = Wert(Preis),
    /// +0x28 = Wert(Gehalt) (Datei 0x29EB4–0x29EF3). Talent (+0x12) und Name bleiben die des Poolspielers.
    /// [APPROX] Nach 1000 Fehlversuchen nimmt das Remake den ersten freien Platz der Art (das Original nähme den letzten
    /// gezogenen, auch einen vergebenen); ist keiner frei, bleibt der Platz leer. Die Spieler des bisherigen Computerkaders
    /// verlassen das Spiel (im Original hat ein Computerverein keine Spieler).
    /// </summary>
    private void SeatAddedSquad(Club club, ManagerSetup setup, Random random)
    {
        foreach (var old in club.Players.ToList())
            club.RemovePlayer(old);

        var pool = TransferMarket.Pool;
        var (g, d, f) = SquadSetup.ManagedSquadSize(Setup.StartLeague);
        foreach (var (role, count, low, high) in new[]
                 {
                     (PlayerRole.Goalkeeper, g, 1, 20), (PlayerRole.Defender, d, 21, 96), (PlayerRole.Forward, f, 97, 199),
                 })
        {
            for (var k = 0; k < count; k++)
            {
                if (DrawFreeSlot(pool, role, low, high, random) is not { } slot)
                    continue;
                var p = slot.Player;
                p.Strain = TransferRules.Random16(random, PlayerFactory.StartStrainMin, PlayerFactory.StartStrainMax);
                p.RollTrend(random); // [ORIGINAL] 0x42CB1 ruft nach der Energie 0x4907A (Datei 0x42E6B), vor +0x19 und Alter
                p.ContractYears = 1;
                slot.Owner = PoolOwner.Taken;
                slot.Manager = -1; // Besitzer ergibt sich aus dem Kader (wie beim Spielstart, SeatManagerSquads)
                HiddenSkills.Roll(p, random);
                slot.Club = club.Index;
                p.Age = TransferRules.Random16(random, SquadSetup.MinAge, SquadSetup.MaxAge);
                if (p.Id == 0 && NextPlayerId > 1)
                    AssignPlayerId(p);
                PlayerFactory.InsertSlot(club, p, managerSquad: true);
                p.StartClubCounters();
            }
        }

    }

    /// <summary>
    /// Startwerte, Preis und Gehalt des Startkaders ([ORIGINAL] Datei 0x29DA4–0x29EF7, nach dem Platztausch; siehe
    /// <see cref="SeatAddedSquad"/>), dann Rückennummern und Aufstellung.
    /// </summary>
    private void RollAddedStartValues(Manager manager, Club club, ManagerSetup setup, Random random)
    {
        // Spur zufallsstrom, Teil 3: dieselbe Schleife wie beim Spielstart (Datei 0x29DA4–0x29EF7); der erste Wurf ist die
        // Kondition (+0x10), der zweite die Technik (+0x11) – vorher vertauscht.
        RollStartValues(manager, SquadSetup.StartBase(Setup.StartLeague, setup.StartStrength), random);
        club.RenumberPlayers();
        club.Lineup.AutoFill(club.Players);
    }

    private static PoolSlot? DrawFreeSlot(IReadOnlyList<PoolSlot> pool, PlayerRole role, int low, int high, Random random)
    {
        bool Free(PoolSlot s) => s.Owner == PoolOwner.Free && s.Player.Role == role && s.Index <= NationalFirstIndex - 1;
        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var index = TransferRules.Random16(random, low, high);
            if (index > NationalFirstIndex - 1 || index - 1 >= pool.Count)
                continue;
            if (Free(pool[index - 1]))
                return pool[index - 1];
        }
        return pool.FirstOrDefault(Free);
    }

    /// <summary>Erster Platz der Nationalspieler im Pool. [ORIGINAL] Datei 0x29962 <c>cmp [bp−0x42], 0xB3</c> (179).</summary>
    private const int NationalFirstIndex = 180;
}

public sealed partial class TransferMarket
{
    /// <summary>
    /// Markt nach dem Hinzukommen eines Managers. [ORIGINAL] Datei 0x29A61–0x29B3E: Für jeden Platz des Markt-Satzes 4, dessen
    /// Poolspieler dem neuen Manager gehört (+0x1A) oder dessen Verein (+0x1D) den Namen des Managervereins trägt (0x38DB), wird
    /// der Platz freigegeben (0x425B6, Argumente 1, 10) und die Suche beginnt von vorn. Im Remake: Markt-Plätze vom Managerverein
    /// entfallen samt Geboten, ein Poolspieler wird wieder frei (wie <see cref="RemoveMarketSlot"/>).
    /// </summary>
    internal void RemoveListingsOfAddedClub(Club club)
    {
        for (var k = _listings.Count - 1; k >= 0; k--)
            if (_listings[k].From == club && _listings[k].Seller is null)
                RemoveMarketSlot(k);
    }
}
