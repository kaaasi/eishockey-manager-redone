using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Zentrale Erzeugung vollständiger Spieler für alle Zugangspfade in einen Kader: Spielstart
/// (<see cref="SquadSetup"/>), Transfermarkt (<see cref="TransferMarket"/>), Talentsuche
/// (<see cref="TalentScout"/>, Jugend-„Spion“) und – für das Paket „lebenszyklus“ – neue Talente zum Saisonwechsel.
/// </summary>
/// <remarks>
/// Vertrag (R2-03, Folgereview 26.10.2026): Ein Spieler, der über <see cref="Join"/> in einen Kader kommt, hat
/// ein Alter, eine im Kader eindeutige Rückennummer, gültige Werte (1–99), Talent 0–9, Energie 1–100 sowie
/// Vertragslaufzeit und Gehalt (<see cref="Validate"/>). Werte und Zufallsfolgen der bisherigen Pfade sind
/// unverändert übernommen, damit bestehende Spielstände und Bildschirmfotos gleich bleiben.
/// </remarks>
public static class PlayerFactory
{
    /// <summary>Talente aus der Talentsuche sind 17–19 Jahre alt. [APPROX] (bisher Transfer-Talentsuche)</summary>
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

    /// <summary>Kleinste freie Rückennummer im Kader (1–99).</summary>
    public static int FreeNumber(Club club, Player? except = null)
    {
        for (var n = 1; n <= MaxNumber; n++)
            if (club.Players.All(q => q == except || q.Number != n))
                return n;
        throw new InvalidOperationException($"Keine freie Rückennummer im Kader von {club.Name}.");
    }

    /// <summary>
    /// Nimmt einen Spieler in einen Kader auf: neue freie Rückennummer, neuer Vertrag zur Gehaltsforderung,
    /// ohne Versicherung und ohne abgelehntes Angebot. Gemeinsamer Abschluss für Transfer, Leihe und Talentsuche.
    /// </summary>
    public static void Join(Club club, Player p, int contractYears, bool loan)
    {
        p.Number = FreeNumber(club, p);
        p.OnLoan = loan;
        p.Insured = false;
        p.RejectedOfferDay = null;
        p.ContractYears = contractYears;
        p.MonthlySalary = Contracts.Demand(p);
        if (!club.Players.Contains(p))
            club.Players.Add(p);
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
        Check(p.Energy is >= 1 and <= 100, $"Energie {p.Energy}");
        Check(p.Number is >= 1 and <= MaxNumber, $"Rückennummer {p.Number}");
        Check(club.Players.Count(q => q.Number == p.Number) <= 1, $"Rückennummer {p.Number} doppelt");
        Check(p.ContractYears is >= Contracts.MinYears and <= Contracts.MaxYears, $"Vertragsdauer {p.ContractYears}");
        Check(p.MonthlySalary >= Contracts.MinSalary, $"Gehalt {p.MonthlySalary}");
        return problems;
    }
}
