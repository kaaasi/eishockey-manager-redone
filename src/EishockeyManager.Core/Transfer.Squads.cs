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
/// Ruhestand und Verlängerungsangebote der Managerkader nach dem Tagesablauf 0x07169 (Spur kader-transfer, Teil 7:
/// <see cref="RetirementRoll"/>, <see cref="OfferLimit"/>, <see cref="OfferedYears"/>). [APPROX] Wahrscheinlichkeiten und
/// Altersgrenzen der Kader-KI: Das Original verwaltet Computervereine nur über Teamwerte (siehe <see cref="SquadSetup"/>), die
/// Kader-KI ist eine Ergänzung des Remakes.
/// </remarks>
public static class SquadRules
{
    /// <summary>
    /// Ab diesem Alter kann ein Spieler eines Computervereins aufhören. [APPROX] Nur Kader-KI der Computervereine (im Original ohne
    /// Kader; Laufbahnende dort im Saisonende 0x2787A mit Alter &gt; random(37, 42), noch nicht umgesetzt). Managerkader:
    /// <see cref="RetirementRoll"/>.
    /// </summary>
    public const int RetireMinAge = 33;

    /// <summary>Ab diesem Alter hört jeder Spieler eines Computervereins auf. [APPROX] wie <see cref="RetireMinAge"/>.</summary>
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

    /// <summary>Chance eines Spielers eines Computervereins, nach Erreichen dieses Alters die Laufbahn zu beenden: 33 → 20 %, …
    /// 37 → 100 %. [APPROX] Kader-KI (siehe <see cref="RetireMinAge"/>).</summary>
    public static double RetirementChance(int age) =>
        age < RetireMinAge ? 0 : Math.Min(1.0, (age - RetireMinAge + 1) / (double)(RetireMaxAge - RetireMinAge + 1));

    /// <summary>
    /// Laufbahnende ohne Managerkader im Saisonende. [ORIGINAL] Datei 0x2787A–0x279DA (Saisonende 0x26F20, Pool-Schleife 1–199):
    /// Alter &gt; random(37, 42) (<c>docs/analyse/re-nachtrag.md</c>, Abschnitt 1). Gilt für Computerspieler und Vertragslose;
    /// der Managerkader folgt der Ruhestandsmeldung (<see cref="RetirementRoll"/>).
    /// </summary>
    public static bool CareerEnds(int age, Random random) => age > TransferRules.Random16(random, 37, 42);

    /// <summary>
    /// Täglicher Wurf „kündigt an, daß er seinen Vertrag nicht mehr verlängern wird“ (Kaderplatz +0x18 Bit 0x80). [ORIGINAL]
    /// Tagesablauf 0x07169, Datei 0x0754D–0x0760F, je Platz des Managers am Zug und Tag: c = 100 − 15 bei einem Torhüter
    /// (Rollenwert Pool +0x18 = 0); Technik − Kondition &gt; 0 → c − (Technik − Kondition) / 2; v = Alter · c / 10; wahr bei
    /// random(350, 450) &lt; v. Feldspieler unter 36 nie, Torhüter unter 42 nie (<c>docs/analyse/re-nachtrag.md</c>, Abschnitt 1).
    /// Der Wurf fällt immer; die Bedingungen (kein Bit 0x80, kein offenes Verlängerungsangebot +0x18 ≥ 100) prüft der Aufrufer.
    /// </summary>
    public static bool RetirementRoll(Player p, Random random)
    {
        var c = p.Role == PlayerRole.Goalkeeper ? 85 : 100;
        if (p.Technique - p.Condition > 0)
            c -= (p.Technique - p.Condition) / 2;
        var v = p.Age * c / 10;
        return TransferRules.Random16(random, 350, 450) < v;
    }

    /// <summary>
    /// Grenze p des Verlängerungswurfs random(0, p) = 0. [ORIGINAL] Datei 0x076FA–0x07951: s = (Kondition + Technik + Form) / 3,
    /// a = 100 − 100 · min(8, |25 − Alter|) / 8, e = min(5, (+0x1C + +0x1E + +0x20) / 36), p = (60 · s + 40 · a) / 100,
    /// p − p · e / 10. [APPROX] +0x1E/+0x20 führt das Remake nicht (<see cref="Player.GamesForClub"/> = +0x1C).
    /// </summary>
    public static int OfferLimit(Player p)
    {
        var s = (p.Condition + p.Technique + p.Form) / 3;
        var a = 100 - 100 * Math.Min(8, Math.Abs(25 - p.Age)) / 8;
        var e = Math.Min(5, p.GamesForClub / 36);
        var limit = (60 * s + 40 * a) / 100;
        return limit - limit * e / 10;
    }

