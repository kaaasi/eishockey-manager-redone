using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class Player
{
    /// <summary>Kein gewürfelter Rollenwert (Spieler außerhalb des Pools, Spielstände vor Schema 65).</summary>
    public const int NoRoleValue = -1;

    /// <summary>Gewürfelter Rollenwert oder <see cref="NoRoleValue"/> (Spielstand: <c>_roleValue</c>, ab Schema 65).</summary>
    private int _roleValue = NoRoleValue;

    /// <summary>
    /// Rollenwert Pool +0x18 (Aufräumkonzept K11, Spur saison-geld Teil 3). [ORIGINAL] Datei 0x5B3BB–0x5B49D: einmal beim
    /// Poolaufbau je Poolplatz gewürfelt (Torhüter 0, Verteidiger 25 + random(0, 16), Stürmer 75 + random(0, 16);
    /// <see cref="TransferPoolRules.RollRoleValue"/>) und danach nie geändert – der Wert gehört dem Poolplatz (ein neuer Spieler
    /// des Platzes übernimmt ihn, <see cref="PoolSlot.Player"/>). Leser: Torschützen und Vorlagen Datei 0x5077–0x52CB
    /// (<see cref="MatchEngine.CreditSkaters"/>) und der Export (Pool +0x18). Beleg <c>k11_rollenwert.py</c>,
    /// <c>docs/analyse/re-abweichungen.md</c> K11.
    /// [APPROX] Spieler, die nie auf einem Poolplatz standen (Kader der Computervereine des Remakes, Talente), und Spielstände vor
    /// Schema 65 haben keinen gewürfelten Wert: dann gilt die Mitte der Spanne (<see cref="DefaultRoleValue"/>), ohne Zufall –
    /// das Original führt diese Spieler nicht, ein Wurf verschöbe den gemeinsamen Strom.
    /// </summary>
    public int RoleValue
    {
        get => _roleValue >= 0 ? _roleValue : DefaultRoleValue(Role);
        set => _roleValue = value;
    }

    /// <summary>Hat der Spieler einen gewürfelten bzw. aus einem Originalstand gelesenen Rollenwert?</summary>
    public bool HasRoleValue => _roleValue >= 0;

    /// <summary>
    /// Mitte der Spanne je Art: Torhüter 0, Verteidiger 33 (25 … 41), Stürmer 83 (75 … 91). [APPROX] für Spieler ohne Poolplatz
    /// (<see cref="RoleValue"/>).
    /// </summary>
    public static int DefaultRoleValue(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => 0,
        PlayerRole.Defender => TransferPoolRules.DefenderRoleBase + TransferPoolRules.RoleValueSpread / 2,
        _ => TransferPoolRules.ForwardRoleBase + TransferPoolRules.RoleValueSpread / 2,
    };

    /// <summary>Übernimmt den Rollenwert eines Poolplatzes (auch „keiner“) von seinem bisherigen Spieler.</summary>
    internal void TakeRoleValueOf(Player previous) => _roleValue = previous._roleValue;
}
