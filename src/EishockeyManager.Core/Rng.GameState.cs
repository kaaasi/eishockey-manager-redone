namespace EishockeyManager.Core;

/// <summary>
/// Zustand aller Zufallsquellen eines Spielstands. Die drei verzögert erzeugten Quellen sind <c>null</c>, solange
/// sie noch nicht erzeugt wurden – sie werden dann beim ersten Zugriff wie bisher aus dem Saison-Seed gebildet.
/// </summary>
public sealed record RngSnapshot(RngState Random, RngState? Finance, RngState? Cup, RngState? Contract);

/// <summary>Spielstand – Teil der Spur „speichern“: Sichern und Wiederherstellen der Zufallsquellen.</summary>
public sealed partial class GameState
{
    /// <summary>Kopie des Zustands von <see cref="Random"/>, <see cref="FinanceRandom"/>, <see cref="CupRandom"/>
    /// und <see cref="ContractRandom"/>. Erzeugt keine noch nicht erzeugte Quelle (zustandsrein).</summary>
    public RngSnapshot CaptureRng() =>
        new(Random.Capture(), _financeRandom?.Capture(), _cupRandom?.Capture(), _contractRandom?.Capture());

    /// <summary>
    /// Setzt alle Zufallsquellen auf den gesicherten Zustand. <see cref="Random"/> wird an Ort und Stelle
    /// zurückgesetzt (gleiche Instanz); eine im Abbild fehlende Quelle gilt danach wieder als „noch nicht erzeugt“.
    /// </summary>
    public void RestoreRng(RngSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Random.Restore(snapshot.Random);
        _financeRandom = snapshot.Finance is { } finance ? GameRandom.FromState(finance) : null;
        _cupRandom = snapshot.Cup is { } cup ? GameRandom.FromState(cup) : null;
        _contractRandom = snapshot.Contract is { } contract ? GameRandom.FromState(contract) : null;
    }
}
