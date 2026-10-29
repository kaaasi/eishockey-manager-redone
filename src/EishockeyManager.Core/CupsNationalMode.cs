namespace EishockeyManager.Core;

/// <summary>
/// Spielmodus aus der Spielvorbereitung. [ORIGINAL] DGROUP 0xD0B9, Werte wie im Original (Datei 0x29838 ff.).
/// </summary>
public enum GameMode : byte
{
    /// <summary>Nur Weltkugel: keine Vereinssaison, Manager 0 ist Bundestrainer und betreut Deutschland bei der WM.</summary>
    WorldOnly = 1,

    /// <summary>Nur Fahne: Liga und DEB-Pokal, kein Europapokal und keine WM.</summary>
    ClubOnly = 2,

    /// <summary>Beide Knöpfe: Liga, DEB-Pokal, Europapokal und WM.</summary>
    Full = 4,
}

public static class GameModes
{
    /// <summary>
    /// [ORIGINAL] Datei 0x29838–0x29862: <c>mode = (bits == 2) + 2·(bits == 1)</c>, bei <c>bits == 3</c> → 4.
    /// Bit 0 = Fahne, Bit 1 = Weltkugel. Ohne Knopf (0) gibt es keinen Modus; „Los jetzt!“ ist dann gesperrt.
    /// </summary>
    public static GameMode? FromBits(int bits) => (bits & 3) switch
    {
        1 => GameMode.ClubOnly,
        2 => GameMode.WorldOnly,
        3 => GameMode.Full,
        _ => null,
    };
}

/// <summary>Art einer Buchung auf dem Konto des Bundestrainers (<see cref="GameState.NationalBookings"/>).</summary>
public enum NationalBookingKind
{
    /// <summary>Etat beim Amtsantritt (Datei 0x554A6).</summary>
    Budget,

    /// <summary>Kosten eines Freundschaftsspiels (Datei 0x3BFC4).</summary>
    Friendly,

    /// <summary>Stand aus einem älteren Spielstand ohne Buchungsliste (Migration Schema 2 → 3).</summary>
    Carryover,
}

/// <summary>Eine Buchung auf dem Nationalkonto (DM, Einnahme positiv).</summary>
public sealed record NationalBooking(int Season, NationalBookingKind Kind, long Amount);

/// <summary>Spielstand – Spur wettbewerbe-weltkugel: Spielmodus „nur Weltkugel“ (Bundestrainer-Ablauf).</summary>
public sealed partial class GameState
{
    /// <summary>Spielmodus der Spielvorbereitung (DGROUP 0xD0B9).</summary>
    public GameMode Mode => Setup.Mode ?? GameMode.Full;

    /// <summary>Modus „nur Weltkugel“: keine Vereinssaison, der einzige Manager ist Bundestrainer.</summary>
    public bool WorldOnly => Mode == GameMode.WorldOnly;

    /// <summary>Erster Tag mit Ereignis 0x10 (WM) im Kalender.</summary>
    public static int FirstWorldCupDay => SeasonCalendar.DaysOf(DayEvents.WorldCup)[0];

    /// <summary>
    /// Hauptmenü des Bundestrainers: im Modus „nur Weltkugel“ führt Manager 0 das Nationalteam (im Original der Zug
    /// „Manager 5“, DGROUP 0xA70E = 5). [APPROX] Schon vor der Ernennung am ersten WM-Termin, damit das Menü des
    /// Vereins im Modus 1 nie erscheint.
    /// </summary>
    public bool NationalCoachMenu => WorldOnly;

    /// <summary>
    /// Modus „nur Weltkugel“, erster Tag der Saison: [ORIGINAL] Datei 0x54CEE–0x54D38 – der Tageszähler springt ohne
    /// Tagesverarbeitung (keine Liga, keine Finanzen, kein Training, kein Markt) bis zum ersten WM-Termin; danach wird
    /// Manager 0 Bundestrainer (DGROUP 0xD0BA = 1, Ernennung samt Etat 0x55436 ff.).
    /// [APPROX] Das Remake hält danach vor dem ersten WM-Spiel an, damit der Bundestrainer seinen Kader wählen kann
    /// (das Original wählt ihn an dieser Stelle nur im Durchlauf ohne Halt, DGROUP 0x1DB, automatisch).
    /// </summary>
    /// <returns>true, wenn gesprungen wurde.</returns>
    internal bool SkipToWorldCup()
    {
        if (!WorldOnly || CurrentDay >= FirstWorldCupDay)
            return false;
        CurrentDay = FirstWorldCupDay;
        AppointNationalCoach(Managers[0]);
        return true;
    }

    /// <summary>Spielt das Nationalteam des Bundestrainers an diesem Tag (Halt der Zeitleiste im Modus „nur Weltkugel“)?</summary>
    internal bool NationalCoachPlaysOn(int day) =>
        WorldOnly && NationalCoach is { } coach && MatchOn(day, coach.Nation) is not null;

    /// <summary>Vereine und – im Modus „nur Weltkugel“ – das Nationalteam, deren Spiele „eigene“ Spiele sind.</summary>
    internal HashSet<int> ManagerTeams()
    {
        var teams = ActiveManagers.Where(_ => !WorldOnly).Select(m => m.ClubIndex).ToHashSet();
        if (WorldOnly && NationalCoach is { } coach)
            teams.Add(coach.Nation);
        return teams;
    }
}
