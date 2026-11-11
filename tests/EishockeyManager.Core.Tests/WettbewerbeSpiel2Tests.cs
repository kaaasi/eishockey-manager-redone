using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Teil 2: Begegnungstabellen für alle Wettbewerbe (Inventur P2 Nr. 11, A111; #344).
/// [ORIGINAL] Die Ergebniseintragung em.exe Datei 0x50283 schreibt über 0x4F5A8 die Begegnungstabelle (DGROUP 0xAB7B) für
/// jedes Spiel, das sie bekommt: Liga 0x50B45 (Code 7), DEB-Pokal 0x34B76 (Aufruf Datei 0x34C5C, Code 6), Play-offs
/// 0x315E9 (Datei 0x3165B, Code 1), Gruppenrunden 0x30852 nur für Wettbewerb 0 (WM) und 1 (Aufstiegsrunden; Prüfung
/// Datei 0x3091C), WM-Endrunde 0x30E7B (Datei 0x3108B, Code 10) und Freundschaftsspiel 0x30365 (Datei 0x303BA, Code 0xD).
/// Europapokal (Wettbewerb 2 und 3 in 0x30852) ruft 0x50283 nicht. Pokal- und Play-off-Ergebnisse gehen ohne Marken
/// (je − 30 im Heimbyte, Datei 0x34C06–0x34C2A, 0x31622–0x31631, 0x31052–0x31061) ein.
/// Befund vorher (Kopf 38accc1): nur Liga- und Länderspiele; DEB-Pokal-Spiele fehlten in Infoleiste und Gegner-Fenster;
/// Länderspiele Deutschlands kamen über einen eigenen Zweig (<c>EnterNationalResult</c>), der Bundestrainer-Zweig von
/// <c>RecordMeeting</c> verlangte einen ernannten Bundestrainer und war ungetestet (#344).
/// </summary>
public class WettbewerbeSpiel2Tests
{
    /// <summary>
    /// Regressionstest (vorher rot): Bis zum Ende der Hauptrunde stehen in der Tabelle des Managers gegen jeden Verein genau
    /// die Liga- und DEB-Pokal-Spiele, Europapokalspiele nicht. Der Wert des DEB-Pokalspiels ist der Endstand ohne Marke.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void MeetingTable_HoldsLeagueAndDebCup_NotEuropeanCup(int seed)
    {
        var game = TestGames.Create(seed);
        var manager = game.ActiveManager;
        var club = manager.ClubIndex;
        PlayRegularSeason(game);
        // Integration Runde 11: Continue kann nach dem letzten Ligaspieltag schon den ersten Play-off-Tag spielen (Seed 3);
        // Play-offs ohne den Manager berühren seine Tabelle nicht (Play-offs selbst: MeetingTable_HoldsPlayoffAndRelegationGames…).
        Assert.DoesNotContain(game.PlayoffResults, m => m.Home == club || m.Away == club);

        var games = game.Results.Where(r => r.Involves(club)).Select(r => (r.Home, r.Away, r.HomeGoals, r.AwayGoals))
            .Concat(game.DebCup!.Matches.Where(m => m.Home == club || m.Away == club).Select(m => (m.Home, m.Away, m.HomeGoals, m.AwayGoals)))
            .ToList();
        var debGames = game.DebCup.Matches.Count(m => m.Home == club || m.Away == club);
        Assert.True(debGames > 0);
        // Europapokal gespielt, nicht eingetragen. WS-03 (Review #468): Europapokalgegner haben Index ≥ 64 und fallen in
        // MeetingHistory.Record ohnehin heraus; die Prüfung unten belegt das „nein“ daher nur strukturell (Gegner < 52).
        Assert.True(game.EuropeanCup!.Matches.Count(m => m.Home == club || m.Away == club) > 0);

        var table = game.MeetingTable(manager);
        for (var opponent = 0; opponent < MeetingHistory.FirstNation; opponent++)
        {
            if (opponent == club)
                continue;
            foreach (var side in new[] { 0, 1 })
            {
                var expected = games.Where(g => side == 0 ? g.Home == club && g.Away == opponent : g.Away == club && g.Home == opponent)
                    // Byte wie 0x4F5A8 (ein Ergebnis mit 16 oder mehr Toren läuft über, Seed 5 im integrierten Stand)
                    .Select(g => (int)(byte)(side == 0 ? g.HomeGoals + (g.AwayGoals << 4) : g.AwayGoals + (g.HomeGoals << 4)))
                    .ToList();
                Assert.True(expected.Count <= MeetingHistory.Slots);
                var actual = Enumerable.Range(0, MeetingHistory.Slots).Select(s => table[opponent * 10 + s * 2 + side])
                    .Where(v => v != MeetingHistory.Empty).ToList();
                Assert.Equal(expected.Order(), actual.Order());
            }
        }
    }

    /// <summary>
    /// #344, Regressionstest (vorher rot): [ORIGINAL] 0x50283 (Datei 0x502B4–0x502C8) vergleicht beide Vereine mit DGROUP
    /// 0x6AC und schreibt dann in die Tabelle von Manager 0 – unabhängig davon, ob und welcher Manager Bundestrainer ist.
    /// 0x6AC beschreibt nur die WM-Auslosung (Datei 0x35FAC, Index Deutschlands nach dem Tausch); im Remake behalten die
    /// Länder ihren Index, 0x6AC ist also immer <see cref="NationalTeams.Germany"/>.
    /// </summary>
    [Fact]
    public void RecordMeeting_Germany_WritesManagerZero_WithoutCoach()
    {
        var game = TwoManagers();
        Assert.Null(game.NationalCoach);

        game.RecordMeeting(NationalTeams.Germany, NationalTeams.Germany + 1, 3, 1);
        game.RecordMeeting(NationalTeams.Germany + 2, NationalTeams.Germany, 4, 2);

        Assert.Equal(["3:1"], MeetingHistory.Entries(game.MeetingTable(0), NationalTeams.Germany + 1));
        Assert.Equal(["2:4"], MeetingHistory.Entries(game.MeetingTable(0), NationalTeams.Germany + 2));
        Assert.All(Enumerable.Range(0, MeetingHistory.Opponents),
            o => Assert.Empty(MeetingHistory.Entries(game.MeetingTable(1), o)));
    }

    /// <summary>
    /// #344: Ist der zweite Manager Bundestrainer, schreibt Deutschland trotzdem in die Tabelle von Manager 0 (0x502BD
    /// <c>mov cl, 1</c> → Manager 0); das Bundestrainermenü liest dieselbe Tabelle (0x43E21, Datei 0x4417C–0x4418F).
    /// Länderspiele laufen über denselben Schreiber wie Vereinsspiele.
    /// </summary>
    [Fact]
    public void RecordMeeting_Germany_WritesManagerZero_WhenSecondManagerIsCoach()
    {
        var game = TwoManagers();
        game.AppointNationalCoach(game.Managers[1]);

        game.EnterNationalResult(NationalTeams.Germany + 3, NationalTeams.Germany, 1, 5, NationalMatchKind.Friendly);
        game.RecordMeeting(2, 5, 2, 2);

        Assert.Equal(["5:1"], MeetingHistory.Entries(game.MeetingTable(0), NationalTeams.Germany + 3));
        Assert.Empty(MeetingHistory.Entries(game.MeetingTable(1), NationalTeams.Germany + 3));
        Assert.Equal(["H 2:2"], MeetingHistory.Entries(game.MeetingTable(0), 5));
        Assert.Equal(["A 2:2"], MeetingHistory.Entries(game.MeetingTable(1), 2));
    }

    /// <summary>
    /// K.-o.-Spiele (Play-offs, Aufstiegsrunden) über <see cref="GameState.RecordMeetings"/>: jedes Spiel einmal, Heim- und
    /// Gastsicht wie 0x50283. Den Aufruf aus dem Play-off-Tag (<c>SeasonFlow.cs</c>, Spur saisonwechsel) setzt die
    /// Leitstelle in der Integration.
    /// </summary>
    [Fact]
    public void RecordMeetings_EachGameOnce_FromBothViews()
    {
        var game = TwoManagers();
        game.RecordMeetings([new KnockoutMatch(1, 2, 7, 4, 4, 7), new KnockoutMatch(2, 7, 2, 1, 3), new KnockoutMatch(3, 5, 2, 0, 2)]);

        Assert.Equal(["A 3:1", "H 4:4"], MeetingHistory.Entries(game.MeetingTable(0), 7));
        Assert.Equal(["A 2:0"], MeetingHistory.Entries(game.MeetingTable(0), 5));
        Assert.Equal(["H 0:2"], MeetingHistory.Entries(game.MeetingTable(1), 2));
    }

    /// <summary>
    /// Integration Runde 11 (Anschluss aus Teil 2, #425): Der Play-off-Tag trägt Play-off- (Datei 0x3165B) und
    /// Aufstiegsrundenspiele (0x30942, Wettbewerb 1) in die Begegnungstabellen ein. Über eine ganze Saison steht gegen jeden
    /// Gegner genau jedes Liga-, DEB-Pokal-, Play-off- und Aufstiegsrundenspiel; die Probe verlangt, dass mindestens ein
    /// Manager eine Nachsaison spielt (Bundesliga- und Zweitligist).
    /// </summary>
    [Fact]
    public void MeetingTable_HoldsPlayoffAndRelegationGames_AfterIntegration()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Oben", Head = 0, ClubIndex = 0 });
        setup.Managers.Add(new ManagerSetup { Name = "Mitte", Head = 1, ClubIndex = 12 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        // Beide Managervereine gewinnen jedes Ligaspiel (wie SaisonwechselDoppelwechselTests), damit sie sicher in die
        // Nachsaison kommen: Verein 0 in die Play-offs, Verein 12 in die Aufstiegsrunde.
        var winners = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        for (var guard = 0; !game.RegularSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Hauptrunde nicht beendet");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            TestSeason.PlayMatchDay(game);
            for (var i = 0; i < game.Results.Count; i++)
            {
                var r = game.Results[i];
                if (winners.Contains(r.Home) && !winners.Contains(r.Away))
                    game.Results[i] = r with { HomeGoals = 9, AwayGoals = 0 };
                else if (winners.Contains(r.Away) && !winners.Contains(r.Home))
                    game.Results[i] = r with { HomeGoals = 0, AwayGoals = 9 };
            }
        }
        // Begegnungstabelle der Hauptrunde aus den umgeschriebenen Ergebnissen neu aufbauen ist nicht nötig: verglichen wird
        // unten nur die Nachsaison gegen den Stand vor ihr.
        var before = game.Managers.Select(m => game.MeetingTable(m).ToArray()).ToList();
        var dayBefore = game.Day;
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            game.Continue();
        }
        var postseason = 0;
        foreach (var manager in game.Managers)
        {
            var club = manager.ClubIndex;
            var post = game.PlayoffResults.Where(m => m.Home == club || m.Away == club)
                .Select(m => (m.Home, m.Away, m.HomeGoals, m.AwayGoals))
                .Concat(game.RelegationGroups.SelectMany(g => g.Results).Where(r => r.Involves(club))
                    .Select(r => (r.Home, r.Away, r.HomeGoals, r.AwayGoals)))
                .ToList();
            postseason += post.Count;
            var games = post;
            var table = game.MeetingTable(manager);
            var old = before[game.Managers.ToList().IndexOf(manager)];
            // Spur pool (#743, R15-E2 = C): DEB-Pokalspiele nach der Hauptrunde trägt 0x34B76 ebenso ein (Datei 0x34C5C); ob ein
            // Managerverein dann noch im Pokal steht, hängt vom Zufallsverlauf ab (mit den Managerkadern im Pool: Saat 3 ja).
            var cup = game.DebCup?.Matches.Where(m => m.Day > dayBefore && (m.Home == club || m.Away == club)).ToList() ?? [];
            for (var opponent = 0; opponent < MeetingHistory.FirstNation; opponent++)
            {
                if (opponent == club)
                    continue;
                foreach (var side in new[] { 0, 1 })
                {
                    var expected = games.Where(g => side == 0 ? g.Home == club && g.Away == opponent : g.Away == club && g.Home == opponent)
                        .Select(g => side == 0 ? g.HomeGoals + (g.AwayGoals << 4) : g.AwayGoals + (g.HomeGoals << 4))
                        .ToList();
                    var oldCount = Enumerable.Range(0, MeetingHistory.Slots).Count(s => old[opponent * 10 + s * 2 + side] != MeetingHistory.Empty);
                    var actual = Enumerable.Range(0, MeetingHistory.Slots).Select(s => table[opponent * 10 + s * 2 + side])
                        .Where(v => v != MeetingHistory.Empty).ToList();
                    // Neue Einträge = Nachsaisonspiele dieser Seite (solange die Plätze reichen); die Werte stehen in der Tabelle.
                    var cupGames = cup.Count(g => side == 0 ? g.Home == club && g.Away == opponent : g.Away == club && g.Home == opponent);
                    Assert.Equal(Math.Min(MeetingHistory.Slots, oldCount + expected.Count + cupGames), actual.Count);
                    Assert.All(expected, v => Assert.Contains(v, actual));
                }
            }
        }
        Assert.True(postseason > 0, "kein Manager in Play-offs oder Aufstiegsrunde");
    }

    private static GameState TwoManagers()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Eins", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Zwei", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 3);
    }

    private static void PlayRegularSeason(GameState game)
    {
        for (var guard = 0; !game.RegularSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Hauptrunde nicht beendet");
            foreach (var manager in game.Managers)
                manager.Money = Math.Max(manager.Money, 10_000_000);
            game.Continue();
        }
    }
}