    /// <summary>
    /// Angebotene Gesamtlaufzeit „von 1 auf n Jahre“. [ORIGINAL] Datei 0x07865 ff.: r = random(0, 100); r &gt; 90 → 4, r &gt; 70 → 3,
    /// sonst 2 (71 / 20 / 10 von 101).
    /// </summary>
    public static int OfferedYears(int roll) => roll > 90 ? 4 : roll > 70 ? 3 : 2;

    public static string[] OfferText(Player p, int years) =>
        [$"{p.Name} bietet Ihnen an, von 1 auf {years}", "Jahre zu verlängern."];

    /// <summary>Meldung Art 4 (DGROUP 0xCA0 + 16): die Ankündigung des Ruhestands (+0x18 Bit 0x80, <see cref="RetirementRoll"/>).</summary>
    public static string[] RefusalText(Player p) =>
        [$"{p.Name} kündigt an, daß er seinen Vertrag", "nicht mehr verlängern wird."];

    public static string[] RetirementText(Player p) =>
        [$"{p.Name} gibt das Eishockeyspielen im Alter von", $"{p.Age} Jahren endgültig auf."];
}

public sealed partial class GameState
{
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
    /// Täglich (Zeitleiste) je Spieler der Manager. [ORIGINAL] Tagesablauf 0x07169 bis zum Saisonende (Zähler &lt; 233):
    /// <list type="number">
    /// <item>Ruhestand (Datei 0x0754D): <see cref="SquadRules.RetirementRoll"/> für jeden Platz, auch Leihspieler und unabhängig von
    ///   der Laufzeit; ohne Ruhestand-Bit und ohne offenes Verlängerungsangebot → <see cref="ContractNotice.Retiring"/>, Meldung
    ///   Art 4 „… kündigt an, daß er seinen Vertrag nicht mehr verlängern wird.“ Der Spieler hört bei Vertragsende auf.</item>
    /// <item>Verlängerungsangebot (Datei 0x076FA–0x07951), nur ohne Leihe: random(0, <see cref="SquadRules.OfferLimit"/>) = 0 und
    ///   random(0, 3) = 0 (der zweite Wurf nur nach dem ersten), keine offene Meldung, kein Ruhestand-Bit, Restlaufzeit +0x0B = 1 →
    ///   „von 1 auf n“ (<see cref="SquadRules.OfferedYears"/>). Keine Frist: Wer sich nicht meldet, kommt in die Verhandlung am
    ///   Saisonende (0x27D05).</item>
    /// </list>
    /// Eine eigene Absage gibt es im Original nicht (A230): „nicht mehr verlängern“ ist die Ruhestandsmeldung.
    /// Ein offenes Angebot verfällt mit random(0, 5) = 0 je Tag (Datei 0x0767A): Meldung gelöscht, Verhandlungssperre
    /// random(9, 17) Tage (Spur kader-transfer, Runde 12, U4).
    /// [APPROX] „Offene Meldung“ (Pool +0x1E) = schon gemeldet (<see cref="Player.Notice"/> ≠ None); Züge aus
    /// <see cref="ContractRandom"/>; Reihenfolge im Tag nach Sperren und Angeboten Bit 0x40.
    /// </summary>
    internal void AnnounceContractEnds(int day)
    {
        AdvanceNegotiationLocks(); // Spur transfer-original-vertrag: Sperre +0x18 (Tagesablauf 0x76F6)
        RollUnsolicitedOffers(); // Spur transfer-vertraege-komplett, Teil 3: +0x09 Bit 0x40 (Tagesablauf 0x07972)
        if (AfterSeasonEnd)
            return;
        foreach (var manager in ActiveManagers)
        {
            var club = Clubs[manager.ClubIndex];
            foreach (var p in club.Players.ToArray())
            {
                if (SquadRules.RetirementRoll(p, ContractRandom) && p.Notice is not (ContractNotice.Retiring or ContractNotice.Offered))
                {
                    p.Notice = ContractNotice.Retiring;
                    p.NoticeYears = 0;
                    NotifyClub(club, SquadRules.RefusalText(p), p);
                }
                // [ORIGINAL] Verfall eines Verlängerungsangebots (Datei 0x0767A–0x076E3): random(0, 5) = 0 → Sperre random(9, 17), Meldung weg.
                if (p.Notice == ContractNotice.Offered && TransferRules.Random16(ContractRandom, 0, 5) == 0)
                {
                    p.Notice = ContractNotice.None;
                    p.NoticeYears = 0;
                    RemoveMessagesOf(p); // [ORIGINAL] Datei 0x0767A–0x076E3 → 0x4B270
                    p.NegotiationLock = TransferRules.Random16(ContractRandom, 9, 17);
                }
                if (p.OnLoan)
                    continue;
                if (TransferRules.Random16(ContractRandom, 0, SquadRules.OfferLimit(p)) != 0
                    || TransferRules.Random16(ContractRandom, 0, 3) != 0)
                    continue;
                if (p.Notice != ContractNotice.None || p.ContractYears != 1)
                    continue;
                p.Notice = ContractNotice.Offered;
                p.NegotiationLock = 0; // [ORIGINAL] +0x18 = 100 + n ersetzt eine laufende Sperre (Datei 0x07951)
                p.NoticeYears = SquadRules.OfferedYears(TransferRules.Random16(ContractRandom, 0, 100));
                NotifyClub(club, SquadRules.OfferText(p, p.NoticeYears), p);
            }
        }
    }

