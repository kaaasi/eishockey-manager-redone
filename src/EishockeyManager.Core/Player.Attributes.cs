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

    /// <summary>Energie 0–100 (Spalte E). Sinkt durch Spiele und hartes Training, steigt an freien Tagen.</summary>
    public int Energy { get; set; }

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

    /// <summary>Verbleibende Spiele, die der Spieler verletzt fehlt (0 = gesund).</summary>
    public int InjuredGames { get; set; }

    /// <summary>Verbleibende Spiele einer Sperre (0 = spielberechtigt).</summary>
    public int SuspendedGames { get; set; }

    /// <summary>Länderspiele (Detailansicht „Länderspiele:“).</summary>
    public int Caps { get; set; }

    public PlayerTrend Trend { get; set; } = PlayerTrend.Constant;

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
