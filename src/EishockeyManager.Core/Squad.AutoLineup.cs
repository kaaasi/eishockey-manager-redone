using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Automatische Aufstellung des Originals (A068, A165, A169; <c>docs/analyse/re-nachtrag.md</c> §3, Review #446).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Datei 0x426BC: nur mit der Einstellung „automatisch aufstellen“ (DGROUP 0x375 + 2 · Manager ≠ 0).
///   Alle Kaderplätze verlieren „eingesetzt“ (+0x0A = 0, Datei 0x426E7–0x42706). Dann Torhüter (1 Block × 1), Abwehr
///   (3 Blöcke × 2), Sturm (3 Blöcke × 3) je mit Datei 0x42838; findet sich keiner, endet der Block (Datei 0x4272B).
///   Danach reihum Sturm, Abwehr, Torhüter je ein Ersatzspieler ohne Platz (+0x15 = 0), bis 22 eingesetzt sind
///   (Zähler [bp-9] ab 1, Grenze 0x17, Datei 0x427C0/0x4282C) oder auch die Torhütersuche leer bleibt (Datei 0x427DF).</item>
/// <item>[ORIGINAL] Datei 0x42838: Kandidat ist ein nicht eingesetzter, nicht verletzter oder gesperrter Spieler
///   (+0x0A = 0, +0x09 &amp; 3 = 0) der Rolle (Datei 0x04180); Wert (Kondition + Technik + Form) / 3, bei Gleichstand der
///   spätere Platz (Datei 0x4292F: übersprungen nur bei größerem Bestwert, Startwert 0). Findet sich keiner, folgen
///   Ausweichrollen (Torhüter → Abwehr → Sturm, Abwehr → Sturm → Torhüter, Sturm → Abwehr → Torhüter; Datei
///   0x4285C–0x4296A). Als Feldspieler zählt der erste freie Torhüter mit −1 (nie gewählt), jeder weitere mit 1
///   (Datei 0x42913–0x4292C); ist der erste übersprungen, endet die Suche, sobald die Ausgangsrolle wieder erreicht ist.</item>
/// <item>[APPROX] Kaderplätze in der Reihenfolge von <see cref="Club.SortedPlayers"/> (wie <see cref="LiveSquad.Create"/>;
///   im Original hängt die Platzfolge an Kauf und Verkauf).</item>
/// <item>[APPROX] Ein Feldspieler aus der Torhütersuche (kein Torhüter frei) bekommt im Original +0x15 = 1 und steht damit
///   in der ersten Abwehrreihe; das Remake setzt ihn nur ein (der Torhüterplatz nimmt nur Torhüter).</item>
/// </list>
/// </remarks>
public static class AutoLineupRule
{
    /// <summary>[ORIGINAL] Datei 0x42901: (Kondition + Technik + Form) / 3.</summary>
    public static int Value(Player player) => (player.Condition + player.Technique + player.Form) / 3;

    /// <summary>Rolle 0 Torhüter, 1 Abwehr, 2 Sturm (Datei 0x04180).</summary>
    private static int RoleOf(Player player) => (int)player.Role;

    /// <summary>
    /// Datei 0x42838: Index des besten freien Spielers der Rolle (mit Ausweichrollen), sonst −1.
    /// </summary>
    public static int FindBest(IReadOnlyList<Player> slots, IReadOnlyList<bool> dressed, int role)
    {
        var original = role;
        var step = role == 2 ? -1 : 1; // [ORIGINAL] Datei 0x4285C–0x4287D (Einstellung < 4: Zweig ≥ 4 ist tot)
        var best = -1;
        var bestValue = 0;
        var goalieSkipped = false;
        var anyFree = false;
        for (var pass = 0; pass < 5; pass++)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var p = slots[i];
                if (dressed[i] || !p.Available)
                    continue;
                anyFree = true;
                if (RoleOf(p) != role)
                    continue;
                var value = Value(p);
                if (RoleOf(p) == 0 && original != 0)
                {
                    value = goalieSkipped ? 1 : -1;
                    goalieSkipped = true;
                }
                if (bestValue > value)
                    continue;
                bestValue = value;
                best = i;
            }
            role += step;
            if (role == 4)
                role = 0;
            if (role == -1)
                role = 3;
            if (role == original && goalieSkipped)
                return best;
            if (!anyFree || best != -1)
                return best;
        }
        return best;
    }

    /// <summary>
    /// Datei 0x426BC für einen Kader: stellt neu auf und liefert die Eingesetzten (Aufgestellte und Ersatz).
    /// </summary>
    public static List<Player> Apply(Lineup lineup, IReadOnlyList<Player> slots)
    {
        var dressed = new bool[slots.Count];
        var count = 1;
        lineup.ClearAll();

        for (var role = 0; role < 3; role++)
        {
            var blocks = role == 0 ? 1 : Lineup.AutoBlocks;
            for (var block = 0; block < blocks; block++)
            {
                for (var k = 0; k <= role; k++)
                {
                    var i = FindBest(slots, dressed, role);
                    if (i == -1)
                        break;
                    dressed[i] = true;
                    count++;
                    var p = slots[i];
                    switch (role)
                    {
                        case 0:
                            if (p.Role == PlayerRole.Goalkeeper)
                                lineup.Place(LineupSlot.Goalkeeper, p);
                            break;
                        case 1:
                            lineup.Place(LineupSlot.Defender(block, k), p);
                            break;
                        default:
                            lineup.Place(LineupSlot.Forward(block, k), p);
                            break;
                    }
                }
            }
        }

        while (count < Club.MaxDressed + 1)
        {
            var last = -1;
            for (var role = 2; role >= 0; role--)
            {
                if (count >= Club.MaxDressed + 1)
                    continue;
                last = FindBest(slots, dressed, role);
                if (last == -1)
                {
                    if (role == 0)
                        break;
                    continue;
                }
                dressed[last] = true;
                count++;
            }
            if (last == -1)
                break;
        }

        lineup.SortBlocksByNumber();
        return slots.Where((_, i) => dressed[i]).ToList();
    }
}

