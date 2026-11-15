using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Datenbanksatz der Kennung eines Vereins (Name, Logo, Farbcode; #857). [ORIGINAL] Der Platztausch 0x36169 mit Schalter 0x80
/// tauscht im Vereinssatz Name, Manager, Logo +0x34 und Farbcode +0x3B (Datei 0x3628F–0x363F6); im Remake wandert dafür das
/// <see cref="Club"/>-Objekt mit Name und Logografik, der Index bleibt am Platz (<c>GameState.SwapClubPlaces</c>). Die Kennung ist
/// deshalb der Datenbanksatz mit dem Namen des Vereins – Vereinsnamen sind in <c>kb\23</c> eindeutig und ändern sich nicht
/// (der Import ordnet die Sätze ebenso über den Namen zu, <c>OriginalImport</c>).
/// </summary>
public static class ClubIdentity
{
    /// <summary>Index des Datenbanksatzes, dessen Kennung der Verein trägt; ohne Treffer der eigene Index.</summary>
    public static int Of(ClubDatabase database, Club club)
    {
        if (club.Index < database.Clubs.Count && database[club.Index].Name == club.Name)
            return club.Index;
        foreach (var record in database.Clubs)
            if (record.Name == club.Name)
                return record.Index;
        return club.Index;
    }
}
