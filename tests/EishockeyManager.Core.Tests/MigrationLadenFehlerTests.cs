using System.Text;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Befund R25-G1 (Issue #1026, <c>docs/reviews/2026-11-25-gesamtreview-runde-25.md</c>): Eine Altdatei mit falsch typisiertem
/// Migrationsfeld darf beim Laden keine Ausnahme werfen, sondern muss das Fehlerergebnis liefern (wie fremde Dateien oder neuere
/// Versionen). Geprüft für die Migrationen 63 → 64 (<c>SaveGame.Migration.Aufstellung.cs</c>) und 64 → 65
/// (<c>SaveGame.Migration.SaisonGeld.cs</c>).
/// </summary>
public class MigrationLadenFehlerTests
{
    private const string Head = "\"kennung\":\"EISHOCKEY-MANAGER-REDONE-SPIELSTAND\"";

    public static TheoryData<string> BrokenOldSaves() => new()
    {
        // Probe aus dem Review: ClubIndex als Zeichenkette (Migration 63 → 64).
        $$"""{{{Head}},"schema":63,"manager":[{"ClubIndex":"x"}]}""",
        // ClubIndex als Kommazahl bzw. außerhalb von int.
        $$"""{{{Head}},"schema":63,"manager":[{"ClubIndex":1.5}]}""",
        $$"""{{{Head}},"schema":63,"manager":[{"ClubIndex":99999999999}]}""",
        // Vereinseinstellung falsch typisiert.
        $$"""{{{Head}},"schema":63,"vereine":[{"_autoLineup":"ja"}],"manager":[{"ClubIndex":0}]}""",
        // Doppelte Schlüssel in einem Objekt, das die Migration anfasst (63 → 64 bzw. 64 → 65).
        $$"""{{{Head}},"schema":63,"manager":[{"ClubIndex":0,"ClubIndex":1}]}""",
        $$"""{{{Head}},"schema":63,"spieler":[{"Id":1,"Id":2}]}""",
        $$"""{{{Head}},"schema":64,"spieler":[{"Id":1,"Id":2}]}""",
    };

    [Theory]
    [MemberData(nameof(BrokenOldSaves))]
    public void Load_MalformedMigrationField_ReturnsError(string json)
    {
        var data = Encoding.UTF8.GetBytes(json);

        var result = SaveGame.Load(data);

        Assert.Null(result.Game);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }
}
