using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Statistics;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur weltkugel-dosbox (Runde 7): Befunde aus den DOSBox-Aufnahmen „nur Weltkugel“ (`docs/dosbox-befunde.md`). Die
/// Bildnamen beziehen sich auf den Ordner `dosbox/nur-weltkugel/` im Referenz-Repo.
/// </summary>
public class WeltkugelDosboxTests
{
    private static readonly MenuTile HighscoreTile = MenuTiles.Small[0][5];

    /// <summary>Spiel auf synthetischer Datenbank (ohne Originaldaten) im Modus 1, 2 oder 4.</summary>
    private static GameState Synthetic(GameMode mode)
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        if (mode == GameMode.WorldOnly)
            setup.ToggleClubCompetitions();
        if (mode == GameMode.ClubOnly)
            setup.ToggleEuropeAndWorldCup();
        var game = NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
        Assert.Equal(mode, game.Mode);
        return game;
    }

    /// <summary>
    /// DOSBox <c>07_kachelreihe_manager_highscore_offen.png</c>: Im Modus „nur Weltkugel“ ist „Highscore“ in der Reihe Manager
    /// des Bundestrainers schon vor der Ernennung offen, die übrigen fünf Kacheln gesperrt. [ORIGINAL] Datei 0x45020–0x4504A:
    /// Der Platz wird beim Aufbau mit „DGROUP 0x256 = 0 oder (Manager 5 und Modus 4)“ überschrieben, die Ernennung (0xD0BA)
    /// wird nicht abgefragt. Modus 1 hat immer die Bestenliste 30000, also bleibt die Kachel auch nach der Ernennung offen.
    /// Vorher sperrte das Remake die ganze Reihe (Sperrtabelle DGROUP 0x5159 + 42 ohne die Überschreibung).
    /// </summary>
    [Fact]
    public void Mode1_CoachMenu_HighscoreOpen_BeforeAndAfterAppointment()
    {
        var game = Synthetic(GameMode.WorldOnly);
        Assert.True(game.NationalCoachMenu);
        Assert.Null(game.NationalCoach);
        Assert.Equal("Highscore", HighscoreTile.Name);
        Assert.False(NationalMenu.IsLocked(game, HighscoreTile));
        foreach (var tile in MenuTiles.Small[0].Take(5))
            Assert.True(NationalMenu.IsLocked(game, tile), tile.Name);

        game.Continue(); // „und tschüß…“: Sprung zum ersten WM-Termin, Ernennung
        Assert.NotNull(game.NationalCoach);
        Assert.True(game.NationalCoachMenu);
        Assert.False(NationalMenu.IsLocked(game, HighscoreTile));
        foreach (var tile in MenuTiles.Small[0].Take(5))
            Assert.True(NationalMenu.IsLocked(game, tile), tile.Name);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x4502C: Im Modus 4 sperrt das Original „Highscore“ im Zug des Bundestrainers ausdrücklich, auch wenn die
    /// Spielvorbereitung eine Bestenliste hat (Video <c>eisbaeren_f24_00049</c>). Unverändert.
    /// </summary>
    [Fact]
    public void Mode4_CoachHighscore_StaysLocked()
    {
        var game = Synthetic(GameMode.Full);
        Assert.False(NationalMenu.CoachHighscoreLocked(Synthetic(GameMode.WorldOnly)));
        Assert.True(NationalMenu.CoachHighscoreLocked(game));
    }

    /// <summary>
    /// DOSBox <c>07</c> → <c>19_highscore.png</c>: Die Kachel öffnet im Menü des Bundestrainers die Bestenliste (Schlüssel 30000)
    /// mit „Joerg – DEUTSCHLAND“. Öffnen ändert den Spielzustand nicht.
    /// </summary>
    [GameDataFact]
    public void Mode1_CoachMenu_HighscoreTile_OpensList()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        var game = d.Host.Game!;
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        Assert.True(game.NationalCoachMenu);
        Assert.Equal(HighscoreRules.WorldOnlyKey, HighscoreRules.Key(game.Setup));

        var before = StateFingerprint.Of(game);
        d.OpenTile("Highscore");
        Assert.IsType<HighscoreScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
        var entry = HighscoreRules.Entry(game, 0, 0);
        Assert.Equal("LetsShow", entry.Name);
        Assert.Equal(game.Clubs[NationalTeams.Germany].Name, entry.Club);
        Assert.Equal(1, entry.Score);
    }

    /// <summary>
    /// DOSBox <c>24_paarungen_wm_endrunde_vor_den_spielen.png</c>: „WM-Endrundenspiele“ vor den Spielen zeigt
    /// „Die Paarungen stehen noch nicht fest...“ und den Knopf „Gruppe“. [ORIGINAL] Datei 0x3337B–0x333C8: Wettbewerb 3 und
    /// WM-Stufe DGROUP 0x7AE &lt; 5 → Text DGROUP 0x1E58 (Zeiger 0x924) mit Schrift 0xC143 (Flow), zentriert 0 … 319, Grundlinie
    /// Rahmenoberkante + 0x10 + 0x12, Farbe 30, Schatten 0; Rahmen ohne Zeilen (n = 0 → 5 Teile ab y = 70, wie im Bild).
    /// Vorher blieb das Fenster leer.
    /// </summary>
    [GameDataFact]
    public void WorldCupKnockout_BeforeKnockout_ShowsNotYetKnown()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        Assert.Empty(game.WorldCup!.KnockoutPairings(0));
        var state = StateFingerprint.Of(game);
        d.Host.Show(new WorldCupKnockoutScreen(d.Host, d.Host.Current!));
        d.Move(0, 0);
        d.Frame();
        Assert.Equal(state, StateFingerprint.Of(game));

        var (parts, y) = WorldCupKnockoutScreen.FrameFor(0, 0, live: false);
        Assert.Equal((5, 70), (parts, y));
        var before = d.Surface.Pixels.ToArray();
        CompetitionWidgets.Centered(d.Surface, d.Host.Assets.Flow, 0, 319, y + 34, WorldCupKnockoutScreen.NotYetKnown,
            Colors.TileBackground, Colors.Black);
        CompetitionWidgets.Centered(d.Surface, d.Host.Assets.Mini, 160, 160, 221, "Gruppe", Colors.White, null);
        Assert.Equal(before, d.Surface.Pixels);

        // Knopf „Gruppe“ führt zur Gruppenansicht
        d.Click(WorldCupKnockoutScreen.Button.X + 5, WorldCupKnockoutScreen.Button.Y + 5);
        Assert.IsType<WorldCupScreen>(d.Host.Current);
    }

    /// <summary>
    /// DOSBox <c>32_wm_vorbereitung_gegner_usa.png</c>, <c>33_…_kanada.png</c>, <c>34_…_schweden.png</c>: Kosten eines
    /// Heimspiels (Vorgabe „Heim“, Bild 31) 100.000, 120.000 und 110.000 DM. DOSBox <c>39</c>/<c>40</c>: Freundschaftsspiele
    /// lassen keine Zeit vergehen (Datum bleibt der 14. April) und werden vom Nationalkonto bezahlt. Schon vorher richtig;
    /// Regressionstest.
    /// </summary>
    [GameDataFact]
    public void Friendlies_CostsAndNoTimeAdvance_AsInDosbox()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        // #195: „Los jetzt!“ springt schon zum ersten WM-Termin (Ernennung)
        var game = d.Host.Game!;
        Assert.NotNull(game.NationalCoach);
        game.AutoNominate();

        int Nation(string name) => game.Clubs.Single(c => c.Index >= NationalTeams.First && c.Index <= NationalTeams.Last
            && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)).Index;
        Assert.Equal(100_000, NationalCoach.FriendlyCost(Nation("USA"), home: true));
        Assert.Equal(120_000, NationalCoach.FriendlyCost(Nation("Kanada"), home: true));
        Assert.Equal(110_000, NationalCoach.FriendlyCost(Nation("Schweden"), home: true));

        d.OpenTile("Training");
        var screen = Assert.IsType<FriendliesScreen>(d.Host.Current);
        Assert.True(screen.Home);
        var day = game.CurrentDay;
        var played = game.PlayedMatchDays;
        var account = game.NationalAccount;
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(Nation("Schweden"), home: true));
        Assert.Equal(day, game.CurrentDay);
        Assert.Equal(played, game.PlayedMatchDays);
        Assert.Equal(account - 110_000, game.NationalAccount);
    }

    /// <summary>
    /// DOSBox <c>25</c> → <c>45</c>: Vor den Freundschaftsspielen Energie 56–79, nach drei Spielen bei allen 46.
    /// [ORIGINAL] Datei 0x3BB51 (<c>mov byte [bx+0x5DD6],0x46</c>, Schleife über 29 Plätze ab 0x3BB22): Das Öffnen von
    /// „WM Vorbereitung“ setzt die Energie aller belegten Kaderplätze auf 70. [APPROX] Entscheidung des Nutzers
    /// (30.10.2026, PR #189): Das Remake setzt sie beim ersten Freundschaftsspiel der Saison, weil Bildschirme beim
    /// Öffnen nichts ändern; der Abzug je Spiel (70 − 3 · 8 = 46) gehört zu `spielberechnung`. Vorher blieb die Energie
    /// unverändert.
    /// </summary>
    [Fact]
    public void FirstFriendly_SetsSquadEnergyTo70()
    {
        var game = Synthetic(GameMode.WorldOnly);
        game.Continue(); // Ernennung
        var coach = game.NationalCoach!;
        game.AutoNominate();
        Assert.NotEmpty(coach.Squad);
        var outsider = game.NationalCandidates().Select(c => c.Player).First(p => !coach.Contains(p));
        var i = 0;
        foreach (var m in coach.Squad)
            m.Player.Energy = 50 + i++ % 30;
        outsider.Energy = 55;
        var opponent = NationalTeams.First == coach.Nation ? NationalTeams.First + 1 : NationalTeams.First;
        game.NationalAccount = 10_000_000;

        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(opponent, home: false));
        Assert.All(coach.Squad, m => Assert.Equal(NationalCoach.FriendlyEnergy, m.Player.Energy));
        Assert.Equal(55, outsider.Energy);

        // Nur beim ersten Spiel der Saison: ein zweites Spiel setzt nicht erneut.
        coach.Squad[0].Player.Energy = 40;
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(opponent, home: false));
        Assert.Equal(40, coach.Squad[0].Player.Energy);
    }

    /// <summary>Abgelehntes erstes Freundschaftsspiel (kein Geld) setzt keine Energie.</summary>
    [Fact]
    public void RejectedFriendly_KeepsEnergy()
    {
        var game = Synthetic(GameMode.WorldOnly);
        game.Continue();
        var coach = game.NationalCoach!;
        game.AutoNominate();
        foreach (var m in coach.Squad)
            m.Player.Energy = 51;
        game.NationalAccount = 0;
        var opponent = NationalTeams.First == coach.Nation ? NationalTeams.First + 1 : NationalTeams.First;
        Assert.Equal(FriendlyResult.NotEnoughMoney, game.PlayFriendly(opponent, home: false));
        Assert.All(coach.Squad, m => Assert.Equal(51, m.Player.Energy));
    }

    /// <summary>
    /// Öffnen von „WM Vorbereitung“ bleibt zustandsrein (Architekturregel), obwohl das Original dabei die Energie setzt
    /// (0x3BB51); erst das erste Spiel setzt sie.
    /// </summary>
    [GameDataFact]
    public void OpeningFriendlies_KeepsState_FirstFriendlySetsEnergy()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        // #195: „Los jetzt!“ springt schon zum ersten WM-Termin (Ernennung)
        var game = d.Host.Game!;
        game.AutoNominate();
        foreach (var m in game.NationalCoach!.Squad)
            m.Player.Energy = 60;
        var before = StateFingerprint.Of(game);
        d.Host.Show(new FriendliesScreen(d.Host, d.Host.Current!));
        d.Move(0, 0);
        d.Frame();
        Assert.Null(StateFingerprint.FirstDifference(before, StateFingerprint.Of(game)));

        var opponent = NationalTeams.First == game.NationalCoach.Nation ? NationalTeams.First + 1 : NationalTeams.First;
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(opponent, home: false));
        Assert.All(game.NationalCoach.Squad, m => Assert.Equal(NationalCoach.FriendlyEnergy, m.Player.Energy));
    }

    /// <summary>
    /// Mehrsaisonprobe (Abnahme der Spur): vier Saisonwechsel im Modus 2 und 4 (passiver Manager) und im Modus 1; die
    /// Kachelsperren der Reihe Manager und der WM-Kacheln bleiben in jeder Saison wie im Original.
    /// </summary>
    [Theory]
    [InlineData(GameMode.WorldOnly)]
    [InlineData(GameMode.ClubOnly)]
    [InlineData(GameMode.Full)]
    public void MultiSeason_LocksStable(GameMode mode)
    {
        var game = Synthetic(mode);
        var first = game.Season;
        for (var guard = 0; game.Season < first + 4; guard++)
        {
            Assert.True(guard < 20000, "Saisonwechsel nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            if (game.NationalCoachMenu)
                Assert.Equal(mode == GameMode.Full, NationalMenu.IsLocked(game, HighscoreTile));
            else
                Assert.Equal(TileLocks.IsLocked(game, HighscoreTile), NationalMenu.IsLocked(game, HighscoreTile));
            if (mode == GameMode.ClubOnly)
                Assert.All(NationalMenu.WorldTiles, name =>
                    Assert.True(NationalMenu.IsLocked(game, MenuTiles.Small.SelectMany(t => t).Single(t => t.Name == name))));
            if (game.Phase == SeasonPhase.SeasonOver)
                game.StartNextSeason();
            else
                game.Continue();
        }
        Assert.Equal(first + 4, game.Season);
        if (mode == GameMode.WorldOnly)
            Assert.True(game.NationalCoachMenu);
    }

    /// <summary>
    /// Mehrsaisonprobe mit aktivem Bundestrainer: vier Saisonwechsel im Modus 1, 2 und 4; in jeder Saison mit
    /// Bundestrainer setzt das erste Freundschaftsspiel die Energie des Kaders auf 70, Modus 2 hat nie einen.
    /// </summary>
    [Theory]
    [InlineData(GameMode.WorldOnly)]
    [InlineData(GameMode.ClubOnly)]
    [InlineData(GameMode.Full)]
    public void MultiSeason_ActiveCoach_FirstFriendlySetsEnergy(GameMode mode)
    {
        var game = Synthetic(mode);
        var first = game.Season;
        var seasonsWithFriendly = new HashSet<int>();
        for (var guard = 0; game.Season < first + 4; guard++)
        {
            Assert.True(guard < 20000, "Saisonwechsel nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            if (game.NationalCoach is { } coach && coach.Friendlies.Count == 0 && !seasonsWithFriendly.Contains(game.Season))
            {
                game.AutoNominate();
                Assert.NotEmpty(coach.Squad);
                foreach (var m in coach.Squad)
                    m.Player.Energy = 55;
                var opponent = NationalTeams.First == coach.Nation ? NationalTeams.First + 1 : NationalTeams.First;
                Assert.Equal(FriendlyResult.Played, game.PlayFriendly(opponent, home: false));
                Assert.All(coach.Squad, m => Assert.Equal(NationalCoach.FriendlyEnergy, m.Player.Energy));
                seasonsWithFriendly.Add(game.Season);
            }
            if (game.Phase == SeasonPhase.SeasonOver)
                game.StartNextSeason();
            else
                game.Continue();
        }
        Assert.Equal(first + 4, game.Season);
        // Modus 1: jede Saison Bundestrainer; Modus 4: nur in Saisons mit Ernennung am 13. April (Managerwert,
        // CupsNationalMode.cs) – wie viele, hängt an den Spielergebnissen (vor der Spielberechnung des Originals zwei,
        // seit Runde 7/R7-02 eine von vier), geprüft wird mindestens eine und nicht jede; Modus 2: nie.
        if (mode == GameMode.Full)
            Assert.InRange(seasonsWithFriendly.Count, 1, 3);
        else
            Assert.Equal(mode == GameMode.WorldOnly ? 4 : 0, seasonsWithFriendly.Count);
    }
}