    private void NotifyClub(Club club, string[] text, Player? about = null)
    {
        foreach (var manager in Managers.Where(m => m.ClubIndex == club.Index && !m.Dismissed))
            if (about is not null && about.Notice == ContractNotice.Offered)
                NotifyAbout(manager, about, text); // offene Meldung des Spielers (Pool +0x1E)
            else
                Notify(manager, text);
    }

    /// <summary>Laufbahnende: Spieler verlässt Kader bzw. Pool endgültig; Manager des Vereins bekommen eine Nachricht.</summary>
    private void Retire(Club? club, Player p)
    {
        if (club is not null)
        {
            club.RemovePlayer(p);
            RemoveMessagesOf(p);
            foreach (var manager in Managers.Where(m => m.ClubIndex == club.Index && !m.Dismissed))
                Notify(manager, SquadRules.RetirementText(p));
        }
        p.ContractYears = 0;
        p.MonthlySalary = 0;
        p.Insured = false;
        p.InsuranceLocked = false;
        p.OnLoan = false;
        p.Loan = null;
        RetiredPlayers.Add(p);
    }

    /// <summary>Saisonwechsel, Computerverein: Laufbahnende bei Alter &gt; random(37, 42) mit Nachfolger (U5), sonst auslaufende Verträge verlängern oder freigeben.</summary>
    private void ComputerContractDecision(Club owner, Club current, Player p)
    {
        if (SquadRules.CareerEnds(p.Age, ContractRandom))
        {
            current.RemovePlayer(p);
            Retire(owner, p);
            // [ORIGINAL] Neuanlage im selben Pooleintrag (Datei 0x279DA): ein junger Spieler übernimmt den Platz im Verein.
            if (owner.Players.Count < PlayerFactory.MaxNumber)
            {
                var names = Clubs.SelectMany(c => c.Players).Concat(TransferMarket.FreeAgents).ToList();
                var successor = PlayerFactory.CreateRenewal(this, PlayerFactory.TalentName(names, ContractRandom), p.Role,
                    ContractRandom);
                PlayerFactory.Join(owner, successor, SquadSetup.Between(ContractRandom, SquadRules.ComputerMinYears,
                    SquadRules.ComputerMaxYears), loan: false);
            }
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
            if (SquadRules.CareerEnds(p.Age, ContractRandom))
            {
                TransferMarket.Sign(p);
                Retire(null, p);
            }
        }
    }

    /// <summary>
    /// Saisonwechsel, Poolplatz ohne Kaderplatz eines Managers: Laufbahnende bei Alter &gt; random(37, 42) und Neuanlage im selben
    /// Platz (<see cref="PlayerFactory.CreateRenewal"/>; Verein und Platz bleiben). [ORIGINAL] Datei 0x2787A–0x279DA. Gerufen aus
    /// <see cref="TransferMarket.StartPoolSeason"/> nach dem Altern des Platzes. Spieler in einem Kader, bei den Vertragslosen
    /// (<see cref="AgeFreeAgents"/>) oder beim Bundestrainer überspringt das Remake. [APPROX] Zufall der Verträge
    /// (<see cref="ContractRandom"/>) statt der Hauptquelle.
    /// </summary>
    internal void RenewAgedPoolPlayer(PoolSlot slot)
    {
        var old = slot.Player;
        if (slot.Owner != PoolOwner.Free || !SquadRules.CareerEnds(old.Age, ContractRandom)
            || Clubs.Any(c => c.Players.Contains(old)) || TransferMarket.FreeAgents.Contains(old)
            || NationalCoach?.Contains(old) == true)
            return;
        var names = Clubs.SelectMany(c => c.Players).Concat(TransferMarket.FreeAgents).ToList();
        var successor = PlayerFactory.CreateRenewal(this, PlayerFactory.TalentName(names, ContractRandom), old.Role, ContractRandom);
        Retire(null, old);
        slot.Player = successor;
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
