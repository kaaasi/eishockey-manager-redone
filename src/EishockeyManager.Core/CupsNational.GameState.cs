using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Spielstand – Teil des Pakets „wettbewerbe“: Nationalteams, Bundestrainer, WM-Kader, Freundschaftsspiele.</summary>
public sealed partial class GameState
{
    private (int ConditionDefense, int TechniqueDefense, int ConditionAttack, int TechniqueAttack)? _germanyRatings;

    /// <summary>Bundestrainer dieser Saison oder null (Modus 1 ab dem Sprung zur WM, Modus 4 ab dem 13. April, Modus 2 nie).</summary>
    public NationalCoach? NationalCoach { get; private set; }

    /// <summary>Etat, den der Bundestrainer beim nächsten Amtsantritt bekommt (DM, im Original DGROUP 0x29C).</summary>
    public long NationalBudget { get; private set; } = NationalCoach.FirstBudget;

    /// <summary>
    /// Konto des Bundestrainers (DM). [ORIGINAL] Datei 0x554A6: Der Etat wird dem Konto des Nationalteam-Datensatzes
    /// (DGROUP 0xA055, Manager-Satz 4) gutgeschrieben, nicht dem Vereinskonto des Managers; Freundschaftsspiele
    /// werden von dort bezahlt (0x3BFC4). Ein Rest bleibt für den nächsten Amtsantritt stehen. (Spur finanzen-2)
    /// </summary>
    public long NationalAccount { get; set; }

    /// <summary>
    /// Buchungen auf dem <see cref="NationalAccount"/> (Etat, Freundschaftsspiele), älteste zuerst. Die Summe ist der
    /// Kontostand, solange niemand das Konto direkt setzt (nur Tests). Spur wettbewerbe-weltkugel.
    /// </summary>
    public List<NationalBooking> NationalBookings { get; } = [];

    /// <summary>Bucht einen Betrag auf das Nationalkonto und in <see cref="NationalBookings"/>.</summary>
    private void BookNational(NationalBookingKind kind, long amount)
    {
        NationalAccount += amount;
        NationalBookings.Add(new NationalBooking(Season, kind, amount));
    }

    /// <summary>
    /// Spielwerte eines Nationalteams. [ORIGINAL] Ohne menschlichen Bundestrainer würfelt das Original die Werte
    /// des eigenen Landes an jedem Tag mit Markierung 255 (13. und 29. April) neu aus (Datei 0x554CF):
    /// Kondition random(84, 94), Technik random(79, 89), Form random(45, 55) – je Abwehr und Sturm. Bis dahin gelten
    /// die Werte aus DGROUP 0x4C4E (<see cref="NationalTeamRatings"/>).
    /// </summary>
    public (int ConditionDefense, int TechniqueDefense, int ConditionAttack, int TechniqueAttack) NationalRatings(int club) =>
        club == NationalTeams.Germany && _germanyRatings is { } r ? r : NationalTeamRatings.Of(club);

    /// <summary>Tage mit Markierung 255: Werte des eigenen Landes neu auswürfeln, wenn es keinen Bundestrainer hat.</summary>
    internal void RerollNationalRatings()
    {
        if (NationalCoach?.Nation == NationalTeams.Germany)
            return;
        var r = CupRandom;
        // Reihenfolge wie im Original: je Abwehr/Sturm Kondition, Technik, Form (Form führt das Remake nicht).
        int[] condition = new int[2], technique = new int[2];
        for (var i = 0; i < 2; i++)
        {
            condition[i] = r.Next(84, 95);
            technique[i] = r.Next(79, 90);
            _ = r.Next(45, 56);
        }
        _germanyRatings = (condition[0], technique[0], condition[1], technique[1]);
    }

