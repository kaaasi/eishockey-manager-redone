namespace EishockeyManager.Core;

/// <summary>
/// Spielstand – Taktik des Nationalteams über die Amtszeiten hinweg (Spur nationalteam, #192).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Die Werte unter „AUTOM.“ stehen im Vereinssatz des Landes (Felder 0x26–0x2B, 0x35–0x3A), die
///   eigenen Werte des Bundestrainers in DGROUP 0xCAE9/0xCB01 + 0x30 · 5; beide werden beim Saisonwechsel und bei der
///   Ernennung nicht zurückgesetzt (keine Schreibstelle außer Spielvorbereitung 0x8365, Taktikbildschirm 0x48607 ff.
///   und Laden). Das Remake führt deshalb eine Taktik je Nationalteam Deutschland, die beim Amtsantritt an den
///   Bundestrainer geht und beim Saisonwechsel zurück in den Spielstand.</item>
/// <item>[ORIGINAL] Vor jedem WM-Spiel des Landes schreibt die Automatik ihre Zeile (<see cref="Tactics.RecordAutoLine"/>),
///   mit und ohne Bundestrainer. Freundschaftsspiele rufen 0x8365 nicht auf (Aufrufe nur aus dem Spieltagsablauf,
///   Datei 0x36F27/0x36F5A; `docs/dosbox-befunde.md`).</item>
/// <item>[ORIGINAL] Stärken für die Automatik: <see cref="NationalMatchStrength"/> beider Länder – die doppelten Stärken
///   aus 0x149D:0x61 = 0x368B1 (Datei 0x83AD/0x83CA, Argumente Verein, 1, 0, 0, 1; Spur wettbewerbe-spiel, Teil 3, A015).</item>
/// </list>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>Taktik des Nationalteams, solange kein Bundestrainer im Amt ist (sonst null; dann hält sie der Bundestrainer).</summary>
    private Tactics? _vacantNationalTactics = Tactics.National();

    /// <summary>Taktik des Nationalteams Deutschland: beim Bundestrainer, sonst im Spielstand.</summary>
    public Tactics NationalTactics => NationalCoach?.Tactics ?? _vacantNationalTactics
        ?? throw new InvalidOperationException("Taktik des Nationalteams fehlt.");

    /// <summary>Amtsantritt: Der neue Bundestrainer übernimmt die Taktik des Nationalteams.</summary>
    private void HandNationalTacticsToCoach(NationalCoach coach)
    {
        if (_vacantNationalTactics is { } tactics)
            coach.TakeOverTactics(tactics);
        _vacantNationalTactics = null;
    }

    /// <summary>Amtsende beim Saisonwechsel: Die Taktik bleibt beim Nationalteam.</summary>
    private void TakeNationalTacticsFromCoach()
    {
        if (NationalCoach is { } coach)
            _vacantNationalTactics = coach.Tactics;
    }

    /// <summary>
    /// Spielvorbereitung 0x8365 vor einem WM-Spiel: Automatik des eigenen Landes für die Anzeige festhalten.
    /// [ORIGINAL] Ohne Bundestrainer steht im Vereinsfeld 0x16 Deutschlands 0: die Tagesfunktion setzt es am Tagesende
    /// auf 0 (Datei 0x5540E, Satz 0x3D Bytes ab DGROUP 0x9657, Land DGROUP 0x6AC) und nur bei der Ernennung auf 6
    /// (Manager 5 + 1, Datei 0x55494), 0x55752 wieder auf 0. Dann schreibt 0x8365 ohne Blick auf die Modi (R8-B04).
    /// </summary>
    internal void PrepareNationalTactics(int home, int away)
    {
        var unmanaged = NationalCoach is null;
        if (home == NationalTeams.Germany)
            NationalTactics.RecordAutoLine(NationalMatchStrength(home), NationalMatchStrength(away), unmanaged);
        else if (away == NationalTeams.Germany)
            NationalTactics.RecordAutoLine(NationalMatchStrength(away), NationalMatchStrength(home), unmanaged);
    }
}
