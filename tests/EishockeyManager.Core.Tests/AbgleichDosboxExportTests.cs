using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #804 (Spur abgleich-dosbox, Teil 3): Vereinsfelder 0x26 (Einsatz) und 0x2C Bits 4–6 (Angriff) des letzten Spiels
/// (<see cref="Tactics.MatchEffort"/>, <see cref="Tactics.MatchAttack"/>) im Import und Export von <c>*.HOK</c>. Vorher las der
/// Import die Felder nicht (Werte aus dem Nachspielen, je Saat verschieden) und der Export schrieb 0 (EXPORT2D: alle 51
/// Computervereine 0/0/0, Original FILE53 nach einem Spieltag 40 × 20, 60 × 14). Bericht: <c>docs/analyse/abgleich-dosbox.md</c> §3.
/// </summary>
public class AbgleichDosboxExportTests(ITestOutputHelper output)
{
    /// <summary>Vereinssätze 0–51: Vereine mit Kader ohne Nationalteams (Sätze 52–63).</summary>
    private const int ClubSlots = 52;

    /// <summary>Vereinssatz <paramref name="slot"/> (far:9657 + 0x3D · Satz).</summary>
    private static ReadOnlySpan<byte> Record(OriginalSaveGame save, int slot) =>
        save.Block("far:9657").AsSpan(slot * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength);

    private static int Attack(ReadOnlySpan<byte> record) => (record[0x2C] >> 4) & 7;

