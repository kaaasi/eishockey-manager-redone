using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Zufallswerte eines neuen Kaderplatzes in der Reihenfolge von „Kaderplatz anlegen“ (Datei 0x42CB1): Belastung +0x13, dann
/// Tendenz +0x0E und Steuerbyte +0x14 (Spur kaderplatz-werte, Teil 1).
/// </summary>
public readonly record struct SlotStart(int Strain, int TrendScore, int TrendControl);

/// <summary>
/// Werte am Kaderplatz statt am Spieler (Spur kaderplatz-werte, Teil 1; A545, A674, A690, A691, A714).
/// </summary>
/// <remarks>
/// [ORIGINAL] Das Original führt jeden Spieler in einem Poolsatz (far:8253, 33 Byte) und nur Spieler der Manager und des Markts
/// zusätzlich in einem Kaderplatz (DGROUP 0x5DC3, 0x34 Byte). „Kaderplatz anlegen“ (Datei 0x42CB1) löscht den Platz (memset
/// 0x42E3A), schreibt +0x0F = Poolindex (0x42E48), Belastung +0x13 = random(80, 120) (0x42E4C–0x42E5E), +0x0E = 0 und ruft
/// 0x4907A (Tendenz, 0x42E6B), dann +0x03 = Pool +0x1B (Ligatore, 0x42E93), +0x06 = Pool +0x1C (Ligaspiele, 0x42EAE), +0x10–+0x12
/// = Pool +0x15–+0x17 (Kondition, Technik, Form; 0x42EC9–0x42F06) und das Gehalt; bei einer Leihe (+0x0C = 99) zusätzlich
/// random(90, 115) (0x42F1F–0x42F34). Alle übrigen Felder – Strafen, Pokal- und Europapokalzähler, Verletzung, Ankündigung +0x18,
/// Über-/Unterzahl +0x19 – sind danach 0.
/// Beleg FILE05 → FILE07 (DAFFNER, Pool 158, vom Computermarkt gekauft): Markt-Platz +0x19 = 0xCA, +0x13 = 103, +0x0E = 40 →
/// neuer Kaderplatz +0x19 = 0, +0x13 = 99, +0x0E = 57; Stärken 67/88/51 und +0x06 = 2 wie im Pool.
/// Der Kauf 0x477E7 legt so einen Platz an und kopiert den Markt-Platz nur bei einer Leihe ([bp−0xC]) oder beim Zurückziehen des
/// eigenen Angebots ([bp−0x11], 0x474E4) darüber (0x478CF–0x47907); beim Kauf von einem Manager ([bp−0x12], 0x4774E) übernimmt er
/// nur +0x09, +0x17, +0x0D, +0x00, +0x01 und +0x02 (0x4787D–0x478CB).
/// </remarks>
public static class SquadSlotRules
{
    /// <summary>Höchstwert der Byte-Zähler Pool +0x1B/+0x1C und Kaderplatz +0x03/+0x06. [ORIGINAL] Datei 0x542D5, 0x54322 (cmp 0xFF).</summary>
    public const int CounterMax = 255;

    /// <summary>
    /// Zieht Belastung und Tendenz eines neuen Kaderplatzes. [ORIGINAL] Datei 0x42E4C–0x42E5E: random(80, 120); Datei 0x4907A:
    /// +0x0E = random(35, 65), +0x14 = random(1, 13) | random(1, 3) &lt;&lt; 4 | random(0, 1) &lt;&lt; 7 (<see cref="Player.RollTrend"/>).
    /// </summary>
    public static SlotStart Roll(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var strain = TransferRules.Random16(random, PlayerFactory.StartStrainMin, PlayerFactory.StartStrainMax);
        var (score, control) = Player.DrawTrend(random);
        return new SlotStart(strain, score, control);
    }

    /// <summary>
    /// Zug des Leihgehalts in 0x42CB1 (Datei 0x42F1F–0x42F34: · random(90, 115) / 100 bei +0x0C = 99). Die Leihe kopiert danach den
    /// Markt-Platz samt Gehalt über den neuen Platz (0x47907, 0x47929), der Wert geht verloren; nur der Zufall wird verbraucht.
    /// </summary>
    public static void ConsumeLoanSalary(Random random) => _ = TransferRules.Random16(random, 90, 115);
}

