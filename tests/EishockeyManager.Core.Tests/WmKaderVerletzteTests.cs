using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur v-rest, Teil 3 (A873, #948): „auto“ und das Umschalten in „Spieler in WM-Kader“ prüfen keine Verletzung. [ORIGINAL] 0x3CC01
/// wählt nach der Summe der Grundstärken (Pool +0x15–0x17) und prüft nur Pool +0x13 (Ausländer) und Pool +0x1D (Datei
/// 0x3CC28–0x3CCAE); der Klick Datei 0x3C952–0x3C983 ebenso, dann nimmt 0x3CABF auf. Die Verletzung steht am Vereinsplatz; der neue
/// Kaderplatz des Satzes 5 (0x42CB1) beginnt unverletzt, die Verletzung des Vereinsplatzes bleibt dort.
/// Vorher rot: Verletzte übersprang „auto“, der Klick lieferte „nicht verfügbar“.
/// </summary>
public class WmKaderVerletzteTests
{
    private static (GameState Game, NationalCoach Coach) WorldOnlyCoach(int seed = 7)
    {
        var game = GameModeTests.Create(seed, false, true, 2);
        game.Continue();
        return (game, game.NationalCoach!);
    }

    private static int Sum(Player p) => GameState.NationalCandidateSum(p);

    /// <summary>„auto“ nimmt den stärksten Stürmer auch verletzt; sein Platz im Satz 5 ist unverletzt, der Vereinsplatz bleibt verletzt.</summary>
    [Fact]
    public void AutoNominate_TakesInjured_SlotUninjured()
    {
        var (game, coach) = WorldOnlyCoach();
        var best = game.NationalCandidates().Where(c => c.Player.Role == PlayerRole.Forward)
            .OrderByDescending(c => Sum(c.Player)).First().Player;
        best.InjuredGames = 5;
        best.InjuryKind = 3;

        game.AutoNominate();
        var member = coach.Member(best);
        Assert.NotNull(member);
        Assert.NotSame(best, member.Player);
        Assert.Equal(0, member.Player.InjuredGames);
        Assert.Equal(InjuryKinds.Unknown, member.Player.InjuryKind);
        Assert.Equal((5, 3), (best.InjuredGames, best.InjuryKind));
        Assert.Equal(NationalCoach.MaxSquad, coach.Squad.Count);
    }

    /// <summary>Der Klick nimmt einen Verletzten auf (0x3CABF) und nimmt ihn beim zweiten Klick heraus.</summary>
    [Fact]
    public void ToggleNomination_AddsInjured()
    {
        var (game, coach) = WorldOnlyCoach();
        coach.ClearSquad();
        var p = game.NationalCandidates()[0].Player;
        p.InjuredGames = 2;
        Assert.Equal(NominationResult.Added, game.ToggleNomination(p));
        Assert.Equal(0, coach.SlotOf(p).InjuredGames);
        Assert.Equal(2, p.InjuredGames);
        Assert.Equal(NominationResult.Removed, game.ToggleNomination(p));
        Assert.Equal(2, p.InjuredGames);
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5, 4 Saisonwechsel, passiv und aktiv): aktiv wird vor „auto“ der stärkste Kandidat jeder Art
    /// verletzt; er steht trotzdem im Kader, unverletzt im Satz 5, und seine Verletzung am Vereinsplatz heilt unabhängig davon.
    /// Speichern und Laden ändert nichts.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_InjuredNominated(bool active)
    {
        var game = GameModeTests.Create(9, false, true, 2);
        var checkedSeasons = 0;
        for (var season = 0; season < 5; season++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = Math.Max(m.Money, 10_000_000);
                if (game.NationalCoach is { Squad.Count: 0 } coach)
                {
                    var injured = new List<Player>();
                    if (active)
                        foreach (var role in Enum.GetValues<PlayerRole>())
                        {
                            var best = game.NationalCandidates().Where(c => c.Player.Role == role)
                                .OrderByDescending(c => Sum(c.Player)).First().Player;
                            best.InjuredGames = 3;
                            injured.Add(best);
                        }
                    game.AutoNominate();
                    Assert.Equal(NationalCoach.MaxSquad, coach.Squad.Count);
                    Assert.All(coach.Squad, m => Assert.Equal(0, m.Player.InjuredGames));
                    Assert.All(injured, p => Assert.Equal(3, coach.SourceOf(coach.SlotOf(p)).InjuredGames));
                    Assert.All(injured, p => Assert.True(coach.Contains(p)));
                    var saved = SaveGame.Save(game);
                    game = SaveGame.Load(saved).Game!;
                    Assert.Equal(saved, SaveGame.Save(game));
                    Assert.Empty(game.ValidatePlayerIdentity());
                    checkedSeasons++;
                }
                game.Continue();
            }
            if (season < 4)
            {
                game.StartNextSeason();
                Assert.Null(game.NationalCoach);
            }
        }
        Assert.Equal(5, checkedSeasons);
    }
}
