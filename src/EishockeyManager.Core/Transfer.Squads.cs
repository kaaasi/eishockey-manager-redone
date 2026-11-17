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
/// <see cref="RetirementRoll"/>, <see cref="OfferLimit"/>, <see cref="OfferedYears"/>). Laufbahnende der Computerkader
/// wie das der Poolplätze (<see cref="CareerEnds"/>, A232/A238). [APPROX] Verlängerungschance, Laufzeiten und Nachbesetzung der
/// Kader-KI: Das Original verwaltet Computervereine nur über Teamwerte (siehe <see cref="SquadSetup"/>), die Kader-KI ist eine
/// Ergänzung des Remakes.
/// </remarks>
public static class SquadRules
{
    /// <summary>
    /// Grenzen des Laufbahnendes außerhalb der Managerkader: Alter &gt; random(<see cref="CareerEndMin"/>, <see cref="CareerEndMax"/>).
    /// [ORIGINAL] Saisonende Datei 0x2788F–0x278A1 (A232/A238): frühestens mit 38, sicher ab 43. Gilt im Remake auch für die
    /// Computerkader (<see cref="CareerEnds"/>, Saisonwechsel); die frühere Kader-KI-Regel 33–37 Jahre ist entfallen.
    /// </summary>
    public const int CareerEndMin = 37, CareerEndMax = 42;

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

    /// <summary>
    /// Laufbahnende ohne Managerkader im Saisonende. [ORIGINAL] Datei 0x2787A–0x279DA (Saisonende 0x26F20, Pool-Schleife 1–199):
    /// Alter &gt; random(37, 42) (<c>docs/analyse/re-nachtrag.md</c>, Abschnitt 1). Gilt für Computerspieler und Vertragslose;
    /// der Managerkader folgt der Ruhestandsmeldung (<see cref="RetirementRoll"/>).
    /// </summary>
    public static bool CareerEnds(int age, Random random) => age > TransferRules.Random16(random, CareerEndMin, CareerEndMax);

