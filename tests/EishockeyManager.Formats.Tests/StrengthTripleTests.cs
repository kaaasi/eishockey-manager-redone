using EishockeyManager.Formats;

namespace EishockeyManager.Formats.Tests;

/// <summary>
/// Befund A01: Die Datenbank speichert je Verein drei Stärkewerte (0x016C, 150 × 3). Bisher wurde nur der erste gelesen;
/// bei den 86 Europapokal-Gegnern sind die Werte verschieden.
/// </summary>
public class StrengthTripleTests
{
    [Fact]
    public void Parse_KeepsAllThreeStrengthValues()
    {
        var data = new byte[ClubDatabase.FileLength];
        var at = ClubDatabase.StrengthOffset;
        data[at + 0 * 3] = 74; data[at + 0 * 3 + 1] = 74; data[at + 0 * 3 + 2] = 74;
        data[at + 64 * 3] = 79; data[at + 64 * 3 + 1] = 72; data[at + 64 * 3 + 2] = 74;
        data[at + 149 * 3] = 11; data[at + 149 * 3 + 1] = 22; data[at + 149 * 3 + 2] = 33;

        var db = ClubDatabase.Parse(data);

        Assert.Equal(new ClubStrengths(74, 74, 74), db[0].Strengths);
        Assert.Equal(new ClubStrengths(79, 72, 74), db[64].Strengths);
        Assert.Equal(new ClubStrengths(11, 22, 33), db[149].Strengths);
        Assert.Equal(79, db[64].Strength); // Strength bleibt der erste Wert (Abwehr-Basis)
    }

    /// <summary>Originaldatenbank (übersprungen ohne Originaldateien): alle 150 Tripel byte-genau, 86 davon ungleich.</summary>
    [Fact]
    public void OriginalDatabase_AllTriplesPreserved()
    {
        GameData game;
        try { game = GameData.Locate(); }
        catch (DirectoryNotFoundException) { return; }
        var raw = File.ReadAllBytes(game.GraphicsPath(23));
        var db = ClubDatabase.Parse(raw);

        var unequal = 0;
        for (var i = 0; i < ClubDatabase.TotalClubs; i++)
        {
            var at = ClubDatabase.StrengthOffset + i * 3;
            Assert.Equal(new ClubStrengths(raw[at], raw[at + 1], raw[at + 2]), db[i].Strengths);
            if (raw[at] != raw[at + 1] || raw[at] != raw[at + 2])
            {
                unequal++;
                Assert.Equal(ClubCategory.EuropeanOpponent, db[i].Category);
            }
        }
        Assert.Equal(86, unequal);
    }
}
