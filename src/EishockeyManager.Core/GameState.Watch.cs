namespace EishockeyManager.Core;

/// <summary>
/// Markierung zum Zusehen je Verein (Spur zusehen, Teil 1; F018, A445): Vereinsfeld 0x34 Bits 6–7 (die unteren sechs Bits sind
/// die Logonummer).
/// <list type="bullet">
/// <item>[ORIGINAL] Modus 0 = nicht markiert, 1 = Tore melden, 2 = Live-Anzeige (Symbole kb\4 (19 | 36 + 11 · Modus), Datei
///   0x4C10A–0x4C14A). Modus 2 (Bit 7) gibt der Paarung eine Reihe der Live-Anzeige (Spielabrechnung Datei 0x54BF7–0x54C23,
///   <see cref="LiveScoreboard"/>); der Abschluss 0xA484 prüft beide Bits (<c>test 0xC0</c>, Datei 0xABD7/0xABEE) für die
///   Tormeldung der Paarungen ohne Reihe.</item>
/// <item>[APPROX] Die Tormeldung selbst (Datei 0xABB4–0xAD71: Text DGROUP 0x495C mit den Vereinsnamen, Pause 0x130EE) zeigt das
///   Remake noch nicht; Modus 1 wirkt daher nur auf das Symbol des Knopfs.</item>
/// <item>[ORIGINAL] Der Lader 0x2EF1B setzt die Bits beim Laden der Datenbank auf 0 (Datei 0x2EFA8): ein neuer
///   <see cref="Club"/> beginnt mit Modus 0.</item>
/// <item>[ORIGINAL] Die Markierung gehört zum Vereinssatz (wie Name und Logo) und wandert beim Platztausch 0x36169 mit dem Verein
///   (<c>docs/analyse/re-runde-13.md</c> „Platztausch“); im Remake hängt sie am <see cref="Club"/>-Objekt, nicht an den
///   Platzfeldern (<c>Club.SwapPlaceFields</c>).</item>
/// <item>[ORIGINAL] Aufrufer von 0x0AE2B: Spielstart und neuer Manager 0x28165 je Manager (Verein, 1, 2) (Datei 0x298EA,
///   0x29C1C; <see cref="MarkStartWatches"/>, <see cref="AddManager"/>), Ernennung zum Bundestrainer (Deutschland, 1, 2) (Datei
///   0x55457; <see cref="AppointNationalCoach"/>), dritter Knopf des Gegner-/Vereinsfensters (Datei 0x4E440–0x4E46A;
///   <see cref="ToggleWatch"/>) und WM-Option 12 (Datei 0x2D3FD, <see cref="ToggleWorldCupOption"/>). Kein Aufrufer nimmt eine Markierung
///   wieder weg, wenn ein Manager entlassen wird oder das Amt des Bundestrainers endet (Datei 0x5574A–0x5575A löscht nur
///   Vereinsfeld 0x16 Deutschlands).</item>
/// </list>
/// </summary>
public sealed partial class Club
{
    /// <summary>Modus der Markierung zum Zusehen (0, 1, 2), Vereinsfeld 0x34 Bits 6–7.</summary>
    public int WatchMode { get; internal set; }

    /// <summary>[ORIGINAL] Bit 7 (Modus 2): Die Paarungen des Vereins bekommen eine Reihe der Live-Anzeige (Datei 0x54C07/0x54C1E).</summary>
    public bool WatchedLive => (WatchMode & 2) != 0;
}

public sealed partial class GameState
{
    /// <summary>Modi der Markierung (0 aus, 1 Tore melden, 2 Live-Anzeige).</summary>
    public const int WatchOff = 0, WatchGoals = 1, WatchLive = 2;

    /// <summary>[ORIGINAL] Höchstens vier Vereine im Modus 2 je Gruppe (Datei 0xAE99: <c>cmp [bp−4],4</c>).</summary>
    public const int MaxLiveWatches = 4;

    /// <summary>[ORIGINAL] Obergrenze der Vereinsgruppe ohne Länder (Datei 0xAE39: <c>[bp−6] = 0x96</c>).</summary>
    private const int WatchClubGroupEnd = 0x96;

