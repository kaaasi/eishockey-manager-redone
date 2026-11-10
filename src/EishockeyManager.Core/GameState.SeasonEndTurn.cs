namespace EishockeyManager.Core;

/// <summary>
/// Managerwechsel am Saisonende als Ereignis im Spielkern (R12U-02, #538; AGENTS.md §4): Bildschirme setzen
/// <see cref="CurrentManager"/> nicht beim Öffnen oder Zeichnen, sondern lösen diese Methoden aus ihrer Eingabe aus.
/// </summary>
/// <remarks>
/// [ORIGINAL] Das Saisonende läuft im Original je Manager in einer Schleife (em.exe Datei 0x26FB0–0x279FA Prämien/Altern,
/// ab 0x27B64 die Verhandlungen); das Fenster „Ereignisse“ folgt Datei 0x5645D → 0x26F20.
/// [ORIGINAL] Zeiger auf den Manager am Zug (DGROUP 0xA70E, Kopie 0xA70D) im Fenster „Saisonende“ Datei 0x26F20:
/// Schleife 1 (Seiten mit Prämien und Ereignissen) setzt beide am Anfang jeder Runde auf den Manager i (Datei 0x26FB0–0x26FB3),
/// also bevor dessen Seite erscheint; Zählung ab 0 bis [0x386] (Datei 0x279ED–0x279FA), im Modus „nur Weltkugel“
/// ([0xD0B9] = 1) ab [0x386], also gar nicht (Datei 0x26F2F–0x26FAA). Kurzzeitige Umstellungen auf den Besitzer eines Spielers
/// (Datei 0x2756B) setzt dieselbe Runde zurück (Datei 0x27636, 0x279DA–0x279DD). Die Verhandlungen setzen beide wieder je
/// Manager (Datei 0x27B64–0x27B76); übersprungen wird ohne Anzeige, wer keinen belegten Kaderplatz hat (0x4BE17 = 0,
/// Datei 0x27BC6) oder keinen auslaufenden Vertrag (Merker [bp−0x18] aus der Platzschleife Datei 0x27B96–0x27BB6, geprüft
/// Datei 0x27BCF–0x27BD8; R13GN-02, #619). Nach dem Fenster setzt der Saisonwechsel beide auf 0 (Datei 0x564E1–0x564F2), die Zugfolge beginnt vorn.
/// Das Remake stellt <see cref="CurrentManager"/> an denselben Stellen um: Seite i (<see cref="ShowSeasonEndManager"/>),
/// nächste Verhandlung (<see cref="SwitchToRenewalManager"/>; Manager ohne Verhandlung zeigt auch das Original nicht),
/// danach der erste Manager (<see cref="ReturnToFirstActiveManager"/>).
/// [APPROX] Entlassene Manager: Das Original zählt alle [0x386] Manager; das Remake führt Entlassene (Entlassung selbst ist
/// [APPROX], SeasonDismissal.cs) ohne Seite und beginnt beim ersten nicht entlassenen.
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Fenster „Saisonende“ zeigt das Ergebnis des Managers <paramref name="index"/> der letzten Saison (begrenzt auf den
    /// letzten): dieser Manager ist am Zug.
    /// </summary>
    public void ShowSeasonEndManager(int index)
    {
        var managers = History[^1].Managers;
        if (managers.Count > 0)
            CurrentManager = managers[Math.Clamp(index, 0, managers.Count - 1)].Manager;
    }

    /// <summary>Verhandlung am Saisonende: der Manager mit dem Index <paramref name="managerIndex"/> ist am Zug.</summary>
    public void SwitchToRenewalManager(int managerIndex) => CurrentManager = managerIndex;

    /// <summary>Nach dem Saisonende: wieder der erste aktive Manager am Zug.</summary>
    public void ReturnToFirstActiveManager()
    {
        CurrentManager = Managers.ToList().IndexOf(ActiveManagers.First());
        OpenStartDay(); // Spur tagesablauf (#699 Punkt 2): Tag 0 der neuen Saison nach den Verhandlungen, vor dem ersten Menü
    }
}
