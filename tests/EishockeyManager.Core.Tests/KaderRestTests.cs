using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader-rest (Runde 17), Teil 1: A066 – die Verletzung senkt die Form im Spielerpool (+0x17), nicht die des Kaderplatzes
/// (+0x12); ein neuer Kaderplatz übernimmt sie (0x42CB1). Startbelastung A130: <see cref="KaderStartTests"/>.
/// </summary>
public class KaderRestTests
{
    private sealed class ScriptedRandom(params int[] values) : Random(0)
    {
        private readonly Queue<int> _values = new(values);

        public override int Next(int minValue, int maxValue)
        {
            var v = _values.Count > 0 ? _values.Dequeue() : minValue;
            Assert.InRange(v, minValue, maxValue - 1);
            return v;
        }
    }

    private static Player P(int form) =>
        new() { Name = "P", Role = PlayerRole.Forward, Condition = 60, Technique = 50, Form = form, Energy = 100 };

    /// <summary>
    /// [ORIGINAL] Datei 0x05B34–0x05B7C: Pool +0x17 &gt; 20 → − random(10, 15), in den Pool zurückgeschrieben; der Kaderplatz bleibt.
    /// Vorher sank <see cref="Player.Form"/> (angezeigte Stärke) um 10–15.
    /// </summary>
    [Fact]
    public void Inflict_LowersPoolForm_SlotFormStays()
    {
        var player = P(52);
        PlayerInjuries.Inflict(player, inMatch: true, difficulty: 2, new ScriptedRandom(10, 2, 3, 2));
        Assert.Equal(52, player.Form);
        Assert.Equal(42, player.PoolForm);

        // Zweite Verletzung rechnet von der Poolform weiter (MENDE FILE36 → FILE44: 60 / 45 → 60 / 34 bzw. 78 … nach Training).
        PlayerInjuries.Inflict(player, inMatch: true, difficulty: 2, new ScriptedRandom(11, 2, 3, 2));
        Assert.Equal(52, player.Form);
        Assert.Equal(31, player.PoolForm);
    }

    /// <summary>[ORIGINAL] 0x05B34: Poolform ≤ 20 bleibt (auch wenn die Form des Kaderplatzes höher ist).</summary>
    [Fact]
    public void Inflict_PoolFormAtMost20_Unchanged()
    {
        var player = P(50);
        player.PoolForm = 20;
        PlayerInjuries.Inflict(player, inMatch: false, difficulty: 2, new ScriptedRandom(2, 0, 2));
        Assert.Equal(50, player.Form);
        Assert.Equal(20, player.PoolForm);
    }

    /// <summary>
    /// [ORIGINAL] Neuer Kaderplatz (0x42CB1, Datei 0x42EFF–0x42F06): +0x12 = Pool +0x17 – nach einem Wechsel zeigt der Spieler die
    /// gesunkene Poolform; danach gibt es keine abweichende Poolform mehr.
    /// </summary>
    [Fact]
    public void Join_NewSlot_TakesPoolForm()
    {
        var game = TestGames.Create(3);
        var buyer = game.Clubs.First(c => c.Players.Count > 0 && c.Index != game.ActiveManager.ClubIndex);
        var player = P(55);
        player.PoolForm = 40;
        PlayerFactory.Join(buyer, player, contractYears: 1, loan: false);
        Assert.Equal(40, player.Form);
        Assert.Null(player.PoolForm);
    }

    /// <summary>
    /// Review #799 P3-1: Die frisch gewürfelte Form der Talentsuche (Datei 0x06DE1–0x06E7D: Pool +0x17 = random(45, 55)) ist die
    /// Poolform; der neue Kaderplatz (0x42CB1) kopiert sie. Vorher überschrieb eine alte abweichende Poolform die neue Form.
    /// </summary>
    [Fact]
    public void ScoutedTalent_RolledFormIsPoolForm_JoinKeepsIt()
    {
        var game = TestGames.Create(3);
        var buyer = game.Clubs.First(c => c.Players.Count > 0 && c.Index != game.ActiveManager.ClubIndex);
        var player = P(55);
        player.PoolForm = 21;
        PlayerFactory.ApplyScoutedTalentValues(player, 50, new ScriptedRandom(19, 46, 50, 50));
        Assert.Equal(46, player.Form);
        Assert.Null(player.PoolForm);
        PlayerFactory.Join(buyer, player, contractYears: 1, loan: false);
        Assert.Equal(46, player.Form);
    }

    /// <summary>
    /// Review #799 P3-1: Listenwechsel (Füllen, Transfer.Pool) würfelt Pool +0x17 = random(45, 55); <see
    /// cref="TransferPoolRules.RollSlotFields"/> (0x42CB1, Datei 0x42EFF–0x42F06) übernimmt sie, nicht eine alte Poolform.
    /// </summary>
    [Fact]
    public void ListChange_RolledFormIsPoolForm_RollSlotFieldsKeepsIt()
    {
        var player = P(55);
        player.PoolForm = 21;
        TransferPoolRules.RollPoolForm(player, new ScriptedRandom(47));
        Assert.Equal(47, player.Form);
        Assert.Null(player.PoolForm);
        TransferPoolRules.RollSlotFields(player, new Random(1));
        Assert.Equal(47, player.Form);
    }

    /// <summary>Import und Export: FILE24 STEFAN (Platz 52, Pool 42, verletzt) und CHAIDAROV (55 / 40) bleiben getrennt.</summary>
    [GameDataFact]
    public void File24_PoolFormImportedAndExported()
    {
        var raw = OriginalExportTests.RawDatabase();
        var original = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE24.HOK")));
        var game = GameState.ImportOriginal(original, ClubDatabase.Parse(raw), 4711);
        var squad = game.Clubs[game.Managers[0].ClubIndex].Players;
        var stefan = squad.Single(p => p.Name == "STEFAN");
        var chaidarov = squad.Single(p => p.Name == "CHAIDAROV");
        Assert.Equal((52, 42), (stefan.Form, stefan.PoolForm));
        Assert.Equal((55, 40), (chaidarov.Form, chaidarov.PoolForm));
        Assert.Contains(squad, p => p.PoolForm is null);

        var exported = OriginalSaveGame.Read(OriginalExport.Export(game, raw));
        for (var k = 0; k < OriginalSaveGame.SquadSlotsPerManager; k++)
        {
            var slot = OriginalImport.SquadSlot(original, 0, k);
            if (slot[0x0F] == 0)
                continue;
            Assert.Equal(OriginalImport.PoolEntry(original, slot[0x0F])[0x17], OriginalImport.PoolEntry(exported, slot[0x0F])[0x17]);
            Assert.Equal(slot[0x12], OriginalImport.SquadSlot(exported, 0, k)[0x12]);
        }
    }

    /// <summary>Spielstand: Poolform bleibt über Speichern/Laden; Schema 49 ohne Feld lädt mit null (Migration KaderRest).</summary>
    [Fact]
    public void SaveGame_PoolForm_RoundTripAndMigration()
    {
        var game = TestGames.Create(3);
        var player = game.Clubs[game.ActiveManager.ClubIndex].Players[0];
        player.PoolForm = 33;
        var loaded = SaveGame.Load(SaveGame.Save(game)).Game!;
        Assert.Equal(33, loaded.Clubs[game.ActiveManager.ClubIndex].Players[0].PoolForm);

        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 49;
        foreach (var p in root["spieler"]!.AsArray().OfType<JsonObject>())
            p.Remove("PoolForm");
        var old = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.Null(old.Error);
        Assert.All(old.Game!.Clubs.SelectMany(c => c.Players), p => Assert.Null(p.PoolForm));
    }
}