public sealed partial class Club
{
    /// <summary>
    /// Einstellung „automatisch aufstellen“ (DGROUP 0x375 + 2 · Manager). [ORIGINAL] Startwert 1 (statisch, Datei
    /// 0x16DD0 + 0x375); ein Eingriff in der Aufstellung schaltet sie ab (Datei 0x40ADE, 0x40DE4).
    /// [APPROX] Das Remake führt sie je Verein statt je Manager und schaltet sie mit „A“ in der Aufstellung wieder ein (im
    /// Original kippt sie der Taktikbildschirm, Datei 0x47F4E/0x49058).
    /// </summary>
    private bool _autoLineup = true;

    /// <summary>
    /// Einstellung „automatisch aufstellen“ (siehe Feld). Setzen stellt nicht sofort auf (das tut <see cref="AutoLineup"/>
    /// bzw. der nächste Tageslauf).
    /// </summary>
    public bool AutoLineupEnabled
    {
        get => _autoLineup;
        set => _autoLineup = value;
    }

    /// <summary>Eingriff von Hand: Einstellung aus (Datei 0x40ADE/0x40DE4, Meldung DGROUP 0x124D in der Oberfläche).</summary>
    private void DisableAutoLineup() => _autoLineup = false;

    /// <summary>
    /// Datei 0x426BC: mit der Einstellung neu aufstellen (<see cref="AutoLineupRule"/>); ohne sie bleibt alles, wie es ist.
    /// </summary>
    public bool ApplyAutoLineup()
    {
        if (!_autoLineup)
            return false;
        var dressed = AutoLineupRule.Apply(Lineup, SortedPlayers.ToList());
        _dressed.Clear();
        _dressed.UnionWith(dressed.Select(p => p.Id));
        _dressedByHand = true;
        return true;
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Datei 0x426BC für alle Managervereine: Spielstart (Datei 0x28165) und am Ende des Tageslaufs (Datei 0x07C07; deckt
    /// auch Gesperrte 0x07621 und Transfers ab). Computervereine führt das Original ohne Spieler (<see cref="CleanLineups"/>).
    /// </summary>
    public void ApplyAutoLineups()
    {
        foreach (var club in Clubs)
            if (Managers.Any(m => m.ClubIndex == club.Index))
                club.ApplyAutoLineup();
    }
}