    /// <summary>
    /// Länderwerte aus der Aufstellung des Bundestrainers: 0x952C mit Manager 5 (Datei 0x0952C–0x0977E) schreibt die
    /// Vereinsfelder 0x18–0x1D des Landes ohne Bedingung aus Reihe 0–2 der Aufstellung, leere Plätze zählen 0
    /// (<see cref="MatchEngine.LineupTeamValues"/>; DOSBox FILE20: nur ein Stürmer 88/96/57 in Reihe 1 → 00 09 00 0A 00 06).
    /// Kondition und Technik (0x18–0x1B) stehen in <see cref="NationalRatings"/>, die Form (0x1C/0x1D) im Vereinsplatz
    /// (<see cref="Club.TeamValues"/>), den der Saisonwechsel wie bei allen Plätzen neu würfelt (DOSBox FILE20 → FILE21:
    /// 0x18–0x1B bleiben, 0x1C/0x1D neu; Review R13RD-04).
    /// <list type="bullet">
    /// <item>[ORIGINAL] Aufrufe (A009, Spur nationalteam Runde 13, <c>docs/analyse/re-runde-13.md</c>): Verlassen der Aufstellung
    ///   (Datei 0x41506–0x4151A, <see cref="OnNationalLineupLeft"/>), Tageslauf (Datei 0x54F9C–0x54FBF) und nach dem Menü des
    ///   Bundestrainers an WM-Tagen (Datei 0x555D6). Nicht nach einem Freundschaftsspiel.</item>
    /// <item>[APPROX] Das Remake rechnet an jedem Tag vor dessen Spielen (<c>GameState.Integration.cs</c>, OnDayManager), also
    ///   nach dem Halt des Bundestrainers. [ORIGINAL] Im Tageslauf vor der kleinen Wanderung (Datei 0x54FBF vor 0x55292, #594):
    ///   an Wandertagen gilt danach die gewürfelte Form 0x1C/0x1D. Im Modus „nur Weltkugel“ ruft das Original nur 0x555D6 (kein Tageslauf); zwischen
    ///   zwei WM-Tagen ändert sich die Aufstellung dort nur über den Bildschirm, der beim Verlassen ohnehin rechnet.</item>
    /// <item>[ORIGINAL] Eine leere Aufstellung schreibt 0 (A009, DOSBox FILE47: 0x18–0x1D = 00 00 00 00 00 00 schon nach dem
    ///   Verlassen der geleerten Aufstellung; FILE18 vorher 58 5A 53 57 33 33).</item>
    /// <item>[APPROX] Mit leerem WM-Kader schreibt das Remake nichts. Das Original schriebe auch dann 0; einen leeren Kader am
    ///   Spieltag schließt dort die Warnung vor dem Weiterschalten aus (A300, DOSBox `file11_11`), und 0x555D6 rechnet nach dem Menü
    ///   neu. Ohne die Ausnahme fiele Deutschland bei einem Bundestrainer ohne Kader (passiver Manager, Proben) auf 0.</item>
    /// <item>[APPROX] Vor dem ersten Spiel eines Spielstands zieht der Aufruf die Teamwerte (<see cref="MatchEngine.EnsureTeamValues"/>),
    ///   damit der Lader die Form nicht überschreibt; das Original lädt sie beim Spielstart.</item>
    /// </list>
    /// </summary>
    internal void UpdateNationalTeamValues()
    {
        if (NationalCoach is not { Nation: NationalTeams.Germany, Squad.Count: > 0 } coach)
            return;
        var lineup = coach.TeamValueFormation();
        var club = Clubs[coach.Nation];
        if (club.TeamValues is null)
            MatchEngine.EnsureTeamValues(this);
        var (defense, attack, _) = MatchEngine.LineupTeamValues(lineup);
        _germanyRatings = (defense.Condition, defense.Technique, attack.Condition, attack.Technique);
        club.TeamValues = MatchEngine.TeamValuesOf(club) with { DefenseForm = defense.Form, AttackForm = attack.Form };
    }

    /// <summary>
    /// WM-Stufe: gespielte WM-Termine. [ORIGINAL] DGROUP 0x7AE: 0 bei der Anmeldung der WM (0x30E7B mit Argument 0, Datei
    /// 0x30E89), + 1 nach jedem Termin (Datei 0x311C8); 5 Gruppentermine, dann drei K.-o.-Termine.
    /// </summary>
    public int WorldCupStage => WorldCup is not { Groups.Count: > 0 } wm
        ? 0
        : wm.Groups[0].Results.Count / (WorldCup.GroupSize / 2) + wm.Knockout.Count;

    /// <summary>
    /// „Transfer“ („Spieler in WM-Kader“) und „Training“ („WM Vorbereitung“) im Menü des Bundestrainers gesperrt (#580,
    /// Nebenbefund Runde 13). [ORIGINAL] Datei 0x4509E–0x450A4: im Zug von Manager 5 Sperrbytes DGROUP 0x518C/0x518D = WM-Stufe
    /// DGROUP 0x7AE, also ab dem ersten gespielten WM-Termin bis zur neuen Saison (DOSBox `file19_26`, 20.04.1994). Die Menüs
    /// der Vereinsmanager sperrt das Original damit nicht (Zweig Datei 0x45057–0x4509C).
    /// </summary>
    public bool NationalSquadLocked => WorldCupStage > 0;

