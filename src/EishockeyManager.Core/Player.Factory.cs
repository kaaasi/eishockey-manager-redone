using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Zentrale Erzeugung vollständiger Spieler für alle Zugangspfade in einen Kader: Spielstart
/// (<see cref="SquadSetup"/>), Transfermarkt (<see cref="TransferMarket"/>), Talentsuche
/// (<see cref="TalentScout"/>, „Spion“ der Transferliste, <see cref="ApplyScoutedTalentValues"/>) und – für das Paket „lebenszyklus“ – neue Talente zum Saisonwechsel.
/// </summary>
/// <remarks>
/// Vertrag (R2-03, Folgereview 26.10.2026): Ein Spieler, der über <see cref="Join"/> in einen Kader kommt, hat
/// ein Alter, eine im Kader eindeutige Rückennummer, gültige Werte (1–99), Talent 0–9, Energie 1–100 sowie
/// Vertragslaufzeit und Gehalt (<see cref="Validate"/>). Werte und Zufallsfolgen der bisherigen Pfade sind
/// unverändert übernommen, damit bestehende Spielstände und Bildschirmfotos gleich bleiben.
/// </remarks>
public static class PlayerFactory
{
    /// <summary>Talente der Computervereine sind 17–19 Jahre alt. [APPROX] (Talentsuche des Managers: <see cref="ScoutedMinAge"/>)</summary>
    public const int TalentMinAge = 17, TalentMaxAge = 19;

    /// <summary>Verstecktes Talent der gefundenen Spieler. [APPROX]</summary>
    public const int TalentMinTalent = 6, TalentMaxTalent = 9;

    /// <summary>Energie eines frisch gefundenen Talents. [APPROX]</summary>
    public const int TalentEnergy = 80;

    /// <summary>Plausibilitätsgrenzen für das Alter in <see cref="Validate"/> (kein Spielregelwert).</summary>
    public const int MinPlausibleAge = 15, MaxPlausibleAge = 45;

    /// <summary>Höchste Rückennummer.</summary>
    public const int MaxNumber = 99;

    /// <summary>
    /// Werte eines Kaderspielers zu Spielbeginn aus den Teamwerten seiner Art (siehe <see cref="SquadSetup"/>).
    /// [ORIGINAL] Alter 19–31, Talent 0–9. [APPROX] ±2 um den Teamwert, Form 40–60, Energie 50–80, Gehalt aus der Stärke.
    /// Die Reihenfolge der Zufallszahlen ist festgelegt (Alter, Talent, Kondition, Technik, Form, Energie).
    /// </summary>
    public static void RollSquadValues(Player p, int teamCondition, int teamTechnique, Random random)
    {
        p.Age = SquadSetup.Between(random, SquadSetup.MinAge, SquadSetup.MaxAge);
        p.Talent = SquadSetup.Between(random, 0, 9);
        p.Condition = Player.Clamp(teamCondition + SquadSetup.Between(random, -2, 2));
        p.Technique = Player.Clamp(teamTechnique + SquadSetup.Between(random, -2, 2));
        p.Form = SquadSetup.Between(random, 40, 60);
        p.Energy = SquadSetup.Between(random, 50, 80);
        p.ContractYears = SquadSetup.StartContractYears;
        p.MonthlySalary = Contracts.BaseSalary(p, random);
    }

    /// <summary>
    /// Neues Talent (noch in keinem Kader). Anschließend mit <see cref="Join"/> aufnehmen.
    /// </summary>
    public static Player CreateTalent(string name, PlayerRole role, int clubStrength, Random random)
    {
        var p = new Player { Name = name, Role = role };
        ApplyTalentValues(p, clubStrength, random);
        return p;
    }

    /// <summary>
    /// Nachfolger eines Spielers, der die Laufbahn beendet hat (noch in keinem Kader). [ORIGINAL] Neuanlage im selben Pooleintrag,
    /// Datei 0x2787A–0x279DA: Alter random(18, 25), s = random(40, 92), die Grundstärken (Pool +0x15, +0x16) je
    /// random(s − 5, s + 5), Länderspiele (+0x19) 0; der Verein bleibt. Im Remake sind das Kondition und Technik.
    /// [APPROX] Das Original behält den Namen des Pooleintrags; das Remake legt einen neuen Spieler an (neuer Name, neue ID), damit
    /// der Ruheständler mit seiner Laufbahn erhalten bleibt. Form random(45, 60), Talent random(0, 9) und Energie
    /// <see cref="TalentEnergy"/> sind ergänzt (im Original nicht belegt).
    /// </summary>
    public static Player CreateRenewal(GameState game, string name, PlayerRole role, Random random)
    {
        ArgumentNullException.ThrowIfNull(game);
        var p = new Player { Name = name, Role = role };
        p.Age = TransferRules.Random16(random, 18, 25);
        var s = TransferRules.Random16(random, 40, 92);
        p.Condition = Player.Clamp(TransferRules.Random16(random, s - 5, s + 5));
        p.Technique = Player.Clamp(TransferRules.Random16(random, s - 5, s + 5));
        p.Form = TransferRules.Random16(random, 45, 60);
        p.Talent = TransferRules.Random16(random, 0, 9);
        p.Energy = TalentEnergy;
        p.Trend = PlayerTrend.Rising;
        game.AssignPlayerId(p);
        HiddenSkills.Roll(p, HiddenSkills.SourceFor(game, p.Id));
        return p;
    }

