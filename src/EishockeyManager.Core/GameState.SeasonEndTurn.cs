namespace EishockeyManager.Core;

/// <summary>
/// Managerwechsel am Saisonende als Ereignis im Spielkern (R12U-02, #538; AGENTS.md §4): Bildschirme setzen
/// <see cref="CurrentManager"/> nicht beim Öffnen oder Zeichnen, sondern lösen diese Methoden aus ihrer Eingabe aus.
/// </summary>
/// <remarks>
/// [ORIGINAL] Das Saisonende läuft im Original je Manager in einer Schleife (em.exe Datei 0x26FB0–0x279FA Prämien/Altern,
/// ab 0x27B64 die Verhandlungen); das Fenster „Ereignisse“ folgt Datei 0x5645D → 0x26F20.
/// [APPROX] Wo das Original den Zeiger auf den Manager am Zug umstellt, ist nicht einzeln belegt; das Remake stellt ihn
/// beim Wechsel zum jeweiligen Manager um.
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
    }
}
