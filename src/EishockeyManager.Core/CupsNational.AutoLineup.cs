namespace EishockeyManager.Core;

/// <summary>
/// Einstellung „automatisch aufstellen“ des Bundestrainers (A009, Spur nationalteam Runde 14 Teil 3, L8; Auswertung
/// <c>docs/analyse/re-runde-14.md</c>, Sitzung 5, Review #649).
/// </summary>
/// <remarks>
/// [ORIGINAL] Satz 5 hat dieselbe Einstellung wie ein Verein: aktuell DGROUP 0x37F (= 0x375 + 2 · 5), gesichert DGROUP 0x380
/// (= 0x376 + 2 · 5), beide anfangs 1 (DOSBox FILE11/18/49: HOK 0x0084E/0x0084F = 01 01). Direkte Zugriffe auf 0x37F/0x380 gibt es
/// nur im Zug des Bundestrainers (Datei 0x55599–0x555C5, gesucht mit <c>ndisasm -b 16</c> über die ganze Datei); sonst schreiben nur
/// die indizierten Stellen 0x375/0x376 + 2 · Manager: Eingriff in der Aufstellung (Datei 0x40ADE, 0x40DE4), Taktikbildschirm
/// (Datei 0x47F4E, 0x49058) und der Tageslauf (Datei 0x54D9A, 0x54FDC/0x54FE8, 0x55307). Kein Saisonwechsel setzt sie zurück.
/// <list type="bullet">
/// <item>Zug des Bundestrainers (Datei 0x55599–0x555D6): 0x37F := 0x380, Hauptmenü (jeder Aufbau stellt mit Einstellung neu auf,
///   Datei 0x450C6 → 0x426BC), danach 0x380 := 0x37F, 0x37F := 0, 0x426BC (wirkungslos) und 0x952C(5, 0)
///   (<see cref="BeginNationalCoachTurn"/>, <see cref="OnNationalCoachMenu"/>, <see cref="EndNationalCoachTurn"/>).</item>
/// <item>Tageslauf an Tagen mit Ereignis (Datei 0x54D41–0x54DAA, Satz 5 nur bei DGROUP 0x9EDA &lt; 0x3F, also mit
///   Bundestrainer): 0x37F := 0x380, 0x426BC, 0x952C(5, 0); später 0x952C(5, 1), 0x380 := 0x37F, 0x37F := 0 (Datei
///   0x54F9C–0x54FE8). Im Modus „nur Weltkugel“ springt die Tagesfunktion an beiden Schleifen vorbei (Datei 0x54CEE–0x54D38 →
///   0x553FF). Siehe <see cref="OnDayNationalTeam"/>.</item>
/// <item>Ein Eingriff in der Aufstellung schaltet die aktuelle Einstellung ab (<see cref="NationalCoach.ClickPlayer"/>,
///   <see cref="NationalCoach.ClearSlot"/>); FILE47: nach dem Leeren 0x37F = 0, 0x380 = 1. Wer im Zug abschaltet, sichert beim
///   Weiterschalten 0 (FILE48 `00 00`): Die Aufstellung bleibt über den Spieltag leer. Nach dem Laden stellt das Original wieder
///   auf, weil der Zug neu beginnt (0x37F := 0x380 = 1, FILE47 → volle Aufstellung).</item>
/// </list>
/// </remarks>
public sealed partial class NationalCoach
{
    /// <summary>[ORIGINAL] DGROUP 0x37F: aktuelle Einstellung „automatisch aufstellen“ (nur im Zug ≠ 0, siehe Klassenkommentar).</summary>
    private bool _autoLineup = true;

    /// <summary>[ORIGINAL] DGROUP 0x380: gesicherte Einstellung, gilt zwischen den Zügen.</summary>
    private bool _autoLineupSaved = true;

    /// <summary>Läuft gerade ein Zug des Bundestrainers (zwischen 0x37F := 0x380 und 0x380 := 0x37F)?</summary>
    private bool _turnOpen;

    /// <summary>
    /// Einstellung „automatisch aufstellen“, wie sie gerade wirkt: im Zug die aktuelle (0x37F), sonst die gesicherte (0x380).
    /// </summary>
    public bool AutoLineupEnabled => _turnOpen ? _autoLineup : _autoLineupSaved;

    /// <summary>Datei 0x426BC für Satz 5: nur mit Einstellung neu aufstellen (Datei 0x426CC–0x426E4).</summary>
    private void ApplyAutoLineup(bool setting)
    {
        if (setting)
            ApplyAutoFormation();
    }

    /// <summary>Eingriff von Hand: aktuelle Einstellung aus (Datei 0x40ADE, 0x40DE4 mit DGROUP 0xA70E = 5).</summary>
    private void DisableAutoLineup() => _autoLineup = false;

