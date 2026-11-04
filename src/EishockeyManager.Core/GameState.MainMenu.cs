namespace EishockeyManager.Core;

/// <summary>
/// Spielstand – Spur ui-meldungen, Teil 4 (E4 = B, Entscheidung Nutzer 03.11.2026): Kennzeichen „neue Nachricht“ je Manager.
/// </summary>
public sealed partial class Manager
{
    /// <summary>
    /// [ORIGINAL] Kennzeichen „neue Nachricht“ DGROUP 0x5A50 + Manager: gesetzt beim Ablegen jeder Nachricht (Datei 0x4B5E6),
    /// gelöscht beim Aufbau des Hauptmenüs, das dann die Nachrichtenleiste zeigt (Datei 0x4527C–0x45291).
    /// </summary>
    private bool _newMessage;

    /// <summary>Liegt eine neue Nachricht vor, die das nächste Hauptmenü mit der Nachrichtenleiste meldet?</summary>
    public bool HasNewMessage => _newMessage;

    internal void MarkNewMessage() => _newMessage = true;

    internal bool TakeNewMessage()
    {
        var had = _newMessage;
        _newMessage = false;
        return had;
    }
}

/// <summary>
/// Spielstand – Spur ui-meldungen, Teil 4: was das Hauptmenü des Originals beim Aufbau am Spielzustand ändert
/// (Hauptmenü Datei 0x44E22, Prüfung <c>docs/analyse/re-runde-12.md</c> E4). Nach Entscheidung E4 = B als Ereignis im Spielkern:
/// Der Zug beginnt in <see cref="BeginManagerTurn"/> (Funktionsanfang 0x44E22), jeder Aufbau des Menüs meldet sich über
/// <see cref="OnMainMenu"/> (Datei 0x44FBD); der Bildschirm zeichnet nur.
/// </summary>
public sealed partial class GameState
{
    private GameRandom? _turnRandom;

    /// <summary>Gezogener Faktor der Gehaltsforderung des laufenden Managerzugs (85–115); 0 = noch nicht gezogen.</summary>
    private int _contractTurnFactor;

    /// <summary>
    /// Zufallsquelle des Faktors je Managerzug (<see cref="ContractRules.TurnFactor"/>). [APPROX] Das Original zieht aus demselben
    /// Generator wie alles andere (0x0C037); das Remake hält den Zug aus der Hauptquelle heraus, damit Spiele, Markt und Proben
    /// ihre Folgen behalten (wie <see cref="TrendRandom"/>). Ältere Spielstände beginnen die Quelle beim ersten Zug.
    /// </summary>
    public GameRandom TurnRandom
    {
        get => _turnRandom ??= new GameRandom(unchecked(Season * 1_000_003 + Managers.Sum(m => m.ClubIndex * 41 + m.Head) + 29));
        init => _turnRandom = value;
    }

    /// <summary>
    /// [ORIGINAL] Funktionsanfang des Hauptmenüs (Datei 0x44E3F/0x44E46), einmal je Managerzug: DGROUP 0xE1A4 = random(0x55, 0x73).
    /// Nach dem Laden (Einstieg Datei 0x44E6D) wird nicht neu gezogen.
    /// </summary>
    private void DrawContractTurnFactor() => _contractTurnFactor = ContractRules.TurnFactor(TurnRandom);

    /// <summary>
    /// Erster Managerzug eines neuen Spiels. [ORIGINAL] Der erste Menüaufbau beginnt am Funktionsanfang 0x44E3F und zieht den Faktor
    /// der Gehaltsforderung wie jeder Zug (R12U-04, #540); im Remake lief bisher nur <see cref="BeginManagerTurn"/> aus
    /// <see cref="StartTurns"/> und <see cref="PassTurn"/>, der erste Tag eines neuen Spiels nutzte den abgeleiteten Ersatzwert.
    /// Zieht nur aus <see cref="TurnRandom"/> (keine andere Zufallsquelle, keine Aufstellung: die macht der erste Aufbau des Menüs).
    /// </summary>
    internal void BeginFirstTurn() => DrawContractTurnFactor();

    /// <summary>
    /// Das Hauptmenü wird für <paramref name="manager"/> aufgebaut (erster Aufbau eines Zugs, Rückkehr aus einem Unterbildschirm,
    /// Laden). [ORIGINAL] Datei 0x44FBD ff.: Neuaufstellung 0x426BC mit „automatisch aufstellen“ (Datei 0x450C6, bei jedem Aufbau,
    /// nicht nur am Zuganfang) und Kennzeichen „neue Nachricht“ (Datei 0x4527C–0x45291): ist es gesetzt, wird es gelöscht und die
    /// Leiste gezeigt. Liefert, ob die Nachrichtenleiste erscheint. Genau einmal je Aufbau aufrufen; Zeichnen ändert nichts.
    /// </summary>
    /// <param name="manager">Manager am Zug.</param>
    /// <param name="isReturn">
    /// Rückkehr ins Menü desselben Zugs (Datei 0x45961/0x4596C → 0x44FBD) statt erster Aufbau. Beide Wege laufen ab 0x44FBD gleich;
    /// der Faktor des Zugs (0x44E3F) ist schon in <see cref="BeginManagerTurn"/> gezogen. Nur zur Nachvollziehbarkeit.
    /// </param>
    public bool OnMainMenu(Manager manager, bool isReturn)
    {
        if (!Managers.Contains(manager))
            throw new ArgumentException("Manager gehört nicht zu diesem Spiel.", nameof(manager));
        _ = isReturn;
        Clubs[manager.ClubIndex].ApplyAutoLineup();
        return manager.TakeNewMessage();
    }
}
