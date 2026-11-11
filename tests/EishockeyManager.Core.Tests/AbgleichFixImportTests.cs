using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-fix, Teil 3 (Start und Import): Befunde aus dem Abgleich gegen die Spielstandpaare des Originals
/// (<c>AbgleichOriginalPaareTests</c>), je Issue ein Regressionstest am Original-Spielstand.
/// </summary>
public class AbgleichFixImportTests
{
    private static GameState Import(string file, int seed = 1993) => GameState.ImportOriginal(
        OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file))), ClubDatabase.Load(TestGameData.Load()), seed);

    /// <summary>
    /// #749: Poolplatz eines nach Vertragsende verlorenen Spielers (R15-E3 = C) bleibt im Import vergeben. [ORIGINAL] FILE46 Pool 196
    /// KJELLBERG: +0x1A = 0 (Manager 0), weder im Kader noch auf dem Markt; der Listenwechsel 0x494EF zieht nur Besitzer 6.
    /// Vorher rot: Der Import führte den Platz als frei (Besitzer &lt; Managerzahl, aber kein Kaderspieler).
    /// </summary>
    [GameDataFact]
    public void Import_LostPlayerPoolSlot_StaysTaken()
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE46.HOK")));
        var entry = OriginalImport.PoolEntry(save, 196);
        Assert.Equal(0, entry[0x1A]);

        var game = Import("FILE46.HOK");
        var slot = game.TransferMarket.Pool.Single(s => s.Index == 196);
        Assert.Equal("KJELLBERG", slot.Player.Name);
        Assert.DoesNotContain(game.Clubs, c => c.Players.Contains(slot.Player));
        Assert.Equal(PoolOwner.Taken, slot.Owner);
        Assert.DoesNotContain(slot.Player, game.TransferMarket.PoolPlayers);
    }

    /// <summary>
    /// #747: Import „nur Weltkugel“ (FILE18, Bundestrainer, Managerverein EV LANDSHUT trägt das Land): Das erste Spiel darf die
    /// importierten Teamwerte nicht neu würfeln. [ORIGINAL] FILE48 hat dieselben Teamwerte wie FILE18 (gleicher Tag 218). Vorher rot:
    /// <see cref="MatchEngine.EnsureTeamValues"/> sah die leeren Werte des Managervereins und zog alle neu (große Wanderung).
    /// </summary>
    [GameDataFact]
    public void ImportWorldOnly_FirstMatch_KeepsImportedTeamValues()
    {
        var game = Import("FILE18.HOK");
        Assert.True(game.WorldOnly);
        var manager = game.Clubs[game.Managers[0].ClubIndex];
        Assert.Null(manager.TeamValues);
        var before = game.Clubs.ToDictionary(c => c.Index, c => c.TeamValues);
        MatchEngine.EnsureTeamValues(game);
        foreach (var club in game.Clubs)
            Assert.True(Equals(before[club.Index], club.TeamValues), $"{club.Name}: {before[club.Index]} → {club.TeamValues}");
    }
}
