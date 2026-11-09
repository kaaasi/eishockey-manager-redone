using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// R2-02: Bildschirme verändern beim Öffnen keinen Spielzustand; Zeit vergeht nur über „und tschüß…“
/// (<see cref="GameState.Continue"/>). Tagesablauf: <c>docs/tagesablauf.md</c>.
/// </summary>
public class PassiveScreenTests
{
    /// <summary>
    /// Spielt Spieltage, bis der angehaltene Tag (Datum im Hauptmenü, noch nicht verarbeitet) ein Trainingstag ist –
    /// genau die Lage, in der das frühere Nachholen in den Bildschirmen Geld, Spielerwerte und Zufall verändert hat.
    /// (Der Transfermarkt ist im regulären Ablauf beim Anhalten schon fortgeschrieben; sein Zustand wird über den
    /// Abdruck trotzdem mitgeprüft.)
    /// </summary>
    private static ScreenDriver PausedOnTrainingDay(int seed = 1993)
    {
        var d = FlowTests.CreateDriver(seed);
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var i = 0; i < 20; i++)
        {
            d.PlayMatchDay();
            if (GameState.IsTrainingDay(game.Day))
                return d;
        }
        throw new InvalidOperationException("Kein angehaltener Trainingstag gefunden.");
    }

    private static void OpenAndCloseEveryTile(ScreenDriver d)
    {
        foreach (var name in MenuTiles.AllNames)
        {
            d.OpenTile(name);
            d.Frame();
            FlowTests.ReturnToMainMenu(d);
        }
    }

    private static void AssertSame(string expected, string actual, string what) =>
        Assert.True(expected == actual, $"{what} hat sich verändert – {StateFingerprint.FirstDifference(expected, actual)}");

    /// <summary>
    /// Der Tag im Hauptmenü ist noch nicht gespielt, alle Tage davor genau einmal. #688 (R688-04): Sein Tageswechsel (Takt mit
    /// Training, Tagesabschluss) liegt wie im Original vor dem Menü (Datei 0x55204–0x552D3 vor 0x44E22).
    /// </summary>
    private static void AssertPausedDayUnprocessed(GameState game)
    {
        Assert.True(game.DayOpened);
        Assert.Equal((game.Season, game.Day), game.TrainedUntil);
        Assert.Equal(game.Day, game.FinanceDay);
        Assert.True(game.TransferMarket.UpdatedDay <= game.PlayedMatchDays);
    }

    [GameDataFact]
    public void OpeningEveryTile_OnPausedDay_ChangesNoGameState()
    {
        var d = PausedOnTrainingDay();
        var game = d.Host.Game!;
        AssertPausedDayUnprocessed(game);
        var money = game.ActiveManager.Money;
        var market = game.TransferMarket.UpdatedDay;
        var before = StateFingerprint.Of(game);

        OpenAndCloseEveryTile(d);

        Assert.Equal(money, game.ActiveManager.Money);
        Assert.Equal(market, game.TransferMarket.UpdatedDay);
        AssertPausedDayUnprocessed(game);
        AssertSame(before, StateFingerprint.Of(game), "Spielzustand (Geld, Spieler, Markt, Zufall …)");
    }

    /// <summary>
    /// Zwei gleiche Spiele (gleicher Seed): in einem werden vor dem Weiterspielen alle Kacheln geöffnet. Danach
    /// muss der Zustand identisch sein – jeder Tag wurde in beiden genau einmal und in gleicher Reihenfolge verarbeitet.
    /// </summary>
    [GameDataFact]
    public void AfterBrowsing_EachDayIsProcessedExactlyOnce()
    {
        var browsing = PausedOnTrainingDay();
        var control = PausedOnTrainingDay();
        AssertSame(StateFingerprint.Of(control.Host.Game!), StateFingerprint.Of(browsing.Host.Game!), "Ausgangslage");

        OpenAndCloseEveryTile(browsing);
        for (var i = 0; i < 2; i++)
        {
            browsing.PlayMatchDay();
            control.PlayMatchDay();
        }

        AssertPausedDayUnprocessed(browsing.Host.Game!);
        AssertSame(StateFingerprint.Of(control.Host.Game!), StateFingerprint.Of(browsing.Host.Game!), "Spielzustand nach dem Weiterspielen");
    }

    /// <summary>
    /// Strenge Fassung für den Folgebefund: alle Kacheln einschließlich „Werbung“, schon am ersten Tag vor dem ersten
    /// Weiterspielen; Vergleich mit einem Spiel ohne Öffnen über einen Monatswechsel hinweg.
    /// </summary>
    [GameDataFact]
    public void OpeningEveryTile_IncludingAdvertising_FromFirstDay_ChangesNoGameState()
    {
        var browsing = FlowTests.CreateDriver();
        var control = FlowTests.CreateDriver();
        browsing.StartNewGame();
        control.StartNewGame();

        var before = StateFingerprint.Of(browsing.Host.Game!);
        OpenAndCloseEveryTile(browsing);
        AssertSame(before, StateFingerprint.Of(browsing.Host.Game!), "Spielzustand direkt nach Spielstart");
        while (browsing.Host.Game!.Day < 40)
        {
            browsing.PlayMatchDay();
            control.PlayMatchDay();
        }

        AssertSame(StateFingerprint.Of(control.Host.Game!), StateFingerprint.Of(browsing.Host.Game!), "Spielzustand");
    }

    [GameDataTheory]
    [InlineData("Werbung")]
    [InlineData("Finanzen")]
    [InlineData("Training")]
    public void OpeningTile_FromFirstDay_ChangesNoGameState(string tile)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var before = StateFingerprint.Of(d.Host.Game!);

        d.OpenTile(tile);
        d.Frame();
        FlowTests.ReturnToMainMenu(d);

        AssertSame(before, StateFingerprint.Of(d.Host.Game!), tile);
    }

    [GameDataFact]
    public void OpeningTraining_PreservesExplicitlyDrawnSpecialPlan()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.Host.Game!.ActiveManager.Training.Fill(TrainingKind.Special);
        var before = StateFingerprint.Of(d.Host.Game);

        d.OpenTile("Training");
        d.Frame();
        FlowTests.ReturnToMainMenu(d);

        AssertSame(before, StateFingerprint.Of(d.Host.Game), "Gezeichneter Trainingsplan");
    }

    /// <summary>
    /// Reihenfolge „Training → Plan ändern“ (#688, R688-04): Am angehaltenen Trainingstag ist dessen Training (Takt 0x07169 vor
    /// dem Menü) schon gerechnet und gebucht; ein im Menü geänderter Plan gilt ab dem nächsten Takt. Vorher [APPROX] galt er schon
    /// für den angehaltenen Tag (einen Takt früher als im Original, Review #688 R688-04).
    /// </summary>
    [GameDataFact]
    public void PlanChangedOnPausedTrainingDay_AppliesFromNextTick()
    {
        var d = PausedOnTrainingDay();
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        long Training() => game.Finance(manager).Book.Current(FinanceCategory.Training);
        var oldCost = manager.Training.CostPerTrainingDay;
        var paused = game.Day;
        var before = Training();

        // Montag ganz auf „Special“ (teuer) streichen
        d.OpenTile("Training");
        var screen = Assert.IsType<TrainingScreen>(d.Host.Current);
        var special = TrainingScreen.KindIcon(TrainingKind.Special);
        d.Click(special.X + 5, special.Y + 5);
        d.Drag(Enumerable.Range(0, TrainingPlan.UnitsPerDay)
            .Select(u => (TrainingScreen.UnitHit(0, u).X + 3, TrainingScreen.UnitHit(0, u).Y + 3)).ToArray());
        var newCost = screen.Plan.CostPerTrainingDay;
        Assert.NotEqual(oldCost, newCost);
        FlowTests.ReturnToMainMenu(d);
        Assert.Equal(before, Training()); // Öffnen und Ändern bucht nichts

        d.PlayMatchDay();
        var trainingDays = Enumerable.Range(paused + 1, game.Day - paused).Count(GameState.IsTrainingDay);
        Assert.True(trainingDays >= 1);
        Assert.Equal(before - trainingDays * newCost, Training());
    }
}