    /// <summary>
    /// Ereignis „Aufstellung des Bundestrainers verlassen“ (Datei 0x41506–0x4151A: 0x952C mit DGROUP 0xA70E). [APPROX] Der
    /// Bildschirm meldet es nur, wenn der Besuch die Aufstellung geändert hat: Bildschirme ändern durch bloßes Öffnen keinen
    /// Spielzustand (AGENTS.md, E4). Unterschied zum Original nur, wenn sich seit der letzten Rechnung Spielerwerte geändert
    /// haben (Bonus eines Freundschaftsspiels); dann rechnet der nächste Tag.
    /// </summary>
    public void OnNationalLineupLeft() => UpdateNationalTeamValues();

    /// <summary>
    /// Ein Manager wird Bundestrainer des eigenen Landes (Datei 0x55440 ff.): Der Etat <see cref="NationalBudget"/>
    /// geht auf das <see cref="NationalAccount"/>, der Kader beginnt leer. Ist er in dieser Saison schon
    /// ernannt, bleibt alles unverändert (kein zweiter Etat, Kader bleibt; Review R3A-03).
    /// </summary>
    public NationalCoach AppointNationalCoach(Manager manager)
    {
        var index = Managers.ToList().IndexOf(manager);
        if (index < 0)
            throw new ArgumentException("Unbekannter Manager.", nameof(manager));
        if (NationalCoach is { } current)
            return current.Manager == index
                ? current
                : throw new InvalidOperationException("In dieser Saison ist bereits ein anderer Manager Bundestrainer.");
        NationalCoach = new NationalCoach(index, NationalTeams.Germany);
        NationalCoach.TakeOverAutoLineup(_nationalAutoLineupSaved); // A009: DGROUP 0x37F/0x380 überdauern das Amt
        HandNationalTacticsToCoach(NationalCoach);
        BookNational(NationalBookingKind.Budget, NationalBudget);
        return NationalCoach;
    }

