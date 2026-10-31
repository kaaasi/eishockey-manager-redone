using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Regeln des Kaderlebenszyklus: Vertragsankündigungen (R2-11), Ruhestand und Kader-KI der Computervereine (R2-12).
/// </summary>
/// <remarks>
/// [ORIGINAL] Texte aus em.exe und Ablauf aus dem 75-Minuten-Video: Spieler im letzten Vertragsjahr melden sich
/// über die ganze Saison verteilt im Posteingang (belegt vom 13. September bis 30. März), meist mit
/// „… bietet Ihnen an, von 1 auf 2 Jahre zu verlängern.“ (im Video 15 verschiedene: 12-mal „auf 2“, 2-mal „auf 3“, einmal „auf 4“),
/// selten mit „… kündigt an, daß er seinen Vertrag nicht mehr verlängern wird.“ (einmal). Die Verlängerung selbst
/// geschieht im Bildschirm „Verträge“. Das Laufbahnende meldet „… gibt das Eishockeyspielen im Alter von # Jahren
/// endgültig auf.“, die Verhandlung „… wird sich mit Ablauf des Vertrages zur Ruhe setzen.“ bzw.
/// „… ist im Moment nicht verhandlungsbereit.“
/// [APPROX] Alle Wahrscheinlichkeiten und Altersgrenzen; das Original verwaltet Computervereine nur über Teamwerte
/// (siehe <see cref="SquadSetup"/>), die Kader-KI ist eine Ergänzung des Remakes.
/// </remarks>
public static class SquadRules
{
    /// <summary>Tägliche Chance je Spieler im letzten Vertragsjahr, sich zu melden. [APPROX]</summary>
    public const double NoticeChancePerDay = 1 / 60.0;

    /// <summary>Anteil der Absagen unter den Ankündigungen (ohne Ruhestand). [APPROX] Video: 1 von 16.</summary>
    public const double RefuseChance = 0.1;

    /// <summary>Ab diesem Alter kann ein Spieler aufhören. [APPROX]</summary>
    public const int RetireMinAge = 33;

    /// <summary>Ab diesem Alter hört jeder Spieler auf. [APPROX]</summary>
    public const int RetireMaxAge = 37;

    /// <summary>Computervereine verlängern einen auslaufenden Vertrag mit dieser Chance. [APPROX]</summary>
    public const double ComputerExtendChance = 0.75;

    /// <summary>Laufzeit neuer Verträge der Computervereine (Verlängerung und Neuzugang). [APPROX]</summary>
    public const int ComputerMinYears = 1, ComputerMaxYears = 3;

    /// <summary>Spieler aus dem Pool vertragsloser Spieler kommen nur bis zu diesem Alter unter. [APPROX]</summary>
    public const int FreeAgentMaxAge = 32;

    /// <summary>Anteil der Neuzugänge aus dem Pool, solange passende Spieler da sind; sonst Talente. [APPROX]</summary>
    public const double FreeAgentShare = 0.6;

    /// <summary>Höchstgröße eines Computerkaders (größter Startkader). [APPROX]</summary>
    public const int MaxComputerSquad = 25;

    /// <summary>Mindestbesetzung eines Computerkaders: wie beim Spielstart (<see cref="SquadSetup"/>).</summary>
    public static int MinimumOf(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => 2,
        PlayerRole.Defender => 8,
        _ => 10,
    };

    /// <summary>Chance, nach Erreichen dieses Alters die Laufbahn zu beenden: 33 → 20 %, … 37 → 100 %. [APPROX]</summary>
    public static double RetirementChance(int age) =>
        age < RetireMinAge ? 0 : Math.Min(1.0, (age - RetireMinAge + 1) / (double)(RetireMaxAge - RetireMinAge + 1));

    /// <summary>Angebotene Gesamtlaufzeit „von 1 auf n Jahre“: 2 (80 %), 3 (13 %), 4 (7 %). [APPROX] Video: 12 / 2 / 1.</summary>
    public static int OfferedYears(double roll) => roll < 0.8 ? 2 : roll < 0.93 ? 3 : 4;

    public static string[] OfferText(Player p, int years) =>
        [$"{p.Name} bietet Ihnen an, von 1 auf {years}", "Jahre zu verlängern."];

