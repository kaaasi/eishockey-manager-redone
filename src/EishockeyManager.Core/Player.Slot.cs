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

    /// <summary>
    /// Tendenz als Wert des Kaderplatzes +0x0E (30 … 68; 0 = nicht bekannt, dann gilt die Stufe <see cref="Trend"/>).
    /// [ORIGINAL] Start random(35, 65) in Datei 0x4907A (<see cref="RollTrend"/>); Eingang des Spielerwerts (0x4A0A7), Anzeige
    /// (Wert − 30) / 13 (0x41AE0), Balken (Wert − 30) · 100 / 38. Spur kader-transfer, Teil 4 (F113).
    /// </summary>
    public int TrendScore { get; set; }

    /// <summary>
    /// Steuerbyte der Tendenz, Kaderplatz +0x14: Bits 0–3 Tage bis zum neuen Zug (1 … 13), Bits 4–6 Schritt (1 … 3), Bit 7
    /// Richtung (1 = steigend). [ORIGINAL] gezogen in Datei 0x4907A; gelesen im Tagesablauf 0x07169 (Datei 0x7B64–0x7BF8, siehe
    /// <c>docs/transfer.md</c>); seit #688 (R688-07, A134) je Takt angeschlossen (<see cref="GameState.AdvanceTrend"/>).
    /// </summary>
    public int TrendControl { get; set; }

    /// <summary>
    /// Übertrag für <see cref="GamesForClub"/>: Spiele beim jetzigen Verein aus abgelaufenen Saisons, vermindert um die Spiele, die
    /// <see cref="Games"/> beim Eintritt schon zählte (kann innerhalb einer Saison negativ sein). Spur kader-transfer, Teil 5 (A226).
    /// </summary>
    public int ClubGamesCarry { get; set; }

    /// <summary>
    /// Spiele für den jetzigen Verein (Kaderplatz +0x1C, Wort; Gewicht 50 im Spielerwert). [ORIGINAL] erhöht mit den Saisonspielen
    /// +0x06 (Datei 0x41613/0x41622, 0x54973/0x5499E), nach einem Kauf 0 (neuer Kaderplatz, FILE07), am Saisonende **nicht**
    /// zurückgesetzt – Datei 0x27A0C–0x27AA8 löscht nur +0x00, +0x01, +0x03–+0x05, +0x06–+0x08. Nur Ligaspiele (A135; Pokal
    /// +0x1E, Europapokal +0x20: <see cref="ClubGames"/>).
    /// </summary>
    public int GamesForClub => ClubGames(CompetitionSlot.League);

    /// <summary>Stufe zu einem Tendenzwert. [ORIGINAL] (Wert − 30) / 13, Datei 0x41AE0 (Texte DGROUP 0xB8C).</summary>
    public static PlayerTrend TrendOf(int score) => (PlayerTrend)Math.Clamp((score - 30) / 13, 0, 2);

    /// <summary>Setzt Tendenzwert und Steuerbyte und die passende Stufe.</summary>
    public void SetTrendScore(int score, int control)
    {
        TrendScore = score;
        TrendControl = control;
        Trend = TrendOf(score);
    }

    /// <summary>
    /// Nachbearbeitung eines neuen Kaderplatzes. [ORIGINAL] Datei 0x4907A (Stub 0x118:0x4D, gerufen aus „Kaderplatz anlegen“
    /// 0x42CB1 an 0x42E6B nach der Energie): +0x0E = random(35, 65); +0x14 = random(1, 13), dann | random(1, 3) &lt;&lt; 4, dann
    /// | random(0, 1) &lt;&lt; 7.
    /// </summary>
    public void RollTrend(Random random)
    {
        var score = TransferRules.Random16(random, 35, 65);
        var control = TransferRules.Random16(random, 1, 13);
        control |= TransferRules.Random16(random, 1, 3) << 4;
        control |= TransferRules.Random16(random, 0, 1) << 7;
        SetTrendScore(score, control);
    }
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
