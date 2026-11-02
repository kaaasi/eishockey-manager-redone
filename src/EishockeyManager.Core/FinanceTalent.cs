using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Laufende Talentsuche des „Spions“.</summary>
public sealed class TalentSearch
{
    /// <summary>
    /// Restzähler (1 … 16). [ORIGINAL] Managersatz +0xEC (DGROUP 0x9747 + 0x1AF · Manager); 0 = keine Suche (im Remake
    /// <c>null</c> statt eines Objekts mit 0).
    /// </summary>
    public required int TicksLeft { get; set; }
}

public sealed partial class ManagerFinance
{
    /// <summary>Laufende Talentsuche oder null.</summary>
    public TalentSearch? TalentSearch { get; set; }

    /// <summary>„Preis:“ der Talentsuche in DM. [ORIGINAL] DGROUP 0x20D + 4 · Manager (siehe <see cref="TalentScout.StartPrice"/>).</summary>
    public long TalentPrice { get; set; } = TalentScout.StartPrice;
}

/// <summary>
/// Talentsuche nach em.exe (Spur geld, Teil 6; R-K1): Start in der Transferliste (Datei 0x465F1), Preis im Tagesabschluss
/// (Datei 0x3ED01), Zähler und Ergebnis zweimal je Woche (Datei 0x06FA5), Talent aus dem Spielerpool (Datei 0x06D7F),
/// Grenze 28 eigene und verliehene Spieler (Datei 0x3CCEB).
/// </summary>
public sealed partial class GameState
{
    /// <summary>„Preis:“ der Talentsuche (ohne Rabatt; „Rabatt:“ gehört zur Stadion-Rückfrage, Datei 0x1B187).</summary>
    public long TalentSearchPrice(Manager manager) => Finance(manager).TalentPrice;

    /// <summary>
    /// Schickt den „Spion“ los (Knopf „Talent“, „Aber immer!“) und bezahlt sofort (Finanzbuch „Talentsuche“).
    /// [ORIGINAL] Datei 0x4697D–0x46A10: nur wenn das Konto <b>größer</b> als der Preis ist (Vergleich 0x469A8–0x469B7), sonst
    /// „Sie haben nicht / genug Geld !“; Zähler = random(3, 10), mit random(0, 3) = 0 zusätzlich + 6. Eine Prüfung des Kaders
    /// gibt es beim Start nicht (A246; die Grenze 28 greift erst beim Ergebnis).
    /// [APPROX] Das Original zieht den Preis direkt vom Konto ab (Datei 0x469CA); das Remake bucht ihn als Ausgabe
    /// („Talentsuche“), weil echte Ausgaben nur über <see cref="Book"/> laufen (AGENTS.md).
    /// </summary>
    public TalentSearchStart StartTalentSearch(Manager manager)
    {
        var finance = Finance(manager);
        if (finance.TalentSearch is not null)
            return TalentSearchStart.AlreadyRunning;
        var price = TalentSearchPrice(manager);
        if (manager.Money <= price)
            return TalentSearchStart.NotEnoughMoney;
        Book(manager, FinanceCategory.Talentsuche, -price);
        var ticks = TransferRules.Random16(FinanceRandom, TalentScout.MinTicks, TalentScout.MaxTicks);
        if (TransferRules.Random16(FinanceRandom, 0, 3) == 0)
            ticks += TalentScout.ExtraTicks;
        finance.TalentSearch = new TalentSearch { TicksLeft = ticks };
        return TalentSearchStart.Started;
    }

    /// <summary>
    /// Neuer Preis der Talentsuche im Tagesabschluss an den Angebotstagen (<see cref="SponsorRules.IsOfferDay"/>), direkt nach
    /// den Werbeangeboten des Managers. [ORIGINAL] Datei 0x3EEAB–0x3EF4E: mit random(0, 1) ≠ 0 Preis = 60.000 + 10.000 ·
    /// random(0, 10) DM (<see cref="TalentScout.PriceBase"/>).
    /// </summary>
    private void RollTalentSearchPrice(Manager manager)
    {
        if (TransferRules.Random16(FinanceRandom, 0, 1) == 0)
            return;
        Finance(manager).TalentPrice = TalentScout.PriceBase
            + TalentScout.PriceStep * TransferRules.Random16(FinanceRandom, 0, TalentScout.PriceSteps);
    }

    /// <summary>
    /// Zähler und Ergebnis der Talentsuche aller Manager an den Takttagen (<see cref="TalentScout.IsTick"/>).
    /// [ORIGINAL] Datei 0x06FA5 je Manager: Zähler 0 → nichts; sonst −1, und erst bei 0 das Ergebnis: Erfolg, wenn
    /// random(0, <see cref="TalentScout.SuccessDie"/>) = 0. Misserfolg: „Ihr Talentspäher gibt die / Suche ohne Erfolg auf...
    /// Pech...“ (DGROUP 0xC98/0xC9C). Erfolg: s = random(40, 90), Talent nach <see cref="JoinScoutedTalent"/>; kommt keins
    /// (Grenze 28), gibt es keine Nachricht, sonst „Ihre Talentsuche war ein Erfolg. / Ein junger Spieler stößt zu Ihrem
    /// Kader.“ (DGROUP 0xC90/0xC94). Der Schalter DGROUP 0x4860 (sofortiges Ende) ist immer 0 und fehlt im Remake.
    /// </summary>
    private void DailyTalentSearch(int day)
    {
        if (!TalentScout.IsTick(day))
            return;
        foreach (var manager in Managers)
        {
            var finance = Finance(manager);
            if (finance.TalentSearch is not { } search || --search.TicksLeft > 0)
                continue;
            finance.TalentSearch = null;
            if (TransferRules.Random16(FinanceRandom, 0, TalentScout.SuccessDie(manager.Level)) != 0)
            {
                Notify(manager, "Ihr Talentspäher gibt die", "Suche ohne Erfolg auf... Pech...");
                continue;
            }
            var s = TransferRules.Random16(FinanceRandom, TalentScout.MinStrength, TalentScout.MaxStrength);
            if (JoinScoutedTalent(manager, s) is not null)
                Notify(manager, "Ihre Talentsuche war ein Erfolg.", "Ein junger Spieler stößt zu Ihrem Kader.");
        }
    }