    /// <summary>Kandidaten für den WM-Kader mit ihrem Verein (siehe <see cref="Core.NationalCoach"/>, [APPROX]).</summary>
    public IReadOnlyList<NationalSquadMember> NationalCandidates()
    {
        var list = new List<NationalSquadMember>();
        foreach (var club in Clubs)
            if (club.League is not null)
                list.AddRange(club.Players.Where(p => !p.IsForeigner).Select(p => new NationalSquadMember(p, club.Index)));
        foreach (var listing in TransferMarket.Listings)
            // Spur transfer-vertraege-komplett, Teil 5: Poolspieler stehen in keinem Kader; ihr Platz wird beim Listen- und
            // Saisonwechsel neu vergeben, deshalb nicht nominierbar (wie bisher nur Kaderspieler und Vertragslose).
            if (!listing.FromPool && !listing.Player.IsForeigner && list.All(c => c.Player != listing.Player))
                list.Add(new NationalSquadMember(listing.Player, listing.From.Index));
        return list.OrderByDescending(c => c.Player.Condition + c.Player.Technique + c.Player.Form)
            .ThenBy(c => c.Player.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Klick auf einen Spieler in „Spieler in WM-Kader“ (Datei 0x3C928): Kaderspieler werden entlassen, andere
    /// unverletzte Spieler aufgenommen, solange weniger als 29 im Kader stehen.
    /// <list type="bullet">
    /// <item>[ORIGINAL] A085 (Sitzung 5, Review #649): Bei vollem Kader bewirkt der Klick nichts (Datei 0x3C928–0x3C9C0, Platzsuche
    ///   über Satz 5 +0x0F; Angabe Nutzer). Herausnehmen rückt auf (Datei 0x3CA71 → 0x425B6, wie der Verkauf), Aufnehmen sortiert nach
    ///   Art ein (Datei 0x3CABF → 0x42CB1/0x42D55, wie der Kauf; <see cref="NationalCoach.Add"/>): ENGLBRECHT (TW) steht in FILE49A
    ///   auf Platz 3, wo KÜNAST stand. Die Aufnahme merkt den Verein im Kaderplatz +0x0C (<see cref="NationalSquadMember.Club"/>),
    ///   das Herausnehmen schreibt ihn zurück. Eingesetzt wird der Neue nicht durch die Aufnahme (0x3CABF setzt +0x0A nicht), sondern
    ///   durch die Neuaufstellung beim nächsten Menüaufbau, wenn „automatisch aufstellen“ an ist (A009).</item>
    /// </list>
    /// </summary>
    public NominationResult ToggleNomination(Player player)
    {
        var coach = NationalCoach ?? throw new InvalidOperationException("Kein Bundestrainer.");
        if (coach.Contains(player))
        {
            coach.Remove(player);
            return NominationResult.Removed;
        }
        if (player.InjuredGames > 0)
            return NominationResult.Unavailable;
        if (coach.Squad.Count >= NationalCoach.MaxSquad)
            return NominationResult.SquadFull;
        var club = NationalCandidates().FirstOrDefault(c => c.Player == player)
            ?? throw new ArgumentException("Spieler steht nicht zur Auswahl.", nameof(player));
        coach.Add(player, club.Club);
        return NominationResult.Added;
    }

    /// <summary>
    /// „auto“ (Datei 0x3CA06 ff.): Kader leeren, dann je Art die Besten nach Kondition + Technik + Form.
    /// <list type="bullet">
    /// <item>[ORIGINAL] Kaderplätze 0–2 Torhüter, 3–12 Verteidiger, 13–28 Stürmer, je Art absteigend nach der Summe; bei
    ///   Gleichstand gewinnt der zuerst durchsuchte Spieler (Datei 0x3CC01: Schleife über Pool 1 … 0xB3, strikter Vergleich
    ///   Datei 0x3CCBA–0x3CCC4). Beleg DOSBox FILE18, Satz 5: LEDLIN (Pool 129) vor DEISENBERGER (169), je 228; LEE (114) vor
    ///   KUHNKE (166), je 220 – nicht nach dem Namen (A085, Spur nationalteam Runde 13).</item>
    /// <item>[APPROX] Einen Poolindex führt das Remake nicht: Gleichstand nach <see cref="Player.Id"/> (Anlagereihenfolge der
    ///   Spieler) statt nach dem Pool des Originals; bis Runde 13 nach dem Namen.</item>
    /// </list>
    /// </summary>
    public void AutoNominate()
    {
        var coach = NationalCoach ?? throw new InvalidOperationException("Kein Bundestrainer.");
        coach.ClearSquad();
        var candidates = NationalCandidates();
        PlayerRole[] roles = [PlayerRole.Goalkeeper, PlayerRole.Defender, PlayerRole.Forward];
        for (var r = 0; r < roles.Length; r++)
            foreach (var c in candidates.Where(c => c.Player.Role == roles[r] && c.Player.InjuredGames == 0)
                         .OrderByDescending(c => c.Player.Condition + c.Player.Technique + c.Player.Form)
                         .ThenBy(c => c.Player.Id)
                         .Take(NationalCoach.AutoSquad[r]))
                coach.Add(c.Player, c.Club);
    }

    /// <summary>
    /// Freundschaftsspiel vereinbaren und sofort austragen („WM Vorbereitung“, Datei 0x3BF9B ff.): Kosten vom
    /// <see cref="NationalAccount"/>, Ergebnis, Wertebonus für alle Kaderspieler, Länderspiele der Aufstellung.
    /// [APPROX] Das Original setzt die Belastung (Kaderplatz +0x13) aller Kaderspieler beim Öffnen von „WM Vorbereitung“ auf
    /// <see cref="NationalCoach.FriendlyEnergy"/> (Datei 0x3BB51). Bildschirme ändern beim Öffnen keinen Zustand,
    /// deshalb setzt das Remake sie beim ersten Freundschaftsspiel der Saison, vor dem Spiel (Entscheidung des Nutzers,
    /// PR #189). Abweichung: Im Original setzt jedes erneute Öffnen wieder auf 70, im Remake nur das erste Spiel. Den
    /// Abzug je Spiel (DOSBox: 46 nach drei Spielen) führt erst die Spur `spielberechnung`.
    /// </summary>
    public FriendlyResult PlayFriendly(int opponent, bool home)
    {
        var coach = NationalCoach ?? throw new InvalidOperationException("Kein Bundestrainer.");
        if (opponent == coach.Nation)
            return FriendlyResult.OwnNation;
        if (coach.Friendlies.Count >= NationalCoach.MaxFriendlies)
            return FriendlyResult.TooMany;
        var cost = NationalCoach.FriendlyCost(opponent, home);
        if (NationalAccount < cost)
            return FriendlyResult.NotEnoughMoney;
        BookNational(NationalBookingKind.Friendly, -cost);
        if (coach.Friendlies.Count == 0)
            foreach (var member in coach.Squad)
                member.Player.Strain = NationalCoach.FriendlyEnergy; // Kaderplatz +0x13 (A184)

        var random = CupRandom;
        // Reihenfolge der Zufallszahlen wie im Original: x vor dem Spiel, Bonus danach.
        var xRoll = random.Next(0, 51);
        var (homeClub, awayClub) = home ? (coach.Nation, opponent) : (opponent, coach.Nation);
        var lineup = coach.Lineup();
        // Integration Runde 7: Spielberechnung des Originals wie bei den WM-Spielen (ohne Anzeige, schneller Weg der
        // Pokale). [APPROX] Freundschaftsspiele wie ein WM-Gruppenspiel (Unentschieden möglich, keine Verlängerung);
        // die Zuordnung im Original (Wettbewerbscode des Freundschaftsspiels) ist nicht gelesen.
        var report = MatchEngine.PlayWithEvents(this, RatedNation(homeClub), RatedNation(awayClub), MatchCompetition.WorldCup,
            knockOut: false, random);
        var (hg, ag) = (report.HomeGoals, report.AwayGoals);
        var (own, against) = home ? (hg, ag) : (ag, hg);
        var (x, bonus) = NationalCoach.FriendlyBonus(cost, xRoll, home, own, against, random);
        foreach (var member in coach.Squad)
            NationalCoach.ApplyBonus(member.Player, x, bonus);
        foreach (var p in lineup.Players)
            p.Caps++;
        coach.AddFriendly(new FriendlyMatch(homeClub, awayClub, hg, ag, bonus));
        EnterNationalResult(homeClub, awayClub, hg, ag, NationalMatchKind.Friendly); // 0x50283, Code 0xD (Datei 0x303BA)
        // A009 (Spur nationalteam Runde 13): [ORIGINAL] Das Freundschaftsspiel schreibt die Länderwerte nicht neu (kein Aufrufer
        // von 0x952C im Overlay 0xF8, Datei 0x3BF9B/0x3C05E; DOSBox FILE18 → FILE19: Werte von Verein 54 unverändert trotz
        // Bonus und neuer Aufstellung). Bis Runde 13 schrieb das Remake sie hier (#193).
        return FriendlyResult.Played;
    }

    /// <summary>
    /// Nationalteam als Spielpartner der Spielberechnung: ein Vereinsplatz ohne Kader, dessen Werte
    /// <see cref="MatchEngine.LiveTeamOf"/> über <see cref="MatchEngine.NationalLiveTeam"/> aus
    /// <see cref="NationalTeamValues"/> bildet (Spur wettbewerbe-spiel, Teil 3).
    /// </summary>
    internal Club RatedNation(int club) => new()
    {
        Index = club,
        Name = Clubs[club].Name,
        Strength = Clubs[club].Strength, // Datenbankwert, wirkt nicht (NationalTeamRatings)
    };

    /// <summary>WM-Spiel des Bundestrainers: Länderspiele für die Aufstellung zählen.</summary>
    internal void CountCaps(IEnumerable<KnockoutMatch> matches)
    {
        if (NationalCoach is not { } coach || !matches.Any(m => m.Home == coach.Nation || m.Away == coach.Nation))
            return;
        foreach (var p in coach.Lineup().Players)
            p.Caps++;
    }

    /// <summary>
    /// Saisonwechsel der Nationalteams (aus <see cref="WorldCup.StartSeason"/>): Kaderspieler kehren zurück
    /// (Datei 0x557B0), Freundschaftsspiele beginnen bei null (Datei 0x30EBE), das Amt endet (Datei 0x5580E).
    /// </summary>
    internal void StartNationalSeason()
    {
        TakeNationalTacticsFromCoach();
        if (NationalCoach is { } coach)
            _nationalAutoLineupSaved = coach.SavedAutoLineup;
        NationalCoach = null;
        _nationalTurn = false;
    }

    /// <summary>Nach der WM: Etat für die nächste Saison aus dem Platz des eigenen Landes (Datei 0x31293 ff.).</summary>
    internal void SetNationalBudget(IReadOnlyList<int> ranking)
    {
        var place = ranking.ToList().IndexOf(NationalTeams.Germany) + 1;
        NationalBudget = NationalCoach.BudgetFor(place == 0 ? null : place);
    }
}