    /// <summary>
    /// [ORIGINAL] Nach dem Import tragen alle Vereine mit Kader (Sätze der Vereine 0–51) Einsatz 0x26 und Angriff 0x2C Bits 4–6
    /// des Stands – unabhängig von der Saat des Nachspielens. FILE03 (drei Managerspiele, Gegner mit Angriff 4/5/6 und 0x26 = 55),
    /// FILE16A, FILE44 (Angriff 4/5 an elf Vereinen), FILE53 (DOSBox `runde-17`).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE03.HOK", 1)]
    [InlineData("FILE03.HOK", 7)]
    [InlineData("FILE16A.HOK", 1)]
    [InlineData("FILE44.HOK", 3)]
    [InlineData("FILE53.HOK", 5)]
    public void Import_ReadsClubMatchFields(string file, int seed)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var database = ClubDatabase.Load(TestGameData.Load());
        var clubOf = OriginalImport.ClubIndices(save, database);
        var game = GameState.ImportOriginal(save, database, seed);
        var wrong = new List<string>();
        var nonZero = 0;
        for (var slot = 0; slot < clubOf.Length; slot++)
        {
            var club = game.Clubs[clubOf[slot]];
            if (TransferMarket.IsNationalTeam(club))
                continue;
            var r = Record(save, slot);
            nonZero += r[0x26] > 0 ? 1 : 0;
            if ((club.Tactics.MatchEffort, club.Tactics.MatchAttack) != (r[0x26], Attack(r)))
                wrong.Add($"{club.Name}: {club.Tactics.MatchEffort}/{club.Tactics.MatchAttack} statt {r[0x26]}/{Attack(r)}");
        }
        output.WriteLine($"{file}: {nonZero} Vereine mit 0x26 > 0, {wrong.Count} Abweichungen");
        Assert.True(nonZero > 30, "Stand ohne gespielte Spiele taugt nicht als Beleg");
        Assert.True(wrong.Count == 0, string.Join("; ", wrong.Take(10)));
    }

    /// <summary>
    /// Original → Import → Export: Einsatz 0x26 und Angriffsbits 0x2C (4–6) aller Vereinssätze 0–51 kommen unverändert zurück;
    /// 0x2D (Grundwert der Automatik) schreibt der Export wie 0x26 (<c>[APPROX]</c>, siehe <c>WriteClubRecords</c>) – im Original
    /// gleich, außer beim Gegner einer Managerpaarung, dessen 0x26 je Minute 0x939A ändert (FILE03: 55/40, FILE44: keiner).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE03.HOK")]
    [InlineData("FILE16A.HOK")]
    [InlineData("FILE44.HOK")]
    [InlineData("FILE53.HOK")]
    public void Export_WritesClubMatchFields(string file)
    {
        var raw = OriginalExportTests.RawDatabase();
        var original = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var game = GameState.ImportOriginal(original, ClubDatabase.Parse(raw), 4711);
        var exported = OriginalSaveGame.Read(OriginalExport.Export(game, raw));
        var wrong = new List<string>();
        var baseDiffers = 0;
        for (var slot = 0; slot < ClubSlots; slot++)
        {
            var o = Record(original, slot);
            var e = Record(exported, slot);
            if ((o[0x26], Attack(o)) != (e[0x26], Attack(e)) || e[0x2D] != e[0x26])
                wrong.Add($"Satz {slot}: {e[0x26]}/{Attack(e)}/{e[0x2D]} statt {o[0x26]}/{Attack(o)}/{o[0x2D]}");
            baseDiffers += o[0x2D] != o[0x26] ? 1 : 0;
        }
        output.WriteLine($"{file}: {wrong.Count} Abweichungen, im Original 0x2D ≠ 0x26 an {baseDiffers} Sätzen");
        Assert.True(wrong.Count == 0, string.Join("; ", wrong.Take(10)));
    }

    /// <summary>
    /// Mehrsaisonprobe (<c>AGENTS.md</c> §5): vier Saisonwechsel, passiver und aktiver Manager. Nach jeder Hauptrunde und jedem
    /// Saisonstart bringt Remake → HOK → Remake Einsatz und Angriff aller Vereine mit Kader zurück (ohne #804 rot: der Import nahm
    /// die Werte des Nachspielens mit eigener Saat).
    /// </summary>
    [GameDataTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_ExportImport_KeepsClubMatchFields(bool active)
    {
        var raw = OriginalExportTests.RawDatabase();
        var database = ClubDatabase.Parse(raw);
        var game = OriginalExportTests.NewGameForExport(database);
        var manager = game.Managers[0];
        if (active)
        {
            var tactics = game.Clubs[manager.ClubIndex].Tactics;
            tactics.EffortMode = TacticMode.Semi;
            tactics.SetEffort(0, ScoreSituation.Base, 70);
        }
        for (var season = GameState.FirstSeason; season < GameState.FirstSeason + 4; season++)
        {
            if (active)
                TestSeason.KeepSquad(game, manager, extendExpired: true);
            TestSeason.PlayRegularSeason(game);
            AssertRoundTrip(game, database, raw, $"Saison {season}, Ende der Hauptrunde");
            for (var guard = 0; game.Season == season && !game.IsGameOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonwechsel bleibt hängen.");
                if (game.Phase == SeasonPhase.SeasonOver)
                    game.StartNextSeason();
                else
                    game.Continue();
            }
            Assert.Equal(season + 1, game.Season);
            AssertRoundTrip(game, database, raw, $"Saison {season + 1}, Saisonstart");
        }
    }

    private void AssertRoundTrip(GameState game, ClubDatabase database, byte[] raw, string label)
    {
        var again = GameState.ImportOriginal(OriginalSaveGame.Read(OriginalExport.Export(game, raw)), database, 99);
        var clubs = game.Clubs.Where(c => !TransferMarket.IsNationalTeam(c) && c.Index < ClubSlots).ToList();
        var wrong = clubs.Where(c => (c.Tactics.MatchEffort, c.Tactics.MatchAttack) !=
                (again.Clubs[c.Index].Tactics.MatchEffort, again.Clubs[c.Index].Tactics.MatchAttack))
            .Select(c => $"{c.Name}: {c.Tactics.MatchEffort}/{c.Tactics.MatchAttack} → " +
                $"{again.Clubs[c.Index].Tactics.MatchEffort}/{again.Clubs[c.Index].Tactics.MatchAttack}").ToList();
        output.WriteLine($"{label}: {clubs.Count(c => c.Tactics.MatchEffort > 0)} Vereine mit Einsatz, {wrong.Count} Abweichungen");
        Assert.True(clubs.Count(c => c.Tactics.MatchEffort > 0) > 30, $"{label}: zu wenige Vereine mit Einsatz");
        Assert.True(wrong.Count == 0, $"{label}: {string.Join("; ", wrong.Take(10))}");
    }
}
