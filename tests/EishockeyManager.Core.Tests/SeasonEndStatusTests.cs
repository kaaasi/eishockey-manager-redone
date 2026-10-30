using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonende-status, Befund #125: Statusbit 0x40 „Verein unter den ersten fünf der 1. Liga“.
/// Befund vorher: Das Remake kannte das Ereignis nicht; im Fenster „Saisonende“ fehlte Symbol 8 (Weltkugel).
/// [ORIGINAL] em.exe Datei 0x553A2–0x553F2: An Tagen mit Markierung 255 (13. und 29. April) ruft das Original
/// <c>0xE8:0x93(0x7F)</c> (Datei 0x34769), löscht danach das Statusbyte jedes Managers (DGROUP 0x974E + 0x1AF · Manager)
/// und setzt Bit 0x40, wenn <c>0xE8:0x93(Manager)</c> wahr ist – das heißt: sein Verein steht auf den Plätzen 1–5 der
/// Tabelle der 1. Liga (DGROUP 0x5CCB, Liga · 12 + Platz) –, aber nicht im Spielmodus 2 (Datei 0x553DB: DGROUP 0xD0B9).
/// Gelesen wird das Byte im Fenster „Saisonende“ (Datei 0x26FBE, Bits 0–6 → Symbole nach Tabelle 0x4B2E; Bit 6 → Symbol 8)
/// und bei 0x55F53 (nur Maske 0x28, also nicht Bit 0x40).
/// </summary>
public class SeasonEndStatusTests
{
    /// <summary>Spielt bis zum Bildschirm „Saisonende“ (29. April), ohne dass ein Manager entlassen wird.</summary>
    private static void PlayToSeasonEnd(GameState game, bool active = false)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 3000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000); // keine Entlassung
            if (active)
                GameModeTests.ActiveSecondManager(game);
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    private static ManagerSeasonResult ResultOf(SeasonRecord record, int club) => record.Managers.Single(r => r.Club == club);

    private static List<int> TopFive(SeasonRecord record) => record.FinalTables[League.Bundesliga].Take(5).ToList();

    // ---- Befund und Regressionstest -----------------------------------------------------------------------------

    /// <summary>Modus 4: Der Manager im Verein unter den ersten fünf bekommt das Ereignis, der andere nicht.</summary>
    [Fact]
    public void Full_ManagerInTopFive_GetsStatusBit_OtherNot()
    {
        var game = GameModeTests.Create(3, true, true, 8, 2);
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        Assert.Contains(2, TopFive(record));      // Vorbedingung (wie NationalCoachMode4Tests)
        Assert.DoesNotContain(8, TopFive(record));

        Assert.Contains(SeasonEvent.TopFive, ResultOf(record, 2).Events);
        Assert.DoesNotContain(SeasonEvent.TopFive, ResultOf(record, 8).Events);
    }

    /// <summary>0xE8:0x93(Manager) gilt je Manager, nicht nur für den höchstplatzierten (der Bundestrainer wird).</summary>
    [Fact]
    public void Full_EveryManagerInTopFive_GetsStatusBit()
    {
        var game = GameModeTests.Create(3, true, true, 2, 1);
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        var top = TopFive(record);
        Assert.Contains(2, top);
        Assert.Contains(1, top);

        Assert.Contains(SeasonEvent.TopFive, ResultOf(record, 2).Events);
        Assert.Contains(SeasonEvent.TopFive, ResultOf(record, 1).Events);
        Assert.NotNull(game.NationalCoach); // nur einer wird Bundestrainer, das Symbol bekommen beide
    }

    /// <summary>Modus 2 („nur Fahne“): Das Original setzt Bit 0x40 nicht (0x553DB: Vergleich mit 2).</summary>
    [Fact]
    public void ClubOnly_ManagerInTopFive_NoStatusBit()
    {
        var game = GameModeTests.Create(3, true, false, 2);
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        Assert.Equal(GameMode.ClubOnly, game.Mode);
        Assert.Contains(2, TopFive(record));

        Assert.DoesNotContain(SeasonEvent.TopFive, ResultOf(record, 2).Events);
    }

    /// <summary>Außerhalb der Bundesliga zählt der Platz nicht: Tabelle 0x5CCB, Liga 0.</summary>
    [Fact]
    public void Full_TopFiveOfOtherLeague_NoStatusBit()
    {
        var game = GameModeTests.Create(3, true, true, 14, 30);
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        foreach (var result in record.Managers)
        {
            var inBundesligaTop = TopFive(record).Contains(result.Club);
            Assert.Equal(inBundesligaTop, result.Events.Contains(SeasonEvent.TopFive));
        }
        Assert.Contains(record.Managers, r => r.League != League.Bundesliga); // Vorbedingung: Verein außerhalb der 1. Liga
    }

    /// <summary>Bit 6 ist das letzte Bit der Schleife 0x2714F: Das Symbol steht hinter den übrigen Ereignissen.</summary>
    [Fact]
    public void StatusBit_IsLastEvent_AndBringsNoPremium()
    {
        var game = GameModeTests.Create(3, true, true, 2, 1);
        PlayToSeasonEnd(game);
        foreach (var result in game.History[^1].Managers.Where(r => r.Events.Contains(SeasonEvent.TopFive)))
        {
            Assert.Equal(SeasonEvent.TopFive, result.Events[^1]);
            var without = result.Events.Where(e => e != SeasonEvent.TopFive).ToList();
            Assert.Equal(GameState.SeasonPremium(result.League, result.Rank, without), result.Premium);
        }
    }

    [Theory]
    [InlineData(League.Bundesliga)]
    [InlineData(League.ZweiteLiga)]
    [InlineData(League.Oberliga)]
    public void StatusBit_AloneBringsNoPremium(League league)
    {
        for (var rank = 1; rank <= 12; rank++)
            Assert.Equal(GameState.SeasonPremium(league, rank, []), GameState.SeasonPremium(league, rank, [SeasonEvent.TopFive]));
    }

    // ---- Mehrsaisonprobe ---------------------------------------------------------------------------------------

    /// <summary>
    /// Fünf Saisons (vier Saisonwechsel), passiv und aktiv: In jeder Saison trägt genau der Manager das Ereignis, dessen
    /// Verein unter den ersten fünf der Abschlusstabelle der 1. Liga steht – in Modus 4 immer, in Modus 2 nie.
    /// </summary>
    [Theory]
    [InlineData(true, true, false, 3)]
    [InlineData(true, true, true, 3)]
    [InlineData(true, true, true, 11)]
    [InlineData(true, false, false, 3)]
    [InlineData(true, false, true, 11)]
    public void MultiSeason_StatusBitFollowsTopFive(bool flag, bool globe, bool active, int seed)
    {
        var game = GameModeTests.Create(seed, flag, globe, 2, 30);
        var withBit = 0;
        var withoutBit = 0;
        for (var change = 0; change <= 4; change++)
        {
            PlayToSeasonEnd(game, active);
            var record = game.History[^1];
            var top = TopFive(record);
            foreach (var result in record.Managers)
            {
                var expected = globe && top.Contains(result.Club);
                Assert.Equal(expected, result.Events.Contains(SeasonEvent.TopFive));
                if (expected) withBit++; else withoutBit++;
                var without = result.Events.Where(e => e != SeasonEvent.TopFive).ToList();
                Assert.Equal(GameState.SeasonPremium(result.League, result.Rank, without), result.Premium);
            }
            if (change < 4)
                game.StartNextSeason();
        }
        Assert.Equal(5, game.History.Count);
        Assert.True(withoutBit > 0, "Probe ohne Saison ohne Ereignis");
        if (globe)
            Assert.True(withBit > 0, "Probe ohne Saison mit Ereignis – Vorbedingung verfehlt");
    }
}
