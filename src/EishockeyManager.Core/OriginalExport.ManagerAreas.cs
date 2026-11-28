using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// Umkehrung von Teil 4a des Imports (<c>OriginalImport.ManagerAreas.cs</c> ff.): Börse, Aktienbesitz, Werbung, Trainingsplan, Taktik und
    /// „automatisch aufstellen“, Serien, Rekorde, Kopf-an-Kopf-Liste. Fundstellen wie dort.
    /// </summary>
    private void WriteOriginalRest(OriginalSaveWriter w, int[] slotOf)
    {
        WriteStocks(w);
        var contractNumber = 1;
        for (var m = 0; m < Managers.Count; m++)
        {
            var manager = Managers[m];
            var f = Finance(manager);
            var holdings = w.Block("D214");
            for (var i = 0; i < f.Holdings.Length; i++)
            {
                OriginalExport.Write32(holdings, 32 * m + 8 * i, OriginalExport.Thirds(f.Holdings[i].PurchaseCost));
                OriginalExport.Write32(holdings, 32 * m + 8 * i + 4, f.Holdings[i].Count);
            }
            WriteSponsoring(w, m, f.Sponsoring, ref contractNumber);
            var units = w.Block("A8A1");
            for (var day = 0; day < TrainingPlan.Days; day++)
                for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
                    units[28 * m + day * TrainingPlan.UnitsPerDay + unit] = (byte)manager.Training[day, unit];
            if (m == 0)
                OriginalExport.Write32(w.Block("02BC"), 0, manager.Training.PriceIndex);
            if (WorldOnly)
                w.Block("0375")[2 * m] = w.Block("0375")[2 * m + 1] = 1; // [APPROX] FILE11, FILE18–FILE21: 1
            else
            {
                var club = Clubs[manager.ClubIndex];
                WriteTacticValues(w, club.Tactics, m);
                // [ORIGINAL] 0x375 + 2 · m aktuell, 0x376 + 2 · m gesichert (gleich außerhalb der Spiele des Tages).
                w.Block("0375")[2 * m] = w.Block("0375")[2 * m + 1] = (byte)(manager.AutoLineupEnabled ? 1 : 0); // A936
            }
        }
        // [ORIGINAL] A614: nächste Vertragsnummer der Banden DGROUP 0x17C (Spielstart 1, FILE76 nach zwei Bandenverträgen 3).
        w.Block("017C")[0] = OriginalExport.Byte(contractNumber);
        if (Managers.Count > 0)
            OriginalExport.Write32(w.Block("02BC"), 0, Managers[0].Training.PriceIndex);
        else
            OriginalExport.Write32(w.Block("02BC"), 0, 100);
        WriteTeamTables(w, slotOf);
    }
}
