using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Ergebnis einer Änderung an der Aufstellung eines Vereins oder des Nationalteams (Meldungstexte aus em.exe).</summary>
public enum LineupResult
{
    /// <summary>Spieler steht auf dem Platz.</summary>
    Assigned,

    /// <summary>„eingesetzt“ umgeschaltet (Kaderliste).</summary>
    Dressed,

    /// <summary>nicht mehr „eingesetzt“ (Kaderliste).</summary>
    Undressed,

    /// <summary>Nichts geändert (z. B. Klick auf einen aufgestellten Spieler in der Liste).</summary>
    Unchanged,

    /// <summary>Spieler ist nicht im WM-Kader.</summary>
    NotInSquad,

    /// <summary>„… ist verletzt.“</summary>
    Injured,

    /// <summary>„… noch gesperrt !“</summary>
    Suspended,

    /// <summary>„Der darf da / nicht rein !“ – falsche Art für den Platz.</summary>
    WrongRole,

    /// <summary>„Schon 22 Mann / im Team !“ – <see cref="NationalCoach.MaxDressed"/> bzw. <see cref="Club.MaxDressed"/> erreicht.</summary>
    RosterFull,
}

/// <summary>
/// Aufstellung und Taktik des Nationalteams (Spur nationalteam-2).
/// </summary>
/// <remarks>
/// Belege aus em.exe: Der Bundestrainer („Manager 5“, DGROUP 0xA70E = 5) benutzt dieselben Bildschirme wie ein Verein –
/// Aufstellung 0x108:0x2a (Datei 0x40364) und Taktik, beide aus dem Menü über 0x118:0x43 (Datei 0x48E28) mit Kennwert 0.
/// Kaderplatz 52 Byte (DGROUP 0x5DC3 + 0x618 · Manager + 0x34 · Platz, 29 Plätze):
/// <list type="bullet">
/// <item>[ORIGINAL] Feld 0x0A „eingesetzt“ (0/1), Feld 0x15 Platz in der Aufstellung (0 = keiner). „Eingesetzt: n“
///   zählt die Plätze mit Feld 0x0A ≠ 0 (0x120:0x66 Modus 3, Datei 0x4BE17; Aufruf Datei 0x41D20).</item>
/// <item>[ORIGINAL] Aufnahme in den WM-Kader nullt den Platz (Datei 0x42E2A ff.): Neue Kaderspieler sind nicht
///   eingesetzt und nicht aufgestellt. Der Kader steht nach Art sortiert (Einfügen hinter den letzten gleicher Art,
///   Datei 0x42D46 ff.).</item>
/// <item>[ORIGINAL] Auf einen Platz setzen macht den Spieler eingesetzt (Datei 0x415B2). Ein nicht eingesetzter
///   Spieler wird abgewiesen, wenn schon 22 eingesetzt sind: „Schon 22 Mann / im Team !“ (Datei 0x40E2A–0x40E61,
///   Text DGROUP 0x11D5).</item>
/// <item>[ORIGINAL] Platz leeren: Feld 0x15 = 0 und – beim Aufruf aus dem Menü (Kennwert 0) – Feld 0x0A = 0
///   (Datei 0x40CF8–0x40D0D).</item>
/// <item>[ORIGINAL] Linksklick (Mausereignis 1) auf einen eingesetzten, nicht aufgestellten Spieler nimmt ihn heraus
///   (Datei 0x40F45–0x40F59); Rechtsklick (Ereignis 3) auf einen nicht eingesetzten setzt ihn ein (Datei 0x411FE).</item>
/// <item>[ORIGINAL] In der Liste steht die Platznummer (Zeile 1–29) nur bei eingesetzten Spielern, sonst bleiben NR und
///   STATUS leer (Video `bundestrainer_00056`: 22 nummerierte Spieler, „Eingesetzt: 22“).</item>
/// <item>[APPROX] Ein vom Platz verdrängter Spieler bleibt eingesetzt (RESERVE); im Original nicht nachgeprüft.</item>
/// <item>[APPROX] Spielstärke aus Torhüter und Reihen 1–3 (16 Plätze, wie bisher), Reihe 4 zählt nicht; die Taktik
///   wirkt wie beim Verein über <see cref="Tactics.EffortStrengthBonus"/>. Die Formel des Originals ist nicht
///   rekonstruiert.</item>
/// </list>
/// </remarks>
public sealed partial class NationalCoach
{
    /// <summary>[ORIGINAL] Höchstens 22 eingesetzte Spieler (Datei 0x40E4D: Vergleich mit 0x16), wie beim Verein.</summary>
    public const int MaxDressed = Club.MaxDressed;

    private readonly HashSet<Player> _dressed = [];

    /// <summary>Aufstellung des Nationalteams: vier Blöcke und Torhüter wie beim Verein (Datei 0x40364).</summary>
    public Lineup Formation { get; } = new();

    /// <summary>Taktik des Nationalteams (gleicher Bildschirm wie beim Verein).</summary>
    public Tactics Tactics { get; } = new();

    /// <summary>Anzahl der eingesetzten Spieler („Eingesetzt: n“).</summary>
    public int DressedCount => _dressed.Count;

    /// <summary>Ist der Spieler eingesetzt (Kaderfeld 0x0A)?</summary>
    public bool IsDressed(Player player) => _dressed.Contains(player);

    /// <summary>Kader in der Reihenfolge des Originals: nach Art (Torhüter, Abwehr, Sturm), je Art in Aufnahmefolge.</summary>
    public IReadOnlyList<Player> Rows => _squad.Select(m => m.Player).OrderBy(p => p.Role).ToList();

