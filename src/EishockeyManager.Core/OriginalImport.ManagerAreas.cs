using System.Buffers.Binary;
using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class Manager
{
    /// <summary>Kennzeichen „neue Nachricht“ aus dem Import (DGROUP 0x5A50 + Manager).</summary>
    internal void SetNewMessage(bool value) => _newMessage = value;
}

/// <summary>
/// Import eines Original-Spielstands, Teil 4a (Spur hok-import, Runde 14, E14-4 = A): Bereiche, die Teil 2 noch aus dem
/// Nachspielen übernahm. Hier: Managerdarlehen und Sparbuch, Nationalkonto, Börse, Werbung, Trainingsplan, Taktik und
/// „automatisch aufstellen“, Serien, Rekorde und Kopf-an-Kopf-Liste, Strafminuten und Platzfelder, Über-/Unterzahl,
/// Talentsuche, Kennzeichen „neue Nachricht“. Fundstellen je Feld in der Feldkarte (<c>docs/formate/spielstand-original.md</c>).
/// </summary>
public sealed partial class GameState
{
    private void ApplyOriginalRest(OriginalSaveGame save, int[] clubOf)
    {
        var records = save.Block("965B");
        ManagerLoans.Clear();
        for (var m = 0; m < Managers.Count; m++)
        {
            var record = records.AsSpan(m * OriginalSaveGame.ManagerRecordLength, OriginalSaveGame.ManagerRecordLength);
            var manager = Managers[m];
            ApplyLoans(m, record);
            ApplyHoldings(save, m);
            ApplySponsoring(save, m);
            ApplyTraining(save, m);
            if (!WorldOnly)
                ApplyTactics(save, Clubs[manager.ClubIndex], m);
            // [ORIGINAL] Über-/Unterzahl 965B+0x177 (DGROUP 0x97D2 + 2 · k), Wörter.
            for (var k = 0; k < manager.PowerPlayCounts.Length; k++)
                manager.PowerPlayCounts[k] = BinaryPrimitives.ReadInt16LittleEndian(record[(0x177 + 2 * k)..]);
            // [ORIGINAL] Talentsuche: Restzähler 965B+0xEC (0 = keine Suche).
            var ticks = record[0xEC];
            Finance(manager).TalentSearch = ticks == 0 ? null : new TalentSearch { TicksLeft = ticks };
            manager.SetNewMessage(save.Block("5A50")[m] != 0);
        }
        ApplyNationalAccount(records);
        ApplyStocks(save);
        ApplyTeamTables(save, clubOf);
        ApplyWatches(save, clubOf);
    }

    /// <summary>
    /// Markierung zum Zusehen und Sicherung des Schalters „WM-Spiele“ aus dem HOK (Spur zusehen-rest, A834).
    /// <list type="bullet">
    /// <item>[ORIGINAL] Vereinsfeld 0x34 Bits 6–7 (<see cref="Club.WatchMode"/>, Schreiber 0x0AE2B) je Satzplatz; ersetzt die
    ///   Markierung aus Aufbau und Nachspielen (<see cref="MarkStartWatches"/>, Ernennung, Schalter „WM-Spiele“). An allen Ständen
    ///   stehen die Managervereine im Modus 2, dazu teils ein Land (FILE11, FILE18–FILE21, FILE47–FILE49A, FILE54); Modus 1 und
    ///   weitere Vereine (Vereinsfenster) kommen an keinem Stand vor. Sätze über die Vereinsliste hinaus (Europapokal-Gegner)
    ///   führt das Remake nicht.</item>
    /// <item>[ORIGINAL] DGROUP 0x22C (Block 0x21D + 15, HOK 0x0A5D7) ist die Sicherung aus Datei 0x5541F; das Remake führt sie nur
    ///   während des Amts des Bundestrainers (<c>GameState.Watch.cs</c>), also hier nur mit Amt (Satz des Landes DGROUP 0x6AC
    ///   +0x16 = 6 wie <c>ApplyNationalCoachOffice</c>), sonst keine offene Sicherung. FILE18–FILE21, FILE47/FILE48: 1,
    ///   FILE49/FILE49A: 0.</item>
    /// </list>
    /// </summary>
    private void ApplyWatches(OriginalSaveGame save, int[] clubOf)
    {
        var records = save.Block("far:9657");
        for (var slot = 0; slot < clubOf.Length; slot++)
            if (clubOf[slot] < Clubs.Count)
                Clubs[clubOf[slot]].WatchMode = records[slot * OriginalSaveGame.ClubRecordLength + 0x34] >> 6;
        var nation = save.Block("06AC")[0];
        var inOffice = records[nation * OriginalSaveGame.ClubRecordLength + 0x16] == 6;
        ImportWorldCupOptionBackup(inOffice ? save.Block("021D")[0x22C - 0x21D] != 0 : null);
    }
}
