using System.IO.Compression;
using System.Text.Json.Nodes;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #914 (Gesamtreview #913): Die Migrationstests der Schemata 51–53 bauen Altstände aus dem aktuellen Stand nach. Diese Dateien
/// schrieb tatsächlich der Code der jeweiligen Schemastufe (synthetische Datenbank, keine Originaldaten):
/// <c>TestGames.Create(8)</c>, zehn Tage <c>Continue()</c>, <c>SaveGame.Save</c> an den Ständen 825aaa8 (Schema 51, main), 4694f3a (52, main) und
/// 9278c82^ (letzter Stand von 53 auf dem Spurzweig zufallsstrom, nie auf main). Zwischenstände (7a715a5) taugen nicht: dort fehlten Felder der Folgeteile.
/// </summary>
public class MigrationEchteStaendeTests
{
    [Theory]
    [InlineData(51)]
    [InlineData(52)]
    [InlineData(53)]
    public void Migration_RealSavesOfSchema51To53_LoadAndPlayOn(int schema)
    {
        using var stream = typeof(MigrationEchteStaendeTests).Assembly
            .GetManifestResourceStream($"Fixtures.schema{schema}-synthetisch.ems.gz")!;
        using var unzip = new GZipStream(stream, CompressionMode.Decompress);
        using var bytes = new MemoryStream();
        unzip.CopyTo(bytes);
        Assert.Equal(schema, JsonNode.Parse(bytes.ToArray())!["schema"]!.GetValue<int>());

        var loaded = SaveGame.Load(bytes.ToArray());

        Assert.True(loaded.Success, loaded.Error);
        var game = loaded.Game!;
        Assert.Empty(game.ValidatePlayerIdentity());
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            game.Continue();
        }
        game.StartNextSeason();
        Assert.Empty(game.ValidatePlayerIdentity());
        var saved = SaveGame.Save(game);
        var again = SaveGame.Load(saved);
        Assert.True(again.Success, again.Error);
        Assert.Equal(saved, SaveGame.Save(again.Game!));
        Assert.Equal(SaveGame.SchemaVersion, JsonNode.Parse(saved)!["schema"]!.GetValue<int>());
    }
}
