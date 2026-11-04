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

    /// <summary>„Kein / Platz mehr !“ – in der gewählten Reihe ist kein Platz der Art mehr frei (Datei 0x414BC–0x414C6).</summary>
    NoRoom,

    /// <summary>„Keine Reihe / ausgewählt !“ – Spieler angeklickt, ohne vorher eine Reihe zu wählen (Datei 0x40E77, DGROUP 0x1221).</summary>
    NoLineSelected,

    /// <summary>Spieler aus seinem Platz genommen (Klick auf einen Aufgestellten in der Kaderliste, Datei 0x40F7C–0x4116F).</summary>
    TakenOut,
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
/// <item>[ORIGINAL] Rechtsklick (Mausereignis 3) auf einen eingesetzten, nicht aufgestellten Spieler nimmt ihn heraus
///   (Datei 0x40F35–0x40F59), auf einen nicht eingesetzten setzt ihn ein (Datei 0x411FE). Ein Linksklick (Ereignis 1) auf einen
///   nicht Aufgestellten geht immer in die gewählte Reihe (Datei 0x40F1E–0x40F32 → 0x41273); bis Runde 13 nahm das Remake
///   einen Reservisten mit Linksklick heraus (DOSBox `file19_08`: Linksklick auf den Reservisten RYAN → „Kein Platz mehr !“).</item>
/// <item>[ORIGINAL] In der Liste steht die Platznummer (Zeile 1–29) nur bei eingesetzten Spielern, sonst bleiben NR und
///   STATUS leer (Video `bundestrainer_00056`: 22 nummerierte Spieler, „Eingesetzt: 22“).</item>
/// <item>[ORIGINAL] Auswahl nach Reihe, nicht nach Platz (A010, Spur nationalteam Runde 13, <c>docs/analyse/re-runde-13.md</c>):
///   Klick auf einen Block wählt die Reihe (Datei 0x40890–0x40943), ein Klick auf einen Spieler der Kaderliste setzt ihn auf
///   den ersten freien Platz seiner Art in dieser Reihe; ist keiner frei, kommt „Kein / Platz mehr !“ und nichts ändert sich
///   (Datei 0x41287–0x414C6; DOSBox `file19_08`). Verdrängt wird niemand. Ein Klick auf einen Aufgestellten nimmt ihn heraus
///   und wählt seine Reihe (Datei 0x40F7C–0x411FE; `file19_06` → `file19_07`, „Eingesetzt“ 22 → 21). Siehe
///   <see cref="ClickPlayer"/>.</item>
/// <item>[ORIGINAL] Spielstärke wie bei einem Managerverein (Spur wettbewerbe-spiel, Teil 3): Reihenwerte aus den
///   eingesetzten Spielern aller vier Blöcke (0x98F8 mit Manager 5, + Länderspiele / 8), Taktik über Einsatz und
///   Angriff im Spiel (0x977F/0x98F8, 0x939A) statt eines Zuschlags auf die Stärke (<see cref="MatchEngine.NationalSquad"/>,
///   <see cref="GameState.NationalMatchStrength"/>).</item>
/// </list>
/// </remarks>
public sealed partial class NationalCoach
{
    /// <summary>[ORIGINAL] Höchstens 22 eingesetzte Spieler (Datei 0x40E4D: Vergleich mit 0x16), wie beim Verein.</summary>
    public const int MaxDressed = Club.MaxDressed;

    private readonly HashSet<Player> _dressed = [];

    /// <summary>Aufstellung des Nationalteams: vier Blöcke und Torhüter wie beim Verein (Datei 0x40364).</summary>
    public Lineup Formation { get; } = new();

    /// <summary>
    /// Taktik des Nationalteams (gleicher Bildschirm wie beim Verein). [ORIGINAL] Sie gehört zum Nationalteam, nicht zum
    /// Amt: Vereinssatz des Landes und eigene Werte des Bundestrainers (Manager 5) bleiben über den Saisonwechsel stehen
    /// (#192, <see cref="Tactics.National"/>, <see cref="GameState.NationalTactics"/>).
    /// </summary>
    public Tactics Tactics { get; private set; } = Tactics.National();