    /// <summary>Neues Talent für den laufenden Spielstand: wie oben, zusätzlich mit neuer Spieler-ID (Spur „speichern“).</summary>
    public static Player CreateTalent(GameState game, string name, PlayerRole role, int clubStrength, Random random)
    {
        ArgumentNullException.ThrowIfNull(game);
        var p = CreateTalent(name, role, clubStrength, random);
        game.AssignPlayerId(p);
        HiddenSkills.Roll(p, HiddenSkills.SourceFor(game, p.Id)); // Spur wettbewerbe-spiel, Teil 7: +0x19 (Datei 0x06F23)
        return p;
    }

    /// <summary>
    /// Talentwerte relativ zur Stärke des aufnehmenden Vereins. [APPROX] Alter 17–19, Talent 6–9,
    /// Kondition und Technik 2–8 unter dem Basiswert des Vereins, Form 45–60, Energie 80, Tendenz steigend.
    /// Spielstatistik, Verletzung und Sperre beginnen bei 0.
    /// </summary>
    public static void ApplyTalentValues(Player p, int clubStrength, Random random)
    {
        p.Age = SquadSetup.Between(random, TalentMinAge, TalentMaxAge);
        p.Talent = SquadSetup.Between(random, TalentMinTalent, TalentMaxTalent);
        var b = SquadSetup.BaseValue(clubStrength);
        p.Condition = Player.Clamp(b - SquadSetup.Between(random, 2, 8));
        p.Technique = Player.Clamp(b - SquadSetup.Between(random, 2, 8));
        p.Form = SquadSetup.Between(random, 45, 60);
        p.Energy = TalentEnergy;
        p.Trend = PlayerTrend.Rising;
        p.Games = p.Goals = p.Assists = p.Penalties2 = p.Penalties5 = p.Caps = 0;
        p.InjuredGames = p.SuspendedGames = 0;
    }

    /// <summary>
    /// Werte des Talents aus der Talentsuche (Spur geld, Teil 6). [ORIGINAL] em.exe Datei 0x06DE1–0x06E7D (0x06D7F mit
    /// Stärkenmitte <paramref name="s"/>): Alter random(18, 21), Form random(45, 55), Technik random(s − 5, s + 1), Kondition
    /// random(s − 5, s + 1) – in dieser Reihenfolge. Talent, Name und Rolle bleiben (Poolspieler). Die Talentsuche der
    /// Computervereine (<see cref="ApplyTalentValues"/>) ist davon nicht berührt.
    /// </summary>
    public static void ApplyScoutedTalentValues(Player p, int s, Random random)
    {
        ArgumentNullException.ThrowIfNull(p);
        p.Age = TransferRules.Random16(random, ScoutedMinAge, ScoutedMaxAge);
        p.Form = TransferRules.Random16(random, 45, 55);
        p.Technique = Player.Clamp(TransferRules.Random16(random, s - 5, s + 1));
        p.Condition = Player.Clamp(TransferRules.Random16(random, s - 5, s + 1));
        p.InjuredGames = p.SuspendedGames = 0;
    }

    /// <summary>Alter des Talents aus der Talentsuche. [ORIGINAL] Datei 0x06DE1: random(18, 21).</summary>
    public const int ScoutedMinAge = 18, ScoutedMaxAge = 21;

    /// <summary>
    /// Name eines Talents. [APPROX] Aus Vor- und Nachnamen zweier zufälliger Spieler zusammengesetzt; haben die Namen
    /// nur einen Teil (Originaldatenbank: nur Nachnamen), wird der Name eines zufälligen Spielers übernommen –
    /// er kann dann doppelt vorkommen (unverändert aus dem bisherigen Jugendpfad, offen).
    /// </summary>
    public static string TalentName(IReadOnlyList<Player> all, Random random)
    {
        if (all.Count == 0)
            return "TALENT";
        static string[] Parts(string name) => name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var first = Parts(all[random.Next(all.Count)].Name);
        var last = Parts(all[random.Next(all.Count)].Name);
        return first.Length > 1 && last.Length > 0 ? $"{first[0]} {last[^1]}" : all[random.Next(all.Count)].Name;
    }

