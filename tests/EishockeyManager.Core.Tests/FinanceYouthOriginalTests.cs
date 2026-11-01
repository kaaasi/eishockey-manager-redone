using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Jugendteams gegen em.exe (Spur finanzen-rest, Teil 1): Förderschritt und Deckel je Mannschaft (Datei 0x3A65F ff.),
/// zwölf Plätze je Jugendliga und Startplatz (Spielstart 0x29BAA/0x29BC1).
/// </summary>
public class FinanceYouthOriginalTests
{
    [Theory]
    [InlineData(YouthTeamKind.Schueler, 1_700, 1_500)] // Schritt 500
    [InlineData(YouthTeamKind.Schueler, 12_000, 10_000)] // Deckel 10.000
    [InlineData(YouthTeamKind.Jugend, 19_000, 19_000)] // UI-Video 8:39 „Jugend : 19000“
    [InlineData(YouthTeamKind.Jugend, 1_900, 1_000)] // Schritt 1.000
    [InlineData(YouthTeamKind.Junioren, 25_000, 24_000)] // Schritt 2.000
    [InlineData(YouthTeamKind.Junioren, 50_000, 40_000)] // Deckel 40.000
    public void Funding_StepAndCapDoubleWithEachTeam(YouthTeamKind kind, long amount, long expected)
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        game.SetYouthFunding(me, kind, amount);
        Assert.Equal(expected, game.Finance(me).Youth[(int)kind].Funding);
    }

    [Theory]
    [InlineData(YouthTeamKind.Schueler, 70, 10_000)] // UI-Video 8:35
    [InlineData(YouthTeamKind.Schueler, 7, 1_000)] // UI-Video 8:36
    [InlineData(YouthTeamKind.Schueler, 1, 0)]
    [InlineData(YouthTeamKind.Jugend, 67, 19_000)] // UI-Video 8:39
    [InlineData(YouthTeamKind.Jugend, 7, 2_000)] // UI-Video 8:38
    [InlineData(YouthTeamKind.Junioren, 70, 40_000)]
    [InlineData(YouthTeamKind.Junioren, 35, 20_000)]
    public void BarFunding_FloorsToTheTeamStep(YouthTeamKind kind, int offset, long expected) =>
        Assert.Equal(expected, YouthTeam.BarFunding(kind, offset));

    [Fact]
    public void Start_SixthOfTwelveInTheClubLeague()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var league = (int)game.Clubs[me.ClubIndex].League!.Value + 1;
        Assert.Equal(12, YouthTeam.TeamsPerLeague);
        Assert.All(game.Finance(me).Youth, t => Assert.Equal((league, 6), (t.League, t.Rank)));
    }

    [Fact]
    public void Start_OberligaClubPlaysInTheThirdYouthLeague()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", ClubIndex = 42 }); // ohne Liga → Oberliga
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 7);
        Assert.All(game.Finance(game.ActiveManager).Youth, t => Assert.Equal((3, 6), (t.League, t.Rank)));
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv ohne und aktiv mit voller Förderung aller drei Teams = 70.000 DM): jede
    /// Buchung „Jugend“ ist ein Vielfaches der Summe der Förderungen, Plätze bleiben 1–12 in Liga 1–3, die Förderung bleibt stehen,
    /// und zwei gleiche Läufe ergeben dieselben Stände.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_FundingIsBookedAndPlacesStayValid(bool active)
    {
        string Run()
        {
            var game = TestGames.Create(5);
            var me = game.Managers[0];
            var log = new List<string>();
            if (active)
                foreach (var kind in Enum.GetValues<YouthTeamKind>())
                    game.SetYouthFunding(me, kind, YouthTeam.MaxFunding(kind));
            var funding = game.Finance(me).YouthFunding;
            Assert.Equal(active ? 70_000 : 0, funding);
            var bookings = 0;
            for (var change = 0; change < 4; change++)
            {
                for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
                {
                    Assert.True(guard < 1000, "Saisonende nicht erreicht");
                    me.Money = Math.Max(me.Money, 1_000_000); // keine Entlassung
                    if (active)
                        TestSeason.KeepSquad(game, me);
                    var before = game.Finance(me).Book.Current(FinanceCategory.Jugend);
                    game.Continue();
                    var after = game.Finance(me).Book.Current(FinanceCategory.Jugend);
                    if (!active)
                        Assert.Equal(0, after);
                    else
                    {
                        // ein Schritt kann mehrere Monatsenden umfassen; ein neuer Abrechnungszeitraum beginnt bei 0
                        Assert.Equal(0, after % funding);
                        bookings += (int)((after < before ? before - after : after > before ? -after : 0) / funding);
                    }
                }
                game.StartNextSeason();
                Assert.Equal(funding, game.Finance(me).YouthFunding);
                foreach (var team in game.Finance(me).Youth)
                {
                    Assert.InRange(team.League, 1, YouthTeam.Leagues);
                    Assert.InRange(team.Rank, 1, YouthTeam.TeamsPerLeague);
                    log.Add($"{change}:{team.Kind}:{team.League}/{team.Rank}");
                }
                log.Add($"{change}:{me.Money}");
            }
            if (active)
                Assert.True(bookings >= 4 * 7, $"nur {bookings} Monatsbuchungen"); // mindestens 7 Monatsenden je Saison
            return string.Join(";", log);
        }

        Assert.Equal(Run(), Run());
    }
}