    /// <summary>
    /// Wurf je Takt „kündigt an, daß er seinen Vertrag nicht mehr verlängern wird“ (Kaderplatz +0x18 Bit 0x80). [ORIGINAL]
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
    /// p − p · e / 10. Spiele beim Verein je Wettbewerb seit A135 (<see cref="Player.ClubGames"/>).
    /// </summary>
    public static int OfferLimit(Player p)
    {
        var s = (p.Condition + p.Technique + p.Form) / 3;
        var a = 100 - 100 * Math.Min(8, Math.Abs(25 - p.Age)) / 8;
        var e = Math.Min(5, (p.ClubGames(CompetitionSlot.League) + p.ClubGames(CompetitionSlot.Cup)
                             + p.ClubGames(CompetitionSlot.Europe)) / 36);
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
    /// Eigene Zufallsquelle der Computerkader zum Saisonwechsel (Laufbahnende, Verlängern, Auffüllen, Vertragslose). Spur
    /// zufallsstrom, Teil 4 (N3 = B): Die frühere Quelle <c>ContractRandom</c> ist für alle Stellen mit Gegenstück im Original
    /// entfernt – Verhandlung (Datei 0x49A3B, 0x4AD3E), unaufgefordertes Angebot (Datei 0x46E0E) und Ablöse am Saisonende (Datei
    /// 0x27DE3) ziehen aus <see cref="Random"/>. [APPROX] Die Kader der Computervereine haben im Original kein Gegenstück (dort
    /// gibt es nur Teamwerte und den Spielerpool); ihre Würfe bleiben deshalb außerhalb des gemeinsamen Stroms, damit er nur Würfe
    /// des Originals trägt (Leitstelle 17.11.2026, wie <see cref="SquadSetup.ComputerSquadSource"/>). Das Feld behält seinen Namen
    /// (kein Wechsel im Spielstand).
    /// </summary>
    internal GameRandom ComputerSquadRandom
    {
        get => _contractRandom ??= new GameRandom(unchecked(Season * 104_729 + Managers.Sum(m => m.ClubIndex * 37 + m.Head) + 5));
        init => _contractRandom = value;
    }

    /// <summary>
    /// Am Takttag (Zeitleiste, #678; R688-09) je Spieler des Managers. [ORIGINAL] Tagesablauf 0x07169 bis zum Saisonende (Zähler &lt; 233):
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
    /// Ein offenes Angebot verfällt mit random(0, 5) = 0 je Lauf von 0x07169 (Datei 0x0767A): Meldung gelöscht, Verhandlungssperre
    /// random(9, 17) Takte (Spur kader-transfer, Runde 12, U4; seit #678 zählt sie je Takt, R688-09).
    /// [APPROX] „Offene Meldung“ (Pool +0x1E) = schon gemeldet (<see cref="Player.Notice"/> ≠ None).
    /// [ORIGINAL] Spur zufallsstrom (A720, A248, A726; N3 = B): alle Würfe aus dem gemeinsamen Strom <see cref="Random"/> in der
    /// Reihenfolge von 0x07169 – erst die Schleife über die Kaderplätze (Datei 0x07402–0x0795D), je Platz Ruhestand random(350, 450)
    /// (0x075CA), Verfall des unaufgeforderten Angebots random(0, 3) (0x0762D, der Wurf fällt für jeden Platz), Verfall des
    /// Verlängerungsangebots random(0, 5) und Sperre random(9, 17) (0x07692, 0x076B9), Verlängerungsangebot (0x0781E, 0x07833,
    /// 0x07876); danach die zweite Schleife über dieselben Plätze mit dem Angebotswurf (Datei 0x07996–0x07AEF,
    /// <see cref="RollUnsolicitedOffers"/>). [APPROX] Die Verletzung außerhalb des Spiels (Datei 0x07462–0x07548, random bei 0x0748C
    /// vor dem Ruhestand) rechnet das Remake nicht; ihr Wurf fehlt im Strom.
    /// #678: [ORIGINAL] 0x07169 rechnet die Kaderplätze des Managers DGROUP 0xA70E (Satz 0x5DC3 + Manager · 0x618, Datei 0x07402);
    /// mit <paramref name="only"/> nur dessen Plätze (Hauptweg je Manager, WM-Schleife Schritt (c) nur Manager 0).
    /// </summary>
    internal void AnnounceContractEnds(int day, Manager? only = null)
    {
        AdvanceNegotiationLocks(only); // Spur transfer-original-vertrag: Sperre +0x18 (Tagesablauf 0x76F6)
        if (AfterSeasonEnd)
            return;
        foreach (var manager in ActiveManagers.Where(m => only is null || m == only))
        {
            var club = Clubs[manager.ClubIndex];
            foreach (var p in club.Players.ToArray())
            {
                RollInjuryOutsideMatch(day, manager, club, p); // A864: Datei 0x07462–0x07548, vor dem Ruhestandswurf
                if (SquadRules.RetirementRoll(p, Random) && p.Notice is not (ContractNotice.Retiring or ContractNotice.Offered))
                {
                    p.Notice = ContractNotice.Retiring;
                    p.NoticeYears = 0;
                    NotifyClub(club, SquadRules.RefusalText(p), p);
                }
                ExpireUnsolicitedOffer(p); // [ORIGINAL] Datei 0x07626–0x07673, zwischen Ruhestand und Verlängerungsangebot
                // [ORIGINAL] Verfall eines Verlängerungsangebots (Datei 0x0767A–0x076E3): random(0, 5) = 0 → Sperre random(9, 17), Meldung weg.
                if (p.Notice == ContractNotice.Offered && TransferRules.Random16(Random, 0, 5) == 0)
                {
                    p.Notice = ContractNotice.None;
                    p.NoticeYears = 0;
                    RemoveMessagesOf(p); // [ORIGINAL] Datei 0x0767A–0x076E3 → 0x4B270
                    p.NegotiationLock = TransferRules.Random16(Random, 9, 17);
                }
                if (p.OnLoan)
                    continue;
                if (TransferRules.Random16(Random, 0, SquadRules.OfferLimit(p)) != 0
                    || TransferRules.Random16(Random, 0, 3) != 0)
                    continue;
                if (p.Notice != ContractNotice.None || p.ContractYears != 1)
                    continue;
                p.Notice = ContractNotice.Offered;
                p.NegotiationLock = 0; // [ORIGINAL] +0x18 = 100 + n ersetzt eine laufende Sperre (Datei 0x07951)
                p.NoticeYears = SquadRules.OfferedYears(TransferRules.Random16(Random, 0, 100));
                NotifyClub(club, SquadRules.OfferText(p, p.NoticeYears), p);
            }
            RollUnsolicitedOffers(manager); // Spur transfer-vertraege-komplett, Teil 3: +0x09 Bit 0x40 (zweite Schleife, Datei 0x07996)
        }
    }

    /// <summary>
    /// A864: Verletzung außerhalb des Spiels, je Kaderplatz im Lauf von 0x07169 vor dem Ruhestandswurf. [ORIGINAL] Datei
    /// 0x07462–0x07548: Der Platz würfelt nur mit +0x0D = 0 (nicht verletzt, 0xFF zählt als verletzt) und +0x09 = 0 (kein
    /// unaufgefordertes Angebot Bit 0x40); x = 0x140 − 2 · Belastung (+0x13) + 0x32 an Tagen ohne Ereignis (DGROUP 0x828 =
    /// 0), mindestens 0x28 (0x7437–0x745D); Wurf random(0, 30 · (5 − Level) + x + 100) = 0 (0x7479–0x7495, DGROUP 0xED8 =
    /// 5 − Level). Treffer: Verletzung 0x05B1A mit Schalter 0, also alle 28 Arten (<see cref="PlayerInjuries.Inflict"/>, 0x74A9),
    /// Meldung „Verletzung außerhalb des Spieles:“ (DGROUP 0xD30) / Name, Leerzeichen (0x490F) und Art (0x74BE–0x7522), bei
    /// Versicherung (+0x30/+0x32 ≠ 0) die Leistung 0x07089 (0x752D–0x7542), dann Neuaufstellung 0x426BC (0x7548).
    /// [APPROX] Der Text der Art in Zeile 2 ist der Name der Art (Ausgabe von 0x05B1A in [bp−0xDA] nicht gelesen); das Remake
    /// führt im Modus „nur Weltkugel“ keine Vereinssaison, dort läuft der Tageslauf nicht.
    /// </summary>
    private void RollInjuryOutsideMatch(int day, Manager manager, Club club, Player p)
    {
        if (p.InjuredGames != 0 || p.UnsolicitedOffer is not null)
            return;
        var x = Math.Max(0x28, 0x140 - 2 * p.Strain + (SeasonCalendar.Events(day) == DayEvents.None ? 0x32 : 0));
        var difficulty = TrainingRules.LevelValue(manager.Level);
        if (TransferRules.Random16(Random, 0, 30 * difficulty + x + 100) != 0)
            return;
        var kind = PlayerInjuries.Inflict(p, inMatch: false, difficulty, Random);
        Notify(manager, "Verletzung außerhalb des Spieles:", $"{p.Name} {InjuryKinds.Name(kind)}");
        PayInsurance(manager, p, careerEnd: false);
        PlayerInjuries.RefillLineup(club);
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
        if (SquadRules.CareerEnds(p.Age, ComputerSquadRandom))
        {
            current.RemovePlayer(p);
            Retire(owner, p);
            // [ORIGINAL] Neuanlage im selben Pooleintrag (Datei 0x279DA): ein junger Spieler übernimmt den Platz im Verein.
            if (owner.Players.Count < PlayerFactory.MaxNumber)
            {
                var names = Clubs.SelectMany(c => c.Players).Concat(TransferMarket.FreeAgents).ToList();
                var successor = PlayerFactory.CreateRenewal(this, p, PlayerFactory.TalentName(names, ComputerSquadRandom),
                    ComputerSquadRandom);
                PlayerFactory.Join(owner, successor, SquadSetup.Between(ComputerSquadRandom, SquadRules.ComputerMinYears,
                    SquadRules.ComputerMaxYears), loan: false, managerSquad: false);
            }
            return;
        }
        if (p.ContractYears > 0)
            return;
        if (ComputerSquadRandom.NextDouble() < SquadRules.ComputerExtendChance)
        {
            p.ContractYears = SquadSetup.Between(ComputerSquadRandom, SquadRules.ComputerMinYears, SquadRules.ComputerMaxYears);
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
            var j = ComputerSquadRandom.Next(i + 1);
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
            var years = SquadSetup.Between(ComputerSquadRandom, SquadRules.ComputerMinYears, SquadRules.ComputerMaxYears);
            if (candidates.Count > 0 && ComputerSquadRandom.NextDouble() < SquadRules.FreeAgentShare)
            {
                var p = candidates[ComputerSquadRandom.Next(candidates.Count)];
                TransferMarket.Sign(p);
                PlayerFactory.Join(club, p, years, loan: false, managerSquad: false);
                return;
            }
            var talent = PlayerFactory.CreateTalent(this, PlayerFactory.TalentName(names, ComputerSquadRandom), role, club.Strength, ComputerSquadRandom);
            PlayerFactory.Join(club, talent, Math.Max(2, years), loan: false, managerSquad: false);
            names.Add(talent);
        }
    }

    /// <summary>Saisonwechsel: Pool altert, Alte hören auf; danach höchstens <see cref="TransferMarket.MaxFreeAgents"/>.</summary>
    private void AgeFreeAgents()
    {
        TransferMarket.AgeLostPlayers(this); // A391: Plätze früher verlorener Spieler vor dem Kaderlauf
        foreach (var p in TransferMarket.FreeAgents.ToArray())
        {
            p.Age++;
            p.NegotiationLock = 0;
            p.Notice = ContractNotice.None;
            if (SquadRules.CareerEnds(p.Age, ComputerSquadRandom))
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
    /// (<see cref="AgeFreeAgents"/>) oder beim Bundestrainer überspringt das Remake. [ORIGINAL] Spur zufallsstrom (A721, N3 = B):
    /// Wurf random(37, 42) (Datei 0x27897) und Neuanlage (Alter 0x2794F, s 0x27972, Grundstärken 0x27985/0x279BF) aus dem
    /// gemeinsamen Strom <see cref="Random"/>, je Platz in Poolfolge nach dem Altern. A631 (Spur kader-regeln): [ORIGINAL] Name,
    /// Talent und Form bleiben die des Platzes, kein weiterer Wurf (Datei 0x27947–0x279D6, <see cref="PlayerFactory.CreateRenewal"/>).
    /// </summary>
    internal void RenewAgedPoolPlayer(PoolSlot slot)
    {
        var old = slot.Player;
        if (slot.Owner != PoolOwner.Free || !SquadRules.CareerEnds(old.Age, Random)
            || Clubs.Any(c => c.Players.Contains(old)) || TransferMarket.FreeAgents.Contains(old)
            || NationalCoach?.Contains(old) == true)
            return;
        var successor = PlayerFactory.CreateRenewal(this, old, old.Name, Random);
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
