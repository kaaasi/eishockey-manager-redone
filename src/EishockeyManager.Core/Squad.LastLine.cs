namespace EishockeyManager.Core;

public sealed partial class Player
{
    /// <summary>
    /// Kaderplatz +0x15 eines Spielers außerhalb der Aufstellung des Remakes (A939): die Reihe, die er zuletzt hatte, als ihn die
    /// Neuaufstellung oder die Verletzung aus seinem Platz nahm; 0 nach dem Leeren von Hand, als Ersatz der Neuaufstellung und auf
    /// einem neuen Kaderplatz. Steht der Spieler in der Aufstellung, gilt deren Platz (<see cref="Club.SlotLine"/>), dieses Feld nicht.
    /// [ORIGINAL] Datei 0x426BC schreibt +0x15 nur für ausgewählte Spieler (Reihe = Block + 1, Bit 0x80 Sturm; Ersatz 0, Datei
    /// 0x42744–0x42777, 0x427FD–0x42816); nicht Ausgewählte behalten ihr altes +0x15 (Beleg <c>k07_automatik.py</c>,
    /// <c>docs/archiv/analyse/re-abweichungen.md</c> K07). Die Verletzung (Datei 0x5C50) löscht nur +0x0A. Das Leeren eines Platzes von Hand
    /// schreibt +0x15 = 0 (Datei 0x40B69–0x40C98, Beleg <c>k07_sechster.py</c> a/b). Ein neuer Kaderplatz beginnt mit 0 (memset
    /// Datei 0x42E3A; der Kauf von einem Manager übernimmt +0x15 nicht, Datei 0x4787D–0x478CB).
    /// </summary>
    public int LastLine { get; set; }
}

public sealed partial class Club
{
    /// <summary>
    /// Kaderplatz +0x15 des Spielers (A939): Platz der Aufstellung, außerhalb der Aufstellung die zuletzt gehaltene Reihe
    /// (<see cref="Player.LastLine"/>). [ORIGINAL] Die Spielabrechnung liest +0x15 jedes Kaderplatzes, auch ohne +0x0A (Datei
    /// 0x548A8–0x548E7, <see cref="SquadSlotRules.RollPlayedLines"/>): ein Verletzter spielt seine alte Reihe ein.
    /// </summary>
    public int SlotLine(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var position = Lineup.PositionOf(player);
        return position != 0 ? position : player.LastLine;
    }

    /// <summary>Plätze der Aufgestellten vor einer Neuaufstellung oder Bereinigung (für <see cref="KeepLastLines"/>).</summary>
    private Dictionary<Player, int> LineupPositions() =>
        Lineup.Players.Distinct().ToDictionary(p => p, Lineup.PositionOf);

    /// <summary>
    /// Nach Neuaufstellung oder Bereinigung: wer seinen Platz verloren hat, behält die alte Reihe ([ORIGINAL] Datei 0x426BC schreibt
    /// nur Ausgewählte, die Verletzung Datei 0x5C50 nur +0x0A); Eingesetzte ohne Platz sind Ersatz der Neuaufstellung (+0x15 = 0,
    /// Datei 0x427FD–0x42816), wenn <paramref name="reservesZero"/>.
    /// </summary>
    private void KeepLastLines(Dictionary<Player, int> before, bool reservesZero)
    {
        foreach (var (player, position) in before)
            if (Players.Contains(player) && !Lineup.Contains(player))
                player.LastLine = position;
        if (!reservesZero)
            return;
        foreach (var player in Dressed)
            if (!Lineup.Contains(player))
                player.LastLine = 0;
    }

    /// <summary>
    /// Eingriff von Hand in die Aufstellung (vor jeder Änderung, <see cref="TakeOverDressed"/>): Aufgestellte führen ihre Reihe im Platz;
    /// wer jetzt von Hand herausgenommen wird, hat danach +0x15 = 0 ([ORIGINAL] Datei 0x40B69–0x40C98).
    /// </summary>
    private void ForgetLastLinesOfLineup()
    {
        foreach (var player in Lineup.Players)
            player.LastLine = 0;
    }

    /// <summary>
    /// Bereinigung des Tageslaufs (<c>GameState.CleanLineups</c>): Verletzte und Abgänge aus der Aufstellung nehmen; der Verletzte behält
    /// +0x15 (A939, [ORIGINAL] Datei 0x5C50 löscht nur +0x0A).
    /// </summary>
    internal void RemoveUnavailableFromLineup()
    {
        var before = LineupPositions();
        Lineup.RemoveUnavailable(Players);
        KeepLastLines(before, reservesZero: false);
    }
}
