using System.Text;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur start-lader (Runde 21): Lader der Teamwerte 0x2EF1B beim Spielstart statt beim ersten Spiel.
/// [ORIGINAL] em.exe Datei 0x439BB <c>call 0xE0:0x75</c> in 0x437D5 vor der Spielvorbereitung 0x28165 (Datei 0x43A61); in deren
/// Managerschleife 0x952C(m, 0) nach der Aufstellung (Datei 0x2A073); danach die Wanderung des Spielstarts Modus 10 (Datei 0x43A73)
/// vor der Europapokal-Tabelle 0x341AB (Datei 0x43A8C). Beim Laden (AL ≠ 0, Datei 0x43A6E) keine Wanderung, die Vereinssätze des
/// Spielstands gelten. Jeder Test war vor der Spur rot: Bis zum ersten Spiel waren alle Teamwerte <c>null</c>.
/// </summary>
public class StartLaderTests
{
    /// <summary>Synthetische Datenbank wie <c>EuropeanCupTests</c>: Vereine 0–47 absteigend je Liga, Gegner 95 bzw. 40.</summary>
    private static ClubDatabase DatabaseWithOpponents()
    {
        var data = new byte[ClubDatabase.FileLength];
        for (var i = 0; i < ClubDatabase.ClubsWithSquad; i++)
        {
            Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, ClubDatabase.SquadOffset + i * ClubDatabase.SquadRecordLength);
            data[ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        for (var i = ClubDatabase.ClubsWithSquad; i < ClubDatabase.TotalClubs; i++)
        {
            Encoding.ASCII.GetBytes($"EUROPA {i}").CopyTo(data, ClubDatabase.EuropeanNamesOffset + (i - ClubDatabase.ClubsWithSquad) * ClubDatabase.NameLength);
            data[ClubDatabase.StrengthOffset + i * 3] = (byte)(i % 2 == 0 ? 95 : 40);
        }
        return ClubDatabase.Parse(data);
    }

    private static List<Club> NonManagedClubs(GameState game, Func<Club, bool> filter) =>
        game.Clubs.Where(c => filter(c) && game.Managers.All(m => m.ClubIndex != c.Index)).ToList();

    /// <summary>A106: Nach dem Spielstart haben alle Vereine Teamwerte; die Gegner des Europapokals (wandern nicht, tauschen
    /// nicht) stehen genau auf den ersten Zügen des Laders aus der Hauptquelle.</summary>
    [Fact]
    public void A106_NewGame_LoaderRunsAtStart_FirstDrawsOfMainSource()
    {
        var game = TestGames.Create(5);
        Assert.All(game.Clubs, c => Assert.NotNull(c.TeamValues));

        var database = TestGames.CreateDatabase();
        var copies = game.Clubs.Select(c => new Club { Index = c.Index, Name = c.Name, Strength = database.Clubs[c.Index].Strength }).ToList();
        MatchEngine.LoadTeamValues(copies, new GameRandom(5));
        Assert.All(game.Clubs.Where(c => c.Index > NationalTeams.Last),
            c => Assert.Equal(copies[c.Index].TeamValues, c.TeamValues));
    }

    /// <summary>A108: Die Wanderung des Spielstarts (Modus 10) ist beim Spielstart gelaufen: Kondition und Technik der
    /// Computervereine liegen in den Liga-Grenzen DGROUP 0x125 (der Lader allein streut bis v + v/10, z. B. 110 bei Stärke 100).</summary>
    [Fact]
    public void A108_NewGame_StartDriftApplied_WithinLeagueBounds()
    {
        var game = TestGames.Create(5);
        var clubs = NonManagedClubs(game, c => c.Index < NationalTeams.First && c.League is not null);
        Assert.Contains(clubs, c => c.Strength + c.Strength / 10 > MatchEngine.DriftBounds(c.League).Max); // Vorbedingung
        foreach (var club in clubs)
        {
            var (min, max) = MatchEngine.DriftBounds(club.League);
            var v = club.TeamValues!.Value;
            foreach (var d in new[] { v.DefenseCondition, v.AttackCondition, v.DefenseTechnique, v.AttackTechnique })
                Assert.InRange(club.Strength + d, min, max);
        }
    }

    /// <summary>A418: Die Auslosung des Europapokals beim Spielstart liest die gewürfelten Werte der Gegner (Form
    /// random(45, 55) − 5, Streuung), nicht die neutralen 45 ohne Streuung.</summary>
    [Fact]
    public void A418_EuropeanDraw_AtStart_SeesLoaderValues()
    {
        var game = NewGame.Create(DatabaseWithOpponents(), new GameSetup { EuropeAndWorldCup = true }, 3);
        var cup = Assert.IsType<EuropeanCup>(game.EuropeanCup);
        var opponents = cup.Groups.SelectMany(g => g.Teams).Where(t => t >= EuropeanCup.FirstOpponent).Select(t => game.Clubs[t]).ToList();
        Assert.NotEmpty(opponents);
        Assert.All(opponents, c => Assert.NotNull(c.TeamValues));
        Assert.Contains(opponents, c => c.TeamValues!.Value.DefenseForm != 45 || c.TeamValues!.Value.DefenseCondition != 0);
    }

    /// <summary>A505: Die monatliche Grundeinnahme vor dem ersten Spiel zählt die gewürfelten Teamwerte (0x3E657 liest 0x18–0x1B).</summary>
    [Fact]
    public void A505_TeamValueSum_BeforeFirstMatch_IncludesLoaderValues()
    {
        var game = TestGames.Create(5);
        var clubs = NonManagedClubs(game, c => c.Index < NationalTeams.First);
        Assert.Contains(clubs, c => FinanceRules.TeamValueSum(game, c) != 4 * c.Strength);
    }

    /// <summary>A485: Länder (Satz 52 … 63) haben Teamwerte ab dem Spielstart; die Länderwerte vor dem ersten Spiel ziehen nichts nach.</summary>
    [Fact]
    public void A485_NationalTeams_HaveValuesAtStart()
    {
        var game = TestGames.Create(5);
        Assert.All(game.Clubs.Where(c => c.Index is >= NationalTeams.First and <= NationalTeams.Last),
            c => Assert.NotNull(c.TeamValues));
    }

    /// <summary>A704: Verkauft der Manager vor dem ersten Spiel an einen Computerverein, steigen dessen Teamwerte (0x58DC).</summary>
    [Fact]
    public void A704_SaleBeforeFirstMatch_StrengthensBuyer()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var buyer = NonManagedClubs(game, c => c.Index < NationalTeams.First).MinBy(c => c.Strength)!;
        var player = club.Players.MaxBy(p => p.Strength)!;
        var before = buyer.TeamValues;
        Assert.NotNull(before);

        Assert.Equal(TransferResult.Accepted, game.AcceptUnsolicitedOffer(new UnsolicitedOffer(manager, player, buyer, 10_000)));
        Assert.NotEqual(before, buyer.TeamValues);
    }

