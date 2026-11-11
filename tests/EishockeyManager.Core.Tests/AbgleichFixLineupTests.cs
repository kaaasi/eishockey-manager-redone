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

    /// <summary>
    /// #748, Beleg am Original: Die Vereinsfelder 0x18–0x1D des Managervereins stehen in den Ständen auf den Werten aus Reihe 0–2
    /// der Aufstellung ([ORIGINAL] 0x952C, Tageslauf Datei 0x54DAA vor den Zügen, 0x54FBF danach), nicht auf Lader und Wanderung.
    /// Alle Stände mit „automatisch aufstellen“ (FILE13/14 mit der Aufstellung des Stands). Nicht hier: die Stände im Handbetrieb
    /// FILE38–40, FILE44/46 (Original 7–27 tiefer): Dort rechnete das Original die Felder vor einer späteren Änderung der Aufstellung
    /// von Hand oder rechnet Lücken anders; nicht untersucht (#748 bleibt dafür offen).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE02.HOK")]
    [InlineData("FILE05.HOK")]
    [InlineData("FILE24.HOK")]
    [InlineData("FILE03.HOK")]
    [InlineData("FILE13.HOK")]
    [InlineData("FILE14.HOK")]
    [InlineData("FILE16A.HOK")]
    [InlineData("FILE17.HOK")]
    [InlineData("FILE25.HOK")]
    [InlineData("FILE36.HOK")]
    [InlineData("FILE37.HOK")]
    public void Original_ManagerClubFields_ComeFromLineup(string file)
    {
        var game = Import(file);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.Equal(Fields(MatchEngine.LineupTeamValues(club.Lineup)), Absolute(club));
    }

    /// <summary>
    /// #748: Nach einem Tag mit Ereignis stehen die Felder des Managervereins wieder auf den Werten der Aufstellung. Vorher rot:
    /// Das Remake rechnete 0x952C nur für den Bundestrainer (Satz 5); die Managervereine behielten Lader und Wanderung
    /// (FILE13→14 KREFELD 16–43 Punkte daneben).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE02.HOK")]
    [InlineData("FILE24.HOK")]
    public void EventDay_ManagerClubFields_FromLineup(string file)
    {
        var game = Import(file);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        for (var i = 0; i < 3; i++)
        {
            TestSeason.PlayMatchDay(game);
            if (game.IsListChangeDay(game.Day))
                Assert.Equal(Fields(MatchEngine.LineupTeamValues(club.Lineup)), Absolute(club));
        }
    }

    private static (int, int, int, int, int, int) Fields((PartValues D, PartValues A, int) v) =>
        (v.D.Condition, v.A.Condition, v.D.Technique, v.A.Technique, v.D.Form, v.A.Form);

    private static (int, int, int, int, int, int) Absolute(Club club)
    {
        var b = MatchEngine.ComputerTeamValue(club);
        var v = club.TeamValues!.Value;
        return (b + v.DefenseCondition, b + v.AttackCondition, b + v.DefenseTechnique, b + v.AttackTechnique, v.DefenseForm, v.AttackForm);
    }
}
