namespace EishockeyManager.Core;

/// <summary>
/// Spur wettbewerbe-regeln, Teil 2 (A384, A670): Jugendteams eines Tages nach den Managermenüs.
/// </summary>
/// <remarks>
/// [ORIGINAL] Hauptweg Datei 0x55003–0x5502C: nach der Managerschleife (Menüs 0x44E22 ab Datei 0x54EB2) prüft em.exe die
/// Kalenderbits 0–2 des Tages und ruft je Manager 0x39DEC (Datei 0x5501C), danach die Ligaspiele 0x35EA4 (Datei 0x55039). Das
/// gilt für jeden Tag, nicht nur für Tag 0 (#699 Punkt 2, FILE01/FILE10/FILE27: Startwerte im ersten Menü). Bis Runde 22 wich das Remake ab:
/// die Tage ab 1 rechnete das Remake im Tageswechsel vor den Menüs (<c>AdvanceFinance</c>) – eine im Menü geänderte Förderung
/// wirkte erst am nächsten Ligaspieltag, und die Würfe lagen vor statt nach den Zügen des Tages.
/// Im Modus „nur Weltkugel“ nicht (eigener Weg zur WM, Datei 0x54CEE; wie bisher für Tag 0).
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Nur aus Spielständen vor Schema 57 (Migration <c>SaveGame.Migration.WettbewerbeRegeln.cs</c>): gesichert nach dem Tageswechsel
    /// eines Tages ab 1, der die Jugendteams damals schon gerechnet hatte. <see cref="ProcessDayCore"/> rechnet sie dann nicht noch
    /// einmal.
    /// </summary>
    private bool _youthOfDayDone;

    private bool TakeYouthOfDayDone()
    {
        var done = _youthOfDayDone;
        _youthOfDayDone = false;
        return done;
    }

    private void YouthAfterMenus(int day, bool alreadyDone)
    {
        if (!WorldOnly && !alreadyDone)
            DailyYouth(day);
    }
}
