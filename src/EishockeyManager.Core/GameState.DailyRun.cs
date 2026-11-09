namespace EishockeyManager.Core;

/// <summary>
/// Tagesablauf 0x07169 des Originals (Training, Markt, Verträge eines Managers) und Listenwechsel 0x494EF im Hauptweg (#678).
/// </summary>
/// <remarks>
/// [ORIGINAL] Selbst nachgelesen (<c>tools/reverse-engineering/disasm.py gamedata/em.exe 0x551F0 0x552E0</c>, <c>… 0x07169 0x07C12</c>,
/// <c>… 0x54D38 0x54EF4</c>):
/// <list type="bullet">
/// <item>Hauptweg nach den Spielen: Datei 0x55204 zählt DGROUP 0x3B7 weiter, Datei 0x55253–0x55288 prüft den neuen Stand ≡ 0 oder
///   ≡ 4 (mod 7) (Zähler + 3 ≡ 0); nur dann Talentsuche 0x06FA5, Wanderung 0x9D1B und je Manager (DGROUP 0xA70E = 0 … Zahl − 1)
///   0x07169 (Datei 0x552A6), am Stand ≡ 0 danach der Stadionbau 0x3D316 (Datei 0x552C3). Im Remake der Tageslauf
///   <c>OnDayManager(e)</c> mit dem Zähler <see cref="DayCounter.Of"/>(e) = e + 1: Takt an den Tagen e ≡ 3, 6 (mod 7), Mittwoch und
///   Samstag (Tag 0 = Sonntag).</item>
/// <item>0x07169 für Manager i: nur bei i = 0 (Datei 0x0718F–0x07197) je Markt-Platz Markt-Verfall (0x071BB), Lösen der Sperre
///   (0x071CC) und Gebotsverfall (0x07215–0x0726A); für jeden Manager der Angebotswurf auf die Markt-Plätze mit Pool +0x1A = i
///   (0x0727C–0x073CE), bis Zähler 233 die Verträge seiner Kaderplätze (0x073E3–0x07AEF), dann je Kaderplatz Heilung 0x07C12 und
///   Training 0x0659C (0x07AF2–0x07C04), Aufstellungsautomatik 0x426BC (0x07C07).</item>
/// <item>Listenwechsel 0x494EF: im Zug jedes Managers (Schleife Datei 0x54E3F–0x54EF4, Wurf 0x54E65–0x54E7C random(0, Zahl + 2) = 0),
///   vor seinem Hauptmenü 0x44E22. Tage mit Tagesbyte 0 (9 gilt als 0) oder 255 springen bei Datei 0x54D5C/0x54D65 nach 0x55073
///   und überspringen die Züge samt Wurf; die WM-Schleife (Datei 0x55570–0x55740) und Modus 1 (Datei 0x54D38) rufen ihn nicht.
///   Weitere Aufrufer: nur der Saisonstart (Datei 0x437D5).</item>
/// </list>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Takttag des Hauptwegs für den Tageslauf <c>OnDayManager(day)</c>: Zähler <see cref="DayCounter.Of"/>(day) ≡ 0 oder 4 (mod 7).
    /// [ORIGINAL] Datei 0x55253–0x55288 (siehe Klassenkommentar).
    /// </summary>
    public static bool IsDailyRunDay(int day) => MatchEngine.IsDriftDay(DayCounter.Of(day));

    /// <summary>
    /// Tag mit Listenwechsel-Wurf in den Managerzügen? [ORIGINAL] Hauptweg (nicht Modus 1, nicht Eintrittstag oder Tage der
    /// WM-Schleife), Tagesbyte ∉ {0, 9, 255} (Datei 0x54D4C–0x54D65).
    /// </summary>
    public bool IsListChangeDay(int day)
    {
        if (WorldOnly || day >= SeasonCalendar.ClubSeasonEnd)
            return false;
        var events = SeasonCalendar.Events(day);
        return events is not (DayEvents.None or DayEvents.SeasonEnd) && (byte)events != 9;
    }

    /// <summary>
    /// Hauptweg: am Takttag 0x07169 je Manager in Managerreihenfolge (Datei 0x55298–0x552D3). [APPROX] Entlassene Manager
    /// überspringt das Remake (sie haben keinen Verein mehr); der Gebotsverfall bleibt an Manager 0 gebunden.
    /// </summary>
    private void MainPathDailyRun(int day)
    {
        TrainedUntil = (Season, day);
        if (!IsDailyRunDay(day))
            return;
        foreach (var manager in Managers.ToList())
            if (!manager.Dismissed)
                ManagerDailyRun(day, manager);
    }

    /// <summary>
    /// 0x07169 im Hauptweg für einen Manager: Markt (<see cref="DailyRunMarket"/>), Verträge (<see cref="AnnounceContractEnds"/> nur
    /// für ihn), Training seiner Kaderplätze (<see cref="TrainManager"/>). [APPROX] Heilung und Aufstellungsautomatik rechnet das
    /// Remake im Tageslauf für alle Vereine (<see cref="HealInjuries"/> davor, <see cref="ApplyAutoLineups"/> danach).
    /// </summary>
    private void ManagerDailyRun(int day, Manager manager)
    {
        DailyRunMarket(manager);
        AnnounceContractEnds(day, manager);
        TrainManager(manager);
    }

    /// <summary>
    /// Markt-Teil von 0x07169: für Manager 0 der Markt-Verfall (Datei 0x071BB, <see cref="MarketDecayAbove"/>), dann Gebotsverfall
    /// und Angebotswurf (<see cref="TransferMarket.CatchUpManagerDay"/>: Verfall nur Manager 0, Wurf nur auf seine Plätze).
    /// [APPROX] Das Lösen der Sperre (0x071CC) liegt im Remake bei den Verträgen (<see cref="AnnounceContractEnds"/>, eigene
    /// Zufallsquelle). Untergrenze 50 des Verfalls wie in der WM-Schleife (R14NT4N-03).
    /// </summary>
    private void DailyRunMarket(Manager manager)
    {
        if (Managers.Count > 0 && Managers[0] == manager)
            foreach (var listing in TransferMarket.Listings)
                if (listing.Player.Strain > MarketDecayAbove)
                    listing.Player.Strain = Math.Max(listing.Player.Strain - MarketDecay, TrainingRules.MinStrain);
        TransferMarket.CatchUpManagerDay(this, manager);
    }

    /// <summary>Hauptweg: an Tagen mit Ereignis je Managerzug der Wurf des Listenwechsels (<see cref="IsListChangeDay"/>).</summary>
    private void MainPathListChanges(int day)
    {
        if (!IsListChangeDay(day))
            return;
        for (var i = 0; i < Managers.Count; i++)
            TransferMarket.ListChangeTurn(this);
    }
}

public sealed partial class TransferMarket
{
    /// <summary>
    /// Wurf des Listenwechsels im Zug eines Managers. [ORIGINAL] Datei 0x54E65–0x54E7C: random(0, Zahl der Manager + 2) = 0 →
    /// 0x494EF (<see cref="ChangeList"/>).
    /// </summary>
    public void ListChangeTurn(GameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        UpdatedDay = game.PlayedMatchDays;
        if (ListChangeRoll(game.Managers.Count, game.Random))
            ChangeList(game, game.Random);
    }
}
