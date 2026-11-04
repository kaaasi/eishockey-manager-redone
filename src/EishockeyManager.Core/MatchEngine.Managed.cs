using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Kaderplatz eines Managervereins während der Minutenrechnung (Kaderplatz des Originals: DGROUP 0x5DC3 + 0x618 · Manager
/// + 0x34 · Platz). Spur spielstaerke-manager-2, Fundstellen <c>docs/spielberechnung.md</c> §13.
/// </summary>
public sealed class LivePlayer
{
    public required Player Player { get; init; }

    /// <summary>
    /// Aufstellungsplatz (Kaderplatz +0x15): 0 Reserve, sonst Bit 7 Teil (0 Abwehr, 1 Sturm) und Bits 0–6 Reihe + 1;
    /// der Torhüter steht auf 1 (Abwehr, Reihe 0).
    /// </summary>
    public int Position { get; init; }

    /// <summary>Eingesetzt (Kaderplatz +0x0A ≠ 0); auf der Strafbank 0.</summary>
    public bool Dressed { get; set; }

    /// <summary>Energie im Spiel (Kaderplatz +0x1B), 1 … 99.</summary>
    public int Energy { get; set; }

    /// <summary>Torhüter (Rollenwert, Spielerfeld 0x18 = 0).</summary>
    public bool Goalie => Player.Role == PlayerRole.Goalkeeper;

    /// <summary>Teil laut Aufstellungsplatz (Bit 7).</summary>
    public int Part => (Position & 0x80) != 0 ? 1 : 0;

    /// <summary>Reihe laut Aufstellungsplatz (−1 = Reserve).</summary>
    public int Line => (Position & 0x7F) - 1;

    /// <summary>Tore dieses Spiels (Gewicht der Schützenwahl 0x386D1, Kaderplatz +0x03 wächst im Original sofort).</summary>
    public int MatchGoals { get; set; }
}

/// <summary>Kader eines Managervereins in der Minutenrechnung.</summary>
public sealed class LiveSquad
{
    /// <summary>Kaderplätze in der Reihenfolge des Kaders.</summary>
    public List<LivePlayer> Players { get; } = [];

    /// <summary>„5 − Level“ des Managers (DGROUP 0xED8).</summary>
    public int Difficulty { get; init; } = 2;

    /// <summary>Taktik des Managers (Linienwechsel, Minuten-Vorgabe).</summary>
    public Tactics? Tactics { get; init; }

    /// <summary>
    /// Kader des Bundestrainers (Manager 5): [ORIGINAL] 0x98F8 zählt im Spiel Länderspiele / 8 auf die Technik
    /// (<see cref="MatchEngine.ManagedLineValues"/>, <see cref="MatchEngine.NationalSquad"/>).
    /// </summary>
    public bool NationalCoach { get; init; }

    /// <summary>
    /// Torhüter auf dem Eis (DGROUP 0xDE16 + Manager): gesetzt beim Bilden der Abwehrwerte (Datei 0x9920/0x999A). Ohne
    /// Torhüter ist jeder gegnerische Abschluss ein Tor (0xA83E) und die Abwehr bricht zusammen (0x9C01).
    /// </summary>
    public bool HasGoalie { get; set; } = true;

    /// <summary>
    /// Feldspieler je Teil (DGROUP 0xCC37 + 2 · Manager + Teil). [ORIGINAL] geschrieben von der Neuzählung 0x38A36
    /// (<see cref="MatchEngine.RecountSkaters"/>; je Minute zu Beginn von 0x38B8F und beim Freiwerden 0x35691).
    /// [APPROX] Startwert 2 und 3 (im Original der Stand der letzten Neuzählung, auch aus 0x952C außerhalb des Spiels).
    /// Sonderfall 4 Verteidiger (Datei 0x9B42) kommt im Remake nicht vor.
    /// </summary>
    public int[] SkatersPerPart { get; } = [2, 3];

    /// <summary>
    /// Minutenzähler der Minuten-Vorgabe je Teil und Reihe (DGROUP 0x4926 + 8 · Manager + 4 · Teil + Reihe).
    /// [APPROX] Im Original schreibt nur 0x8EF2 den Zähler (Datei 0x911C + 1, Datei 0x9166 = 0 beim Wechsel; kein weiterer
    /// Leser, Schreiber oder Blockzugriff auf DGROUP 0x4926–0x4955): er läuft über das Spielende in das nächste Spiel
    /// weiter und gehört nicht zum Spielstand. Das Remake beginnt jedes Spiel bei 0.
    /// </summary>
    public int[,] MinuteCounters { get; } = new int[2, 4];

    /// <summary>
    /// Kader eines Managervereins für ein Spiel. [ORIGINAL] Reihenfolge der Kaderplätze (<see cref="Club.Players"/>, A091):
    /// beim Start Torhüter, Abwehr, Sturm; Kauf hinten an (Datei 0x42CB1, FILE07), Verkauf rückt auf (Datei 0x425B6, FILE26),
    /// Saisonwechsel unverändert (A085, FILE22/FILE23 HOK 0x06A27 + 0x34 · Platz, +0x0F je Platz gleich). Eingesetzt
    /// (+0x0A) sind die im Verein eingesetzten spielfähigen Spieler (<see cref="Club.IsDressed"/>; A168); die Energie im
    /// Spiel setzt die Spielvorbereitung (<see cref="MatchEngine.PreparedPlayerEnergy"/>).
    /// </summary>
    public static LiveSquad Create(Club club, int difficulty, Tactics? tactics)
    {
        var squad = new LiveSquad { Difficulty = difficulty, Tactics = tactics };
        foreach (var p in club.Players)
        {
            var slot = club.Lineup.FindSlot(p);
            squad.Players.Add(new LivePlayer
            {
                Player = p,
                Position = slot is { } s ? PositionOf(s) : 0,
                Dressed = p.Available && club.IsDressed(p),
                Energy = MatchEngine.PreparedPlayerEnergy(p),
            });
        }
        return squad;
    }

    /// <summary>Kaderplatz +0x15 für einen Platz der Aufstellung: Block b = Reihe b beider Teile.</summary>
    public static int PositionOf(LineupSlot slot) => slot.Role switch
    {
        PlayerRole.Goalkeeper => 1,
        PlayerRole.Defender => slot.Block + 1,
        _ => 0x80 | (slot.Block + 1),
    };

    /// <summary>Gibt es einen eingesetzten Spieler in der vierten Reihe dieses Teils? [ORIGINAL] Datei 0x8E5F.</summary>
    public bool HasFourthLine(int part) => Players.Any(p => p.Dressed && p.Position == (part << 7) + 4);
}

public static partial class MatchEngine
{
    /// <summary>[ORIGINAL] Obergrenze beim Vorbereiten der Energie (Datei 0x8635/0x86EC: herunterzählen, solange &gt; 0x5B).</summary>
    public const int EnergyCeiling = 0x5B;

    /// <summary>
    /// Energie eines Kaderplatzes bei der Spielvorbereitung. [ORIGINAL] Datei 0x86B2–0x86EE: 101 − Belastung / 8
    /// (Spielerfeld 0x13 &gt;&gt; 3), dann herunterzählen, solange der Wert über 0x5B liegt – also min(…, 91) − 1.
    /// Belastung = <see cref="Player.Strain"/> (Datei 0x86B5, seit A184 eigenes Feld statt 150 − Energie).
    /// </summary>
    public static int PreparedPlayerEnergy(Player p) => Math.Min(101 - (TrainingRules.Fatigue(p) >> 3), EnergyCeiling) - 1;

    /// <summary>
    /// [ORIGINAL] Datei 0x388F6(Verein, Manager, Modus, Teil): eingesetzte Kaderplätze (+0x0A) des Teils (+0x15 Bit 7).
    /// Art: Torhüter (Gruppe 0x04180 = 0) 0, sonst nach +0x15 Bit 7 Abwehr 1 / Sturm 2 – ein Stürmer auf einem
    /// Abwehrplatz zählt also als Verteidiger. Modus 0 zählt Torhüter mit beliebiger Reihe ≠ 0, Modus 1/2 nur die Reihe
    /// auf dem Eis (<paramref name="lineOnIce"/>, Vereinsfeld 0x2E/0x30).
    /// </summary>
    public static int CountOnIce(LiveSquad squad, int mode, int part, int lineOnIce)
    {
        var n = 0;
        foreach (var p in squad.Players)
        {
            if ((p.Position & 0x80) != part << 7)
                continue;
            var line = p.Position & 0x7F;
            var kind = p.Goalie ? 0 : (p.Position & 0x80) != 0 ? 2 : 1;
            if ((mode != 0 || line == 0) && line - 1 != lineOnIce)
                continue;
            if (p.Dressed && kind == mode)
                n++;
        }
        return n;
    }

    /// <summary>
    /// Neuzählung der Feldspieler eines Managervereins (A089, A070). [ORIGINAL] Datei 0x38A36 (nur Vereine ≤ 0x95 mit
    /// Manager, Vereinsfeld 0x16 ≠ 0): Torhüter g (0x388F6 Modus 0; keiner → Abwehrreihe 0, Datei 0x38A2A), Verteidiger d
    /// und Stürmer f der Reihen auf dem Eis, belegte Strafbankplätze b (0x35540 Schalter 0). Grenze 6, ohne Torhüter und
    /// mit d + f &lt; 6 nur 5; g + d + f + b über der Grenze → f = 3 − Überschuss. Dann DGROUP 0xCC37 + 2 · Manager = d,
    /// +1 = f (<see cref="LiveSquad.SkatersPerPart"/>), Vereinsfeld 0x32 = d + f (<see cref="PenaltyBox.Skaters"/>),
    /// 0x2C Bit 3 (<see cref="LiveTeam.GoaliePulled"/>) genau bei d + f = 6.
    /// [APPROX] f unter 0 (mehr als drei über der Grenze) hält das Remake bei 0; im Original ein Byte (nicht geprüft).
    /// </summary>
    public static void RecountSkaters(LiveTeam team)
    {
        if (team.InjuryManager is null || team.Squad is not { } squad)
            return;
        var goalies = CountOnIce(squad, 0, 0, team.DefenseLine);
        if (goalies == 0)
            team.DefenseLine = 0;
        var defenders = CountOnIce(squad, 1, 0, team.DefenseLine);
        var forwards = CountOnIce(squad, 2, 1, team.AttackLine);
        var limit = goalies == 0 && defenders + forwards < 6 ? 5 : 6;
        var total = goalies + defenders + forwards + team.Box.Count;
        if (total > limit)
            forwards = Math.Max(0, 3 - (total - limit));
        squad.SkatersPerPart[0] = defenders;
        squad.SkatersPerPart[1] = forwards;
        team.Box.Skaters = defenders + forwards;
        team.GoaliePulled = defenders + forwards == 6;
    }

    /// <summary>[ORIGINAL] Drittelpause 0x84D7(1) (Datei 0x37A86): Energie + 20, dann wie oben min(…, 91) − 1.</summary>
    public static int RecoveredEnergy(int energy) => Math.Min(energy + 20, EnergyCeiling) - 1;

    /// <summary>
    /// Werte eines Mannschaftsteils in einer Reihe aus den eingesetzten Spielern eines Managervereins.
    /// [ORIGINAL] Datei 0x98F8–0x9C63 (0x977F, Weg der Managervereine in der Managerpaarung bzw. ohne Spiel):
    /// <list type="number">
    /// <item>Je Kaderplatz, der eingesetzt ist (+0x0A) und in Reihe/Teil steht (+0x15), höchstens so viele Feldspieler wie
    ///   <see cref="LiveSquad.SkatersPerPart"/> (nur im Spiel); in der Abwehr zählt der Torhüter (+0x15 = 1) immer mit.</item>
    /// <item>Energiefaktor e/2 + 50 % (+0x1B; ohne Spiel und beim Torhüter 100 %) auf die Technik; Kondition (+0x10) und
    ///   Form (+0x12) unverändert; Halbbytes Unterzahl/Überzahl (+0x19) aufsummiert.</item>
    /// <item>Im Spiel: Level 4/5 (0xED8 &lt; 2) Technik − 10 außerhalb der eigenen Reihe (+0x02; [APPROX] im Remake ist die
    ///   Reihe die aufgestellte, der Abzug entfällt); 0xED8 &lt; 5 Technik − 15 auf fremder Position (Gruppe 0x238:0 ≠
    ///   Teil); Bundestrainer (Manager 5) + Länderspiele / 8, höchstens 99.</item>
    /// <item>Abwehr: Technik · 80 % (Feldspieler) bzw. 140 % (Torhüter) – DGROUP 0x4946.</item>
    /// <item>Summen / 3 (auch bei weniger Spielern). Abwehr in Unterzahl oder ohne Torhüter: Technik − (45 − Unterzahl +
    ///   16 · (−1 − Feldspielerdifferenz)), ohne Torhüter zusätzlich / 3, nicht unter 0. Ohne Torhüter im Spiel (Abwehr,
    ///   Sturm bis Minute 57) bei Differenz ≤ 0: Technik und Kondition 0, vor Minute 58 auch die Form.</item>
    /// <item>Sturm in Überzahl (Technik ≠ 0): + Überzahl + 16 · (Differenz − 1).</item>
    /// <item>Im Spiel: Kondition − (100 − Einsatz · Kondition / 100) / 4; alles auf 0 … 100 (0x14CF:0x52).</item>
    /// </list>
    /// </summary>
    public static PartValues ManagedLineValues(LiveSquad squad, int line, int part, int skaterAdvantage, int effort,
        int minute, bool inMatch, bool nationalCoach = false)
    {
        if (part == 0)
            squad.HasGoalie = false;
        int condition = 0, technique = 0, form = 0, overplay = 0, underplay = 0, taken = 0;
        foreach (var p in squad.Players)
        {
            if (!p.Dressed)
                continue;
            var goalie = false;
            if (p.Position == 1 && p.Goalie && part == 0)
            {
                goalie = true;
                squad.HasGoalie = true;
            }
            var inLine = ((p.Position - 1) & 0x7F) == line && p.Position >> 7 == part;
            if (!inLine && !goalie)
                continue;
            if (squad.SkatersPerPart[part] <= taken && inMatch)
                continue;
            var factor = !inMatch || goalie ? 100 : (p.Energy >> 1) + 50;
            var (over, under) = HiddenSkills.ForMatch(p.Player, squad.NationalCoach); // Nationalteam 0x77 (Datei 0x3CB2C)
            overplay += over;
            underplay += under;
            condition += p.Player.Condition;
            var t = p.Player.Technique * factor / 100;
            if (squad.Difficulty < 5 && inMatch && !goalie && TrainingRules.Group(p.Player.Role) - 1 != part && t > 15)
                t -= 15;
            if (nationalCoach && inMatch)
                t = Math.Min(99, t + (p.Player.Caps >> 3));
            if (part == 0)
                t = t * (goalie ? 140 : 80) / 100;
            technique += t;
            form += p.Player.Form;
            if (!goalie)
                taken++;
        }
        condition /= 3;
        technique /= 3;
        form /= 3;
        if (part == 0 && (skaterAdvantage < 0 || !squad.HasGoalie))
        {
            technique -= 45 - underplay + ((-1 - skaterAdvantage) << 4);
            if (!squad.HasGoalie)
                technique /= 3;
            technique = Math.Max(0, technique);
        }
        if (!squad.HasGoalie && inMatch && (part == 0 || minute < 58) && skaterAdvantage <= 0)
        {
            technique = condition = 0;
            if (minute < 58)
                form = 0;
        }
        if (part == 1 && skaterAdvantage > 0 && technique != 0)
            technique += overplay + ((skaterAdvantage - 1) << 4);
        if (inMatch)
            condition -= (100 - effort * condition / 100) / 4;
        return new PartValues(Math.Clamp(condition, 0, 100), Math.Clamp(technique, 0, 100), Math.Clamp(form, 0, 100));
    }

    /// <summary>
    /// Teamwerte und Managerwert eines Managervereins aus der Aufstellung. [ORIGINAL] Datei 0x952C (gerufen u. a. aus der
    /// Spielabrechnung 0x54DAA/0x54FBF und nach Freundschaftsspielen 0x555D6): je Teil die Werte der Reihen 0 … 2 ohne Spiel
    /// (<see cref="ManagedLineValues"/> mit „im Spiel“ = 0), Vereinsfelder 0x18 + Teil = Summe Kondition / 3, 0x1A + Teil =
    /// Summe Technik / 3, 0x1C + Teil = Summe Form / 3. Managerwert (DGROUP 0x974B + 0x1AF · Manager) =
    /// clamp(d / 5 + 20, 0, 40) mit d = Summe über Teil und Reihe der laufenden Summen Kondition − Technik (so im Original:
    /// Reihe 0 zählt dreifach, Reihe 1 doppelt); bei weniger als neun Spielern 100 (Spielwertung, <c>StadiumStatistics</c>).
    /// </summary>
    public static (PartValues Defense, PartValues Attack, int ManagerValue) ManagedTeamValues(LiveSquad squad, bool nineOrMore = true)
    {
        var c = new int[2];
        var t = new int[2];
        var f = new int[2];
        var d = 0;
        for (var part = 0; part < 2; part++)
        {
            for (var line = 0; line < 3; line++)
            {
                var v = ManagedLineValues(squad, line, part, 0, 0, 0, inMatch: false);
                c[part] += v.Condition;
                t[part] += v.Technique;
                f[part] += v.Form;
                d += c[part] - t[part];
            }
        }
        var managerValue = nineOrMore ? Math.Clamp(d / 5 + 20, 0, 40) : 100;
        return (new PartValues(c[0] / 3, t[0] / 3, f[0] / 3), new PartValues(c[1] / 3, t[1] / 3, f[1] / 3), managerValue);
    }

    /// <summary>
    /// Teamwerte aus einer Aufstellung (z. B. Nationalteam des Bundestrainers, #193): wie <see cref="ManagedTeamValues"/>
    /// über einen Kader aus den Spielern der Aufstellung. [ORIGINAL] 0x952C mit Manager 5 (Bundestrainer) schreibt die
    /// Vereinsfelder 0x18–0x1D des Nationalteams; Aufruf nach Freundschaftsspielen (0x555D6) – den Anschluss in
    /// <c>Core/CupsNational*.cs</c> setzt die Leitstelle.
    /// </summary>
    public static (PartValues Defense, PartValues Attack, int ManagerValue) LineupTeamValues(Lineup lineup)
    {
        var squad = new LiveSquad();
        var slots = LineupSlot.All.Where(s => lineup[s] is not null)
            .OrderBy(s => s.Role).ThenBy(s => lineup[s]!.Number);
        foreach (var s in slots)
        {
            var p = lineup[s]!;
            squad.Players.Add(new LivePlayer
            {
                Player = p, Position = LiveSquad.PositionOf(s), Dressed = p.Available, Energy = PreparedPlayerEnergy(p),
            });
        }
        return ManagedTeamValues(squad);
    }
}

public static partial class OriginalMinutes
{
    /// <summary>
    /// Energie je Minute in Managerpaarungen. [ORIGINAL] Datei 0x87F7 (Aufruf 0x37FA8/0x37FB2 nach den Abschlüssen, nur
    /// Managerpaarungen):
    /// <list type="bullet">
    /// <item>Computerverein, je Teil und Reihe 0 … 3: Reihe nicht auf dem Eis und Energie &lt; 90 → + 1, mit 1/5 und
    ///   Kondition (Vereinsfeld 0x18 + Teil) &gt; lrandom(0, 100) + 1 mehr. Reihe auf dem Eis: Minuten auf dem Eis + 1 = m;
    ///   − ((lrandom(0, Einsatz) + 75) / 40 + 1) − 4 (m ≥ lrandom(0, 5)) − 8 (m &gt; lrandom(5, 15)), begrenzt 1 … 99.</item>
    /// <item>Managerverein je eingesetztem Kaderplatz ebenso mit der Kondition des Spielers (+0x10); auf dem Eis vorher
    ///   lrandom(0, 4 + (Kondition &gt; Technik)) = 0 → Belastung + 1 (<see cref="Player.Strain"/>, Datei 0x8C82; A184,
    ///   ohne das Nationalteam, siehe dort). Danach Energie der Reihen = (Summe der Feldspieler, Abwehr mit 90 für den fehlenden
    ///   dritten Platz) / 3 (Vereinsfeld 0x1E + 4 · Teil + Reihe).</item>
    /// </list>
    /// </summary>
    public static void DrainEnergy(LiveTeam team, Random random)
    {
        if (team.Squad is not { } squad)
        {
            for (var part = 0; part < 2; part++)
            {
                var condition = part == 0 ? team.Defense.Condition : team.Attack.Condition;
                for (var line = 0; line < 4; line++)
                {
                    var e = team.LineEnergy(part, line);
                    if (team.Line(part) != line)
                    {
                        if (e < 90)
                            team.SetLineEnergy(part, line, e + 1 + Bonus(condition, random));
                        continue;
                    }
                    var m = ++team.OnIceMinutes[part];
                    team.SetLineEnergy(part, line, Math.Clamp(e - Drain(team.Effort, m, random), 1, 99));
                }
            }
            return;
        }
        var counts = new int[4, 2];
        var sums = new int[2, 4];
        for (var line = 0; line < 4; line++)
        {
            for (var part = 0; part < 2; part++)
            {
                counts[line, part] = squad.SkatersPerPart[part];
                sums[part, line] = counts[line, part] > 1 ? (3 - counts[line, part]) * 90 : 0;
            }
        }
        var started = new bool[2];
        var minutes = 0;
        foreach (var p in squad.Players)
        {
            if (!p.Dressed)
                continue;
            var part = p.Position >> 7;
            if ((p.Position & 0x7F) - 1 != team.Line(part))
            {
                if (p.Energy < 90)
                    p.Energy += 1 + Bonus(p.Player.Condition, random);
            }
            else
            {
                if (!started[part])
                {
                    minutes = ++team.OnIceMinutes[part];
                    started[part] = true;
                }
                // [ORIGINAL] Datei 0x8C53–0x8C82: lrandom(0, 4 + (Kondition > Technik)) = 0 → Belastung (+0x13) + 1, Byte (A184).
                // [APPROX] Das Nationalteam hat im Original eigene Kaderplätze (Satz des Bundestrainers); die + 1 trifft dort nur
                // die Kopie. Im Remake ist der Spieler derselbe wie im Verein – der Zug bleibt, die Belastung im Verein nicht.
                if (OriginalMatch.Rnd(random, 0, 4 + (p.Player.Condition > p.Player.Technique ? 1 : 0)) == 0 && !squad.NationalCoach)
                    p.Player.Strain = (p.Player.Strain + 1) & 0xFF;
                p.Energy = Math.Clamp(p.Energy - Drain(team.Effort, minutes, random), 1, 99);
            }
            var l = p.Position & 0x7F;
            if (l == 0 || p.Goalie)
                continue;
            l--;
            var part2 = ((p.Position - 1) & 0x80) != 0 ? 1 : 0;
            if (counts[l, part2] == 0)
                continue;
            counts[l, part2]--;
            sums[part2, l] += p.Energy;
        }
        for (var part = 0; part < 2; part++)
            for (var line = 0; line < 4; line++)
                team.SetLineEnergy(part, line, sums[part, line] / 3);
    }

    private static int Bonus(int condition, Random random) =>
        OriginalMatch.Rnd(random, 0, 4) == 0 && condition > OriginalMatch.Rnd(random, 0, 100) ? 1 : 0;

    private static int Drain(int effort, int minutes, Random random)
    {
        var d = (OriginalMatch.Rnd(random, 0, effort) + 75) / 40 + 1;
        d += minutes >= OriginalMatch.Rnd(random, 0, 5) ? 4 : 0;
        d += minutes > OriginalMatch.Rnd(random, 5, 15) ? 8 : 0;
        return d;
    }

    /// <summary>[ORIGINAL] Drittelpause 0x84D7(1): Energie aller Reihen und Kaderplätze + 20, höchstens 90.</summary>
    public static void RecoverEnergy(LiveTeam team)
    {
        for (var part = 0; part < 2; part++)
            for (var line = 0; line < 4; line++)
                team.SetLineEnergy(part, line, MatchEngine.RecoveredEnergy(team.LineEnergy(part, line)));
        if (team.Squad is { } squad)
            foreach (var p in squad.Players)
                p.Energy = MatchEngine.RecoveredEnergy(p.Energy);
    }

    /// <summary>
    /// Linienwechsel-Modus und Torhüter der Computervereine, ungerade Minuten der Managerpaarung. [ORIGINAL] Datei 0x80CB
    /// (a, b, Tore a, Tore b, Minute), Stärken aus dem Puffer der Minute (0x368B1, Gewichtung 1):
    /// <list type="number">
    /// <item>Modi (a, b) = (2, 2); (2, 1), wenn a nach Minute 40 führt oder a weniger Feldspieler hat; (1, 2), wenn a nach
    ///   Minute 40 zurückliegt oder mehr Feldspieler hat – dann ohne Stärkevergleich. Sonst mit s = 2 · Stärke (8 Bit,
    ///   übernommen): s_a + 20 &lt; s_b → (3, 2); s_b + 15 &lt; s_a → (2, 3).</item>
    /// <item>Modus schreiben bei Computervereinen und bei Managern mit „Automatisch“.</item>
    /// <item>Nur Computervereine: in Minute 58, 59, 68, 69 mit 5 Feldspielern und genau einem Tor Rückstand Torhüter raus
    ///   (6 Feldspieler); sonst kommt ein draußen stehender Torhüter zurück (− 1 Feldspieler). Gerufen wird nur in ungeraden
    ///   Minuten, wirksam also 59 und 69.</item>
    /// </list>
    /// </summary>
    public static void ComputerLineModes(LiveTeam a, LiveTeam b, int goalsA, int goalsB, int minute, int strengthA, int strengthB)
    {
        int modeA = 2, modeB = 2;
        var skA = a.Box.Skaters;
        var skB = b.Box.Skaters;
        if ((minute > 40 && goalsA > goalsB) || skA < skB)
            (modeA, modeB) = (2, 1);
        if ((minute > 40 && goalsA < goalsB) || skA > skB)
        {
            (modeA, modeB) = (1, 2);
        }
        else
        {
            var sa = (strengthA << 1) & 0xFF;
            var sb = (strengthB << 1) & 0xFF;
            if (sa < sb && sa + 20 < sb)
                (modeA, modeB) = (3, 2);
            if (sb < sa && sb + 15 < sa)
                (modeA, modeB) = (2, 3);
        }
        Apply(a, modeA, goalsA, goalsB, minute);
        Apply(b, modeB, goalsB, goalsA, minute);

        static void Apply(LiveTeam team, int mode, int own, int other, int minute)
        {
            if (team.LineChange is null or LineChangeMode.Automatic)
                team.LineMode = mode;
            if (team.Managed)
                return;
            if (minute is 58 or 59 or 68 or 69 && team.Box.Skaters == 5 && own < other && own + 1 == other)
            {
                team.GoaliePulled = true;
                team.Box.Skaters = 6;
            }
            else if (team.GoaliePulled)
            {
                team.GoaliePulled = false;
                team.Box.Skaters--;
            }
        }
    }

    /// <summary>
    /// Reihenwechsel je Teil, jede Minute der Managerpaarung. [ORIGINAL] Datei 0x8EF2 (mit 0x8E5F vierte Reihe, 0x91FF
    /// Reihen nach Energie):
    /// <list type="bullet">
    /// <item>Manager: „Voll manuell“ → kein Wechsel; sonst ein anderer Modus als „Automatisch“ überschreibt den Modus
    ///   (Vereinsfeld 0x2C Bits 0–2). Abwehr nicht bei herausgenommenem Torhüter.</item>
    /// <item>Reihen nach Energie absteigend (0x91FF): betrachtet 4 Reihen mit vierter Reihe, sonst 3, im Modus 1 nur 2
    ///   ([APPROX] mit vierter Reihe und Modus ≠ 1 ist der Zähler im Original nicht gesetzt).</item>
    /// <item>Modus 1/2: Wechsel nach mehr als 3 Minuten auf dem Eis zur energiestärksten Reihe (bzw. zur zweiten, wenn die
    ///   stärkste schon spielt). Die zusätzliche Bedingung „Energie + 4 &lt; beste Reihe“ vergleicht im Original die Energie
    ///   mit der Nummer der Reihe und greift nie (übernommen).</item>
    /// <item>Modus 3: Reihe des gegnerischen anderen Teils, außer dieser spielt mit der vierten Reihe (dann die
    ///   energiestärkste); die Energieprüfung „+ 8“ vergleicht wieder mit der Nummer (übernommen).</item>
    /// <item>Modus 4 (Minuten-Vorgabe, nur Manager): Zähler der Reihe + 1; über der umgerechneten Vorgabe
    ///   (<see cref="Tactics.MinuteQuota"/>, Datei 0x09140 gegen DGROUP 0xCC73) Zähler 0 und weiter zur
    ///   nächsten Reihe mit Vorgabe ≠ 0 (Ring über 3 bzw. 4 Reihen, höchstens 250 Schritte).</item>
    /// <item>Neue Reihe: Minuten auf dem Eis 0.</item>
    /// </list>
    /// </summary>
    public static void ChangeLines(LiveTeam team, LiveTeam opponent)
    {
        if (team.LineChange is { } managerMode)
        {
            if (managerMode == LineChangeMode.FullyManual)
                return;
            if (managerMode != LineChangeMode.Automatic)
                team.LineMode = (int)managerMode;
        }
        for (var part = 0; part < 2; part++)
        {
            if (part == 0 && team.GoaliePulled)
                continue;
            var fourth = team.Squad?.HasFourthLine(part) ?? false;
            var old = team.Line(part);
            var current = old;
            var (best, second) = LinesByEnergy(team, part, fourth, team.LineMode);
            switch (team.LineMode)
            {
                case 1:
                case 2:
                    if (team.LineEnergy(part, current) + 4 < best || team.OnIceMinutes[part] > 3)
                        current = current == best ? second : best;
                    break;
                case 3:
                    var other = opponent.Line(1 - part);
                    current = team.LineEnergy(part, other) + 8 < best || other == 3 ? best : other;
                    break;
                case 4 when team.Squad is { } squad:
                    var limit = (int l) => squad.Tactics?.MinuteQuota(l, forwards: part == 1) ?? 0;
                    var counter = ++squad.MinuteCounters[part, current];
                    if (limit(current) >= counter)
                        break;
                    squad.MinuteCounters[part, current] = 0;
                    var tries = 0;
                    do
                    {
                        current++;
                        if (current == (fourth ? 1 : 0) + 3)
                            current = 0;
                    }
                    while (tries++ < 250 && limit(current) == 0);
                    break;
            }
            if (current != old)
                team.OnIceMinutes[part] = 0;
            team.SetLine(part, current);
        }
    }

    /// <summary>[ORIGINAL] Datei 0x91FF: Reihennummern nach Energie absteigend (Tausch bei kleinerer Energie), die ersten zwei.</summary>
    public static (int Best, int Second) LinesByEnergy(LiveTeam team, int part, bool fourthLine, int mode)
    {
        var skip = fourthLine ? 0 : 1;
        if (mode == 1)
            skip = 2;
        var index = new[] { 0, 1, 2, 3 };
        var energy = new[] { team.LineEnergy(part, 0), team.LineEnergy(part, 1), team.LineEnergy(part, 2), team.LineEnergy(part, 3) };
        for (var i = 0; i < 3 - skip; i++)
        {
            for (var j = i + 1; j < 4 - skip; j++)
            {
                if (energy[i] >= energy[j])
                    continue;
                (energy[i], energy[j]) = (energy[j], energy[i]);
                (index[i], index[j]) = (index[j], index[i]);
            }
        }
        return (index[0], index[1]);
    }

    /// <summary>[ORIGINAL] 0x37A30: höchstens 150 Züge der Spielerwahl 0x378C5 (R8W3I-03).</summary>
    public const int MaxPenaltyDraws = 150;

    /// <summary>
    /// Bestrafter Spieler eines Managervereins bzw. des Nationalteams des Bundestrainers. [ORIGINAL] Datei 0x378C5:
    /// n = Kaderplätze − 1, unter 5 keiner. Höchstens 150 Versuche (R8W3I-03: der Zähler [bp-0xb] wird vor jedem Zug
    /// geprüft, 0x378ED springt zur Prüfung 0x37A2A, `cmp al,0x96; jnc` an 0x37A30 vor dem Zug 0x378FC): Platz
    /// random(0, n); eingesetzt und aufgestellt, in der Managerpaarung nur auf dem Eis (Reihe = Reihe des Teils; Schalter
    /// DGROUP 0x4E80 [APPROX] aus); Technik &lt; Kondition → mit random(0, 10) &gt; 3, sonst &gt; 7; ein Torhüter bleibt mit
    /// random(0, 2) ≠ 0 verschont.
    /// </summary>
    /// <returns>Der Kaderplatz oder null (Original 100).</returns>
    public static LivePlayer? PenalizedPlayer(LiveTeam team, Random random)
    {
        if (team.PenaltyPlayers is not { } squad)
            return null;
        var n = squad.Players.Count - 1;
        if (n < 5)
            return null;
        var tries = 0;
        while (true)
        {
            var p = squad.Players[OriginalMatch.Rnd(random, 0, n)];
            var line = p.Position & 0x7F;
            var picked = false;
            if (p.Dressed && line != 0 && team.Line(p.Part) + 1 == line)
            {
                picked = p.Player.Technique - p.Player.Condition < 0
                    ? OriginalMatch.Rnd(random, 0, 10) > 3
                    : OriginalMatch.Rnd(random, 0, 10) > 7;
                if (picked && p.Goalie && OriginalMatch.Rnd(random, 0, 2) != 0)
                    picked = false;
            }
            if (picked)
                return p;
            if (++tries >= MaxPenaltyDraws)
                return null;
        }
    }
}
