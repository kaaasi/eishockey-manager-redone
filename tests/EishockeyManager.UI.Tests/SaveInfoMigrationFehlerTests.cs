using System.Text;
using EishockeyManager.UI.Screens.SaveLoad;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Befund R25-G1 (Issue #1026): Die Spielstand-Info (Rechtsklick im Laden-Dialog) liest Remake-Stände über <c>SaveGame.Load</c>.
/// Ein Stand mit falsch typisiertem Feld ergibt „kein Spielstand“ (null), keine Ausnahme. Seit Runde 27 (keine Migrationen) mit
/// Schema 65 durch den Reader bzw. mit Schema 64 in die Versionsmeldung.
/// </summary>
public class SaveInfoMigrationFehlerTests
{
    [Theory]
    [InlineData("""{"kennung":"EISHOCKEY-MANAGER-REDONE-SPIELSTAND","schema":65,"manager":[{"ClubIndex":"x"}]}""")]
    [InlineData("""{"kennung":"EISHOCKEY-MANAGER-REDONE-SPIELSTAND","schema":64,"spieler":[{"Id":1,"Id":2}]}""")]
    public void FromRemake_MalformedMigrationField_ReturnsNull(string json)
    {
        var data = Encoding.UTF8.GetBytes(json);

        Assert.Null(SaveInfo.FromRemake(data));
        Assert.Null(SaveInfo.Read(data, SaveEntryKind.Save));
    }
}
