using System.Buffers.Binary;
using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// [ORIGINAL] Trainingsplan DGROUP 0xA8A1 + 28 · Manager, Einheit Tag · 4 + Einheit, Wert = Art (FILE05: Standardplan wie
    /// <see cref="TrainingPlan.Prepare"/>). Preisindex DGROUP 0x2BC (an allen Ständen 100). [APPROX] Die Preisstufe bleibt aus dem
    /// Nachspielen (Liga beim Spielstart).
    /// </summary>
    private void ApplyTraining(OriginalSaveGame save, int m)
    {
        var plan = Managers[m].Training;
        var units = save.Block("A8A1").AsSpan(28 * m, 28);
        for (var day = 0; day < TrainingPlan.Days; day++)
            for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
                plan[day, unit] = (TrainingKind)Math.Min((int)units[day * TrainingPlan.UnitsPerDay + unit], TrainingPlan.Kinds - 1);
        plan.PriceIndex = BinaryPrimitives.ReadInt32LittleEndian(save.Block("02BC"));
    }

    /// <summary>
    /// [ORIGINAL] Taktik des Managervereins: Modi DGROUP 0xA6FB (Einsatz), 0xA707 (Angriff), 0xA701 (Reihenwechsel) je Manager; Werte
    /// 0xCAE9 + 0x30 · Manager (Einsatz, Block · 6 + Spielstand) und + 0x18 (Angriff); Minuten 0xCC43 + 8 · Manager + 4 · Teil + Reihe
    /// (Teil 0 Abwehr, 1 Sturm; re-geld.md); „automatisch aufstellen“ 0x375 + 2 · Manager (re-runde-14.md, Taktik 375h).
    /// FILE24/FILE40: Block 0 trägt die Werte der Automatik, Blöcke 1–3 stehen auf 0.
    /// </summary>
    private void ApplyTactics(OriginalSaveGame save, Club club, int m)
    {
        ApplyTacticValues(save, club.Tactics, m);
        // [ORIGINAL] #733: nach dem Laden gilt das gesicherte Byte 0x376 + 2 · m. Laden setzt den Tageslauf 0x54C9F am Schleifenkopf
        // fort (Datei 0x54CC2 → 0x54CEE), der am Ereignistag vor den Managerzügen 0x375 := 0x376 zurückholt und mit 0x426BC neu
        // aufstellt (Datei 0x54D84–0x54D9E). Ein Umschalten im gesicherten Zug (0x375 ≠ 0x376: FILE06, FILE24, FILE32) ist damit
        // verworfen (FILE24 → FILE25, FILE47 „beim erneuten Laden wieder eingesetzt“). [APPROX] Lädt der Stand an einem Tag ohne
        // Ereignis mit Menü (0x54EA0, [bp-0x1A]), zeigt das Original bis zum nächsten Ereignistag noch 0x375; nicht nachgebildet.
        // #908 (Spur zusehen-rest Teil 3): Der Import bildet das Laden nach, nicht das Weiterspielen in derselben Sitzung. FILE72
        // (Manager 1: 0x375/0x376 = 0/1, Reihentausch) → FILE73 (0/0, Tausch bleibt) ist ohne Laden entstanden: Nach einem Laden
        // holte der Schleifenkopf 0x376 = 1 zurück und stellte neu auf (Datei 0x54D84–0x54D9E), der Spieltag sicherte dann 0x376 = 1
        // (Datei 0x54FDC). Beleg ZusehenRestAufstellungTests.
        club.AutoLineupEnabled = save.Block("0375")[2 * m + 1] != 0;
        Managers[m].AutoLineupEnabled = club.AutoLineupEnabled; // A936: Einstellung des Managers (Squad.AutoLineup.Manager.cs)
    }

    /// <summary>
    /// #930 (Review #929 P3-1, FILE72 München 69 69 55 / 70 70 48 statt 68 69 55 / 70 69 48): Nach dem Laden eines Stands mit
    /// 0x375 ≠ 0x376 an einem Tag mit Ereignis folgt auf die Neuaufstellung 0x426BC (nur mit „automatisch aufstellen“) im selben
    /// Schleifendurchlauf <c>0x952C(m, 0)</c> (Datei 0x54DAA): die Vereinsfelder stehen danach auf den Werten der neuen
    /// Aufstellung (<see cref="WriteLineupTeamValues"/>). [APPROX] Nur für den Manager am Zug mit 0x375 ≠ 0x376 (dessen Neuaufstellung
    /// läuft beim Beginn des Zugs; die der übrigen Sätze bildet der Import nicht nach, FILE06 <c>OriginalExportRoundTripTests</c>);
    /// übrige Sätze behalten die gesicherten Felder, auch wenn die Wanderung 0x9D1B sie seit dem letzten Ereignistag bewegt hat.
    /// </summary>
    private void ApplyLoadLineupValues(OriginalSaveGame save)
    {
        if (WorldOnly || !IsListChangeDay(CurrentDay))
            return;
        for (var m = 0; m < Managers.Count; m++)
        {
            if (m != CurrentManager || save.Block("0375")[2 * m] == save.Block("0375")[2 * m + 1])
                continue;
            WriteLineupTeamValues(Clubs[Managers[m].ClubIndex]);
        }
    }

    /// <summary>Modi, Werte und Minuten des Satzes <paramref name="m"/> (0–3 Manager, 5 Bundestrainer; Teil 4b).</summary>
    private static void ApplyTacticValues(OriginalSaveGame save, Tactics tactics, int m)
    {
        tactics.EffortMode = (TacticMode)Math.Min((int)save.Block("A6FB")[m], 2);
        tactics.AttackMode = (TacticMode)Math.Min((int)save.Block("A707")[m], 2);
        tactics.LineChange = (LineChangeMode)Math.Min((int)save.Block("A701")[m], (int)LineChangeMode.FullyManual);
        var values = save.Block("CAE9").AsSpan(0x30 * m, 0x30);
        for (var b = 0; b < Tactics.Blocks; b++)
        {
            // Erst leeren, dann Grundwert, dann Zuschläge: die Grenzen hängen jeweils von den anderen Feldern ab.
            for (var i = 0; i < Tactics.Situations; i++)
            {
                tactics.SetEffort(b, (ScoreSituation)i, 0);
                tactics.SetAttack(b, (ScoreSituation)i, 0);
            }
            for (var i = 0; i < Tactics.Situations; i++)
            {
                tactics.SetEffort(b, (ScoreSituation)i, values[6 * b + i]);
                tactics.SetAttack(b, (ScoreSituation)i, values[0x18 + 6 * b + i]);
            }
        }
        var minutes = save.Block("CC43").AsSpan(8 * m, 8);
        for (var part = 0; part < 2; part++)
            for (var line = 0; line < Lineup.Blocks; line++)
                tactics.SetMinutes(line, part == 1, 0);
        for (var part = 0; part < 2; part++)
            for (var line = 0; line < Lineup.Blocks; line++)
                tactics.SetMinutes(line, part == 1, minutes[4 * part + line]);
    }
}
