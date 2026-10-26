namespace EishockeyManager.Audio;

/// <summary>
/// Soundeffekte des Originals. Der Zahlenwert ist der Index, mit dem die Torszenen (<c>tore\*</c>) einen Effekt
/// auslösen; die Datei ist kb\(237 + Index).
/// </summary>
/// <remarks>
/// [ORIGINAL] Belegt im Disassembly der Torszene (Overlay ab Datei 0x56800): Eine Schleife lädt kb\(231 + i)
/// für i = 0–13 in die Zeigertabelle DGROUP 0xE2B9 (Ausnahmen: i = 5 Hallenkarte 242–245, i = 12/13 → 246/247),
/// die Effekte stehen also ab 0xE2D1 = Eintrag 6 = kb\237. Jedes 28-Byte-Bild der Szene (ab Byte 0x54) nennt in
/// Byte 3 den Effekt (0 = keiner); die Szene beendet den laufenden Effekt und spielt den neuen über CT-VOICE
/// (Funktion 6, Aufruf 99F:000A). Ist die Ausgabe frei (Status DGROUP 0x59E4 = 0), startet sie 237 neu.
/// Häufigkeit in den 49 Szenen: Index 3 = 149×, 4 = 53×, 1 = 25× (24× am Ende der Szenen mit Treffer *.at/*.ht, einmal in 4.hv), 2 = nie.
/// Die Namen folgen dem Klang und passen zu dieser Verwendung.
/// </remarks>
public enum SoundEffect
{
    /// <summary>kb\237: Publikum, Endlosschleife (VOC-Wiederholung 0xFFFF); läuft, solange nichts anderes spielt.</summary>
    Crowd = 0,

    /// <summary>kb\238: Jubel, fast nur am Ende der Torszenen mit Treffer.</summary>
    Cheer = 1,

    /// <summary>kb\239: Pfiff. In den Torszenen nie benutzt; weitere Aufrufe der Abspielfunktion 99F:000A gibt es in em.exe nicht (nur über Eintrag 0x38, alle in der Torszene) – im Original also nie zu hören.</summary>
    Whistle = 2,

    /// <summary>kb\240: Puck (Pass, Schuss), häufigster Effekt der Szenen.</summary>
    Puck = 3,

    /// <summary>kb\241: Check bzw. Abwehr, in den Szenen ohne Treffer oft gegen Ende.</summary>
    Check = 4,
}

public static class SoundEffects
{
    public const int FirstFileNumber = 237;

    public static IReadOnlyList<SoundEffect> All { get; } = Enum.GetValues<SoundEffect>();

    public static int FileNumber(this SoundEffect effect) => FirstFileNumber + (int)effect;

    /// <summary>Effekt zum Byte 3 eines Torszenenbildes; 0 = kein Effekt (der Publikumslärm läuft ohnehin).</summary>
    public static SoundEffect? FromSceneIndex(int index) =>
        index is > 0 and <= (int)SoundEffect.Check ? (SoundEffect)index : null;
}