    /// <summary>
    /// Spieler im Besitz des Managers: der eigene Kader ganz, dazu die an andere Vereine verliehenen eigenen Spieler.
    /// [ORIGINAL] Datei 0x3CCEB: alle belegten Plätze des eigenen Satzes, aus den Sätzen der anderen Manager und des Markts
    /// (Satz 4) die Plätze mit Poolbesitzer +0x1A = Manager. [APPROX] Im Remake steht ein verliehener Spieler mit
    /// <see cref="PlayerLoan.Origin"/> = eigener Verein im fremden Kader; Angebote auf dem Markt bleiben im eigenen Kader.
    /// </summary>
    public int OwnedPlayers(Manager manager)
    {
        var club = Clubs[manager.ClubIndex];
        return club.Players.Count
            + Clubs.Where(c => c != club).Sum(c => c.Players.Count(p => p.Loan?.Origin == club));
    }

    /// <summary>
    /// Talent der erfolgreichen Suche in den Kader des Managers (oder null, wenn er schon 28 Spieler besitzt).
    /// [ORIGINAL] Datei 0x06D7F(m, s, 80): Poolindex random(1, 179), bis Besitzer +0x1A = 6 (frei); Pool +0x19 = 0; Alter
    /// random(18, 21); Form +0x17 = random(45, 55); Technik +0x16 und Kondition +0x15 je random(s − 5, s + 1) (in dieser
    /// Reihenfolge); Verein +0x1D = Verein des Managers; Kaderplatz über 0x42CB1 (Energie +0x13 = random(80, 120), Datei
    /// 0x42E4C; Kondition/Technik/Form aus Pool +0x15–0x17, Datei 0x42EC3–0x42F06); Besitzer = Manager; Kaderplatz +0x19 =
    /// random(3, 12) · 16 + random(3, 12); Gehalt +0x28 = Wert(Gehalt) · 80 / 100; Preis +0x2C = Wert(Preis); Laufzeit +0x0B =
    /// random(2, 3). Name, Talent (+0x12) und Rolle bleiben die des Poolspielers.
    /// [APPROX] Die Nachbearbeitung 0x118:0x4D (Tendenz, 0x4907A) und der Preis +0x2C sind nicht abgebildet (wie beim Kauf,
    /// <see cref="TransferPoolRules.RollSlotFields"/>); Energie auf 99 begrenzt. Das Original sucht ohne Grenze nach einem
    /// freien Platz; das Remake gibt nach <see cref="TalentPoolAttempts"/> Versuchen auf und erzeugt dann einen neuen Spieler
    /// (<see cref="PlayerFactory.TalentName"/>, Rolle zufällig) – ebenso ohne Spielerpool (Spielstände vor Schema 22).
    /// </summary>
    public Player? JoinScoutedTalent(Manager manager, int s)
    {
        if (OwnedPlayers(manager) >= TalentScout.MaxOwned)
            return null;
        var random = FinanceRandom;
        var club = Clubs[manager.ClubIndex];
        var slot = DrawFreePoolSlot(random);
        var p = slot?.Player ?? NewTalent(random);
        if (slot is not null)
        {
            slot.Owner = PoolOwner.Taken;
            slot.Club = club.Index;
            if (p.Id == 0)
                AssignPlayerId(p);
        }
        PlayerFactory.ApplyScoutedTalentValues(p, s, random);
        PlayerFactory.Join(club, p, contractYears: Contracts.MinYears, loan: false);
        p.Energy = Player.Clamp(TransferRules.Random16(random, 80, 120));
        _ = TransferRules.Random16(random, 3, 12); // Kaderplatz +0x19 (Bedeutung offen, A185/A186), nur gezogen
        _ = TransferRules.Random16(random, 3, 12);
        p.MonthlySalary = Math.Max(Contracts.MinSalary,
            Contracts.ValueSalary(this, manager, p) * TalentScout.SalaryPercent / 100);
        p.ContractYears = TransferRules.Random16(random, 2, 3);
        return p;
    }

    /// <summary>[APPROX] Höchstzahl Versuche für einen freien Poolplatz (wie beim Listenwechsel, Datei 0x49566 ff.: 1.500).</summary>
    public const int TalentPoolAttempts = 1_500;

    private PoolSlot? DrawFreePoolSlot(Random random)
    {
        var pool = TransferMarket.Pool;
        if (pool.Count == 0)
            return null;
        var last = TransferMarket.PoolLayout.LeagueLast;
        for (var attempt = 0; attempt < TalentPoolAttempts; attempt++)
        {
            var index = TransferRules.Random16(random, 1, last);
            if (pool.FirstOrDefault(s => s.Index == index) is { Owner: PoolOwner.Free } slot)
                return slot;
        }
        return null;
    }

    private Player NewTalent(Random random)
    {
        var role = (PlayerRole)TransferRules.Random16(random, 0, 2);
        var name = PlayerFactory.TalentName(Clubs.SelectMany(c => c.Players).ToList(), random);
        var p = new Player { Name = name, Role = role, Talent = TransferRules.Random16(random, 0, 9) };
        AssignPlayerId(p);
        return p;
    }
}
