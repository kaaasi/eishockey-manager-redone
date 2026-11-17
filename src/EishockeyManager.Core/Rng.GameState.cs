namespace EishockeyManager.Core;

/// <summary>
/// Zustand aller Zufallsquellen eines Spielstands. Die verzögert erzeugte Quelle der Computerkader ist <c>null</c>, solange
/// sie noch nicht erzeugt wurde – sie wird dann beim ersten Zugriff wie bisher aus dem Saison-Seed gebildet.
/// </summary>
public sealed record RngSnapshot(RngState Random, RngState? ComputerSquads);

/// <summary>Spielstand – Teil der Spur „speichern“: Sichern und Wiederherstellen der Zufallsquellen.</summary>
public sealed partial class GameState
{
    /// <summary>Kopie des Zustands von <see cref="Random"/> und <see cref="ComputerSquadRandom"/>. Erzeugt keine noch nicht erzeugte Quelle (zustandsrein).</summary>
    public RngSnapshot CaptureRng() =>
        new(Random.Capture(), _contractRandom?.Capture());

    /// <summary>
    /// Setzt alle Zufallsquellen auf den gesicherten Zustand. <see cref="Random"/> wird an Ort und Stelle
    /// zurückgesetzt (gleiche Instanz); eine im Abbild fehlende Quelle gilt danach wieder als „noch nicht erzeugt“.
    /// </summary>
    public void RestoreRng(RngSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Random.Restore(snapshot.Random);
        _contractRandom = snapshot.ComputerSquads is { } contract ? GameRandom.FromState(contract) : null;
    }
}