    /// <summary>Nummer in der Liste und auf den Platzknöpfen: Zeile 1–29, nur für eingesetzte Spieler.</summary>
    public int? NumberOf(Player player)
    {
        if (!IsDressed(player))
            return null;
        var row = Rows.ToList().IndexOf(player);
        return row < 0 ? null : row + 1;
    }

    /// <summary>
    /// Status in der Liste; null = nicht eingesetzt (leer). [APPROX] Verletzte und Gesperrte zeigen „VERL.“/„GESP.“
    /// auch, wenn sie nicht eingesetzt sind.
    /// </summary>
    public PlayerStatus? StatusOf(Player player) =>
        player.InjuredGames > 0 ? PlayerStatus.Injured
        : player.SuspendedGames > 0 ? PlayerStatus.Suspended
        : !IsDressed(player) ? null
        : Formation.Contains(player) ? PlayerStatus.InTeam
        : PlayerStatus.Reserve;

    /// <summary>Spieler auf einen Platz setzen; er wird dabei eingesetzt (Datei 0x415B2).</summary>
    public LineupResult Assign(LineupSlot slot, Player player)
    {
        if (!Contains(player))
            return LineupResult.NotInSquad;
        if (player.InjuredGames > 0)
            return LineupResult.Injured;
        if (player.SuspendedGames > 0)
            return LineupResult.Suspended;
        if (player.Role != slot.Role)
            return LineupResult.WrongRole;
        if (!IsDressed(player) && DressedCount >= MaxDressed)
            return LineupResult.RosterFull;
        if (!Formation.Assign(slot, player))
            return LineupResult.WrongRole;
        _dressed.Add(player);
        return LineupResult.Assigned;
    }

    /// <summary>Platz leeren; der Spieler ist danach nicht mehr eingesetzt (Datei 0x40CF8–0x40D0D).</summary>
    public void ClearSlot(LineupSlot slot)
    {
        if (Formation[slot] is not { } player)
            return;
        Formation.Clear(slot);
        _dressed.Remove(player);
    }

    /// <summary>Linksklick in der Kaderliste: eingesetzten, nicht aufgestellten Spieler herausnehmen (Datei 0x40F59).</summary>
    public LineupResult Undress(Player player)
    {
        if (!Contains(player) || Formation.Contains(player) || !_dressed.Remove(player))
            return LineupResult.Unchanged;
        return LineupResult.Undressed;
    }

    /// <summary>Rechtsklick in der Kaderliste: nicht eingesetzten Spieler einsetzen (Datei 0x40E2A, 0x41208).</summary>
    public LineupResult Dress(Player player)
    {
        if (!Contains(player) || IsDressed(player))
            return LineupResult.Unchanged;
        if (DressedCount >= MaxDressed)
            return LineupResult.RosterFull;
        _dressed.Add(player);
        return LineupResult.Dressed;
    }

    /// <summary>
    /// Auto-Aufstellung („A“). [APPROX] Wie beim Verein (<see cref="Core.Lineup.AutoFill"/>, drei Blöcke), die
    /// Aufgestellten werden eingesetzt, dazu die stärksten übrigen einsatzfähigen Spieler (Kondition + Technik + Form)
    /// bis 22. Die Auswahlregel des Originals ist nicht rekonstruiert.
    /// </summary>
    public void AutoFormation()
    {
        var available = _squad.Select(m => m.Player).Where(p => p.Available).ToList();
        Formation.AutoFill(available);
        _dressed.Clear();
        foreach (var p in Formation.Players)
            _dressed.Add(p);
        foreach (var p in available.Where(p => !_dressed.Contains(p))
                     .OrderByDescending(p => p.Condition + p.Technique + p.Form).ThenBy(p => p.Role))
        {
            if (_dressed.Count >= MaxDressed)
                break;
            _dressed.Add(p);
        }
    }

    /// <summary>Alle Plätze leeren („C“); wie beim Leeren einzelner Plätze sind die Aufgestellten danach nicht eingesetzt.</summary>
    public void ClearFormation()
    {
        foreach (var slot in LineupSlot.All)
            ClearSlot(slot);
    }

    /// <summary>Wirksame Aufstellung aus der Formation (Reihen 1–3 und Torhüter, nur einsatzfähige Spieler).</summary>
    private NationalLineup FormationLineup()
    {
        Player? Playing(LineupSlot slot) => Formation[slot] is { Available: true } p && Contains(p) ? p : null;
        var defenders = new List<Player?>();
        var forwards = new List<Player?>();
        for (var b = 0; b < Lines; b++)
        {
            for (var i = 0; i < Core.Lineup.DefendersPerBlock; i++)
                defenders.Add(Playing(LineupSlot.Defender(b, i)));
            for (var i = 0; i < Core.Lineup.ForwardsPerBlock; i++)
                forwards.Add(Playing(LineupSlot.Forward(b, i)));
        }
        return new NationalLineup(Playing(LineupSlot.Goalkeeper), defenders, forwards);
    }

    /// <summary>[APPROX] Zuschlag der Taktik auf die Spielstärke wie beim Verein (Einsatz bei ausgeglichenem Stand).</summary>
    public int TacticsBonus => (int)Math.Round(Tactics.EffortStrengthBonus());

    private void ReleaseFromFormation(Player player)
    {
        Formation.Remove(player);
        _dressed.Remove(player);
    }

    private void ClearFormationState()
    {
        Formation.ClearAll();
        _dressed.Clear();
    }
}
