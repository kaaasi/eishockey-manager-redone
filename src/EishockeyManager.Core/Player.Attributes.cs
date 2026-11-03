namespace EishockeyManager.Core;

/// <summary>Status eines Spielers in den Listen „Aufstellung“, „Verträge“ (Spalte STATUS).</summary>
public enum PlayerStatus
{
    /// <summary>„IM TEAM“ – in der Aufstellung.</summary>
    InTeam,

    /// <summary>„RESERVE“ – im Kader, aber nicht aufgestellt.</summary>
    Reserve,

    /// <summary>„VERL.“ – verletzt.</summary>
    Injured,

    /// <summary>„GESP.“ – gesperrt.</summary>
    Suspended,
}

/// <summary>Tendenz eines Spielers („Fallend.“, „Konstant.“, „Steigend.“).</summary>
public enum PlayerTrend
{
    Falling,
    Constant,
    Rising,
}

/// <summary>
/// Spielerwerte nach der Spielerstruktur des Originals (33 Byte, siehe docs/reverse-engineering.md):
/// Talent 0–9, Alter, Kondition, Technik, Form, dazu die in den Listen sichtbaren Spalten
/// SP (Spiele), TO (Tore), 2m/5m (Strafen), STATUS, E (Energie), V.DAUER und Gehalt.
/// </summary>
public sealed partial class Player
{
    public const int MaxValue = 99;

    /// <summary>Rückennummer (Spalte NR).</summary>
    public int Number { get; set; }

    public int Age { get; set; }

    /// <summary>Verstecktes Talent 0–9 (Spielerfeld 0x12, random(0..9)).</summary>
    public int Talent { get; set; }

    /// <summary>Kondition (KO) – erste Zahl unter STÄRKEN.</summary>
    public int Condition { get; set; }

    /// <summary>Technik (TE) – zweite Zahl unter STÄRKEN.</summary>
    public int Technique { get; set; }

    /// <summary>Form (FO) – dritte Zahl unter STÄRKEN.</summary>
    public int Form { get; set; }

    /// <summary>
    /// Belastung (Kaderplatz +0x13, ein Byte; 50 = frisch … 150 = erschöpft). [ORIGINAL] Schreiber in em.exe: neuer
    /// Kaderplatz random(80, 120) (Datei 0x42E4C–0x42E5E), Training − Erholung, danach 50 … 150 (Datei 0x6B08–0x6B1A),
    /// Spielminute + 1 bei lrandom(0, 4 + (Kondition &gt; Technik)) = 0 (Datei 0x8C82, Byte), nach dem Spiel eines
    /// Managervereins 50 … 150 (Datei 0x549A2–0x549BB), „WM Vorbereitung“ 70 (Datei 0x3BB51), Transferliste − 10 über 56
    /// (Datei 0x71BB). Leser: Spielvorbereitung 0x84D7 (101 − Belastung/8), Trainingswirkung 0x659C, Spalte E der Listen
    /// (Belastung · 10 / 15, Datei 0x4235E), Hinweiszeile „Erschöpfung“ (Datei 0x41B29). A184, Runde 12.
    /// </summary>
    public int Strain { get; set; } = TrainingRules.DefaultStrain;

    /// <summary>
    /// Energie 0–100 des Remakes, aus der Belastung abgeleitet (150 − <see cref="Strain"/>). [APPROX] Übergang bis die
    /// Aufrufer in fremden Dateien (Anzeigen, neue Kaderplätze) die Belastung direkt lesen und schreiben (A184, Liste im
    /// PR der Spur wettbewerbe-spiel Teil 2); wird nicht gespeichert.
    /// </summary>
    public int Energy
    {
        get => Math.Clamp(150 - Strain, 0, 100);
        set => Strain = 150 - value;
    }

    /// <summary>Spiele gesamt (Liga und Pokal), Spalte SP.</summary>
    public int Games { get; set; }

    /// <summary>Tore gesamt (Liga und Pokal), Spalte TO (beim Torhüter „–“).</summary>
    public int Goals { get; set; }

    /// <summary>Vorlagen (für Scorerlisten).</summary>
    public int Assists { get; set; }

    /// <summary>Anzahl 2-Minuten-Strafen.</summary>
    public int Penalties2 { get; set; }

    /// <summary>Anzahl 5-Minuten-Strafen.</summary>
    public int Penalties5 { get; set; }

    /// <summary>
    /// Gegentore als Torhüter. [ORIGINAL] Beim Torhüter zählt das Original im Spielerfeld 0x1B (bei Feldspielern die
    /// Tore) die Gegentore, Torhüter-Rangliste Datei 0x25FC7 (Spur spielberechnung, docs/spielberechnung.md).
    /// </summary>
    public int GoalsAgainst { get; set; }

    /// <summary>
    /// Spielminuten als Torhüter. [ORIGINAL] Spielerfeld 0x1F (Wort; bei Feldspielern die Vorlagen), Rangliste nur ab
    /// 400 Minuten (Datei 0x25FC7: Vergleich mit 0x18F).
    /// </summary>
    public int GoalieMinutes { get; set; }

    /// <summary>Verbleibende Spiele, die der Spieler verletzt fehlt (0 = gesund).</summary>
    public int InjuredGames { get; set; }

    /// <summary>Verbleibende Spiele einer Sperre (0 = spielberechtigt).</summary>
    public int SuspendedGames { get; set; }

    /// <summary>Länderspiele (Detailansicht „Länderspiele:“).</summary>
    public int Caps { get; set; }

    /// <summary>
    /// Stufe der Tendenz („Fallend.“, „Konstant.“, „Steigend.“). Eine Stufe, die nicht zu <see cref="TrendScore"/> passt, löscht
    /// den Wert (von Hand gesetzte Stufe gilt, Spur kader-transfer, Teil 4).
    /// </summary>
    public PlayerTrend Trend
    {
        get;
        set
        {
            field = value;
            if (TrendScore != 0 && TrendOf(TrendScore) != value)
                TrendScore = 0;
        }
    } = PlayerTrend.Constant;

    /// <summary>
    /// Stärke = Mittel aus Kondition, Technik und Form, abgerundet. Belegt im Video:
    /// 33/30/41 → ST 34 (Verträge), 16/20/53 → „Stärke:29“ (Transferliste).
    /// </summary>
    public int Strength => (Condition + Technique + Form) / 3;

    /// <summary>Kann der Spieler eingesetzt werden?</summary>
    public bool Available => InjuredGames == 0 && SuspendedGames == 0;

    /// <summary>Kurzbezeichnung der Spalte ART: „TOR“, „ABW“, „STU“.</summary>
    public string RoleShort => Role switch
    {
        Formats.PlayerRole.Goalkeeper => "TOR",
        Formats.PlayerRole.Defender => "ABW",
        _ => "STU",
    };

    /// <summary>Ein Buchstabe wie in der Transferliste: „T“, „A“, „S“.</summary>
    public char RoleLetter => RoleShort[0] == 'A' ? 'A' : RoleShort[0] == 'T' ? 'T' : 'S';

    /// <summary>Bezeichnung für die Statuszeile: „Torwart“, „Abwehrspieler“, „Stürmer“.</summary>
    public string RoleName => Role switch
    {
        Formats.PlayerRole.Goalkeeper => "Torwart",
        Formats.PlayerRole.Defender => "Abwehrspieler",
        _ => "Stürmer",
    };

    public static int Clamp(int value) => Math.Clamp(value, 1, MaxValue);
}
