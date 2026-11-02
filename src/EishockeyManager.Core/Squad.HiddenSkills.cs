namespace EishockeyManager.Core;

/// <summary>
/// Kaderplatz +0x19: unteres Halbbyte = Unterzahl (<see cref="Player.HiddenSkill1"/>), oberes = Überzahl
/// (<see cref="Player.HiddenSkill2"/>), je 0 … 15. Analyse: docs/analyse/re-nachtrag.md, Abschnitt 6 (A185, A186).
/// </summary>
/// <remarks>
/// [ORIGINAL] Schreiber: Spielstart Managerkader Datei 0x299F4 und Managerkader-Neuanlage in Datei 0x5AF50 (Datei 0x5B239,
/// 0x5B262), Talent Datei 0x06F23, Listenwechsel auf den Transfermarkt Datei 0x49803 – je (random(3, 12) &lt;&lt; 4) |
/// random(3, 12), das obere Halbbyte zuerst gezogen; Kader des Nationalteams Datei 0x3CB2C = 0x77; Training Datei 0x06B01
/// (0 … 15, <see cref="TrainingRules.Apply"/>). Der Kauf vom Markt kopiert den ganzen Kaderplatz (Datei 0x478DB–0x47907):
/// der Wert wandert mit. Beleg Spielstände: FILE01/09/10/12 alle Plätze 3 … 12.
/// [APPROX] Im Original gehört der Wert zum Kaderplatz, im Remake zum Spieler. Gewürfelt wird deshalb beim Eintritt in
/// einen Managerkader (Spielstart, Talent) bzw. in den Markt; Länderspiele rechnen mit 7/7 (<see cref="ForMatch"/>), ohne
/// den Vereinswert zu ändern. Computerspieler haben im Original keinen Wert; im Remake behalten sie den Vorgabewert 7, der
/// nur in Spielen von Managervereinen wirkt.
/// </remarks>
public static class HiddenSkills
{
    /// <summary>Grenzen des Startwurfs je Halbbyte. [ORIGINAL] random(3, 12), siehe oben.</summary>
    public const int StartMin = 3, StartMax = 12;

    /// <summary>Wert je Halbbyte im Kader des Nationalteams. [ORIGINAL] Datei 0x3CB2C: 0x77.</summary>
    public const int NationalTeam = 7;

    /// <summary>Größter Wert je Halbbyte (Training). [ORIGINAL] Datei 0x4BD35: 0 … 15.</summary>
    public const int Max = 15;

    /// <summary>
    /// Startwurf eines Kaderplatzes: erst Überzahl (oberes Halbbyte), dann Unterzahl. [ORIGINAL] Datei 0x299B7–0x299F4.
    /// </summary>
    public static void Roll(Player p, Random random)
    {
        ArgumentNullException.ThrowIfNull(p);
        p.HiddenSkill2 = TransferRules.Random16(random, StartMin, StartMax);
        p.HiddenSkill1 = TransferRules.Random16(random, StartMin, StartMax);
    }

    /// <summary>
    /// Werte im Spiel: (Überzahl, Unterzahl). Im Länderspiel des Bundestrainers 7/7 (Satz 5, Datei 0x3CB2C), sonst die des
    /// Spielers.
    /// </summary>
    public static (int Overplay, int Underplay) ForMatch(Player p, bool nationalTeam) =>
        nationalTeam ? (NationalTeam, NationalTeam) : (p.HiddenSkill2, p.HiddenSkill1);

    /// <summary>
    /// Zufallsquelle für die Startwürfe, abgeleitet aus dem Zustand von <see cref="GameState.Random"/>, ohne ihn zu verbrauchen.
    /// [APPROX] Das Original zieht aus seinem einzigen Generator; eine eigene Quelle hält die Zufallsfolgen von Kaderaufbau,
    /// Finanzen und Verträgen unverändert (wie <see cref="TransferMarket.ReservePool"/>).
    /// </summary>
    public static GameRandom SourceFor(GameState game, int salt)
    {
        var state = game.Random.Capture();
        var hash = unchecked(state.Next * 31 + state.NextP);
        foreach (var value in state.SeedArray)
            hash = unchecked(hash * 16_777_619 ^ value);
        return new GameRandom(unchecked(hash * 7_919 + salt) & int.MaxValue);
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Spielstart: Startwürfe +0x19 für alle Spieler der Managerkader in Kaderreihenfolge. [ORIGINAL] Datei 0x299F4 (Managerkader
    /// beim Spielstart). [APPROX] Eigene Zufallsquelle (<see cref="HiddenSkills.SourceFor"/>).
    /// </summary>
    internal void RollStartHiddenSkills()
    {
        var random = HiddenSkills.SourceFor(this, 0x19);
        foreach (var index in Managers.Select(m => m.ClubIndex).Distinct())
            foreach (var p in Clubs[index].Players)
                HiddenSkills.Roll(p, random);
    }
}
