using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #857 (Review <c>docs/reviews/2026-11-14-neuer-manager.md</c>): Nach dem Platztausch eines neuen Managers trägt der Vereinssatz
/// die Kennung des gewählten Vereins – [ORIGINAL] 0x36169 Schalter 0x80 tauscht Name, +0x16, Logo +0x34 und Farbcode +0x3B
/// (Datei 0x3628F–0x363F6). Der HOK-Export schrieb Logo und Farbcode aus dem Datenbanksatz des Platzes (<c>database[club.Index]</c>),
/// also die Werte des Verdrängten.
/// </summary>
public sealed class NeuerManagerExportTests
{
    private const int Chosen = 7;

    [GameDataFact]
    public void Export_AfterSwap_WritesIdentityOfChosenClub()
    {
        var raw = OriginalExportTests.RawDatabase();
        var database = ClubDatabase.Parse(raw);
        for (var seed = 1; seed < 40; seed++)
        {
            var setup = new GameSetup();
            setup.AddManager(new ManagerSetup { Name = "Joerg", Head = 6, ClubIndex = 2 });
            var game = NewGame.Create(database, setup, seed);
            for (var i = 0; i < 3; i++)
                TestSeason.PlayMatchDay(game);
            var r = game.AddManager(new ManagerSetup { Name = "Neu", Head = 4, ClubIndex = Chosen }).ClubIndex;
            // Nur Paare mit unterscheidbarer Kennung (Farbcode und Logo verschieden) beweisen etwas.
            if (r == Chosen || database[r].ColorCode == database[Chosen].ColorCode || database[r].LogoNumber == database[Chosen].LogoNumber)
                continue;

            var save = OriginalSaveGame.Read(OriginalExport.Export(game, raw));
            var slots = OriginalExport.Slots(game);
            var records = save.Block("far:9657");
            Span<byte> At(int club) => records.AsSpan(slots[club] * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength);

            // Platz r: Kennung des gewählten Vereins, Managerbit 0x80 und Manager 2.
            Assert.Equal(database[Chosen].ColorCode, BitConverter.ToInt16(At(r)[0x3B..0x3D]));
            Assert.Equal(database[Chosen].LogoNumber | 0x80, At(r)[0x34]);
            Assert.Equal(2, At(r)[0x16]);
            Assert.Equal(OriginalExport.DatabaseClubName(raw, Chosen), At(r)[..OriginalImport.ClubNameLength].ToArray());
            // Platz Chosen: Kennung des Verdrängten, ohne Manager.
            Assert.Equal(database[r].ColorCode, BitConverter.ToInt16(At(Chosen)[0x3B..0x3D]));
            Assert.Equal(database[r].LogoNumber, At(Chosen)[0x34]);
            Assert.Equal(OriginalExport.DatabaseClubName(raw, r), At(Chosen)[..OriginalImport.ClubNameLength].ToArray());
            return;
        }
        Assert.Fail("Kein Platztausch in 40 Spielständen.");
    }
}
