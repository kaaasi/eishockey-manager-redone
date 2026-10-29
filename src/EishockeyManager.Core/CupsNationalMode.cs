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
    /// Vereins im Modus 1 nie erscheint. Im Modus 4 nur während des Zugs des Bundestrainers (<see cref="NationalTurn"/>).
    /// </summary>
    public bool NationalCoachMenu => WorldOnly || _nationalTurn;

    /// <summary>Zug „Manager 5“ im Modus 4 (Spur bundestrainer-modus4), siehe <see cref="PassNationalTurn"/>.</summary>
    private bool _nationalTurn;

    /// <summary>
    /// Modus 4: Ist gerade der Bundestrainer am Zug? [ORIGINAL] Datei 0x55587–0x555D6: Hat das Nationalteam einen
    /// menschlichen Bundestrainer, folgt an WM-Tagen (Ereignis 0x10) nach den Vereinsmanagern ein eigener Zug
    /// „Manager 5“ (DGROUP 0xA70D = 0xA70E = 5) mit dem Menü des Bundestrainers (Hauptmenü 0x28:0x146C).
    /// Manager 5 trägt den Namen des ernannten Managers (Datei 0x55475, strcpy nach DGROUP 0x9EC6);
    /// <see cref="CurrentManager"/> zeigt deshalb auf ihn.
    /// </summary>
    public bool NationalTurn => _nationalTurn;

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

    /// <summary>
    /// Spielt das Nationalteam des Bundestrainers an diesem Tag? Hält die Zeitleiste an – im Modus „nur Weltkugel“ und
    /// im Modus 4 (Zug des Bundestrainers). [APPROX] Das Original gibt den Zug an jedem WM-Tag (0x555AD); alle
    /// WM-Termine sind Spieltage Deutschlands, solange es im Turnier steht.
    /// </summary>
    internal bool NationalCoachPlaysOn(int day) =>
        NationalCoach is { } coach && MatchOn(day, coach.Nation) is not null;

    /// <summary>Vereine (außer im Modus „nur Weltkugel“) und das Nationalteam des Bundestrainers: „eigene“ Spiele.</summary>
    internal HashSet<int> ManagerTeams()
    {
        var teams = ActiveManagers.Where(_ => !WorldOnly).Select(m => m.ClubIndex).ToHashSet();
        if (NationalCoach is { } coach)
            teams.Add(coach.Nation);
        return teams;
    }

    /// <summary>
    /// Tage mit Markierung 255 (13. und 29. April): erst Ernennung im Modus 4, dann – ohne Bundestrainer – die Werte
    /// Deutschlands neu auswürfeln (<see cref="RerollNationalRatings"/>).
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] Datei 0x553A2–0x554D9: Nach dem Tag ruft das Original 0xE8:0x93(0x7F) (Datei 0x34769): Die ersten fünf
    /// Plätze der 1. Liga (DGROUP 0x5CCB, Liga · 12 + Platz) werden von oben durchsucht; der erste Verein mit Manager
    /// (Vereinsfeld 0x16 = Manager + 1) setzt DGROUP 0xD0BA[Manager] = 1. Die Ernennungsschleife 0x55436 ernennt dann jeden
    /// Manager mit gesetztem Wert – außer im Modus 2 – samt Etat (siehe <see cref="AppointNationalCoach"/>). Die Suche
    /// läuft nur, solange Deutschland keinen Bundestrainer hat (0x55374); im Modus 1 ist er seit dem Sprung ernannt.
    /// Ausgewürfelt wird danach nur, wenn es weiter keinen Bundestrainer gibt (0x554CF).
    /// [APPROX] Platzierung aus der Abschlusstabelle der Hauptrunde (Gesamt). Das Original sortiert DGROUP 0x5CCB in
    /// place (0x50E60 ff., je nach Ansicht Gesamt/Heim/Auswärts) und liest die zuletzt sortierte Reihenfolge.
    /// </remarks>
    internal void OnNationalMarkerDay()
    {
        if (Mode == GameMode.Full && NationalCoach is null && TopFiveManager() is { } manager)
            AppointNationalCoach(manager);
        RerollNationalRatings();
    }

    /// <summary>Manager des am höchsten platzierten Vereins unter den ersten fünf der 1. Liga oder null.</summary>
    private Manager? TopFiveManager()
    {
        foreach (var club in FinalTables()[League.Bundesliga].Take(5))
            if (ActiveManagers.FirstOrDefault(m => m.ClubIndex == club) is { } manager)
                return manager;
        return null;
    }

    /// <summary>
    /// Modus 4: Zug des Bundestrainers nach den Vereinsmanagern (aus <see cref="Continue"/>, nachdem der letzte
    /// Vereinsmanager bestätigt hat). Liefert true, wenn der Zug an den Bundestrainer geht (Tag noch nicht spielen),
    /// false, wenn der Tag gespielt werden soll – auch nach dem Zug des Bundestrainers.
    /// </summary>
    internal bool PassNationalTurn()
    {
        if (_nationalTurn)
        {
            _nationalTurn = false;
            return false;
        }
        if (WorldOnly || NationalCoach is not { } coach || Managers[coach.Manager].Dismissed || !NationalCoachPlaysOn(CurrentDay))
            return false;
        _readyManagers.Add(ActiveManager);
        _nationalTurn = true;
        CurrentManager = coach.Manager;
        return true;
    }
}
