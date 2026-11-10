using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-export, Teil 1: Export eines Remake-Spielstands als Original-Spielstand <c>*.HOK</c> (<see cref="OriginalExport"/>).
/// Prüft Aufbau (Länge, Kennungen, Prüfsummen), die Lesbarkeit mit <see cref="OriginalSaveGame.Read"/> und den Import des
/// Ergebnisses; die Rundreisen stehen in <see cref="OriginalExportRoundTripTests"/> (Teil 2).
/// </summary>
public sealed class OriginalExportTests
{
    private const int Seed = 4711;

    /// <summary>Ein neues Spiel lässt sich exportieren; das Ergebnis hat Länge, Kennungen und gültige Prüfsummen des Originals.</summary>
    [GameDataFact]
    public void NewGame_ExportsReadableHok()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = NewGameForExport(database);

        var bytes = OriginalExport.Export(game, RawDatabase());
        var save = OriginalSaveGame.Read(bytes);

        Assert.Equal(OriginalSaveGame.FileLength, bytes.Length);
        Assert.True(save.ChecksumsValid);
        Assert.Equal(1, save.HeaderManagerCount);
        Assert.Equal("Joerg", save.Managers[0].Name);
        Assert.Equal(game.Clubs[game.Managers[0].ClubIndex].Name, save.Managers[0].Club);
        Assert.Equal(1, BitConverter.ToInt32(save.Block("03B7")));
        Assert.Equal(1993, save.HeaderYear);
    }

    /// <summary>Der Export verändert den Spielstand nicht und ist bei gleicher Saat gleich (keine Zufallszüge aus dem Spiel).</summary>
    [GameDataFact]
    public void Export_IsPure()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = NewGameForExport(database);
        TestSeason.PlayMatchDay(game);
        var before = SaveGame.Save(game);

        var a = OriginalExport.Export(game, RawDatabase());
        var b = OriginalExport.Export(game, RawDatabase());

        Assert.Equal(a, b);
        Assert.Equal(before, SaveGame.Save(game));
    }

    /// <summary>B4: Zwei verschiedene Stände desselben Spiels (Spielstart, nach einem Spieltag) tragen verschiedene Spielstand-IDs.</summary>
    [GameDataFact]
    public void Export_DifferentStands_HaveDifferentSaveIds()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = NewGameForExport(database);
        var first = OriginalSaveGame.Read(OriginalExport.Export(game, RawDatabase()));
        TestSeason.PlayMatchDay(game);
        var second = OriginalSaveGame.Read(OriginalExport.Export(game, RawDatabase()));
        Assert.NotEqual(first.SaveId, second.SaveId);
    }

    /// <summary>
    /// B1 (Review 2026-11-10, R727P-03): Die Teamwerte der Computervereine far:9657 + 0x18 … 0x1D schreibt der Export absolut
    /// (Datenbankwert + Abweichung des Remakes, wie <see cref="MatchEngine.ClubFieldValues"/>), auch vor dem ersten Spiel (Werte noch
    /// nicht gezogen); vorher stand dort 0 (Spielstart) bzw. die Abweichung (Stärke 15–24). Geprüft: (1) die Bytes gleichen den
    /// Feldwerten des Remakes; (2) die mittlere Stärke (Σ 0x18–0x1D) / 6 je Liga liegt im Bereich der Originalstände FILE01–FILE49A
    /// dieser Liga; (3) jede Stärke im Bereich aller Originalstände ± <see cref="DriftMargin"/>.
    /// </summary>
    [GameDataTheory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(22)]
    [InlineData(44)]
    public void ComputerClubStrength_AfterExport_InRangeOfOriginalStands(int matchDays)
    {
        var ranges = OriginalLeagueStrengthRanges();
        var (min, max) = (ranges.Min(r => r.Min) - DriftMargin, ranges.Max(r => r.Max) + DriftMargin);
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = NewGameForExport(database);
        while (game.PlayedMatchDays < matchDays && !game.RegularSeasonOver)
            TestSeason.PlayMatchDay(game);
        var before = SaveGame.Save(game);

        var save = OriginalSaveGame.Read(OriginalExport.Export(game, RawDatabase()));

        Assert.Equal(before, SaveGame.Save(game));
        var records = save.Block("far:9657");
        var slotOf = OriginalExport.Slots(game);
        var problems = new List<string>();
        for (var league = 0; league < 3; league++)
        {
            var strengths = new List<int>();
            for (var slot = league * GameState.ClubsPerLeague; slot < (league + 1) * GameState.ClubsPerLeague; slot++)
            {
                var r = records.AsSpan(slot * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength);
                if (r[0x16] != 0)
                    continue; // Managerverein
                var strength = LeagueStrength(r);
                strengths.Add(strength);
                if (strength < min || strength > max)
                    problems.Add($"Satz {slot} {OriginalImport.ClubName(records, slot)}: {strength} außerhalb {min}–{max}");
                if (matchDays > 0)
                {
                    var club = game.Clubs[Array.IndexOf(slotOf, slot)];
                    var (defense, attack) = MatchEngine.ClubFieldValues(club);
                    Assert.Equal(((byte)defense.Condition, (byte)attack.Condition, (byte)defense.Technique, (byte)attack.Technique),
                        (r[0x18], r[0x19], r[0x1A], r[0x1B]));
                }
            }
            var mean = strengths.Average();
            if (mean < ranges[league].Min || mean > ranges[league].Max)
                problems.Add($"Liga {league}: Mittel {mean:0.0} außerhalb {ranges[league].Min}–{ranges[league].Max}");
        }
        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    /// <summary>
    /// Spielraum über den Bereich der Originalstände hinaus: die kleine Wanderung im Tageslauf begrenzt nur auf 1 … 99 (em.exe 0x9D1B,
    /// <see cref="MatchEngine.DriftTeamValues"/>); im Remake nach 22 bzw. 44 Spieltagen Stärken 19 bzw. 75 (Originalstände 24–83).
    /// </summary>
    private const int DriftMargin = 10;

    /// <summary>B1/#729: Der Import übernimmt die Teamwerte so, dass die Feldwerte des Remakes den Bytes des Originals gleichen.</summary>
    [GameDataTheory]
    [InlineData("FILE01.HOK")]
    [InlineData("FILE05.HOK")]
    [InlineData("FILE39.HOK")]
    public void Import_ClubFieldValues_EqualHokBytes(string file)
    {
        var original = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var game = GameState.ImportOriginal(original, ClubDatabase.Parse(RawDatabase()), Seed);
        var records = original.Block("far:9657");
        foreach (var club in game.Clubs.Where(c => c.Index < GameState.ClubsPerLeague * 4 && game.Managers.All(m => m.ClubIndex != c.Index)))
        {
            var slot = OriginalExport.Slots(game)[club.Index];
            var r = records.AsSpan(slot * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength);
            var (defense, attack) = MatchEngine.ClubFieldValues(club);
            Assert.Equal((r[0x18], r[0x19], r[0x1A], r[0x1B]), ((byte)defense.Condition, (byte)attack.Condition, (byte)defense.Technique, (byte)attack.Technique));
        }
    }

    /// <summary>
    /// B2 (Review 2026-11-10, R727P-02): Umlaute wie im Original. (1) Jeder Vereinsname der Datenbank mit Umlaut (Ä Ö Ü ß) kodiert
    /// <see cref="OriginalSaveWriter.Encode"/> zu den Bytes der Datenbank (Ö = 0x99). (2) Spielernamen kopiert das Original roh aus der
    /// Datenbank (Ö meist 0x99, BRANNSTRÖM mit 0x5C): Im Export eines neuen Spiels trägt jeder Poolsatz mit Umlaut genau die Bytes
    /// der Datenbank. (3) Ohne Datenbankvorlage weicht <see cref="OriginalSaveWriter.Encode"/> nur bei 0x5C-Namen ab.
    /// </summary>
    [GameDataFact]
    public void Umlauts_ExportedLikeDatabase()
    {
        var raw = RawDatabase();
        static (string Text, byte[] Bytes) Field(byte[] raw, int offset, int length)
        {
            var field = raw.AsSpan(offset, length);
            var end = field.IndexOf((byte)0);
            var bytes = (end < 0 ? field : field[..end]).ToArray();
            return (GameText.Decode(bytes), bytes);
        }
        static bool HasUmlaut(string text) => text.Any(c => c is 'Ä' or 'Ö' or 'Ü' or 'ß');

        var wrong = new List<string>();
        var clubs = 0;
        for (var club = 0; club < ClubDatabase.TotalClubs; club++)
        {
            var (text, bytes) = Field(raw, club < ClubDatabase.ClubsWithSquad
                ? ClubDatabase.SquadOffset + club * ClubDatabase.SquadRecordLength
                : ClubDatabase.EuropeanNamesOffset + (club - ClubDatabase.ClubsWithSquad) * ClubDatabase.NameLength, ClubDatabase.NameLength);
            if (!HasUmlaut(text))
                continue;
            clubs++;
            if (!OriginalSaveWriter.Encode(text).SequenceEqual(bytes))
                wrong.Add($"Verein {text}: {Convert.ToHexString(bytes)}");
        }

        var players = new Dictionary<string, byte[]>();
        for (var club = 0; club < ClubDatabase.ClubsWithSquad; club++)
            for (var p = 0; p < ClubDatabase.SquadSize; p++)
            {
                var (text, bytes) = Field(raw, ClubDatabase.SquadOffset + club * ClubDatabase.SquadRecordLength + ClubDatabase.NameLength
                                               + p * ClubDatabase.PlayerRecordLength, ClubDatabase.PlayerNameLength);
                if (HasUmlaut(text))
                    players[text] = bytes;
            }
        var notEncodable = players.Where(p => !OriginalSaveWriter.Encode(p.Key).SequenceEqual(p.Value)).ToList();
        Assert.All(notEncodable, p => Assert.Contains((byte)'\\', p.Value));

        var game = NewGameForExport(ClubDatabase.Parse(raw));
        var pool = OriginalSaveGame.Read(OriginalExport.Export(game, raw)).Block("far:8253");
        var exported = 0;
        for (var i = 1; i < pool.Length / OriginalSaveGame.PoolRecordLength; i++)
        {
            var (text, bytes) = Field(pool, i * OriginalSaveGame.PoolRecordLength, 0x12);
            if (!players.TryGetValue(text, out var expected))
                continue;
            exported++;
            if (!bytes.SequenceEqual(expected))
                wrong.Add($"Pool {i} {text}: {Convert.ToHexString(bytes)} statt {Convert.ToHexString(expected)}");
        }
        Assert.True(clubs >= 3 && players.Count > 30 && exported >= 5, $"Vereine {clubs}, Spieler {players.Count}, im Pool {exported}");
        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }

    private static int LeagueStrength(ReadOnlySpan<byte> record) =>
        (record[0x18] + record[0x19] + record[0x1A] + record[0x1B] + record[0x1C] + record[0x1D]) / 6;

    /// <summary>Kleinste und größte Stärke eines Computervereins je Liga (Sätze 0–11, 12–23, 24–35) über alle Originalstände.</summary>
    private static (int Min, int Max)[] OriginalLeagueStrengthRanges()
    {
        var folder = Path.GetDirectoryName(OriginalImportTests.OriginalSave("FILE01.HOK"))!;
        var ranges = Enumerable.Repeat((Min: int.MaxValue, Max: int.MinValue), 3).ToArray();
        foreach (var file in Directory.GetFiles(folder, "FILE*.HOK"))
        {
            var records = OriginalSaveGame.Read(File.ReadAllBytes(file)).Block("far:9657");
            for (var slot = 0; slot < 3 * GameState.ClubsPerLeague; slot++)
            {
                var r = records.AsSpan(slot * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength);
                if (r[0x16] != 0)
                    continue;
                var strength = LeagueStrength(r);
                var league = slot / GameState.ClubsPerLeague;
                ranges[league] = (Math.Min(ranges[league].Min, strength), Math.Max(ranges[league].Max, strength));
            }
        }
        return ranges;
    }

    internal static byte[] RawDatabase() => File.ReadAllBytes(TestGameData.Load().GraphicsPath(23));

    internal static GameState NewGameForExport(ClubDatabase database)
    {
        var setup = new GameSetup();
        setup.AddManager(new ManagerSetup { Name = "Joerg", Head = 6, ClubIndex = 2 });
        return NewGame.Create(database, setup, Seed);
    }
}
