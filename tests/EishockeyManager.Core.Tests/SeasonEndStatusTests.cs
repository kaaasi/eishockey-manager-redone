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
/// Das Remake leitet das Bit ab (<see cref="GameState.HasTopFiveStatus"/>), statt es zu speichern – kein neuer Zustand, keine
/// Schemaänderung (der Schemawächter in <c>SaveGameTests</c> bleibt unverändert).
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

    private static bool HasBit(GameState game, SeasonRecord record, int club) => game.HasTopFiveStatus(record, ResultOf(record, club));

    private static List<int> TopFive(SeasonRecord record) => record.FinalTables[League.Bundesliga].Take(5).ToList();

    // ---- Befund und Regressionstest -----------------------------------------------------------------------------

    /// <summary>Modus 4: Der Manager im Verein unter den ersten fünf bekommt das Bit, der andere nicht.</summary>
    [Fact]
    public void Full_ManagerInTopFive_GetsStatusBit_OtherNot()
    {
        var game = GameModeTests.Create(4, true, true, 8, 2); // kader-start U15-a (L13): Saat 3 → 4, Vorbedingung nach dem Starttausch (Teamwerte bleiben am Platz)
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        // Spielberechnung Teil 3 (Verletzungen 0x38B8F) schwächt die Kader: Verein 2 mit Spielerwerten 99 (wie PlayAtTopStrength).
        foreach (var p in game.Clubs[2].Players)
            (p.Condition, p.Technique, p.Form) = (Player.MaxValue, Player.MaxValue, 50);
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        Assert.Contains(2, TopFive(record));      // Vorbedingung (wie NationalCoachMode4Tests)
        Assert.DoesNotContain(8, TopFive(record));

        Assert.True(HasBit(game, record, 2));
        Assert.False(HasBit(game, record, 8));
    }

    /// <summary>0xE8:0x93(Manager) gilt je Manager, nicht nur für den höchstplatzierten (der Bundestrainer wird).</summary>
    [Fact]
    public void Full_EveryManagerInTopFive_GetsStatusBit()
    {
        var game = GameModeTests.Create(71, true, true, 2, 1); // kader-start U15-a/R-K5 (L13): Saat 3 → 17; #675 (Tageszähler 0x3B7, anderer Zufallsverlauf): 17 → 56; #678 (Tagestakt 0x07169): 56 → 71, Vorbedingung beide unter den ersten fünf
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        var top = TopFive(record);
        Assert.Contains(2, top);
        Assert.Contains(1, top);

        Assert.True(HasBit(game, record, 2));
        Assert.True(HasBit(game, record, 1));
        Assert.NotNull(game.NationalCoach); // nur einer wird Bundestrainer, das Symbol bekommen beide
    }

    /// <summary>Modus 2 („nur Fahne“): Das Original setzt Bit 0x40 nicht (0x553DB: Vergleich mit 2).</summary>
    [Fact]
    public void ClubOnly_ManagerInTopFive_NoStatusBit()
    {
        var game = GameModeTests.Create(22, true, false, 2); // kader-start U15-a/R-K5 (L13): Saat 3 → 22, Vorbedingung nach dem Starttausch (Teamwerte bleiben am Platz)
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        Assert.Equal(GameMode.ClubOnly, game.Mode);
        Assert.Contains(2, TopFive(record));

        Assert.False(HasBit(game, record, 2));
    }

    /// <summary>Außerhalb der Bundesliga zählt der Platz nicht: Tabelle 0x5CCB, Liga 0.</summary>
    [Fact]
    public void Full_TopFiveOfOtherLeague_NoStatusBit()
    {
        var game = GameModeTests.Create(3, true, true, 14, 30);
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        foreach (var result in record.Managers)
            Assert.Equal(TopFive(record).Contains(result.Club), game.HasTopFiveStatus(record, result));
        Assert.Contains(record.Managers, r => r.League != League.Bundesliga); // Vorbedingung: Verein außerhalb der 1. Liga
    }

    /// <summary>Das Bit bringt keine Prämie und ändert die gespeicherten Ereignisse nicht (es steht nicht in <see cref="ManagerSeasonResult.Events"/>).</summary>
    [Fact]
    public void StatusBit_DoesNotChangeStoredEventsOrPremium()
    {
        var game = GameModeTests.Create(5, true, true, 2, 1); // #675 (Tageszähler 0x3B7, anderer Zufallsverlauf): Saat 3 → 5, Vorbedingung Bit
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        foreach (var result in record.Managers)
            Assert.Equal(GameState.SeasonPremium(result.League, result.Rank, GameState.PremiumEvents(record, result.Club, result.Events)), result.Premium);
        Assert.True(HasBit(game, record, 2) || HasBit(game, record, 1)); // Vorbedingung: Bit tritt auf
    }

    // ---- Mehrsaisonprobe ---------------------------------------------------------------------------------------

    /// <summary>
    /// Fünf Saisons (vier Saisonwechsel), passiv und aktiv: In jeder Saison trägt genau der Manager das Bit, dessen
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
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        // wettbewerbe-spiel, Teil 7 (+0x19 gewürfelt): Mit Datenbankstärke stand Verein 2 bei Seed 3/11 in keiner Saison mehr
        // unter den ersten fünf (60 Seeds, eine Saison: 48 statt 50 von 60, Mittel Platz 4,28 statt 4,18 – Streuung, keine
        // Schwächung). Vorbedingung deshalb wie in den Einzelfällen über Spielerwerte 99 für Manager 0.
        ManagedSquads.PlayAtTopStrength(game, 0);
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
                Assert.Equal(expected, game.HasTopFiveStatus(record, result));
                if (expected) withBit++; else withoutBit++;
                Assert.Equal(GameState.SeasonPremium(result.League, result.Rank, GameState.PremiumEvents(record, result.Club, result.Events)), result.Premium);
            }
            if (change < 4)
                game.StartNextSeason();
        }
        Assert.Equal(5, game.History.Count);
        Assert.True(withoutBit > 0, "Probe ohne Saison ohne Bit");
        if (globe)
            Assert.True(withBit > 0, "Probe ohne Saison mit Bit – Vorbedingung verfehlt");
    }

    /// <summary>
    /// Spielstand: Das Bit ist abgeleitet und deshalb kein Zustand – nach Sichern und Laden liefert die Ableitung dasselbe,
    /// und die Bytes bleiben gleich. Der Schemawächter (<c>SaveGameTests.SchemaFingerprint_MatchesSchemaVersion</c>) bleibt unverändert.
    /// </summary>
    [Fact]
    public void SaveLoad_StatusBitIsDerivedFromSavedData()
    {
        var game = GameModeTests.Create(4, true, true, 8, 2); // kader-start U15-a (L13): Saat 3 → 4, Vorbedingung nach dem Starttausch (Teamwerte bleiben am Platz)
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        // Spielberechnung Teil 3 (Verletzungen 0x38B8F) schwächt die Kader: Verein 2 mit Spielerwerten 99 (wie PlayAtTopStrength).
        foreach (var p in game.Clubs[2].Players)
            (p.Condition, p.Technique, p.Form) = (Player.MaxValue, Player.MaxValue, 50);
        PlayToSeasonEnd(game);
        Assert.True(HasBit(game, game.History[^1], 2));
        Assert.False(HasBit(game, game.History[^1], 8));

        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes);
        Assert.True(loaded.Success, loaded.Error);
        var record = loaded.Game!.History[^1];
        Assert.True(HasBit(loaded.Game, record, 2));
        Assert.False(HasBit(loaded.Game, record, 8));
        Assert.Equal(bytes, SaveGame.Save(loaded.Game));
    }

    /// <summary>
    /// Mehrsaisonprobe Modus 1 („nur Weltkugel“), vier Saisonwechsel, passiv und aktiv (Befund #126): Es gibt keine Vereinssaison, also
    /// kein Ergebnis je Manager und kein Statusbit; die Bestenliste hat in jeder Saison den Schlüssel 0x7530 (im Original erreichbar,
    /// Datei 0x28A3A) und einen gültigen Eintrag.
    /// </summary>
    [Theory]
    [InlineData(false, 7)]
    [InlineData(true, 7)]
    [InlineData(true, 19)]
    public void WorldOnly_MultiSeason_NoStatusBit_HighscoreKeyStays30000(bool active, int seed)
    {
        var game = GameModeTests.Create(seed, false, true, 5);
        ManagedSquads.PlayAtDatabaseStrength(game); // Vorbedingung Tabellenplatz (spielstaerke-manager-2, 0x98F8)
        for (var change = 0; change <= 4; change++)
        {
            Assert.Equal(HighscoreRules.WorldOnlyKey, HighscoreRules.Key(game.Setup));
            game.Continue(); // Sprung zum ersten WM-Termin, Ernennung
            if (active)
            {
                game.AutoNominate();
                for (var opponent = NationalTeams.First; opponent <= NationalTeams.Last; opponent++)
                    if (opponent != NationalTeams.Germany)
                        game.PlayFriendly(opponent, home: opponent % 2 == 0);
            }
            while (game.Phase == SeasonPhase.Running)
                game.Continue();
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            Assert.Empty(game.History[^1].Managers);
            var entry = HighscoreRules.Entry(game, 0, stamp: 1);
            Assert.True(entry.Score >= 1);
            Assert.Equal(game.Clubs[NationalTeams.Germany].Name, entry.Club);
            if (change < 4)
                game.StartNextSeason();
        }
        Assert.Equal(5, game.History.Count);
        Assert.Equal(HighscoreRules.WorldOnlyKey, HighscoreRules.Key(game.Setup));
    }
}