    /// <summary>Übernimmt beim Amtsantritt die Taktik des Nationalteams (siehe <see cref="GameState.NationalTactics"/>).</summary>
    internal void TakeOverTactics(Tactics tactics) => Tactics = tactics;

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
    /// Reihe des Torhüterplatzes in <see cref="ClickPlayer"/> und <see cref="TakeOut"/> (im Original Block 4 neben den
    /// Reihen 0–3, Datei 0x40E8F).
    /// </summary>
    public const int GoalkeeperLine = Core.Lineup.Blocks;

    /// <summary>
    /// Klick auf einen Spieler der Kaderliste im Aufstellungsbildschirm (Datei 0x40D8D–0x4120D, Menüaufruf mit Kennwert 0).
    /// <list type="bullet">
    /// <item>[ORIGINAL] Verletzte und Gesperrte (+0x09 &amp; 3 ≠ 0): nur die Meldung, keine Änderung (Datei 0x40E01–0x40E27,
    ///   für beide Maustasten). Danach ein nicht Eingesetzter bei 22 Eingesetzten: „Schon 22 Mann / im Team !“ (Datei
    ///   0x40E2A–0x40E61).</item>
    /// <item>[ORIGINAL] Aufgestellter (+0x15 ≠ 0): aus seinem Platz nehmen, seine Reihe wird die gewählte (Datei 0x40F7C–0x411C1).
    ///   Linksklick: danach nicht mehr eingesetzt (+0x0A = Kennwert = 0, Datei 0x4115F–0x4116F); Rechtsklick: wieder eingesetzt
    ///   (Datei 0x411FE–0x41208).</item>
    /// <item>[ORIGINAL] Rechtsklick sonst: Reservist heraus (Datei 0x40F59), nicht Eingesetzter einsetzen (Datei 0x41208).</item>
    /// <item>[ORIGINAL] Linksklick sonst: keine Reihe gewählt → „Keine Reihe / ausgewählt !“; Torhüter nur in den Torhüterplatz,
    ///   Feldspieler nicht dorthin → „Der darf da / nicht rein !“ (Datei 0x40E77–0x40EB9); dann der erste freie Platz der Art in
    ///   der Reihe (Sturm Plätze 0–2, Abwehr 3–4, Datei 0x412A1–0x41381), eingesetzt (Datei 0x415B2), sonst „Kein / Platz mehr !“.
    ///   [APPROX] Ist die eigene Hälfte der Reihe voll, versucht das Original die andere Hälfte (Verteidiger in den Sturm und
    ///   umgekehrt, Datei 0x414D3–0x414ED) und öffnet in Reihe 1 einen sechsten Feldspielerplatz (Datei 0x413E8–0x414B4). Beides
    ///   übernimmt das Remake nicht: seine Aufstellung hat feste Plätze je Art (<see cref="LineupSlot"/>); es meldet gleich
    ///   „Kein / Platz mehr !“.</item>
    /// </list>
    /// </summary>
    /// <param name="player">angeklickter Spieler (muss im Kader stehen).</param>
    /// <param name="line">gewählte Reihe 0–3, <see cref="GoalkeeperLine"/> oder null (keine); nach dem Herausnehmen die Reihe
    /// des Spielers.</param>
    /// <param name="right">Rechtsklick (Ereignis 3) statt Linksklick (Ereignis 1).</param>
    public LineupResult ClickPlayer(Player player, ref int? line, bool right)
    {
        if (!Contains(player))
            return LineupResult.NotInSquad;
        if (player.InjuredGames > 0)
            return LineupResult.Injured;
        if (player.SuspendedGames > 0)
            return LineupResult.Suspended;
        if (!IsDressed(player) && DressedCount >= MaxDressed)
            return LineupResult.RosterFull;
        if (TakeOut(player, keepDressed: right) is { } from)
        {
            line = from;
            return LineupResult.TakenOut;
        }
        if (right)
            return IsDressed(player) ? Undress(player) : Dress(player);
        if (line is not { } l)
            return LineupResult.NoLineSelected;
        if ((player.Role == PlayerRole.Goalkeeper) != (l == GoalkeeperLine))
            return LineupResult.WrongRole;
        foreach (var slot in SlotsOf(l, player.Role))
            if (Formation[slot] is null)
                return Assign(slot, player);
        return LineupResult.NoRoom;
    }