    /// <summary>
    /// Setzt die Markierung zum Zusehen eines Vereins: em.exe <c>0x30:0xB</c> = Datei 0x0AE2B (Verein, Verdrängen, Modus).
    /// Rückgabe ist der tatsächlich gesetzte Modus.
    /// <list type="number">
    /// <item>[ORIGINAL] Gruppe: Länder DGROUP 0x6AD … 0x6AD + 11 (0x128:0x20, Datei 0xAE41–0xAE53), sonst Vereinssätze 0 … 0x95.</item>
    /// <item>[ORIGINAL] Nur bei Modus 2 (Datei 0xAE56): die Gruppe in Satzfolge durchlaufen und die Vereine im Modus 2 zählen
    ///   (Bits 6–7 = 2, der Verein selbst eingeschlossen). Erreicht der Zähler genau 4, dann ohne Verdrängen Modus 0 (abgelehnt,
    ///   Datei 0xAF3D), mit Verdrängen den ersten Verein davor im Modus 2 ohne Manager (Vereinsfeld 0x16 = 0) auf Modus 1 und
    ///   den Zähler zurück (Datei 0xAEB4–0xAF2E). Findet sich keiner, bleibt Modus 2; spätere Vereine erhöhen den Zähler über 4
    ///   und prüfen nicht mehr.</item>
    /// <item>[ORIGINAL] Bits 6–7 des Vereins = Modus, die Logobits bleiben (Datei 0xAF4F–0xAF82).</item>
    /// </list>
    /// [APPROX] Vereinsfeld 0x16 (Manager + 1) ist im Remake <see cref="MatchEngine.IsManaged"/> (nicht entlassene Manager, Land
    /// des Bundestrainers); bei Ländern schreibt das Original das Feld auch in der WM-Schleife (Strafen, siehe
    /// <c>CupsNational.WorldCupLoop.cs</c>), das bleibt hier unberücksichtigt. Vereinssätze über die Vereinsliste hinaus
    /// (Europapokal-Gegner, die das Remake nicht führt) zählen nicht.
    /// </summary>
    public int SetWatchMode(Club club, bool evict, int mode)
    {
        ArgumentNullException.ThrowIfNull(club);
        if (mode is < WatchOff or > 3)
            throw new ArgumentOutOfRangeException(nameof(mode));
        var national = club.Index is >= NationalTeams.First and <= NationalTeams.Last;
        var first = national ? NationalTeams.First : 0;
        var end = Math.Min(national ? NationalTeams.Last + 1 : WatchClubGroupEnd, Clubs.Count);
        if (mode == WatchLive)
        {
            var count = 0;
            for (var c = first; c < end && mode == WatchLive; c++)
            {
                if (Clubs[c].WatchMode != WatchLive)
                    continue;
                if (++count != MaxLiveWatches)
                    continue;
                if (!evict)
                {
                    mode = WatchOff;
                    break;
                }
                for (var d = first; d < c; d++)
                {
                    var other = Clubs[d];
                    if (other.WatchMode == WatchLive && !MatchEngine.IsManaged(this, other))
                    {
                        other.WatchMode = WatchGoals;
                        count--;
                        break;
                    }
                }
            }
        }
        club.WatchMode = mode;
        return mode;
    }

    /// <summary>
    /// Dritter Knopf des Gegner-/Vereinsfensters: [ORIGINAL] Datei 0x4E440–0x4E46F schaltet 0 → 1 → 2 → 0 und ruft 0x0AE2B mit
    /// Verdrängen = Vereinsfeld 0x16 (nur Vereine mit Manager verdrängen). Rückgabe ist der neue Modus.
    /// </summary>
    public int ToggleWatch(Club club)
    {
        ArgumentNullException.ThrowIfNull(club);
        var next = (club.WatchMode + 1) % 3;
        return SetWatchMode(club, MatchEngine.IsManaged(this, club), next);
    }

