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
    /// <item>Gemeinsame Werte: Level und Start-Stärke von Manager 0, Start-Liga des Spiels ([ORIGINAL] ein Wert für alle,
    ///   DGROUP 0x118/0x11A/0x11B; im Hinzufügen-Modus nicht verstellbar, Datei 0x286DA). [APPROX] Das Remake führt Level und
    ///   Start-Stärke je Manager; der neue übernimmt die von Manager 0.</item>
    /// <item>Verein: der gewählte, ohne Wahl ein zufälliger der Start-Liga ohne Manager (<see cref="NewGame.PickRandomClub"/>).
    ///   [APPROX] Kein Platztausch in die Start-Liga (Datei 0x29C3B–0x29CF2, Tausch 0x36169 mitten in der Saison): Das Remake
    ///   führt feste Vereinsindizes mit Tabellen, Ergebnissen und Spielplan je Verein; ein Tausch im laufenden Spiel ist nicht
    ///   nachgebildet, der Verein bleibt in seiner Liga.</item>
    /// <item>Startkader aus dem Pool (<see cref="SeatAddedSquad"/>), Startwerte, Gehälter, Aufstellung.</item>
    /// <item>Konto <see cref="FinanceRules.StartMoney"/> ([ORIGINAL] Datei 0x29BD5), Werbeangebote ([ORIGINAL] 0x3F833/0x3FB47,
    ///   Datei 0x2A03B–0x2A061), Trainingsplan; Stadion und Jugend legt <see cref="Finance"/> beim ersten Zugriff an.</item>
    /// </list>
    /// Der neue Manager ist am selben Tag noch am Zug (Hot-Seat: nicht bereit); der Manager am Zug bleibt ([ORIGINAL] Datei
    /// 0x4575B–0x45761). Verbraucht keinen Zufall aus <see cref="Random"/>: eigene Quelle (<see cref="HiddenSkills.SourceFor"/>),
    /// Tendenz aus <see cref="TrendRandom"/> wie beim Spielstart.
    /// </summary>
    /// <exception cref="InvalidOperationException">Kein Platz für einen weiteren Manager oder Modus „nur Weltkugel“.</exception>
    /// <exception cref="ArgumentException">Name leer, Verein nicht wählbar oder schon vergeben.</exception>
    public Manager AddManager(ManagerSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (!CanAddManager)
            throw new InvalidOperationException("Kein weiterer Manager möglich.");
        if (setup.Name.Length == 0)
            throw new ArgumentException("Der Manager braucht einen Namen.", nameof(setup));
        if (setup.ClubIndex is { } chosen && (chosen < 0 || chosen >= NewGame.SelectableClubs || IsClubManaged(chosen)))
            throw new ArgumentException($"Verein {chosen} ist nicht wählbar.", nameof(setup));

        var first = Setup.Managers.Count > 0 ? Setup.Managers[0] : null;
        setup.Level = first?.Level ?? setup.Level;
        setup.StartStrength = first?.StartStrength ?? setup.StartStrength;
        var random = HiddenSkills.SourceFor(this, AddManagerSalt + Managers.Count);
        var clubIndex = setup.ClubIndex ?? NewGame.PickRandomClub(Clubs, Setup.StartLeague, setup.StartStrength,
            Managers.Select(m => m.ClubIndex).ToHashSet(), random);
        setup.ClubIndex = clubIndex;

        // Die Spielvorbereitung des Spiels führt die Einträge parallel zu Managers (StartLeagueOf, Kadergröße, Export).
        while (Setup.Managers.Count < Managers.Count)
            Setup.Managers.Add(new ManagerSetup { Name = Managers[Setup.Managers.Count].Name, StartLeague = Setup.StartLeague });
        Setup.AddManager(setup);

        var manager = new Manager
        {
            Name = setup.Name, Head = Math.Max(0, setup.Head), ClubIndex = clubIndex, Level = setup.Level,
            Money = FinanceRules.StartMoney(setup.Level), // [ORIGINAL] Datei 0x29BD5
        };
        ManagerList.Add(manager);

        var club = Clubs[clubIndex];
        SeatAddedSquad(manager, club, setup, random);
        TransferMarket.RemoveListingsOfAddedClub(club);

        RenewOffers(manager);
        PrepareTraining(manager);
        club.ApplyAutoLineup();
        club.ReferenceRating = TeamStrength.Rating(club.Lineup);
        return manager;
    }

    /// <summary>Salz der eigenen Zufallsquelle (Datei 0x2993A, Kaderwahl im Hinzufügen-Modus).</summary>
    private const int AddManagerSalt = 0x2993A;

    /// <summary>Managerliste zum Anfügen; Spielstart (<see cref="NewGame"/>) und Laden legen eine <see cref="List{T}"/> an.</summary>
    private List<Manager> ManagerList => Managers as List<Manager>
        ?? throw new InvalidOperationException("Die Managerliste dieses Spielstands ist nicht erweiterbar.");

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
    /// (<see cref="SquadSetup.StartBase"/>): je Platz Technik, Kondition = random(Basis − 2, Basis + 2), ein dritter Wurf derselben
    /// Spanne (verworfen, Datei 0x29DF2 zieht vor der Unterscheidung), Form random(40, 60); dann +0x2C = Wert(Preis),
    /// +0x28 = Wert(Gehalt) (Datei 0x29EB4–0x29EF3). Talent (+0x12) und Name bleiben die des Poolspielers.
    /// [APPROX] Nach 1000 Fehlversuchen nimmt das Remake den ersten freien Platz der Art (das Original nähme den letzten
    /// gezogenen, auch einen vergebenen); ist keiner frei, bleibt der Platz leer. Die Spieler des bisherigen Computerkaders
    /// verlassen das Spiel (im Original hat ein Computerverein keine Spieler). Eigene Zufallsquelle statt des gemeinsamen Zufalls.
    /// </summary>
    private void SeatAddedSquad(Manager manager, Club club, ManagerSetup setup, Random random)
    {
        foreach (var old in club.Players.ToList())
            club.RemovePlayer(old);

        var pool = TransferMarket.Pool;
        var start = SquadSetup.StartBase(Setup.StartLeague, setup.StartStrength);
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

        foreach (var p in club.Players)
        {
            p.Technique = Player.Clamp(TransferRules.Random16(random, start - 2, start + 2));
            p.Condition = Player.Clamp(TransferRules.Random16(random, start - 2, start + 2));
            _ = TransferRules.Random16(random, start - 2, start + 2);
            p.Form = TransferRules.Random16(random, 40, 60);
            p.RollTrend(TrendRandom);
            p.PurchasePrice = Contracts.ValuePrice(this, manager, p);
            p.MonthlySalary = Contracts.ValueSalary(this, manager, p);
        }
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
