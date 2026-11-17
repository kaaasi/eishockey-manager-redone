using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zufallsstrom, Teil 2 (N3 = B): Talent (A388), Stadionausbau (A510), versteckte Werte (A675, A676), neuer Manager (A464,
/// Kader und Tendenz) ziehen aus dem gemeinsamen Strom <see cref="GameState.Random"/>; die Wanderung der Teamwerte (A107) geht
/// in der Satzfolge des Originals. Jede Probe rechnet die Folge der Würfe mit einer Kopie des Stroms nach (Fundstellen in
/// em.exe). Vor der Spur rot: gezogen wurde aus <c>FinanceRandom</c>, <c>TrendRandom</c> bzw. einer aus dem Strom abgeleiteten
/// Quelle, die Wanderung ging nach Vereinsindex.
/// </summary>
public class ZufallsstromTeil2Tests
{
    private static GameState Create(int seed = 3, params int[] clubs)
    {
        var setup = new GameSetup();
        foreach (var (club, i) in (clubs.Length == 0 ? [2] : clubs).Select((c, i) => (c, i)))
            setup.Managers.Add(new ManagerSetup { Name = $"Trainer {i}", Head = i, ClubIndex = club });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static GameRandom Copy(GameRandom random) => GameRandom.FromState(random.Capture());

    private static int R(Random random, int a, int b) => TransferRules.Random16(random, a, b);

    private static (int Score, int Control) Trend(Random random)
    {
        var score = R(random, 35, 65);
        var control = R(random, 1, 13);
        control |= R(random, 1, 3) << 4;
        control |= R(random, 0, 1) << 7;
        return (score, control);
    }

    /// <summary>
    /// A388, A675: 0x06D7F zieht alles aus einem Generator – Poolindex random(1, 179), bis der Platz frei ist (Datei 0x06DAE), Alter
    /// random(18, 21), Form random(45, 55), Technik und Kondition random(s − 5, s + 1) (0x06DE9–0x06E62), in 0x42CB1 (Aufruf
    /// 0x06EB9) Energie random(80, 120) und Tendenz 0x4907A, +0x19 = random(3, 12) · 16 + random(3, 12) (0x06F02, 0x06F16),
    /// Laufzeit random(2, 3) (0x06F80).
    /// </summary>
    [Fact]
    public void ScoutedTalent_AllDrawsFromSharedStreamInOriginalOrder()
    {
        var game = Create(3);
        var manager = game.Managers[0];
        var sim = Copy(game.Random);
        const int s = 60;
        var last = game.TransferMarket.PoolLayout.LeagueLast;
        PoolSlot? slot = null;
        while (slot is null)
        {
            var index = R(sim, 1, last);
            slot = game.TransferMarket.Pool.FirstOrDefault(x => x.Index == index && x.Owner == PoolOwner.Free);
        }
        var age = R(sim, 18, 21);
        var form = R(sim, 45, 55);
        var technique = R(sim, s - 5, s + 1);
        var condition = R(sim, s - 5, s + 1);
        _ = R(sim, 80, 120);
        var (score, control) = Trend(sim);
        var over = R(sim, 3, 12);
        var under = R(sim, 3, 12);
        var years = R(sim, 2, 3);

        var p = game.JoinScoutedTalent(manager, s);

        Assert.NotNull(p);
        Assert.Same(slot.Player, p);
        Assert.Equal((age, form, technique, condition), (p.Age, p.Form, p.Technique, p.Condition));
        Assert.Equal((score, control), (p.TrendScore, p.TrendControl));
        Assert.Equal((over, under), (p.HiddenSkill2, p.HiddenSkill1));
        Assert.Equal(years, p.ContractYears);
        Assert.Equal(sim.Capture(), game.Random.Capture());
    }

    /// <summary>
    /// A510: Angebot der Baufirma Datei 0x3E1FE ff. – Bauzeit random(lo, hi), Rabatt random(0, 10), random(0, 2) = 0 streicht ihn;
    /// Antwort „Ja“ Datei 0x3E389 ff. – Restwochen w + random(0, 4) − 2. Alles aus dem gemeinsamen Strom.
    /// </summary>
    [Fact]
    public void StadiumConstruction_QuoteAndAnswerDrawFromSharedStream()
    {
        var game = Create(3);
        var manager = game.Managers[0];
        manager.Money = 50_000_000;
        var sim = Copy(game.Random);
        var (weeks, discount) = Stadium.DrawOffer(sim, StadiumProject.Feature, StadiumFeature.Kino);

        var quote = game.QuoteConstruction(manager, StadiumProject.Feature, StadiumFeature.Kino, 1);

        Assert.NotNull(quote);
        Assert.Equal((weeks, discount), (quote.Weeks, quote.Discount));
        Assert.Equal(sim.Capture(), game.Random.Capture());

        _ = Stadium.DrawWeeksLeft(sim, weeks);
        Assert.True(game.AnswerConstruction(manager, quote, accept: true));
        Assert.Equal(sim.Capture(), game.Random.Capture());
    }

    /// <summary>
    /// A107: 0x9D1B zählt die Vereinssätze i = 0 … [0x6AD] + 11 (Datei 0x09D23–0x09D33); die Sätze stehen in der Satzfolge der Ligen
    /// (0x34FD5: 0–11 Bundesliga, 12–23 2. Liga, 24–35 Oberliga), nicht nach dem Vereinsindex des Remakes. Die Probe rechnet die
    /// kleine Wanderung (Modus 1) in dieser Folge nach: r = lrandom(0, 2), s = lrandom(0, 2r + 2) − 2, Kondition je Teil
    /// + lrandom(s − 1, s + 1), Technik je Teil erst lrandom(0, 2) und nur bei 0 der Schritt, begrenzt auf 1 … 99, dann dreimal
    /// Form lrandom(45, 55); Nationalteams nur die Form.
    /// </summary>
    [Fact]
    public void TeamValueDrift_FollowsOriginalRecordOrder()
    {
        var game = Create(3);
        var order = MatchEngine.DriftOrder(game);
        Assert.Equal(game.ClubRecordOrder().Where(i => i < NationalTeams.First), order.Take(NewGame.SelectableClubs).Select(c => c.Index));
        Assert.NotEqual(order.Select(c => c.Index).Order(), order.Select(c => c.Index)); // Saat 3: Startmischung ≠ Indexfolge
        Assert.Equal(game.Clubs.Where(c => c.Index <= NationalTeams.Last).Select(c => c.Index).Order(), order.Select(c => c.Index).Order());

        var sim = Copy(game.Random);
        var expected = new Dictionary<int, ClubTeamValues>();
        foreach (var club in order)
        {
            var v = club.TeamValues!.Value;
            if (club.Index < NationalTeams.First)
            {
                var r = OriginalMatch.Rnd(sim, 0, 2);
                var step = OriginalMatch.Rnd(sim, 0, 2 * r + 2) - 2;
                int Move(int deviation) =>
                    Math.Clamp(club.Strength + deviation + OriginalMatch.Rnd(sim, step - 1, step + 1), 1, 99) - club.Strength;
                int Keep(int deviation) => Math.Clamp(club.Strength + deviation, 1, 99) - club.Strength;
                var dc = Move(v.DefenseCondition);
                var ac = Move(v.AttackCondition);
                var dt = OriginalMatch.Rnd(sim, 0, 2) == 0 ? Move(v.DefenseTechnique) : Keep(v.DefenseTechnique);
                var at = OriginalMatch.Rnd(sim, 0, 2) == 0 ? Move(v.AttackTechnique) : Keep(v.AttackTechnique);
                v = v with { DefenseCondition = dc, AttackCondition = ac, DefenseTechnique = dt, AttackTechnique = at };
            }
            var df = OriginalMatch.Rnd(sim, 45, 55);
            var af = OriginalMatch.Rnd(sim, 45, 55);
            _ = OriginalMatch.Rnd(sim, 45, 55);
            expected[club.Index] = v with { DefenseForm = df, AttackForm = af };
        }

        MatchEngine.DriftTeamValues(game, TeamValueDrift.Weekly);

        Assert.Equal(sim.Capture(), game.Random.Capture());
        Assert.All(order, club => Assert.Equal(expected[club.Index], club.TeamValues));
    }

    /// <summary>
    /// A464 (Kader und Tendenz): Spielvorbereitung im Hinzufügen-Modus, Datei 0x2993A–0x29A5E – je Kaderplatz Poolindex
    /// random(1, 20) für den ersten Torhüter, bis der Platz frei ist, dann 0x42CB1 mit Energie random(80, 120) und Tendenz
    /// (0x42E4C, 0x42E6B → 0x4907A), +0x19 (0x299BF, 0x299D3), Alter random(19, 31) (0x29A1B) – aus dem gemeinsamen Strom.
    /// Die Probe rechnet den ersten Platz nach und prüft, dass der Strom weiterzählt.
    /// </summary>
    [Fact]
    public void AddManager_SquadDrawsFromSharedStream()
    {
        var game = Create(3);
        var before = game.Random.Capture();
        var sim = Copy(game.Random);
        PoolSlot? slot = null;
        while (slot is null)
        {
            var index = R(sim, 1, 20);
            var candidate = game.TransferMarket.Pool[index - 1];
            if (candidate.Owner == PoolOwner.Free && candidate.Player.Role == PlayerRole.Goalkeeper)
                slot = candidate;
        }
        _ = R(sim, 80, 120);
        var (score, control) = Trend(sim);
        var over = R(sim, 3, 12);
        var under = R(sim, 3, 12);
        var age = R(sim, SquadSetup.MinAge, SquadSetup.MaxAge);

        var manager = game.AddManager(new ManagerSetup { Name = "Neu", Head = 1, ClubIndex = 14 });

        var first = slot.Player;
        Assert.Contains(first, game.Clubs[manager.ClubIndex].Players);
        Assert.Equal((score, control), (first.TrendScore, first.TrendControl));
        Assert.Equal((over, under, age), (first.HiddenSkill2, first.HiddenSkill1, first.Age));
        Assert.NotEqual(before, game.Random.Capture());
    }

    /// <summary>
    /// A675: Neue Spieler ohne Kaderplatz eines Managers (Neuanlage im Pool Datei 0x2787A–0x279DA, Nachwuchs der Computervereine)
    /// würfeln +0x19 nicht – im Original gibt es den Wert nur am Kaderplatz – und verbrauchen dafür keinen Zufall.
    /// </summary>
    [Fact]
    public void PlayersWithoutManagerSlot_DoNotRollHiddenSkills()
    {
        var game = Create(3);
        var sim = Copy(game.Random);
        _ = R(sim, 18, 25);
        var s = R(sim, 40, 92);
        _ = R(sim, s - 5, s + 5);
        _ = R(sim, s - 5, s + 5); // A631 (kader-regeln): Form und Talent bleiben die des Pooleintrags (Datei 0x27947–0x279D6)

        var p = PlayerFactory.CreateRenewal(game, new Player { Name = "NEU", Role = PlayerRole.Forward }, "NEU", game.Random);

        Assert.Equal((HiddenSkills.NationalTeam, HiddenSkills.NationalTeam), (p.HiddenSkill2, p.HiddenSkill1));
        Assert.Equal(sim.Capture(), game.Random.Capture());
    }
}