    /// <summary>
    /// Spielstart (Datei 0x298EA/0x29C1C: 0x0AE2B(Verein des Managers, 1, 2) je Manager in Managerfolge). [APPROX] Nach dem
    /// Import eines Original-Spielstands (<c>OriginalImport</c>) und für einen dort schon ernannten Bundestrainer setzt das Remake
    /// die Markierungen ebenso, statt die Bits des Spielstands zu lesen (Vereinsfeld 0x34 wird noch nicht übernommen).
    /// </summary>
    internal void MarkStartWatches()
    {
        foreach (var manager in Managers)
            if (manager.ClubIndex >= 0 && manager.ClubIndex < Clubs.Count)
                SetWatchMode(Clubs[manager.ClubIndex], evict: true, WatchLive);
        if (NationalCoach is { } coach && coach.Nation < Clubs.Count)
            SetWatchMode(Clubs[coach.Nation], evict: true, WatchLive);
    }
    /// <summary>
    /// Sicherung des Schalters „WM-Spiele“ (Option 12, DGROUP 0x229) während der WM mit Bundestrainer: DGROUP 0x22C (Spur
    /// zusehen, Teil 2, A541); null = keine Sicherung offen.
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] Tagesfunktion 0x54C9F, Eintritt in die WM-Schleife Datei 0x553FF (13. April bzw. Modus 1 über 0x54D38): Datei
    /// 0x5541F sichert 0x229 nach 0x22C vor der Ernennungsschleife, jede Ernennung setzt nach 0x0AE2B(Deutschland, 1, 2) den
    /// Schalter auf 1 (Datei 0x5545F); nach dem letzten Schleifentag stellt Datei 0x55757–0x5575A ihn zurück, bevor das Amt endet
    /// (0x5580E). Die Schleife 0x55570–0x55740 springt nur nach 0x55570 zurück, nie vor 0x5541F: die Sicherung läuft je Saison
    /// genau einmal, die Rückstellung wirkt. Der HOK-Block 0x0A5D7 (16 Byte ab 0x21D) enthält 0x22C.
    /// [APPROX] Das Remake sichert erst bei der Ernennung (<see cref="AppointNationalCoach"/>): Ohne Ernennung gibt es in der
    /// Schleife kein Menü, in dem der Schalter sich ändern könnte – Sicherung und Rückstellung sind dann wirkungslos.
    /// </remarks>
    private bool? _worldCupOptionBackup;

    /// <summary>[ORIGINAL] Datei 0x5541F + 0x5545F: Schalter sichern und für die WM des Bundestrainers einschalten.</summary>
    internal void ShowWorldCupForNationalCoach()
    {
        _worldCupOptionBackup ??= Options.ShowWorldCup;
        Options.ShowWorldCup = true;
    }

    /// <summary>[ORIGINAL] Datei 0x55757–0x5575A: Schalter nach der WM zurückstellen (vor dem Amtsende 0x5580E).</summary>
    internal void RestoreWorldCupOption()
    {
        if (_worldCupOptionBackup is { } saved)
            Options.ShowWorldCup = saved;
        _worldCupOptionBackup = null;
    }

    /// <summary>
    /// Schalter „WM-Spiele“ (Option 12) im Optionsbildschirm umschalten (Spur zusehen, Teil 2, A295). [ORIGINAL] Datei
    /// 0x2D3CA–0x2D402: nach dem <c>xor 1</c> ruft das Original, wenn der Verein des Managers am Zug (Managersatz +0x14, DGROUP
    /// 0x966F + 0x1AF · m) nicht Deutschland (DGROUP 0x6AC = 56) ist, 0x0AE2B(Deutschland, 1, 2 · neuer Wert): Einschalten markiert
    /// Deutschland für die Live-Anzeige (mit Verdrängen), Ausschalten nimmt die Markierung weg. Der Bundestrainer an seinem Zug
    /// (Verein = Deutschland) schaltet nur den Schalter. Rückgabe ist der neue Wert.
    /// </summary>
    public bool ToggleWorldCupOption()
    {
        Options.ShowWorldCup = !Options.ShowWorldCup;
        var turn = NationalCoachMenu ? NationalCoach?.Nation ?? NationalTeams.Germany : ActiveManager.ClubIndex;
        if (turn != NationalTeams.Germany && NationalTeams.Germany < Clubs.Count)
            SetWatchMode(Clubs[NationalTeams.Germany], evict: true, Options.ShowWorldCup ? WatchLive : WatchOff);
        return Options.ShowWorldCup;
    }
}
