using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Teil 3: Stärke der Nationalteams (Inventur P2 Nr. 2; A011, A014, A015, A019, A022, A024, A025,
/// A030, A031, A079, A084). [ORIGINAL] Nationalteams sind Vereinsplätze 52–63: 0x368B1/0x977F lesen ihre Vereinsfelder
/// 0x17–0x1D, die der Lader aus DGROUP 0x4C4E − 2 ohne Streuung setzt (Datei 0x2F0CC–0x2F120); das Land des
/// Bundestrainers (Vereinsfeld 0x16 = 6) spielt wie ein Managerverein aus dessen Kader (0x98F8 mit Manager 5, Technik +
/// Länderspiele / 8). Befund vorher (Kopf dcedcdc): Länder spielten mit einem Mittelwert für Kondition und Technik und
/// Form 50 (<c>RatedNation</c>), das Land des Bundestrainers mit dem Mittel der 16 Plätze (K + T) / 2 plus einem
/// Taktikzuschlag ohne Kader, Reihe 4 zählte nicht, „Stärke:“ nahm mit Bundestrainer die Länderwerte.
/// </summary>
public class WettbewerbeSpiel3Tests
{
    private const int Nation = NationalTeams.First + 1;

    /// <summary>
    /// Regressionstest (vorher rot): Ein Land ohne Manager spielt mit Kondition und Technik je Teil aus DGROUP 0x4C4E − 2
    /// und Form/Tagesform seines Vereinsplatzes.
    /// </summary>
    [Fact]
    public void Nation_PlaysWithOwnTeamValues()
    {
        var game = TestGames.Create(3);
        game.Clubs[Nation].TeamValues = new ClubTeamValues(0, 0, 0, 0, 48, 52, 51);
        var (cd, td, ca, ta) = NationalTeamRatings.Of(Nation);

        var team = MatchEngine.LiveTeamOf(game, Shell(game, Nation), Shell(game, NationalTeams.Germany), managed: false,
            managerMatch: false);

        Assert.Equal(new PartValues(cd, td, 48), team.Defense);
        Assert.Equal(new PartValues(ca, ta, 52), team.Attack);
        Assert.Equal(51, team.ClubForm);
        Assert.Null(team.Squad);
    }

    /// <summary>
    /// Regressionstest (vorher rot, A015/A030): Stärke für die Automatik = 0x368B1(Land, 1, 0, 0, 1) aus den Reihenwerten
    /// 0x977F (Reihe 0, Energie der Spielvorbereitung, Einsatz der Automatik beim Stand 0:0).
    /// </summary>
    [Fact]
    public void NationStrength_Is0x368B1OfTeamValues()
    {
        var game = TestGames.Create(3);
        game.Clubs[Nation].TeamValues = new ClubTeamValues(0, 0, 0, 0, 48, 52, 51);
        var (cd, td, ca, ta) = NationalTeamRatings.Of(Nation);
        var effort = new Tactics().EffectiveEffort(0, 0);
        Assert.Equal(80, effort); // Automatik, ausgeglichen: 60 + 20

        // 0x977F, Reihe 0: Wert + 4; Technik · (90 / 4 + 75) %; Kondition − (100 − Einsatz · Kondition / 100) / 4.
        PartValues Line(int c, int t, int f)
        {
            c += 4;
            t = (t + 4) * 97 / 100;
            f += 4;
            c -= (100 - effort * c / 100) / 4;
            return new PartValues(Math.Min(100, c), Math.Min(100, t), Math.Min(100, f));
        }
        var d = Line(cd, td, 48);
        var a = Line(ca, ta, 52);
        // 0x368B1, Gewichtung 1 (50/50), Minute 0, Tagesform 51.
        var expected = (d.Condition + d.Technique + d.Form) / 2 + (a.Condition + a.Technique + a.Form) / 2 + (51 - 44) / 2;

        Assert.Equal(expected, game.NationalMatchStrength(Nation));
    }

    /// <summary>
    /// Regressionstest (vorher rot, A011/A014/A025/A079): feste Aufstellung des Bundestrainers. 0x98F8 mit Manager 5:
    /// Abwehr = (Torhüter · 140 % + 2 Verteidiger · 80 %) / 3, Sturm = 3 Stürmer / 3; Technik · (Energie / 2 + 50) %
    /// (Torhüter 100 %) + Länderspiele / 8; Kondition − (100 − Einsatz · Kondition / 100) / 4; die Taktik wirkt nur über
    /// den Einsatz (kein Zuschlag auf die Stärke).
    /// </summary>
    [Theory]
    [InlineData(80, 193)]
    [InlineData(100, 197)]
    [InlineData(0, 179)]
    public void CoachFormation_StrengthFromPlayers(int effort, int expected)
    {
        var (game, coach) = FixedFormation();
        coach.Tactics.EffortMode = TacticMode.Semi;
        for (var b = 0; b < Tactics.Blocks; b++)
        {
            foreach (var s in Enum.GetValues<ScoreSituation>())
                coach.Tactics.SetEffort(b, s, 0);
            coach.Tactics.SetEffort(b, ScoreSituation.Base, effort);
        }
        // Abwehr: Technik (90 · 1,4 + 2 · (57 + 2) · 0,8) / 3 = (126 + 47 + 47) / 3 = 73, Kondition 220 / 3 = 73, Form 43.
        // Sturm: Technik 3 · (80 + 1) / 3 = 81, Kondition 75, Form 55. Bei Einsatz 80: Kondition 63 bzw. 65.
        // 0x368B1: (73 + 63 + 43) / 2 + (81 + 65 + 55) / 2 + (52 − 44) / 2 = 89 + 100 + 4 = 193.
        Assert.Equal(expected, game.NationalMatchStrength(NationalTeams.Germany));
    }

    /// <summary>Regressionstest (vorher rot, A079/A022): Das Land des Bundestrainers spielt mit Kader, Taktik und vierter Reihe.</summary>
    [Fact]
    public void CoachNation_PlaysWithSquad_IncludingFourthLine()
    {
        var (game, coach) = FixedFormation();
        var spare = coach.Rows.Where(p => !coach.Formation.Contains(p) && p.Available).ToList();
        var d = spare.Where(p => p.Role == PlayerRole.Defender).Take(2).ToList();
        for (var i = 0; i < d.Count; i++)
            Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Defender(3, i), d[i]));

        var team = MatchEngine.LiveTeamOf(game, Shell(game, NationalTeams.Germany), Shell(game, Nation), managed: true,
            managerMatch: true);

        var squad = Assert.IsType<LiveSquad>(team.Squad);
        Assert.True(squad.NationalCoach);
        Assert.True(squad.HasFourthLine(0));
        Assert.Same(coach.Tactics, team.Tactics);
        Assert.Equal(coach.Squad.Count, squad.Players.Count);
        Assert.All(squad.Players.Where(p => p.Dressed), p => Assert.True(coach.IsDressed(p.Player)));
    }

    /// <summary>
    /// Regressionstest (vorher rot, A019): „Stärke:“ mit Bundestrainer aus den Werten, die 0x952C (Manager 5) schreibt –
    /// seit Runde 13 (A009) beim Verlassen der Aufstellung (<see cref="FixedFormation"/>).
    /// </summary>
    [Fact]
    public void InfoStrength_WithCoach_FromFormation()
    {
        var (game, coach) = FixedFormation();
        var (defense, attack, _) = MatchEngine.LineupTeamValues(coach.Formation);
        var expected = (defense.Condition + attack.Condition + defense.Technique + attack.Technique + defense.Form
                        + attack.Form) / 6;

        Assert.Equal(expected, game.NationalInfoStrength(NationalTeams.Germany));
        Assert.Equal(31, expected); // nur Block 1 besetzt, Torhüter zählt in jeder Abwehrreihe (0x98F8)
    }

    private static Club Shell(GameState game, int nation) => new() { Index = nation, Name = game.Clubs[nation].Name, Strength = 60 };

    /// <summary>
    /// Bundestrainer mit fester Aufstellung im Block 1: Torhüter K 80/T 90/F 50, zwei Verteidiger K 70/T 60/F 40 mit 16
    /// Länderspielen, drei Stürmer K 75/T 85/F 55 mit 8 Länderspielen; Energie 100 (Spielvorbereitung 90), Tagesform 52.
    /// </summary>
    private static (GameState Game, NationalCoach Coach) FixedFormation()
    {
        var game = TestGames.Create(3);
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.ClearFormation();
        game.Clubs[NationalTeams.Germany].TeamValues = new ClubTeamValues(0, 0, 0, 0, 50, 50, 52);
        void Set(Player p, int c, int t, int f, int caps)
        {
            p.Condition = c;
            p.Technique = t;
            p.Form = f;
            p.Caps = caps;
            p.Energy = 100;
        }
        var available = coach.Rows.Where(p => p.Available).ToList();
        var keeper = available.First(p => p.Role == PlayerRole.Goalkeeper);
        Set(keeper, 80, 90, 50, 0);
        Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Goalkeeper, keeper));
        var defenders = available.Where(p => p.Role == PlayerRole.Defender).Take(2).ToList();
        for (var i = 0; i < 2; i++)
        {
            Set(defenders[i], 70, 60, 40, 16);
            Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Defender(0, i), defenders[i]));
        }
        var forwards = available.Where(p => p.Role == PlayerRole.Forward).Take(3).ToList();
        for (var i = 0; i < 3; i++)
        {
            Set(forwards[i], 75, 85, 55, 8);
            Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Forward(0, i), forwards[i]));
        }
        // Spur nationalteam Runde 13 (A009): 0x952C schreibt die Länderwerte beim Verlassen der Aufstellung, nicht bei jedem Lesen.
        game.OnNationalLineupLeft();
        return (game, coach);
    }
}