    /// <summary>
    /// Klick auf einen belegten Platz, dessen Spieler verletzt oder gesperrt ist: nur die Einstellung aus, der Platz bleibt
    /// (Datei 0x40ACF–0x40ADE vor der Prüfung 0x40B37–0x40B5D).
    /// </summary>
    public void SwitchOffAutoLineup() => DisableAutoLineup();

    /// <summary>Zuganfang (Datei 0x55599/0x5559C): 0x37F := 0x380, dann Neuaufstellung wie beim ersten Aufbau des Menüs.</summary>
    internal void BeginTurn()
    {
        _autoLineup = _autoLineupSaved;
        _turnOpen = true;
        ApplyAutoLineup(_autoLineup);
    }

    /// <summary>Aufbau des Menüs im Zug (Datei 0x450C6 → 0x426BC).</summary>
    internal void OnMenu() => ApplyAutoLineup(_autoLineup);

    /// <summary>Zugende (Datei 0x555BF–0x555CA): 0x380 := 0x37F, 0x37F := 0; der folgende Aufruf von 0x426BC ist wirkungslos.</summary>
    internal void EndTurn()
    {
        _autoLineupSaved = _autoLineup;
        _autoLineup = false;
        _turnOpen = false;
    }

    /// <summary>Tageslauf des Hauptwegs an Tagen mit Ereignis (Datei 0x54D8C–0x54D9E): mit der gesicherten Einstellung neu aufstellen
    /// (nicht in der WM-Schleife, R14NT-02).</summary>
    internal void OnEventDay() => ApplyAutoLineup(_autoLineupSaved);

    /// <summary>Einstellung beim Amtsantritt: die gesicherte des Vorgängers (globale Bytes, kein Zurücksetzen).</summary>
    internal void TakeOverAutoLineup(bool saved)
    {
        _autoLineupSaved = saved;
        _autoLineup = saved;
    }

    /// <summary>Gesicherte Einstellung für den Nachfolger (siehe <see cref="TakeOverAutoLineup"/>).</summary>
    internal bool SavedAutoLineup => _autoLineupSaved;
}

public sealed partial class GameState
{
    /// <summary>
    /// [ORIGINAL] DGROUP 0x380 zwischen zwei Ämtern: Die Bytes sind global und überdauern den Saisonwechsel (kein Schreiber dort,
    /// siehe <see cref="NationalCoach"/>); der neue Bundestrainer übernimmt den Stand.
    /// </summary>
    private bool _nationalAutoLineupSaved = true;

    /// <summary>Zuganfang des Bundestrainers (Modus 4: <see cref="PassNationalTurn"/>; „nur Weltkugel“: Zuganfang von Manager 0).</summary>
    internal void BeginNationalCoachTurn()
    {
        if (NationalCoachMenu && NationalCoach is { } coach)
            coach.BeginTurn();
    }

    /// <summary>Zugende des Bundestrainers (Weiterschalten, Datei 0x555BF ff.).</summary>
    internal void EndNationalCoachTurn() => NationalCoach?.EndTurn();

    /// <summary>
    /// Aufbau des Hauptmenüs im Zug des Bundestrainers (aus <see cref="OnMainMenu"/>). [ORIGINAL] Jeder Aufbau stellt mit der
    /// Einstellung neu auf (Datei 0x450C6). Ein Aufbau, der kein Rücksprung ist (erster Aufbau, Laden), beginnt den Zug neu:
    /// nach dem Laden setzt das Original 0x37F := 0x380 (FILE47: 0x37F = 0, 0x380 = 1 → wieder 22 eingesetzt).
    /// </summary>
    private void OnNationalCoachMenu(bool isReturn)
    {
        if (!NationalCoachMenu || NationalCoach is not { } coach)
            return;
        if (isReturn)
            coach.OnMenu();
        else
            coach.BeginTurn();
    }

    /// <summary>
    /// Tageslauf des Nationalteams in der Vereinssaison (aus <c>OnDayClubSeason</c>): an Tagen mit Ereignis Neuaufstellung mit der
    /// gesicherten Einstellung (Datei 0x54D41–0x54DAA), dann die Länderwerte (Datei 0x54F9C–0x54FBF, <see cref="UpdateNationalTeamValues"/>).
    /// [ORIGINAL] Ereignis = Kalenderbyte ≠ 0, ≠ 0xFF (9 zählt wie 0, Datei 0x54D4C–0x54D65). Nur der Hauptweg: Nach dem
    /// 13. April nimmt im Original die WM-Schleife den Tag (<see cref="OnDayWorldCupLoop"/>, R14NT-01/02, #664); hier wirkt es nur
    /// bei einem vor dem 13. April ernannten Bundestrainer (nur Tests).
    /// </summary>
    private void OnDayNationalTeam(int day)
    {
        var events = SeasonCalendar.Events(day);
        if (NationalCoach is { } coach && events is not (DayEvents.None or DayEvents.SeasonEnd) && (byte)events != 9)
            coach.OnEventDay();
        UpdateNationalTeamValues();
    }
}