    public static string[] RefusalText(Player p) =>
        [$"{p.Name} kündigt an, daß er seinen Vertrag", "nicht mehr verlängern wird."];

    public static string[] RetirementText(Player p) =>
        [$"{p.Name} gibt das Eishockeyspielen im Alter von", $"{p.Age} Jahren endgültig auf."];
}

public sealed partial class GameState
{
    /// <summary>
    /// Spätester Tag für Vertragsankündigungen: 30. März (späteste im Video). Wer sich bis dahin nicht gemeldet hat,
    /// meldet sich an diesem Tag; auch danach ins letzte Jahr gerutschte Spieler melden sich sofort.
    /// </summary>
    public const int ContractNoticeDeadline = 201;

    private GameRandom? _contractRandom;

    /// <summary>Spieler, die ihre Laufbahn beendet haben, in der Reihenfolge ihres Abschieds.</summary>
    public List<Player> RetiredPlayers { get; } = [];

    /// <summary>
    /// Eigene Zufallsquelle des Kaderlebenszyklus, damit Spielergebnisse, Markt und Finanzen ihre Folgen behalten.
    /// </summary>
    public GameRandom ContractRandom
    {
        get => _contractRandom ??= new GameRandom(unchecked(Season * 104_729 + Managers.Sum(m => m.ClubIndex * 37 + m.Head) + 5));
        init => _contractRandom = value;
    }

    /// <summary>
    /// Täglich (Zeitleiste): Spieler der Manager im letzten Vertragsjahr kündigen an, ob sie verlängern wollen.
    /// Jeder Spieler genau einmal je Saison, spätestens am <see cref="ContractNoticeDeadline"/>.
    /// </summary>
    internal void AnnounceContractEnds(int day)
    {
        AdvanceNegotiationLocks(); // Spur transfer-original-vertrag: Sperre +0x18 (Tagesablauf 0x76F6)
        foreach (var manager in ActiveManagers)
        {
            var club = Clubs[manager.ClubIndex];
            foreach (var p in club.Players.ToArray())
            {
                if (p.ContractYears != 1 || p.OnLoan || p.Notice != ContractNotice.None)
                    continue;
                if (day < ContractNoticeDeadline && ContractRandom.NextDouble() >= SquadRules.NoticeChancePerDay)
                    continue;
                Announce(club, p);
            }
        }
    }

    private void Announce(Club club, Player p)
    {
        var roll = ContractRandom.NextDouble();
        if (roll < SquadRules.RetirementChance(p.Age + 1))
            p.Notice = ContractNotice.Retiring;
        else if (ContractRandom.NextDouble() < SquadRules.RefuseChance)
            p.Notice = ContractNotice.Refused;
        else
        {
            p.Notice = ContractNotice.Offered;
            p.NoticeYears = SquadRules.OfferedYears(ContractRandom.NextDouble());
        }
        var text = p.Notice == ContractNotice.Offered ? SquadRules.OfferText(p, p.NoticeYears) : SquadRules.RefusalText(p);
        foreach (var manager in Managers.Where(m => m.ClubIndex == club.Index && !m.Dismissed))
            Notify(manager, text);
    }

    /// <summary>Laufbahnende: Spieler verlässt Kader bzw. Pool endgültig; Manager des Vereins bekommen eine Nachricht.</summary>
    private void Retire(Club? club, Player p)
    {
        if (club is not null)
        {
            club.Players.Remove(p);
            club.Lineup.Remove(p);
            foreach (var manager in Managers.Where(m => m.ClubIndex == club.Index && !m.Dismissed))
                Notify(manager, SquadRules.RetirementText(p));
        }
        p.ContractYears = 0;
        p.MonthlySalary = 0;
        p.Insured = false;
        p.OnLoan = false;
        p.Loan = null;
        RetiredPlayers.Add(p);
    }