    /// <summary>
    /// Kleinste freie Rückennummer im Kader (1–99). Seit A091 vergibt das Spiel die Nr. als Kaderplatz + 1
    /// (<see cref="Club.RenumberPlayers"/>); bleibt für Testhelfer, die Spieler direkt in einen Kader legen.
    /// </summary>
    public static int FreeNumber(Club club, Player? except = null)
    {
        for (var n = 1; n <= MaxNumber; n++)
            if (club.Players.All(q => q == except || q.Number != n))
                return n;
        throw new InvalidOperationException($"Keine freie Rückennummer im Kader von {club.Name}.");
    }

    /// <summary>
    /// Nimmt einen Spieler in einen Kader auf: hinten an, Nr. = Kaderplatz + 1 (<see cref="Club.RenumberPlayers"/>), neuer Vertrag zur Gehaltsforderung,
    /// ohne Versicherung und ohne abgelehntes Angebot. Gemeinsamer Abschluss für Transfer, Leihe und Talentsuche.
    /// </summary>
    public static void Join(Club club, Player p, int contractYears, bool loan, PlayerLoan? agreement = null)
    {
        if (loan != (agreement is not null) || agreement?.Origin == club)
            throw new ArgumentException("Eine Leihe benötigt einen anderen Stammverein und dessen Vertrag.", nameof(agreement));
        p.OnLoan = loan;
        p.Loan = agreement;
        p.Insured = false;
        p.NegotiationLock = 0;
        p.ContractYears = contractYears;
        p.MonthlySalary = Contracts.Demand(p);
        // [ORIGINAL] neuer Kaderplatz (Datei 0x42CB1): Spiele für den Verein +0x1C = 0; die Saisonspiele (+0x06) bleiben (FILE07).
        p.ClubGamesCarry = -p.Games;
        // [ORIGINAL] A091: neuer Kaderplatz hinten an (Datei 0x42CB1; FILE05 → FILE07: DAFFNER auf Platz 25, Nr. 26).
        if (!club.Players.Contains(p))
            club.Players.Add(p);
        club.RenumberPlayers();
    }

    /// <summary>
    /// Prüft die Invarianten eines neu erzeugten bzw. neu aufgenommenen Spielers (leere Liste = vollständig).
    /// Für Spieler im laufenden Spiel gilt zusätzlich Restlaufzeit 0 („läuft zum Saisonende aus“) als gültig.
    /// </summary>
    public static IReadOnlyList<string> Validate(Club club, Player p)
    {
        var problems = new List<string>();
        void Check(bool ok, string text)
        {
            if (!ok)
                problems.Add(text);
        }

        Check(!string.IsNullOrWhiteSpace(p.Name), "Name fehlt");
        Check(p.Age is >= MinPlausibleAge and <= MaxPlausibleAge, $"Alter {p.Age}");
        Check(p.Talent is >= 0 and <= 9, $"Talent {p.Talent}");
        Check(p.Condition is >= 1 and <= Player.MaxValue, $"Kondition {p.Condition}");
        Check(p.Technique is >= 1 and <= Player.MaxValue, $"Technik {p.Technique}");
        Check(p.Form is >= 1 and <= Player.MaxValue, $"Form {p.Form}");
        // A184 (Spur wettbewerbe-spiel, Runde 12): Belastung (Kaderplatz +0x13) statt Energie; gültig 50 … 150 wie nach Training
        // und Spiel (Datei 0x6B13, 0x549A2). Belastung 150 hieß vorher Energie 0 und war ungültig.
        Check(p.Strain is >= TrainingRules.MinStrain and <= TrainingRules.MaxStrain, $"Belastung {p.Strain}");
        Check(p.Number is >= 1 and <= MaxNumber, $"Rückennummer {p.Number}");
        Check(club.Players.Count(q => q.Number == p.Number) <= 1, $"Rückennummer {p.Number} doppelt");
        Check(p.ContractYears is >= Contracts.MinYears and <= Contracts.MaxYears, $"Vertragsdauer {p.ContractYears}");
        Check(p.MonthlySalary >= Contracts.MinSalary, $"Gehalt {p.MonthlySalary}");
        Check(p.OnLoan == (p.Loan is not null) && p.Loan?.Origin != club, "Leihursprung fehlt oder ist ungültig");
        return problems;
    }
}