    /// <summary>
    /// A519: Ein neuer Manager vor dem ersten Spiel bekommt die Teamwerte aus seiner Aufstellung (0x952C(m, 0), Datei 0x2A073),
    /// nicht die des verdrängten Vereins.
    /// </summary>
    [Fact]
    public void A519_AddManagerBeforeFirstMatch_WritesLineupValues()
    {
        var game = TestGames.Create(5);
        var target = NonManagedClubs(game, c => c.Index is > 0 and < NationalTeams.First && c.League == League.ZweiteLiga).First();

        game.AddManager(new ManagerSetup { Name = "NEU", Head = 1, ClubIndex = target.Index });

        var club = game.Clubs[game.Managers[^1].ClubIndex];
        var (defense, attack, _) = MatchEngine.ClubLineupTeamValues(club);
        var b = MatchEngine.ComputerTeamValue(club);
        var v = club.TeamValues!.Value;
        Assert.Equal((defense.Condition, defense.Technique, defense.Form), (b + v.DefenseCondition, b + v.DefenseTechnique, v.DefenseForm));
        Assert.Equal((attack.Condition, attack.Technique, attack.Form), (b + v.AttackCondition, b + v.AttackTechnique, v.AttackForm));
    }

    /// <summary>
    /// A519/A080: Beim Spielstart schreibt die Spielvorbereitung die Teamwerte der Managervereine aus der Aufstellung (Datei 0x2A073),
    /// erst danach wandert alles (Datei 0x43A73). Gegenprobe über die Wanderung: Kondition und Technik weichen von der Aufstellung
    /// höchstens um einen Schritt der großen Wanderung ab (s − 1 … s + 1 mit s = −6 … 12) oder liegen auf einer Liga-Grenze.
    /// </summary>
    [Fact]
    public void A080_ManagerClubs_StartFromLineupThenDrift()
    {
        var game = TestGames.Create(5);
        foreach (var manager in game.Managers)
        {
            var club = game.Clubs[manager.ClubIndex];
            var (defense, attack, _) = MatchEngine.ClubLineupTeamValues(club);
            var b = MatchEngine.ComputerTeamValue(club);
            var (min, max) = MatchEngine.DriftBounds(club.League);
            var v = club.TeamValues!.Value;
            void Near(int lineup, int field)
            {
                if (field != min && field != max)
                    Assert.InRange(field - Math.Clamp(lineup, min, max), -7, 13);
            }
            Near(defense.Condition, b + v.DefenseCondition);
            Near(attack.Condition, b + v.AttackCondition);
            Near(defense.Technique, b + v.DefenseTechnique);
            Near(attack.Technique, b + v.AttackTechnique);
        }
    }

