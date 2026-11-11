using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-fix, Teil 4: „automatisch aufstellen“ und Aufstellung des Managervereins (Befunde aus
/// <c>AbgleichOriginalPaareTests</c>), je Issue ein Regressionstest am Original-Spielstand.
/// </summary>
public class AbgleichFixLineupTests
{
    private static GameState Import(string file, int seed = 1993) => GameState.ImportOriginal(
        OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file))), ClubDatabase.Load(TestGameData.Load()), seed);

    /// <summary>
    /// #733: Nach dem Laden gilt die gesicherte Einstellung (Block 0375 Byte 2 · m + 1 = DGROUP 0x376 + 2 · m), nicht das aktuelle
    /// Byte. [ORIGINAL] Laden setzt den Tageslauf 0x54C9F mit Argument ≠ 0 am Schleifenkopf fort (Datei 0x54CC2 → 0x54CEE); am
    /// Ereignistag holt Datei 0x54D84–0x54D9E vor den Managerzügen (Hauptmenü Datei 0x54EB2) 0x375 := 0x376 zurück und stellt mit
    /// 0x426BC neu auf. Belege: FILE24 `00 01` → FILE25 `01 01` ohne Eingriff; FILE47 (Bundestrainer `00 01`, Aufstellung geleert)
    /// „beim erneuten Laden sind die Spieler wieder eingesetzt“ (Angabe Nutzer); FILE33 `00 01` → FILE34 `00 00` nur, weil der
    /// Nutzer vor dem Spieltag erneut „Manuell“ stellte. Vorher rot: Der Import las das aktuelle Byte 0x375 (FILE24: aus).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE24.HOK", true)]
    [InlineData("FILE32.HOK", true)]
    [InlineData("FILE06.HOK", true)]
    [InlineData("FILE34.HOK", false)]
    [InlineData("FILE05.HOK", true)]
    public void Import_AutoLineup_UsesSavedByte(string file, bool expected)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var game = Import(file);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.Equal(save.Block("0375")[1] != 0, expected);
        Assert.Equal(expected, club.AutoLineupEnabled);
    }
}
