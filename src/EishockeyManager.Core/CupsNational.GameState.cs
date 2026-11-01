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

    /// <summary>
    /// Spielstärke eines Nationalteams in WM und Freundschaftsspiel: mit Bundestrainer aus dessen Aufstellung und
    /// Taktik (Spur nationalteam-2), sonst wie <see cref="NationalTeamRatings.MatchStrength"/> aus den Länderwerten.
    /// </summary>
    public int NationalMatchStrength(int club)
    {
        if (NationalCoach is { } coach && coach.Nation == club)
            return Math.Max(0, coach.Lineup().Strength + coach.TacticsBonus);
        var (cd, td, ca, ta) = NationalRatings(club);
        return (cd + ca + td + ta) / 4;
    }

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

    /// <summary>„auto“ (Datei 0x3CA06 ff.): Kader leeren, dann je Art die Besten nach Kondition + Technik + Form.</summary>
    public void AutoNominate()
    {
        var coach = NationalCoach ?? throw new InvalidOperationException("Kein Bundestrainer.");
        coach.ClearSquad();
        var candidates = NationalCandidates();
        PlayerRole[] roles = [PlayerRole.Goalkeeper, PlayerRole.Defender, PlayerRole.Forward];
        for (var r = 0; r < roles.Length; r++)
            foreach (var c in candidates.Where(c => c.Player.Role == roles[r] && c.Player.InjuredGames == 0)
                         .OrderByDescending(c => c.Player.Condition + c.Player.Technique + c.Player.Form)
                         .Take(NationalCoach.AutoSquad[r]))
                coach.Add(c.Player, c.Club);
    }

    /// <summary>
    /// Freundschaftsspiel vereinbaren und sofort austragen („WM Vorbereitung“, Datei 0x3BF9B ff.): Kosten vom
    /// <see cref="NationalAccount"/>, Ergebnis, Wertebonus für alle Kaderspieler, Länderspiele der Aufstellung.
    /// [APPROX] Das Original setzt die Energie aller Kaderspieler beim Öffnen von „WM Vorbereitung“ auf
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
                member.Player.Energy = NationalCoach.FriendlyEnergy;

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
        // #193 (Integration Runde 8, Welle 3, Leitstelle): [ORIGINAL] Nach dem Freundschaftsspiel schreibt 0x952C mit
        // Manager 5 (Bundestrainer) die Teamwerte des Landes aus dessen Aufstellung (Aufruf 0x555D6, Spur
        // spielstaerke-manager-2). [APPROX] Ohne eigene Aufstellung (Automatik) bleiben die Länderwerte stehen.
        if (coach.Nation == NationalTeams.Germany && coach.Formation.Count > 0)
        {
            var (defense, attack, _) = MatchEngine.LineupTeamValues(coach.Formation);
            _germanyRatings = (defense.Condition, defense.Technique, attack.Condition, attack.Technique);
        }
        return FriendlyResult.Played;
    }

    /// <summary>Nationalteam als Spielpartner der Spielberechnung (Stärke aus <see cref="NationalMatchStrength"/>).</summary>
    internal Club RatedNation(int club) => new()
    {
        Index = club,
        Name = Clubs[club].Name,
        Strength = NationalMatchStrength(club),
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
