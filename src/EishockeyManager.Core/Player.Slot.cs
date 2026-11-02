namespace EishockeyManager.Core;

/// <summary>
/// Felder des Kaderplatzes (0x34 Byte, DGROUP 0x5DC3 + 0x618 · Manager), die das Spielerdaten-Fenster 0x041AC und die
/// Hinweiszeile 0x41683 zeigen (Spur kader-transfer, Teil 2; Fundstellen in <c>docs/transfer.md</c>, „Spielerdaten im
/// Modell“).
/// </summary>
public sealed partial class Player
{
    /// <summary>
    /// Kaufpreis in DM (Kaderplatz +0x2C ohne Bit 31, im Original Drittel-DM; 0 = unbekannt, Anzeige „-“).
    /// [ORIGINAL] Spielstart Datei 0x29EB4 (0x28165): Wert(Preis) (0x118:0x70 Art 0) je Kaderplatz des Managers; Kauf und Leihe
    /// Datei 0x4781B–0x47833: das angenommene Gebot (bei der Leihe die Leihgebühr), ohne Bit 31 (Versicherung,
    /// <see cref="InsuranceLocked"/>) zu ändern; Anzeige 0x041AC Feld 0 (Drittel / 3 + „ DM“).
    /// [APPROX] Nur Spieler der Manager tragen den Preis (Computervereine haben im Original keinen Kaderplatz); Spielstände
    /// vor Schema 23 kennen ihn nicht (0).
    /// </summary>
    public long PurchasePrice { get; set; }

    /// <summary>
    /// Art der letzten Verletzung 0 … 27 (Kaderplatz +0x17, Namen <see cref="InjuryKinds.Names"/>) oder
    /// <see cref="InjuryKinds.Unknown"/>. [ORIGINAL] Datei 0x05B1A schreibt die gezogene Art; die Hinweiszeile 0x41683
    /// zeigt sie bei Status „verletzt“ als „Verletzt.(Art)“ (Versätze 0x3D4–0x421). Die Heilung 0x7C12 lässt das Feld stehen.
    /// </summary>
    public int InjuryKind { get; set; } = InjuryKinds.Unknown;

    /// <summary>Name der Verletzungsart oder null, wenn die Art unbekannt ist.</summary>
    public string? InjuryName => InjuryKinds.Name(InjuryKind);
}

/// <summary>Verletzungsarten des Originals (Kaderplatz +0x17).</summary>
public static class InjuryKinds
{
    /// <summary>Art nicht bekannt (Spielstände vor Schema 23, noch nie verletzt). [APPROX] Das Original hat kein solches Merkmal (0).</summary>
    public const int Unknown = -1;

    /// <summary>
    /// [ORIGINAL] Namen der 28 Arten, Zeigertabelle DGROUP 0xA00 (je 4 Byte; Zeichensatz des Spiels: „{“ = ä, „}“ = ü,
    /// „~“ = ß). Index wie die Tabellen 0x181/0x1B9 der Verletzung 0x05B1A.
    /// </summary>
    public static readonly string[] Names =
    [
        "Schädelbruch", "Kahnbeinbruch", "Gehirnerschütterung", "Fingerbruch", "Kieferbruch", "Meniskuskorbhenkelriß",
        "Zehenbruch", "Kniescheibenbruch", "Oberschenkelzerrung", "Augenverletzung", "Wirbelsäulenverdrehung",
        "Achillessehnenriß", "Bänderdehnung", "Mittelfußknochenbruch", "Lungenriß", "Fersenbruch", "Handrückenbruch",
        "Handrückenverletzung", "Schultergelenkprellung", "Nierenriß", "Rippenprellung", "Rippenbruch",
        "Schilddrüsenerkrankung", "Blutvergiftung", "Lebererkrankung", "Blinddarmoperation", "Mandeloperation",
        "Darmerkrankung",
    ];

    /// <summary>Name der Art oder null außerhalb 0 … 27.</summary>
    public static string? Name(int kind) => kind >= 0 && kind < Names.Length ? Names[kind] : null;
}