    /// <summary>Plätze einer Reihe für eine Art in der Reihenfolge der Suche des Originals (Datei 0x412C9–0x41386).</summary>
    private static IEnumerable<LineupSlot> SlotsOf(int line, PlayerRole role)
    {
        if (line == GoalkeeperLine)
        {
            yield return LineupSlot.Goalkeeper;
            yield break;
        }
        var count = role == PlayerRole.Forward ? Core.Lineup.ForwardsPerBlock : Core.Lineup.DefendersPerBlock;
        for (var i = 0; i < count; i++)
            yield return role == PlayerRole.Forward ? LineupSlot.Forward(line, i) : LineupSlot.Defender(line, i);
    }

    /// <summary>
    /// Nimmt einen Aufgestellten aus seinem Platz (Datei 0x40F7C–0x4116F). Liefert die Reihe (0–3, Torhüter
    /// <see cref="GoalkeeperLine"/>) oder null, wenn er nicht aufgestellt ist.
    /// </summary>
    public int? TakeOut(Player player, bool keepDressed)
    {
        if (Formation.FindSlot(player) is not { } slot)
            return null;
        Formation.Clear(slot);
        if (!keepDressed)
            _dressed.Remove(player);
        return slot.Role == PlayerRole.Goalkeeper ? GoalkeeperLine : slot.Block;
    }

    /// <summary>
    /// Auto-Aufstellung („A“). [ORIGINAL] Dieselbe Regel wie beim Verein: Satz 5 ruft Datei 0x426BC/0x42838
    /// (<see cref="AutoLineupRule.Apply"/>; Einstellung DGROUP 0x37F, Aufrufe Datei 0x3B4C6 und im Tageslauf 0x54D84,
    /// <c>docs/analyse/re-nachtrag.md</c> §3.4): alle verlieren „eingesetzt“, dann Torhüter, Abwehr und Sturm nach
    /// (Kondition + Technik + Form) / 3, Ersatz reihum bis 22. Kaderplätze in der Reihenfolge der Liste (<see cref="Rows"/>,
    /// nach Art sortiert wie Datei 0x42D46). Spur wettbewerbe-spiel, Runde 12, Teil 4 (U8); vorher Spielwert mit Energie
    /// (<see cref="Core.Lineup.AutoFill"/>) und Rest nach Kondition + Technik + Form.
    /// [APPROX] Innerhalb eines Blocks nach Rückennummer des Vereins sortiert (wie beim Verein,
    /// <see cref="Core.Lineup.SortBlocksByNumber"/>). Die Einstellung „automatisch“ des Satzes 5 im Tageslauf führt das
    /// Remake nicht; im Zug des Bundestrainers ist der Aufruf wirkungslos (Einstellung vorher 0, Datei 0x555B7–0x555D6).
    /// </summary>
    public void AutoFormation()
    {
        var dressed = AutoLineupRule.Apply(Formation, Rows);
        _dressed.Clear();
        foreach (var p in dressed)
            _dressed.Add(p);
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

    /// <summary>
    /// Aufstellung, aus der 0x952C mit Manager 5 die Länderwerte schreibt (<see cref="GameState.UpdateNationalTeamValues"/>):
    /// die <see cref="Formation"/>, leere Plätze zählen 0. [APPROX] Ist die Formation ganz leer, gilt wie in
    /// <see cref="Lineup"/> die Automatik des Remakes (passiver Bundestrainer); eine ganz leere Aufstellung des Originals ist
    /// nicht aufgenommen (L7) und ergäbe nach dem Code 0.
    /// </summary>
    internal Core.Lineup TeamValueFormation()
    {
        if (Formation.Count > 0)
            return Formation;
        var auto = Lineup();
        var lineup = new Core.Lineup();
        if (auto.Goalkeeper is { } g)
            lineup.Assign(LineupSlot.Goalkeeper, g);
        for (var i = 0; i < auto.Defenders.Count; i++)
            if (auto.Defenders[i] is { } d)
                lineup.Assign(LineupSlot.Defender(i / Core.Lineup.DefendersPerBlock, i % Core.Lineup.DefendersPerBlock), d);
        for (var i = 0; i < auto.Forwards.Count; i++)
            if (auto.Forwards[i] is { } f)
                lineup.Assign(LineupSlot.Forward(i / Core.Lineup.ForwardsPerBlock, i % Core.Lineup.ForwardsPerBlock), f);
        return lineup;
    }

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
