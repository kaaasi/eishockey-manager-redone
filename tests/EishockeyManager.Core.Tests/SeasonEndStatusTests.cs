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
        // Runde 21, Spur zufallsstrom Teil 3 Spielstart (R21-D2 = A, nach Rebase auf zusehen): Saat 4 statt 3 – der Spielstart zieht in der Reihenfolge des Originals aus dem gemeinsamen Strom (Kaderplätze der Manager im Poolaufbau Datei 0x5B0BB–0x5B2BE, Platzwahl Datei 0x29C91 nach dem Pool, Startwerte Datei 0x29DA4, Markt Datei 0x43A83 nach der Wanderung), anderer Zufallsverlauf; bei Saat 3 steht Verein 8 unter den ersten fünf und Verein 2 nicht (Vorbedingung); 4–10 erfüllen sie. Die Probe hängt an der Saat.
        var game = GameModeTests.Create(4, true, true, 8, 2); // #621 (R13GN-04): Saat wieder 3 – die Änderung 3 → 4 (L13) war nicht nötig
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
        // Runde 21, Spur zufallsstrom Teil 3 Spielstart (R21-D2 = A, nach Rebase auf zusehen): Saat 69 statt 24 – der Spielstart zieht in der Reihenfolge des Originals aus dem gemeinsamen Strom (Platzwahl Datei 0x29C91 nach dem Pool, Startwerte Datei 0x29DA4), anderer Zufallsverlauf; unter 24–90 stehen nur bei 69, 83 und 86 beide Managervereine unter den ersten fünf. Die Probe hängt an der Saat.
        // Runde 21, Spur zufallsstrom Teil 4 (R21-D2 = A, R15-E2 = C): Saat 72 statt 69 – Pokalauslosungen (Datei 0x298B1, 0x43A8C) und Pokalspiele ziehen aus dem gemeinsamen Strom statt aus CupRandom, anderer Zufallsverlauf ab dem Spielstart; unter 60–72 stehen nur bei 72 beide Managervereine unter den ersten fünf. Die Probe hängt an der Saat.
        // Runde 21, Spur zufallsstrom Teil 4 (R21-D2 = A, R15-E2 = C): Saat 77 statt 72 – Werbung, Börse, Zuschauer, Talentsuche, Jugend und Baufirmen ziehen aus dem gemeinsamen Strom statt aus FinanceRandom (Werbeangebote des Spielstarts Datei 0x2A03B–0x2A061, Börse Datei 0x43AD5), anderer Zufallsverlauf ab dem Spielstart; unter 60–77 stehen nur bei 77 beide Managervereine unter den ersten fünf.
        // Runde 22, Spur kaderplatz-werte Teil 2 (A093; R21-D2 = A, R15-E2 = C): Saat 81 statt 77 – die Spielabrechnung 0x544C0 würfelt vor jedem Spiel eines Managervereins je Kaderplatz random(0, 4) (Datei 0x548A8–0x548E7) aus dem gemeinsamen Strom, anderer Zufallsverlauf ab dem ersten Managerspiel; unter 77–81 stehen nur bei 81 beide Managervereine unter den ersten fünf (weiter 88, 92).
        // Rückschaltprobe: src/ von faa64d2 mit Saat 81 ebenfalls grün – die Probe hängt an der Saat.
        // Runde 22, Spur wettbewerbe-regeln Teil 2 (R15-E2 = C): Saat 88 statt 81 – die Jugend läuft nach den Menüs vor den
        // Ligaspielen (A384/A670, Datei 0x55003–0x5502C), andere Lage der Würfe ab dem ersten Ligaspieltag; unter 81–92 stehen nur bei
        // 88 und 92 beide Managervereine unter den ersten fünf. Rückschaltprobe: alter Code mit Saat 88 ebenfalls grün.
        // Spur wettbewerbe-regeln Fixlauf Rebase auf main f7f0410 (Runde 22, R15-E2 = C): Saat 92 statt 88 – mit den Strömen von kader-regeln, transfer-pool und
        // einzelpunkte stehen bei 88 (und 89–91, 93–96) nicht beide Managervereine unter den ersten fünf. Rückschaltprobe: src/ von f7f0410
        // mit Saat 92 ebenfalls grün – die Probe hängt an der Saat.
        // Spur saison-geld Teil 1 (Runde 25, T03, R21-D2 = A, R15-E2 = C): Saat 101 statt 92 – Markt-Takt random(0, 3) je Markt-Platz (A829, Datei
        // 0x07215), Oberliga-Zug und Wanderung am 29. April ändern den Zufallsverlauf ab dem ersten Takttag; unter 92–159 stehen nur
        // bei 101, 116, 117, 125, 128, 133 beide Managervereine unter den ersten fünf. Die Probe hängt an der Saat (Vorbedingung
        // ausdrücklich geprüft).
        // Spur saison-geld Teil 2 (Runde 25, F08 A512/A526, R21-D2 = A, R15-E2 = C): Saat 122 statt 101 – die Baufirmen ziehen im
        // Tagesabschluss 11 Würfe je Manager (Datei 0x3ED19–0x3ED48), die Sommerpause läuft je Manager verschränkt (0x564D2–0x56593);
        // unter 101–140 stehen nur bei 122, 125, 128, 133 beide Managervereine unter den ersten fünf. Die Probe hängt an der Saat.
        // Runde 26, Spur tagesabschluss (#1018, SG-01, R21-D2 = A, R15-E2 = C): Saat 125 statt 122 – Tagesabschluss 0x3ED01 und
        // Werbebewertung 0x3F833 je Manager verschränkt (Datei 0x55217–0x55251), Talentsuche danach (Datei 0x5528A); unter 122–140
        // stehen nur bei 125, 128, 133 beide Managervereine unter den ersten fünf. Rückschaltprobe: src/ von 56b7f7c mit Saat 125
        // ebenfalls grün – die Probe hängt an der Saat.
        var game = GameModeTests.Create(125, true, true, 2, 1); // kader-start U15-a/R-K5 (L13): Saat 3 → 17; #675 (Tageszähler 0x3B7, anderer Zufallsverlauf): 17 → 56; #678 (Tagestakt 0x07169): 56 → 71; #784 (Gast nach dem Heimverein, Runde 17; Saaten 71–83 ohne Vorbedingung): 71 → 84, Vorbedingung beide unter den ersten fünf; Spur start-lader (Lader beim Spielstart, Datei 0x439BB, anderer Zufallsverlauf): 84 → 24
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
        // Runde 21, Spur zufallsstrom Teil 4 (R21-D2 = A, R15-E2 = C): Saat 10 statt 9 – Pokalauslosungen (Datei 0x298B1, 0x43A8C) und Pokalspiele ziehen aus dem gemeinsamen Strom statt aus CupRandom, anderer Zufallsverlauf ab dem Spielstart; bei Saat 9 ist Verein 2 nicht unter den ersten fünf, unter 7–16 bei 10, 11, 16. Die Probe hängt an der Saat.
        // Spur saison-geld Teil 1 (Runde 25, T03, R21-D2 = A, R15-E2 = C): Saat 11 statt 10 – anderer Zufallsverlauf ab dem ersten Takttag
        // (A829, Datei 0x07215); unter 10–59 steht Verein 2 bei 11, 13, 16, 17, 28 … unter den ersten fünf. Die Probe hängt an der Saat.
        var game = GameModeTests.Create(11, true, false, 2); // kader-start U15-a/R-K5 (L13): Saat 3 → 22, Vorbedingung nach dem Starttausch (Teamwerte bleiben am Platz); Spur start-lader (Lader beim Spielstart, Datei 0x439BB, anderer Zufallsverlauf): 22 → 9
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
        // Runde 21, Spur zufallsstrom Teil 4 (R21-D2 = A, R15-E2 = C): Saat 6 statt 5 – Pokalauslosungen (Datei 0x298B1, 0x43A8C) und Pokalspiele ziehen aus dem gemeinsamen Strom statt aus CupRandom, anderer Zufallsverlauf ab dem Spielstart; bei Saat 5 tritt das Bit nicht auf, unter 3–10 bei 3, 6, 8, 9. Die Probe hängt an der Saat.
        var game = GameModeTests.Create(6, true, true, 2, 1); // #675 (Tageszähler 0x3B7, anderer Zufallsverlauf): Saat 3 → 5, Vorbedingung Bit
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
        // Runde 21, Spur zufallsstrom Teil 3 Spielstart (R21-D2 = A, nach Rebase auf zusehen): Saat 4 statt 3 – der Spielstart zieht in der Reihenfolge des Originals aus dem gemeinsamen Strom (Kaderplätze der Manager im Poolaufbau Datei 0x5B0BB–0x5B2BE, Platzwahl Datei 0x29C91 nach dem Pool, Startwerte Datei 0x29DA4, Markt Datei 0x43A83 nach der Wanderung), anderer Zufallsverlauf; bei Saat 3 steht Verein 8 unter den ersten fünf und Verein 2 nicht (Vorbedingung); 4–10 erfüllen sie. Die Probe hängt an der Saat.
        var game = GameModeTests.Create(4, true, true, 8, 2); // #621 (R13GN-04): Saat wieder 3 – die Änderung 3 → 4 (L13) war nicht nötig
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
