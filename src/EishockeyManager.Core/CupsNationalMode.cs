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
    /// „Manager 5“, DGROUP 0xA70E = 5). [ORIGINAL] Im Modus 1 springt die Tagesfunktion am ersten Tag (0x54CEE) und ruft
    /// das Hauptmenü erst am Tagesende (0x555A3), so dass das Menü des Vereins nie erscheint (dosbox-befunde.md). Im Modus 4 nur während des Zugs des Bundestrainers (<see cref="NationalTurn"/>).
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
    /// [ORIGINAL] Danach hält das Spiel vor dem ersten WM-Spiel mit leerem Kader an (A027, Spur nationalteam Runde 13): Das
    /// Original setzt nur die Ernennung (Datei 0x54D19, DGROUP 0xD0BA = 1) und zeigt das Menü des Bundestrainers am ersten
    /// WM-Tag; „auto“ (0x3CA06) und Satz 5 nur im Durchlauf (DGROUP 0x1DB ≠ 0, Datei 0x54D1E–0x54D2D), den das Remake nicht
    /// kennt. DOSBox FILE11: 14.04.1994, Satz 5 leer, DGROUP 0xA70E = 5; `file11_12` „Eingesetzt: 0“.
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
    /// Ist der Bundestrainer an diesem Tag am Zug? Hält die Zeitleiste an – im Modus „nur Weltkugel“ und im Modus 4 (Zug
    /// des Bundestrainers). [ORIGINAL] An jedem WM-Tag (Ereignis 0x10), solange es einen Bundestrainer gibt – auch wenn
    /// Deutschland ausgeschieden ist oder an dem Tag nicht spielt; das Menü zeigt dann „Spielfrei“ (A028, Spur nationalteam
    /// Runde 13): Datei 0x5557F–0x555A3 ruft das Hauptmenü 0x44E22 bei DGROUP 0x9EDA ≠ 0xFD und Tagesereignis ≠ 0 und fragt
    /// das Spiel der eigenen Mannschaft nicht ab; DOSBox `file19_28` (24.04.1994 „Spielfrei“), `file20_02`, `file20_04`
    /// (Deutschland Gruppe 1 Platz 6). Bis Runde 13 hielt das Remake nur an Spieltagen Deutschlands. Nach dem letzten WM-Tag
    /// folgt der Saisonwechsel ohne Halt (`file21_02` → `file21_03`).
    /// </summary>
    internal bool NationalCoachPlaysOn(int day) =>
        NationalCoach is not null && SeasonCalendar.Events(day).HasFlag(DayEvents.WorldCup);

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
    /// [ORIGINAL] Platzierung aus der Gesamttabelle: Die Platzliste DGROUP 0x5CCB sortiert 0x50B45 (Datei 0x50D6D–0x51011);
    /// nach einer angesehenen Heim- oder Auswärtstabelle stellt 0x50256 (Liga, 1, Gesamt) die Gesamtordnung wieder her
    /// (`docs/reverse-engineering.md`, Abschnitt Tabellen). Abweichen kann nur die Reihenfolge vollständig gleicher
    /// Vereine (Sortierung nicht stabil); <see cref="FinalTables"/> nutzt dieselbe Sortierung (`LeagueTable.Compute`, Gesamt), den
    /// Einfluss angesehener Heim-/Auswärtstabellen auf Gleichstände führt `Season.cs` als [APPROX].
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
            EndNationalCoachTurn(); // A009: Datei 0x555BF–0x555CA
            _nationalTurn = false;
            return false;
        }
        if (WorldOnly)
        {
            EndNationalCoachTurn(); // „nur Weltkugel“: Manager 0 führt nur den Zug des Bundestrainers (A009)
            return false;
        }
        if (NationalCoach is not { } coach || Managers[coach.Manager].Dismissed || !NationalCoachPlaysOn(CurrentDay))
            return false;
        _readyManagers.Add(ActiveManager);
        _nationalTurn = true;
        CurrentManager = coach.Manager;
        BeginNationalCoachTurn(); // A009: Datei 0x55599/0x5559C
        return true;
    }
}
