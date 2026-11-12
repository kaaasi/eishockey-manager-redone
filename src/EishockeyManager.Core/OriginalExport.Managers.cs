using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>Managersatz 965B + 0x1AF · Satz: Feld <paramref name="offset"/> (Feldkarte „Manager, ManagerFinance“).</summary>
    private static int ManagerField(int record, int offset) => record * OriginalSaveGame.ManagerRecordLength + offset;

    /// <summary>
    /// Managersätze 0–3 (Manager), 4 (Bank/Transfermarkt), 5 (Bundestrainer) und die Blöcke je Manager (Talentpreis 0x20D,
    /// Zinsdrittel 0x2A0, Jugend 0xA075, Kennzeichen „neue Nachricht“ 0x5A50).
    /// <list type="bullet">
    /// <item>[ORIGINAL] wie Import <c>ApplyManager</c>/<c>ApplyOriginalRest</c>: +0x00 Name, +0x14 Verein (Satzplatz), +0xE4 Eintritt,
    ///   +0xEB Liga-Stufe, +0xEC Talentsuche, +0xED/+0xFE/+0x114/+0x115/+0x183/+0x187/+0x18B Zuschauerstatistik, +0x116–0x13A Stadion,
    ///   +0x173 Werbebewertung, +0x177 Über-/Unterzahl, +0x18F Konto, +0x19B Darlehen (Eintrag 4 = Bank).</item>
    /// <item>[ORIGINAL] +0xE9: 1 bei „eine Saison, ein Manager“ (Datei 0x29CFA–0x29D23), sonst 30 (re-runde-14.md U15-c).</item>
    /// <item>[ORIGINAL] Managerkopf DGROUP 0x178 + m (das zeigt das Hauptmenü) und als Kopie +0x13 (an allen Ständen gleich; unbenutzte
    ///   Sätze 2, 3, 4, 0, 0 wie FILE01); Beleg Bilder gegen kb\0, Review 2026-11-10 B3. Ungedeutet und nach FILE01 vorbelegt: +0xE5 = 16,
    ///   +0xE6 = 1 (Stände 1–3), +0xE7/+0xE8/+0xEA = 30; Satz 4/5 +0x14 = 0xFD, Satz 5 +0xEB = 50 ohne Bundestrainer.</item>
    /// <item>[APPROX] Bankseite (Satz 4, Eintrag m) = Gegenbuchung des Bankeintrags von Manager m (FILE08: −2.103.000 / +2.103.000);
    ///   Managerdarlehen: Schuldner Eintrag Gläubiger negativ, Gläubiger Eintrag Schuldner positiv (an keinem Stand belegt).</item>
    /// </list>
    /// </summary>
    private void WriteManagers(OriginalSaveWriter w, int[] slotOf)
    {
        var rec = w.Block("965B");
        for (var record = 0; record < 6; record++)
        {
            foreach (var o in new[] { 0xE6, 0xE7, 0xE8, 0xE9, 0xEA })
                rec[ManagerField(record, o)] = 30;
            if (record is >= 1 and <= 3)
                rec[ManagerField(record, 0x13)] = (byte)(record + 1);
            if (record >= OriginalImport.MarketRecord)
                rec[ManagerField(record, 0x14)] = 0xFD;
        }
        rec[ManagerField(OriginalImport.NationalRecord, 0xEB)] = 50;
        // [ORIGINAL] Unbenutzte Managerplätze: Talentpreis wie beim Spielstart (FILE01: 270.000 Drittel-DM), „automatisch aufstellen“ 1.
        for (var m = Managers.Count; m < GameSetup.MaxManagers; m++)
            OriginalExport.Write32(w.Block("020D"), 4 * m, OriginalExport.Thirds(TalentScout.StartPrice));
        for (var m = Managers.Count; m < OriginalImport.NationalRecord; m++)
            w.Block("0375")[2 * m] = w.Block("0375")[2 * m + 1] = 1;
        var oneSeasonOneManager = Setup.Duration == GameDuration.EineSaison && Managers.Count == 1;

        for (var m = 0; m < Managers.Count; m++)
        {
            var manager = Managers[m];
            var f = Finance(manager);
            int F(int o) => ManagerField(m, o);
            OriginalSaveWriter.Name(manager.Name).CopyTo(rec, F(0));
            rec[F(0x13)] = (byte)manager.Head; // [ORIGINAL] Kopie des Kopfs 0x178 + m (an allen Ständen gleich)
            w.Block("0178")[m] = (byte)manager.Head; // [ORIGINAL] Kopf, den das Hauptmenü zeigt (Review 2026-11-10 B3)
            rec[F(0x14)] = (byte)slotOf[manager.ClubIndex];
            rec[F(0xE4)] = OriginalExport.Byte(f.Stadium.TicketPrice);
            rec[F(0xE5)] = 16;
            rec[F(0xE6)] = (byte)DebCupRoundOf(manager.ClubIndex);
            rec[F(0xE9)] = (byte)(oneSeasonOneManager ? 1 : 30);
            rec[F(0xEB)] = (byte)f.CostLeague;
            rec[F(0xEC)] = OriginalExport.Byte(f.TalentSearch?.TicksLeft ?? 0);

            // Zuschauerstatistik (Datei 0x54664–0x54760); Gegner als Satzplatz, „noch keiner“ (−1) → Satz 0 wie beim Spielstart.
            var stats = f.HomeStatisticsForExport; // ohne die Statistik anzulegen (Export ändert den Spielstand nicht)
            rec[F(0xED)] = OriginalExport.Byte(stats.Games);
            for (var i = 0; i < Math.Min(stats.Curve.Count, 22); i++)
                rec[F(0xFE) + i] = OriginalExport.Byte(stats.Curve[i]);
            rec[F(0x114)] = (byte)(stats.RecordOpponent >= 0 ? slotOf[stats.RecordOpponent] : 0);
            rec[F(0x115)] = (byte)(stats.MinimumOpponent >= 0 ? slotOf[stats.MinimumOpponent] : 0);
            OriginalExport.Write32(rec, F(0x183), stats.Total);
            OriginalExport.Write32(rec, F(0x187), stats.Record);
            OriginalExport.Write32(rec, F(0x18B), stats.Minimum);

            // Stadion: Stufe/Ziel je Bereich +0x116 + 2 · i, Restwochen +0x128 + Projekt, Plätze +0x133/+0x135/+0x137/+0x139.
            var stadium = f.Stadium;
            for (var i = 0; i < Stadium.FeatureCount; i++)
            {
                var feature = (StadiumFeature)i;
                rec[F(0x116 + 2 * i)] = OriginalExport.Byte(stadium.Level(feature));
                rec[F(0x117 + 2 * i)] = OriginalExport.Byte(stadium.TargetLevel(feature));
            }
            for (var p = 0; p < Stadium.ProjectCount; p++)
                rec[F(0x128 + p)] = OriginalExport.Byte(p switch
                {
                    Stadium.SeatsProject => stadium.WeeksLeft(StadiumProject.Sitzplaetze, default),
                    Stadium.StandingProject => stadium.WeeksLeft(StadiumProject.Stehplaetze, default),
                    _ => stadium.WeeksLeft(StadiumProject.Feature, (StadiumFeature)p),
                });
            OriginalExport.Write16(rec, F(0x133), stadium.Seats);
            OriginalExport.Write16(rec, F(0x135), stadium.SeatsTarget);
            OriginalExport.Write16(rec, F(0x137), stadium.Standing);
            OriginalExport.Write16(rec, F(0x139), stadium.StandingTarget);

            OriginalExport.Write16(rec, F(0x173), f.SponsorRating);
            for (var k = 0; k < manager.PowerPlayCounts.Length; k++)
                OriginalExport.Write16(rec, F(0x177 + 2 * k), manager.PowerPlayCounts[k]);
            OriginalExport.Write32(rec, F(0x18F), OriginalExport.Thirds(manager.Money));

            // Darlehen +0x19B + 4 · i (i = 0–3 Manager, 4 Bank), negativ = Schuld, Drittel-DM.
            var bank = OriginalExport.Thirds(f.Bank.Balance);
            OriginalExport.Write32(rec, F(0x19B + 4 * 4), bank);
            OriginalExport.Write32(rec, ManagerField(OriginalImport.MarketRecord, 0x19B + 4 * m), -bank);

            OriginalExport.Write32(w.Block("020D"), 4 * m, OriginalExport.Thirds(f.TalentPrice));
            OriginalExport.Write32(w.Block("02A0"), 4 * m, f.InterestAccrualThirds);
            w.Block("5A50")[m] = (byte)(manager.HasNewMessage ? 1 : 0);

            // Jugend DGROUP 0xA075 + 0x15 · m: Stärke +k, Platz − 1 +3 + k, Liga − 1 +6 + k, Förderung (Drittel-DM) +9 + 4 · k.
            var youth = w.Block("A075");
            foreach (var team in f.Youth)
            {
                var k = (int)team.Kind;
                youth[0x15 * m + k] = OriginalExport.Byte(team.Strength);
                youth[0x15 * m + 3 + k] = OriginalExport.Byte(team.Rank - 1);
                youth[0x15 * m + 6 + k] = OriginalExport.Byte(team.League - 1);
                OriginalExport.Write32(youth, 0x15 * m + 9 + 4 * k, OriginalExport.Thirds(team.Funding));
            }
        }
        // [ORIGINAL] Ein Darlehensfeld je Paar, gespiegelt (Buchung Datei 0x591E2–0x5932B: eigenes Feld ± v, das des anderen ∓ v):
        // der Saldo je Partner (ManagerLoanNet). Spur kader-rest, Teil 3 (A196); vorher je Kredit geschrieben, Kredite in beide
        // Richtungen überschrieben einander.
        for (var a = 0; a < Managers.Count && a < OriginalImport.MarketRecord; a++)
            for (var b = 0; b < Managers.Count && b < OriginalImport.MarketRecord; b++)
                if (a != b)
                    OriginalExport.Write32(rec, ManagerField(a, 0x19B + 4 * b), OriginalExport.Thirds(ManagerLoanNet(a, b)));
        // [ORIGINAL] Konto des Nationalteams: Satz 5 +0x18F (DGROUP 0xA055), Drittel-DM.
        OriginalExport.Write32(rec, ManagerField(OriginalImport.NationalRecord, 0x18F), OriginalExport.Thirds(NationalAccount));
    }
}

public sealed partial class ManagerFinance
{
    /// <summary>Zuschauerstatistik für den Export, ohne sie anzulegen (<see cref="HomeStatistics"/> legt sie beim ersten Zugriff an).</summary>
    internal StadiumStatistics HomeStatisticsForExport => _homeStatistics ?? new StadiumStatistics();
}
