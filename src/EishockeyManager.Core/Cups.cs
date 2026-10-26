namespace EishockeyManager.Core;

/// <summary>
/// K.-o.-Duell zweier Vereine im Pokal: wer zuerst <see cref="WinsNeeded"/> Spiele gewinnt (höchstens drei Spiele).
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe, Auswertung eines Pokaltermins (Datei 0x34B76): Siege je Paarung werden gezählt; hat nach dem
/// ersten oder zweiten Spiel ein Verein zwei Siege, wird das nächste Spiel als „nicht auszutragen“ (Wert 100)
/// markiert. Nach jedem Spiel tauschen die beiden Vereine ihre Plätze – das Heimrecht wechselt also von Spiel zu
/// Spiel: Spiele 1 und 3 beim <see cref="First"/>, Spiel 2 beim <see cref="Second"/>. Im Video (Karriere 1994)
/// belegt: 22.9. Heimspiel gegen SB Rosenheim, 24.9. Auswärtsspiel, nach zwei Niederlagen kein dritter Termin.
/// </remarks>
public sealed class CupTie(int first, int second)
{
    public const int WinsNeeded = 2;
    public const int MaxGames = 3;

    /// <summary>Verein mit Heimrecht im ersten und dritten Spiel (in der Auslosung links).</summary>
    public int First { get; } = first;

    /// <summary>Verein mit Heimrecht im zweiten Spiel (in der Auslosung rechts).</summary>
    public int Second { get; } = second;

    public List<KnockoutMatch> Games { get; } = [];

    public int Wins(int club) => Games.Count(g => g.Winner == club);
    public bool IsDecided => Wins(First) >= WinsNeeded || Wins(Second) >= WinsNeeded;
    public int? Winner => Wins(First) >= WinsNeeded ? First : Wins(Second) >= WinsNeeded ? Second : null;
    public int? Loser => Winner is { } w ? (w == First ? Second : First) : null;
    public bool Involves(int club) => First == club || Second == club;

    /// <summary>Heim- und Gastverein im Spiel mit der Nummer <paramref name="game"/> (0–2).</summary>
    public static (int Home, int Away) Pairing(CupTie tie, int game) =>
        game % 2 == 0 ? (tie.First, tie.Second) : (tie.Second, tie.First);

    /// <summary>Wird an Termin <paramref name="game"/> (0–2) dieser Runde noch gespielt?</summary>
    public bool PlaysGame(int game) => !IsDecided && Games.Count == game;
}

/// <summary>Spielklasse eines Vereins im Sinne des Originals (für Heimrecht und Farben der Auslosung).</summary>
public static class CupClass
{
    /// <summary>
    /// [ORIGINAL] em.exe Datei 0x34FD5: 1 = Bundesliga (und alle Vereine ab Index 53 = DGROUP 0x6AD + 1), 2 = 2. Liga, 4 = sonst
    /// (Oberliga, ligalos). Im Remake nach der aktuellen Liga statt nach dem Datenbankindex.
    /// </summary>
    public static int Of(Club club) => club.League switch
    {
        League.Bundesliga => 1,
        League.ZweiteLiga => 2,
        null when club.Index > NationalTeams.First => 1,
        _ => 4,
    };

    /// <summary>Kennziffer hinter dem Vereinsnamen in der Auslosung: „(1)“, „(2)“, „(3)“.</summary>
    public static int Digit(Club club) => (Of(club) >> 1) + 1;
}

/// <summary>Bereich der Nationalmannschaften in der Datenbank (DGROUP 0x6AD = 52 … 63).</summary>
public static class NationalTeams
{
    public const int First = 52;
    public const int Last = 63;

    /// <summary>Deutschland (Datenbank 56).</summary>
    public const int Germany = 56;
}
