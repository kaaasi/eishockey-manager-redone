namespace EishockeyManager.Core;

/// <summary>
/// Spur kader-rest Teil 3 (Runde 17): Jugend am Saisonende im Fenster „Saisonende“ mit Ereignissymbol 9.
/// <para>[ORIGINAL] Am 29. April läuft der Saisonwechsel Datei 0x54C9F ab Datei 0x55743 (<c>docs/analyse/re-saisonwechsel.md</c>
/// Abschnitt 1): je Manager zuerst der Saisonwechsel der Jugendteams (0x3A13B, Datei 0x55889, Schritt 3), später das Fenster
/// „Saisonende“ 0x26F20 (Datei 0x5645D). Dessen Managerschleife zieht nach den Einzelprämien die Jugend (Datei 0x2728C–0x27320)
/// und setzt je Neuzugang das Symbol 9 an die nächste freie Stelle (Datei 0x27302–0x2730F); erst danach folgen in derselben
/// Schleife Rückkehr der Markt-Plätze, Altern und Herunterzählen der Laufzeit (Datei 0x27547, 0x274A7, 0x27500).</para>
/// <para>Abbildung: <see cref="FinishSeason"/> rechnet den Jugendwechsel und die Jugend je Manager und vermerkt
/// <see cref="SeasonEvent.YouthJoined"/> im Ergebnis des Managers; der Kaderübergang (<see cref="StartSquadSeason"/>) lässt die
/// Neuen nach dem Fenster mit allen Kaderplätzen altern. Vorher zog das Remake die Jugend erst nach dem Fenster, das Symbol fehlte.</para>
/// <para>[ORIGINAL] #732: Im Modus „nur Weltkugel“ (DGROUP 0xD0B9 = 1) läuft die Managerschleife von 0x26F20 nie (Datei
/// 0x26F2F–0x26F4C, 0x26FAD → 0x279F1): keine Jugend. <see cref="FinishSeason"/> überspringt dort alle Manager.</para>
/// </summary>
public sealed partial class GameState
{
    /// <summary>
    /// Nur aus Spielständen vor Schema 50, gespeichert auf dem Bildschirm „Saisonende“: Jugendwechsel und Jugend am Saisonende
    /// standen damals noch aus (Migration <c>SaveGame.Migration.KaderRest.cs</c>). <see cref="StartNextSeason"/> holt sie nach.
    /// </summary>
    private bool _seasonEndYouthPending;

    /// <summary>[ORIGINAL] Saisonwechsel der Jugendteams aller Manager am 29. April (0x3A13B, Datei 0x55889; ohne Zufall).</summary>
    private void SeasonEndYouthTeams()
    {
        foreach (var manager in Managers)
            YouthSeasonChange(manager);
    }

    /// <summary>
    /// Jugend am Saisonende eines Managers im Fenster „Saisonende“ (<see cref="JoinSeasonEndYouth(Manager, int)"/>, Jahr des 29.
    /// April = <see cref="Season"/> + 1) und je Neuzugang ein <see cref="SeasonEvent.YouthJoined"/> (Symbol 9).
    /// </summary>
    private IEnumerable<SeasonEvent> SeasonEndYouthEvents(Manager manager) =>
        JoinSeasonEndYouth(manager, Season + 1).Select(_ => SeasonEvent.YouthJoined).ToList();

    /// <summary>
    /// [APPROX] Spielstand vor Schema 50 vom Bildschirm „Saisonende“: Jugendwechsel und Jugend wie bis dahin beim Saisonwechsel,
    /// ohne Ereignissymbol (das Fenster ist schon gezeigt). <see cref="Season"/> ist hier noch die alte Saison.
    /// </summary>
    private void CatchUpSeasonEndYouth()
    {
        if (!_seasonEndYouthPending)
            return;
        _seasonEndYouthPending = false;
        SeasonEndYouthTeams();
        if (!WorldOnly)
            JoinSeasonEndYouth(Season + 1);
    }
}
