using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #734 (R15-E6 = A): FILE13→14 ist durch den Zustand der DOSBox-Sitzung aus Runde 10 belastet (5 Eingesetzte nach dem Laden,
/// `file13_2`; frisch geladen voll aufgestellt, Probe des Nutzers 11.11.2026). Konto und aufstellungsabhängige Felder des
/// Managervereins sind darum Eingriff (<see cref="HokCompareClasses.Interventions"/>), nicht Remake-Fehler; die Belastung bleibt
/// verglichen (#730).
/// </summary>
public class AbgleichFile13InterventionTests
{
    [GameDataFact]
    public void File13To14_MoneyAndLineupFields_AreIntervention()
    {
        var report = HokCompare.Compare(OriginalImportTests.OriginalSave("FILE13.HOK"), OriginalImportTests.OriginalSave("FILE14.HOK"),
            ClubDatabase.Load(TestGameData.Load()), HokCompare.DefaultSeed);

        Assert.Null(report.PlayError);
        HokCompareRow Row(string path) => report.Rows.Single(r => r.Path == path);
        foreach (var path in new[]
                 {
                     "Managers[0].Money", "Clubs[6 KREFELDER EV]._autoLineup", "Clubs[6 KREFELDER EV].TeamValues.AttackForm",
                     "Clubs[6 KREFELDER EV].TeamValues.DefenseTechnique",
                 })
        {
            var row = Row(path);
            Assert.Equal(HokCompareClass.Intervention, row.Class);
            Assert.False(row.Deviation, row.ToString());
        }

        // Gegenprobe: Die Belastung der Spieler ist kein Eingriff (#730 bleibt sichtbar), andere Vereine ebenso wenig.
        Assert.All(report.Rows.Where(r => r.Path.EndsWith(".Strain", StringComparison.Ordinal)),
            r => Assert.NotEqual(HokCompareClass.Intervention, r.Class));
        Assert.All(report.Rows.Where(r => r.Path.StartsWith("Clubs[23 ", StringComparison.Ordinal)),
            r => Assert.NotEqual(HokCompareClass.Intervention, r.Class));
    }
}
