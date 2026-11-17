using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur teamwert-aufstellung (Runde 21): A174, A544, A080 – Teamwert der Managervereine aus der Aufstellung wie im Original.
/// [ORIGINAL] 0x977F (Datei 0x097CF–0x0984F): Managerverein (Vereinsfeld 0x16 ≠ 0) im Spiel aus den Spielern nur mit Eintrag in
/// der Liste DGROUP 0x233 (0x52B91), sonst aus den Vereinsfeldern 0x18–0x1D, die 0x952C aus der Aufstellung schreibt (Datei
/// 0x09721–0x09771, Tageslauf Datei 0x54FBF). Die Spielvorbereitung 0x8365 (Datei 0x36D12/0x36D49) läuft vor dem Füllen der Liste.
/// </summary>
public class TeamwertAufstellungTests(ITestOutputHelper output)
{
    /// <summary>
    /// Regressionstest A080 gegen Originalstände (rot vor dem Fix bei FILE31, FILE61, FILE66, FILE67 und FILE73; FILE40 war
    /// zufällig richtig): je Paar ein Spieltag ohne Eingriff. Die Zeile der Automatik, die 0x8365 vor dem Spiel in den Vereinssatz
    /// schreibt – Zuschläge des Einsatzes 0x27–0x2B, Grundwert 0x2D, Angriff 0x35–0x3A –, steht beim Managerverein und bei seinem
    /// Gegner wie im Original. Sie hängt nur an den Stärken 0x368B1 vor dem Spiel (ohne Zufall): ausgeglichen bei
    /// |Differenz der doppelten Stärken| &lt; 25, Angriffszeile nach Favorit. Vorher verglich das Remake die Stärke aus den
    /// Spielern auf dem Eis (z. B. FILE31: KAUFBEUREN Angriff 5 6 4 4 0 statt 4 5 6 7 4, LANDSHUT Grundwert 60 statt 40).
    /// FILE50 → FILE61/66/67 sind drei DOSBox-Läufe desselben Spieltags, FILE72 → FILE73 hat zwei Managervereine. FILE73 entstand
    /// ohne erneutes Laden (Tausch bleibt, 0x375/0x376 = 0/0); der Import von FILE72 bildet das Laden nach (Automatik, Tausch
    /// verworfen, #908, <c>ZusehenRestAufstellungTests</c>). Die Zeile der Automatik hängt nicht am Tausch.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE30", "FILE31", 1)]
    [InlineData("FILE30", "FILE31", 2)]
    [InlineData("FILE39", "FILE40", 1)]
    [InlineData("FILE50", "FILE61", 1)]
    [InlineData("FILE50", "FILE66", 1)]
    [InlineData("FILE50", "FILE67", 1)]
    [InlineData("FILE50", "FILE67", 3)]
    [InlineData("FILE72", "FILE73", 1)]
    [InlineData("FILE72", "FILE73", 2)]
    public void A080_Preparation_AutoLineOfManagerPairing_LikeOriginal(string from, string to, int seed)
    {
        var game = Import(from, seed);
        var target = Import(to, seed);
        var before = game.Results.Count;
        Assert.Null(HokCompare.PlayTo(game, target));
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var played = game.Results.Skip(before).Where(m => managed.Contains(m.Home) || managed.Contains(m.Away)).ToList();
        Assert.Equal(managed.Count, played.Count); // ein Spieltag, keine Paarung zweier Managervereine

        var records = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(to + ".HOK"))).Block("far:9657");
        var situations = Enum.GetValues<ScoreSituation>();
        var compared = 0;
        for (var slot = 0; slot < 52; slot++)
        {
            var r = records.AsSpan(slot * OriginalSaveGame.ClubRecordLength, OriginalSaveGame.ClubRecordLength).ToArray();
            var club = game.Clubs.Single(c => c.Name == OriginalImport.ClubName(records, slot));
            if (!played.Any(m => m.Involves(club.Index)))
                continue;
            var tactics = club.Tactics;
            Assert.Equal(TacticMode.Auto, tactics.EffortMode);
            Assert.Equal(TacticMode.Auto, tactics.AttackMode);
            var original = $"{r[0x2D]} {string.Join(' ', r[0x27..0x2C])} | {string.Join(' ', r[0x35..0x3B])}";
            var remake = $"{string.Join(' ', situations.Select(s => tactics.ShownEffort(0, s)))} | "
                + string.Join(' ', situations.Select(s => tactics.ShownAttack(0, s)));
            output.WriteLine($"{club.Name}: Original {original}, Remake {remake}");
            Assert.Equal(original, remake);
            compared++;
        }
        Assert.Equal(2 * managed.Count, compared);
    }

    /// <summary>
    /// Review #906 P3-2 (#902): Die Vorbereitung rechnet für einen Managerverein mit Squad auf einer Kopie ohne Spieler
    /// (<c>PrepareFromClubFields</c>) und überträgt das Ergebnis mit <c>CopyPreparation</c> auf die Mannschaft der Paarung: Einsatz
    /// (Modus AUTOM.) und die Stärken der Zeile der Automatik stehen danach an der Mannschaft des Managervereins.
    /// </summary>
    [Fact]
    public void BeginMatch_ManagedClub_TakesEffortAndStrengthsOfPreparation()
    {
        var game = TestGames.Create(5);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs.First(c => c != club && c.Players.Count > 0 && c.League == club.League);
        Assert.Equal(TacticMode.Auto, club.Tactics.EffortMode);

        var setup = MatchEngine.BeginMatch(game, club, opponent, MatchCompetition.League, knockOut: false, new Random(1));

        var team = setup.HomeTeam;
        Assert.NotNull(team.Squad);
        Assert.Equal(Tactics.PreparedEffort(team.TacticsStrength, team.TacticsOpponentStrength), team.Effort);
        Assert.Equal(Tactics.PreparedEffort(setup.AwayTeam.TacticsStrength, setup.AwayTeam.TacticsOpponentStrength), setup.AwayTeam.Effort);
        Assert.True(team.TacticsStrength > 0 && team.TacticsOpponentStrength > 0);
    }

    /// <summary>
    /// A080 (rot vor dem Fix: Teamwert, Teamwert, 50): Die Vereinsfelder 0x18–0x1D eines Managervereins in der Spielberechnung
    /// sind die aus der Aufstellung geschriebenen (Datei 0x097FD–0x0983D liest 0x18/0x1A/0x1C + Teil), mit Form.
    /// </summary>
    [Fact]
    public void A080_ManagerClub_LiveTeamCarriesClubFieldsFromLineup()
    {
        var game = TestGames.Create(5);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs.First(c => c.Index != club.Index && c.League == club.League);
        game.UpdateManagerTeamValues();
        var (defense, attack, _) = MatchEngine.ClubLineupTeamValues(club);

        var team = MatchEngine.LiveTeamOf(game, club, opponent, managed: true, managerMatch: true);

        Assert.Equal(defense, team.Defense);
        Assert.Equal(attack, team.Attack);
        Assert.Equal(MatchEngine.ClubFieldValues(club), (team.Defense, team.Attack));
    }

    /// <summary>
    /// A080 (rot vor dem Fix): Die Spielvorbereitung vergleicht beim Managerverein die Stärke 0x368B1 aus den Vereinsfeldern, nicht
    /// aus den Spielern auf dem Eis. Der Managerverein stellt dafür in Reihe 0 um (Reihe 0 schwach, Felder = Mittel dreier Reihen):
    /// beide Lesarten liegen auseinander, der Gegner hält die Stärke aus den Feldern fest.
    /// </summary>
    [Fact]
    public void A080_Preparation_OpponentComparesFieldStrengthOfManagerClub()
    {
        var game = TestGames.Create(5);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs.First(c => c.Index != club.Index && c.League == club.League);
        // weaken the players on the ice (line 0) only; the club fields are the mean of lines 0–2
        foreach (var p in club.Lineup.Block(0))
        {
            p.Technique = Math.Max(1, p.Technique - 40);
            p.Condition = Math.Max(1, p.Condition - 40);
        }
        game.UpdateManagerTeamValues();
        var players = MatchEngine.LiveTeamOf(game, club, opponent, managed: true, managerMatch: true);
        var fields = MatchEngine.LiveTeamOf(game, club, opponent, managed: true, managerMatch: false);
        var other = MatchEngine.LiveTeamOf(game, opponent, club, managed: false, managerMatch: true);
        var fromPlayers = MatchEngine.PreparedStrength(club, players, other, club.Tactics.MatchEffort);
        var fromFields = MatchEngine.PreparedStrength(club, fields, other, club.Tactics.MatchEffort);
        Assert.NotNull(players.Squad);
        Assert.Null(fields.Squad);
        Assert.True(fromFields - fromPlayers > 5, $"Felder {fromFields}, Spieler {fromPlayers}");

        var match = MatchEngine.BeginMatch(game, opponent, club, MatchCompetition.League, knockOut: false, new Random(1));

        Assert.Equal(fromFields, match.HomeTeam.TacticsOpponentStrength);
        Assert.Equal(fromFields, match.AwayTeam.TacticsStrength);
        Assert.NotNull(match.AwayTeam.Squad); // the minutes still use the players (0x98F8)
    }

    /// <summary>
    /// A544 (rot vor dem Fix: Datenbankstärke + 2 · Veränderung der eigenen Bewertung): Der Teamwert eines Managervereins ist das
    /// Mittel seiner Vereinsfelder Kondition/Technik – geschrieben von 0x952C aus der Aufstellung an Tagen mit Ereignis (Datei
    /// 0x54DAA/0x54FBF) und beim Verlassen der Aufstellung (A858), nicht bei jeder Umstellung im Kern.
    /// </summary>
    [Fact]
    public void A544_TeamValue_ManagerClub_IsMeanOfLineupFields()
    {
        var game = TestGames.Create(5);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        game.UpdateManagerTeamValues();
        var (defense, attack, _) = MatchEngine.ClubLineupTeamValues(club);
        var expected = (int)Math.Round((defense.Condition + defense.Technique + attack.Condition + attack.Technique) / 4.0);
        Assert.NotEqual(club.Strength, expected); // Startkader folgt der Start-Stärke, nicht der Datenbank (spielberechnung.md §14)
        Assert.Equal(expected, MatchEngine.TeamValue(game, club));

        foreach (var player in club.Lineup.Players.OrderByDescending(p => p.Technique + p.Condition).Take(8).ToList())
            club.Lineup.Remove(player);
        // Felder bis zum nächsten Schreiben unverändert: ohne Bildschirm kein Verlassen der Aufstellung (Datei 0x41513–0x4151A,
        // GameState.OnLineupLeft, A858; ZusehenRestAufstellungTests). Vor jedem Spiel schreibt der Tageslauf neu (Datei 0x54FBF).
        Assert.Equal(expected, MatchEngine.TeamValue(game, club));
        game.UpdateManagerTeamValues();
        var (d, a, _) = MatchEngine.ClubLineupTeamValues(club);
        var weaker = (int)Math.Round((d.Condition + d.Technique + a.Condition + a.Technique) / 4.0);
        Assert.True(weaker < expected);
        Assert.Equal(weaker, MatchEngine.TeamValue(game, club));
    }

    /// <summary>
    /// A174 (rot vor dem Fix: Torhüter 25 %, Blöcke 35/30/25/10 %): <see cref="TeamStrength.Rating"/> ist der Teamwert nach 0x952C
    /// (Reihen 0–2, je Teil Summe / 3) – der vierte Block und die Energie gehen nicht ein, eine leere Aufstellung zählt 0.
    /// </summary>
    [Fact]
    public void A174_Rating_IsLineupTeamValueOf0x952C()
    {
        var game = TestGames.Create(5);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var (defense, attack, _) = MatchEngine.LineupTeamValues(club.Lineup);
        var expected = (defense.Condition + defense.Technique + attack.Condition + attack.Technique) / 4.0;
        Assert.Equal(expected, TeamStrength.Rating(club.Lineup));

        foreach (var p in club.Players)
            p.Energy = 10;
        foreach (var p in club.Lineup.Block(3).ToList())
            club.Lineup.Remove(p);
        Assert.Equal(expected, TeamStrength.Rating(club.Lineup));
        Assert.Equal(0, TeamStrength.Rating(new Lineup()));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5, vier Saisonwechsel, passiver und aktiver Manager): Nach jedem Schritt ist der Teamwert des
    /// Managervereins das Mittel seiner Vereinsfelder (0 … 100) und die Spielberechnung trägt genau diese Felder.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_ManagerTeamValueFollowsClubFields(bool active)
    {
        // Runde 21, Spur zufallsstrom Teil 3 Spielstart (R21-D2 = A, nach Rebase auf zusehen): Saat 8 statt 5 – der Spielstart zieht in der Reihenfolge des Originals aus dem gemeinsamen Strom (Kaderplätze der Manager im Poolaufbau Datei 0x5B0BB–0x5B2BE, Platzwahl Datei 0x29C91 nach dem Pool, Startwerte Datei 0x29DA4, Markt Datei 0x43A83 nach der Wanderung), anderer Zufallsverlauf; bei Saat 3, 5, 7 rückt beim passiven Manager keine Jugend nach (FinanceRandom, Datei 0x27293), sein Kader ist nach zwei Saisons leer und die Probe zählt zu wenige Schritte; 8 erfüllt die Vorbedingung. Die Probe hängt an der Saat.
        // Runde 21, Spur zufallsstrom Teil 4 (R21-D2 = A, R15-E2 = C): Saat 10 statt 8 – Werbung, Börse, Zuschauer, Talentsuche, Jugend und Baufirmen ziehen aus dem gemeinsamen Strom statt aus FinanceRandom (Werbeangebote des Spielstarts Datei 0x2A03B–0x2A061, Börse Datei 0x43AD5), anderer Zufallsverlauf ab dem Spielstart; unter 5–12 erreicht nur Saat 10 passiv und aktiv mehr als 200 Schritte mit Kader. Die Probe hängt an der Saat.
        // Runde 22, Spur kaderplatz-werte Teil 2 (A093; R21-D2 = A, R15-E2 = C): Saat 16 statt 10 – die Spielabrechnung 0x544C0 würfelt vor jedem Spiel eines Managervereins je Kaderplatz random(0, 4) (Datei 0x548A8–0x548E7) aus dem gemeinsamen Strom, anderer Zufallsverlauf ab dem ersten Managerspiel; bei Saat 10 zählt der passive Lauf nur 141 Schritte mit Kader, unter 5–24
        // erreichen nur 16, 17, 20 und 22 passiv und aktiv mehr als 200. Rückschaltprobe: src/ von faa64d2 mit
        // Saat 16 passiv rot (alter Zufallsverlauf, dort hielt Saat 10) – die Probe hängt an der Saat.
        // Runde 22, Spur wettbewerbe-regeln Teil 1 (R15-E2 = C): Saat 20 statt 16 – mit #925/A838 anderer Zufallsverlauf, Saat 16 passiv
        // 186 Schritte; von 16–30 erreichen nur 20, 22, 24, 27, 29 passiv und aktiv mehr als 200. Die Probe hängt an der Saat.
        // Rückschaltprobe: alter Code mit Saat 20 ebenfalls grün.
        var game = TestGames.Create(20);
        var changes = 0;
        var steps = 0;
        var values = new HashSet<int>();
        var first = game.Season;
        RngTwinProbeTests.Run(game, active, afterStep: g =>
        {
            foreach (var manager in g.ActiveManagers)
            {
                var club = g.Clubs[manager.ClubIndex];
                if (club.Players.Count == 0)
                    continue;
                var (defense, attack) = MatchEngine.ClubFieldValues(club);
                var value = MatchEngine.TeamValue(g, club);
                Assert.InRange(value, 0, 100);
                Assert.Equal(Math.Clamp((int)Math.Round((defense.Condition + defense.Technique + attack.Condition + attack.Technique) / 4.0), 0, 100), value);
                var opponent = g.Clubs.First(c => c.Index != club.Index && c.Index < NationalTeams.First);
                var team = MatchEngine.LiveTeamOf(g, club, opponent, managed: true, managerMatch: true);
                Assert.Equal((defense, attack), (team.Defense, team.Attack));
                values.Add(value);
                steps++;
            }
        }, beforeChange: _ => changes++);
        Assert.Equal(4, changes);
        Assert.Equal(first + 4, game.Season);
        Assert.True(steps > 200, $"Schritte {steps}");
        Assert.True(values.Count > 3, "Teamwert bewegt sich über die Saisons");
    }

    private static GameState Import(string file, int seed) =>
        HokCompare.Import(OriginalImportTests.OriginalSave(file + ".HOK"), ClubDatabase.Load(TestGameData.Load()), seed);
}