    /// <summary>
    /// Laden: Ein vor dem ersten Spiel gesicherter Stand behält die Teamwerte; das erste Spiel danach zieht sie nicht neu
    /// (Datei 0x43A6E: kein Lader-Ergebnis und keine Wanderung des Spielstarts nach dem Laden).
    /// </summary>
    [Fact]
    public void SaveBeforeFirstMatch_LoadKeepsTeamValues_FirstMatchDoesNotReload()
    {
        var game = TestGames.Create(5);
        var loaded = SaveGame.Load(SaveGame.Save(game)).Game!;
        Assert.Equal(game.Clubs.Select(c => c.TeamValues), loaded.Clubs.Select(c => c.TeamValues));

        TestSeason.PlayMatchDay(loaded);
        Assert.All(loaded.Clubs.Where(c => c.Index > NationalTeams.Last),
            c => Assert.Equal(game.Clubs[c.Index].TeamValues!.Value with { DayForm = 0 }, c.TeamValues!.Value with { DayForm = 0 }));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5, vier Saisonwechsel, passiver und aktiver Manager): Die Teamwerte vom Spielstart bleiben
    /// das ganze Spiel über gesetzt und werden nie neu geladen – die Gegner des Europapokals (Index &gt; 64, wandern nicht) behalten
    /// sie bis auf die Tagesform; nach jedem Saisonwechsel (Wanderung Modus 10, Datei 0x56448) liegen die Computervereine in den
    /// Grenzen ihrer Liga.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_TeamValuesFromStartKept(bool active)
    {
        var game = TestGames.Create(5);
        var start = game.Clubs.Where(c => c.Index > NationalTeams.Last)
            .ToDictionary(c => c.Index, c => c.TeamValues!.Value with { DayForm = 0 });
        var changes = 0;
        var checkBounds = false;
        var first = game.Season;
        RngTwinProbeTests.Run(game, active, afterStep: g =>
        {
            Assert.All(g.Clubs, c => Assert.NotNull(c.TeamValues));
            Assert.All(g.Clubs.Where(c => c.Index > NationalTeams.Last),
                c => Assert.Equal(start[c.Index], c.TeamValues!.Value with { DayForm = 0 }));
            if (!checkBounds)
                return;
            checkBounds = false; // direkt nach StartNextSeason
            foreach (var club in NonManagedClubs(g, c => c.Index < NationalTeams.First && c.League is not null))
            {
                var (min, max) = MatchEngine.DriftBounds(club.League);
                var v = club.TeamValues!.Value;
                foreach (var d in new[] { v.DefenseCondition, v.AttackCondition, v.DefenseTechnique, v.AttackTechnique })
                    Assert.InRange(club.Strength + d, min, max);
            }
        }, beforeChange: _ =>
        {
            changes++;
            checkBounds = true;
        });
        Assert.Equal(4, changes);
        Assert.Equal(first + 4, game.Season);
    }
}
