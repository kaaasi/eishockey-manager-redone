using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur pruefung-karte Teil 5 (Entscheidung Nutzer 09.11.2026, C): Ladeeigenheit des Markierungstags beim HOK-Import
/// (<c>GameState.LoadQuirk.cs</c>, em.exe Datei 0x437D5 → 0x54C9F mit <c>bp+6</c> ≠ 0 → 0x54D41 → 0x55073 → 0x55204).
/// </summary>
public class LoadQuirkTests
{
    private static OriginalSaveGame Save(string file) =>
        OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));

    private static GameState Import(string file) =>
        GameState.ImportOriginal(Save(file), ClubDatabase.Load(TestGameData.Load()), 4711);

    /// <summary>
    /// [ORIGINAL] Nur FILE22 und FILE44 (Zähler 217, Tagesindex 0xA969 = 216) sind im Menü des Markierungstags gesichert; alle übrigen
    /// Stände haben Zähler = Index (auch FILE18/FILE19/FILE21/FILE47/FILE49 am 14. April mit 217/217) und keinen Versatz.
    /// </summary>
    [GameDataFact]
    public void OnlyMarkerDaySaves_GetTheOffset()
    {
        var directory = Path.GetDirectoryName(OriginalImportTests.OriginalSave("FILE44.HOK"))!;
        var marked = Directory.GetFiles(directory, "FILE*.HOK").Select(Path.GetFileName).Order(StringComparer.Ordinal)
            .Where(f => OriginalImport.SavedOnMarkerDay(Save(f!))).ToList();
        Assert.Equal(["FILE22.HOK", "FILE44.HOK"], marked);
        Assert.Equal(0, Import("FILE47.HOK").LoadCounterOffset);
    }

    /// <summary>
    /// Regressionstest (vorher rot: Datum „Samstag, 14. April“, Finanztag 216): Nach dem Import von FILE44 steht das Remake wie das
    /// Original nach dem Laden (<c>file46_1</c>) auf „Sonntag, 15. April 1994“ mit unverändertem Konto 18.027.183 DM; der
    /// Tagesabschluss zum Zähler 218 ist gerechnet (Finanztag 217), der Tag des Remakes bleibt 216 (WM-Einträge unverändert).
    /// </summary>
    [GameDataFact]
    public void File44_AfterImport_SecondDayChange_FifteenthOfApril()
    {
        var game = Import("FILE44.HOK");
        Assert.Equal(1, game.LoadCounterOffset);
        Assert.Equal(216, game.CurrentDay);
        Assert.Equal(217, game.FinanceDay);
        Assert.Equal(218, game.Counter(game.CurrentDay));
        Assert.Equal("Sonntag, 15. April  1994", game.DateText);
        Assert.Equal(18_027_183, game.Managers[0].Money);
    }

    /// <summary>
    /// Der Versatz übersteht Speichern und Laden und endet beim Saisonwechsel (Datei 0x26F55 setzt 233). Mehrsaisonprobe: danach vier
    /// Saisonwechsel mit dem Zählerbezug des Remakes (Versatz 0, Tagesabschluss der Spielzeit endet am 29. April), passiv und aktiv
    /// (je Saison ein Stadionauftrag, Konto aufgefüllt; der Bau läuft über den Wochentakt des Finanztags).
    /// </summary>
    [GameDataTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void File44_Offset_SurvivesSave_EndsAtSeasonChange_FourSeasons(bool active)
    {
        var imported = Import("FILE44.HOK");
        var game = SaveGame.Load(SaveGame.Save(imported)).Game!;
        Assert.Equal(1, game.LoadCounterOffset);
        Assert.Equal(imported.DateText, game.DateText);
        Assert.Equal(imported.FinanceDay, game.FinanceDay);

        var me = game.Managers[0];
        for (var season = 0; season < 4; season++)
        {
            if (active)
            {
                me.Money = Math.Max(me.Money, 100_000_000);
                var level = game.Finance(me).Stadium.Level(StadiumFeature.Gastronomie);
                if (game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Gastronomie, level + 1) is { } quote)
                    Assert.True(game.AnswerConstruction(me, quote with { Weeks = 30 }, accept: true));
            }
            TestSeason.PlayRegularSeason(game);
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 5000, "Saisonende nicht erreicht");
                if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                    game.AutoNominate();
                game.Continue();
            }
            Assert.Equal(SeasonCalendar.SeasonEnd + (season == 0 ? 1 : 0), game.FinanceDay);
            game.StartNextSeason();
            Assert.Equal(0, game.LoadCounterOffset);
        }
        Assert.Equal(1997, game.Season);
    }

    /// <summary>Remake-Spielstände haben keinen Versatz: Neues Spiel, Zähler wie <see cref="DayCounter.Of"/>, Datum vom Tag.</summary>
    [Fact]
    public void NewGame_HasNoOffset()
    {
        var game = TestGames.Create(3);
        Assert.Equal(0, game.LoadCounterOffset);
        TestSeason.PlayMatchDay(game);
        Assert.Equal(DayCounter.Of(game.CurrentDay), game.Counter(game.CurrentDay));
        Assert.Equal(SeasonCalendar.Format(game.CurrentDay, game.Season), game.DateText);
    }

    /// <summary>
    /// Migration 46 → 47 (<c>SaveGame.Migration.Ladeeigenheit.cs</c>): Ein Stand ohne <c>LoadCounterOffset</c> lädt mit Versatz 0;
    /// das aktuelle Schema ohne das Feld lehnt der strenge Leser ab. Rot ohne den Eintrag <c>[46] = MigrateLadeeigenheit</c>.
    /// </summary>
    [Fact]
    public void Schema46_WithoutOffset_LoadsWithZero()
    {
        var game = TestGames.Create(3);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        Assert.Equal(47, (int)root["schema"]!);
        var state = root["spiel"]!.AsObject();
        Assert.Equal(0, (int)state["LoadCounterOffset"]!);
        Assert.True(state.Remove("LoadCounterOffset"));

        var current = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.False(current.Success);
        Assert.Contains("LoadCounterOffset", current.Error);

        root["schema"] = 46;
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal(0, result.Game!.LoadCounterOffset);
    }
}