public sealed partial class Player
{
    /// <summary>
    /// Kondition im Poolsatz (Pool +0x15), wenn sie von der des Kaderplatzes (+0x10, <see cref="Condition"/>) abweicht; sonst null.
    /// [ORIGINAL] Die Drift 0x054D9 ändert den Poolsatz jedes Platzes 1 … 199, auch von Spielern mit Kaderplatz (Datei 0x0564C–0x05712,
    /// A714); der Kaderplatz übernimmt den Poolwert erst, wenn er neu angelegt wird (0x42EC9). Spieler ohne Kaderplatz (Computervereine,
    /// freie Poolplätze) tragen den Poolwert direkt in <see cref="Condition"/>.
    /// </summary>
    public int? PoolCondition { get; set; }

    /// <summary>Technik im Poolsatz (Pool +0x16), wenn sie von der des Kaderplatzes (+0x11) abweicht; sonst null (wie <see cref="PoolCondition"/>).</summary>
    public int? PoolTechnique { get; set; }

    /// <summary>Kondition und Technik des Poolsatzes (Pool +0x15/+0x16).</summary>
    public (int Condition, int Technique) PoolStrengths => (PoolCondition ?? Condition, PoolTechnique ?? Technique);

    /// <summary>
    /// Schreibt Kondition und Technik in den Poolsatz. Mit Kaderplatz (<paramref name="hasSlot"/>) bleiben die Werte des Platzes, die
    /// Poolwerte stehen abweichend in <see cref="PoolCondition"/>/<see cref="PoolTechnique"/>; ohne Kaderplatz sind sie die Werte des
    /// Spielers.
    /// </summary>
    public void SetPoolStrengths(int condition, int technique, bool hasSlot)
    {
        if (!hasSlot)
        {
            (Condition, Technique) = (condition, technique);
            PoolCondition = PoolTechnique = null;
            return;
        }
        PoolCondition = condition == Condition ? null : condition;
        PoolTechnique = technique == Technique ? null : technique;
    }

    /// <summary>
    /// Kaderplatz aus dem Pool (Kondition, Technik, Form). [ORIGINAL] Datei 0x42EC9–0x42F06 (in 0x42CB1). Gilt auch, wenn der
    /// Spieler einen Managerkader verlässt (Verkauf an einen Computerverein, Datei 0x46F3C–0x46FCF; Abgabe vom Markt 0x474B1): der
    /// Kaderplatz entfällt, es bleiben die Poolwerte. [APPROX] Computervereine haben keine Kaderplätze; ihr Spieler trägt die Poolwerte.
    /// </summary>
    public void AdoptPoolValues()
    {
        (Condition, Technique) = PoolStrengths;
        PoolCondition = PoolTechnique = null;
        AdoptPoolForm();
    }

    /// <summary>
    /// Felder eines neu angelegten Kaderplatzes (Datei 0x42CB1, siehe <see cref="SquadSlotRules"/>): Belastung und Tendenz aus
    /// <paramref name="start"/>, Über-/Unterzahl 0 (memset; A674), Liga-Tore und -Spiele aus dem Pool (+0x1B/+0x1C, höchstens 255;
    /// A545), Pokal- und Europapokalzähler 0, beim Torhüter keine Gegentore (Pool +0x1B hat für ihn keinen Schreiber), Ankündigung und
    /// Sperre +0x18 = 0, Stärken aus dem Pool (<see cref="AdoptPoolValues"/>). Strafen und Verletzung (+0x00, +0x01, +0x09, +0x0D,
    /// +0x17) übernimmt nur der Kauf von einem Manager (<paramref name="fromManager"/>, Datei 0x4787D–0x478CB).
    /// </summary>
    public void ApplyNewSlot(SlotStart start, bool fromManager)
    {
        var leagueGames = Math.Min(SquadSlotRules.CounterMax, SeasonGames(CompetitionSlot.League));
        var leagueGoals = Role == PlayerRole.Goalkeeper ? 0 : Math.Min(SquadSlotRules.CounterMax, SeasonGoals(CompetitionSlot.League));
        Games = leagueGames;
        Goals = leagueGoals;
        CupGames = EuropeGames = CupGoals = EuropeGoals = 0;
        if (Role == PlayerRole.Goalkeeper)
            GoalsAgainst = 0;
        if (!fromManager)
        {
            Penalties2 = Penalties5 = 0;
            InjuredGames = 0;
            InjuryKind = InjuryKinds.Unknown;
        }
        Notice = ContractNotice.None;
        NoticeYears = 0;
        NegotiationLock = 0;
        HiddenSkill1 = HiddenSkill2 = 0;
        Strain = start.Strain;
        SetTrendScore(start.TrendScore, start.TrendControl);
        AdoptPoolValues();
    }
}