    /// <summary>Saisonwechsel, Computerverein: Ruhestand nach Alter, sonst auslaufende Verträge verlängern oder freigeben.</summary>
    private void ComputerContractDecision(Club owner, Club current, Player p)
    {
        if (ContractRandom.NextDouble() < SquadRules.RetirementChance(p.Age))
        {
            current.Players.Remove(p);
            current.Lineup.Remove(p);
            Retire(owner, p);
            return;
        }
        if (p.ContractYears > 0)
            return;
        if (ContractRandom.NextDouble() < SquadRules.ComputerExtendChance)
        {
            p.ContractYears = SquadSetup.Between(ContractRandom, SquadRules.ComputerMinYears, SquadRules.ComputerMaxYears);
            p.MonthlySalary = Contracts.Demand(p);
        }
    }

    /// <summary>
    /// Saisonwechsel: Computervereine füllen ihre Kader je Position wieder auf (mindestens wie zum Spielstart, höchstens
    /// bis zur Größe vor dem Wechsel bzw. <see cref="SquadRules.MaxComputerSquad"/>), aus dem Pool oder mit Talenten.
    /// </summary>
    private void RefillComputerSquads(IReadOnlyDictionary<Club, Dictionary<PlayerRole, int>> before)
    {
        var order = before.Keys.OrderBy(c => c.Index).ToList();
        for (var i = order.Count - 1; i > 0; i--)
        {
            var j = ContractRandom.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        var names = Clubs.SelectMany(c => c.Players).Concat(TransferMarket.FreeAgents).ToList();

        foreach (var club in order)
        {
            var roles = Enum.GetValues<PlayerRole>();
            // Zuerst die Mindestbesetzung, dann bis zur bisherigen Stärke je Position.
            foreach (var role in roles)
                while (club.Players.Count(p => p.Role == role) < SquadRules.MinimumOf(role))
                    Sign(club, role);
            foreach (var role in roles)
                while (club.Players.Count(p => p.Role == role) < before[club][role]
                       && club.Players.Count < SquadRules.MaxComputerSquad)
                    Sign(club, role);
        }

        void Sign(Club club, PlayerRole role)
        {
            var level = SquadSetup.BaseValue(club.Strength);
            var candidates = TransferMarket.FreeAgents
                .Where(p => p.Role == role && p.Age <= SquadRules.FreeAgentMaxAge)
                .OrderBy(p => Math.Abs(p.Strength - level)).ThenBy(p => p.Name, StringComparer.Ordinal)
                .Take(3).ToList();
            var years = SquadSetup.Between(ContractRandom, SquadRules.ComputerMinYears, SquadRules.ComputerMaxYears);
            if (candidates.Count > 0 && ContractRandom.NextDouble() < SquadRules.FreeAgentShare)
            {
                var p = candidates[ContractRandom.Next(candidates.Count)];
                TransferMarket.Sign(p);
                PlayerFactory.Join(club, p, years, loan: false);
                return;
            }
            var talent = PlayerFactory.CreateTalent(this, PlayerFactory.TalentName(names, ContractRandom), role, club.Strength, ContractRandom);
            PlayerFactory.Join(club, talent, Math.Max(2, years), loan: false);
            names.Add(talent);
        }
    }

    /// <summary>Saisonwechsel: Pool altert, Alte hören auf; danach höchstens <see cref="TransferMarket.MaxFreeAgents"/>.</summary>
    private void AgeFreeAgents()
    {
        foreach (var p in TransferMarket.FreeAgents.ToArray())
        {
            p.Age++;
            p.NegotiationLock = 0;
            p.Notice = ContractNotice.None;
            if (ContractRandom.NextDouble() < SquadRules.RetirementChance(p.Age))
            {
                TransferMarket.Sign(p);
                Retire(null, p);
            }
        }
    }

    private void LimitFreeAgents()
    {
        // [APPROX] Überzählige beenden die Laufbahn: die ältesten, bei Gleichstand die schwächsten.
        var surplus = TransferMarket.FreeAgents
            .OrderByDescending(p => p.Age).ThenBy(p => p.Strength).ThenBy(p => p.Name, StringComparer.Ordinal)
            .Take(Math.Max(0, TransferMarket.FreeAgents.Count() - TransferMarket.MaxFreeAgents)).ToList();
        foreach (var p in surplus)
        {
            TransferMarket.Sign(p);
            Retire(null, p);
        }
    }
}
