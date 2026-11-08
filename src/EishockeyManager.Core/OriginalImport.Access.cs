using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Ergebnis von <see cref="OriginalImport.TryImport"/>: Spielzustand oder Grund des Scheiterns.</summary>
/// <param name="Game">Importierter Spielzustand oder <c>null</c>.</param>
/// <param name="ChecksumError">Die Datei ist ein Original-Spielstand, aber eine Prüfsumme stimmt nicht („Der kleine Mogelguru“).</param>
/// <param name="Error">Fehlertext (deutsch) oder <c>null</c>.</param>
public sealed record OriginalImportResult(GameState? Game, bool ChecksumError, string? Error)
{
    public bool Success => Game is not null;
}

public static partial class OriginalImport
{
    /// <summary>Endung der Original-Spielstände (DOS, <c>*.HOK</c>).</summary>
    public const string FileExtension = ".HOK";

    /// <summary>
    /// Zugang (Spur hok-import, Teil 3): liest einen Original-Spielstand und importiert ihn, ohne Ausnahmen nach außen.
    /// Eine falsche Prüfsumme bricht ab wie im Original.
    /// <para>
    /// [ORIGINAL] KAPUTT.HOK (FILE03 mit falscher letzter Prüfsumme) zeigt beim Laden im Original den Bildschirm „Der kleine
    /// Mogelguru“ (Fehlercode 5, em.exe Anzeige ab Datei 0x5CEFA; <c>docs/formate/spielstand-original.md</c>).
    /// [APPROX] Was das Original danach tut, ist nicht belegt (Offene Punkte dort); hier wird nichts geladen, der laufende Stand
    /// bleibt – wie bei jedem anderen Ladefehler.
    /// </para>
    /// </summary>
    public static OriginalImportResult TryImport(ReadOnlySpan<byte> data, ClubDatabase database, int seed)
    {
        throw new NotImplementedException();
    }
}
