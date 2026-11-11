using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-rest Teil 3 (#732, R16-D2 = A): Kader und Finanzen des Satzes 0 im Modus „nur Weltkugel“ wie im Original.
/// [ORIGINAL] Der Tagesabschluss 0x100:0x4D (0x3ED01) läuft je Manager 0 … [0x386) ohne Modusprüfung – in der Sommerpause des
/// Saisonwechsels (Datei 0x564E1–0x56564, Aufruf 0x56512) und in Schritt (d) der WM-Schleife (Datei 0x5568B–0x5569F); der Sprung
/// zum ersten WM-Tag (Datei 0x54CF7–0x54D38) zählt ohne Tagesabschluss. Beleg FILE20 (28.04.1994, 500.000 DM) → FILE21 (14.04.1995,
/// −1.530.327 DM, `emtool hok dump`): Monatsabschlüsse April bis August mit den Gehältern des Kaders von Satz 0.
/// </summary>
public class AbgleichRestWorldOnlyTests
{
    private static GameState Import(string file, int seed) =>
        HokCompare.Import(OriginalImportTests.OriginalSave(file + ".HOK"), ClubDatabase.Load(TestGameData.Load()), seed);

    /// <summary>
    /// Regressionstest (rot vor Teil 3: Remake 500.000 DM, Gehalt und Preis neu bewertet): FILE20 → FILE21 trifft das Konto des
    /// Originals auf 1 % der Änderung (gemessen: −1.532.012 zu −1.530.327 DM bei allen Saaten), Gehalt und Preis des Kaders von
    /// Satz 0 bleiben wie im Original stehen (der Import übernimmt den Kader, statt den Vereinskader neu zu bewerten).
    /// </summary>
    [GameDataTheory]
    [InlineData(1993)]
    [InlineData(1)]
    [InlineData(77)]
    public void File20To21_WorldOnly_ManagerMoneyAndSquadLikeOriginal(int seed)
    {
        var game = Import("FILE20", seed);
        Assert.True(game.WorldOnly);
        var target = Import("FILE21", seed);
        var before = game.Managers[0].Money;
        Assert.Equal(500_000, before);

        Assert.Null(HokCompare.PlayTo(game, target));

        var change = target.Managers[0].Money - before;
        Assert.Equal(-2_030_327, change);
        Assert.InRange(game.Managers[0].Money - target.Managers[0].Money, change / 100, -change / 100);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var original = target.Clubs[target.Managers[0].ClubIndex].Players.ToDictionary(p => (p.Name, p.Role));
        Assert.Equal(25, club.Players.Count);
        Assert.All(club.Players, p =>
        {
            var o = original[(p.Name, p.Role)];
            Assert.Equal((o.MonthlySalary, o.PurchasePrice), (p.MonthlySalary, p.PurchasePrice));
        });
    }

    /// <summary>
    /// Am ersten WM-Tag kein Tagesabschluss ([ORIGINAL] Datei 0x54CF7–0x54D38): Die WM-Schleife rechnet nur ihre eigenen Tage ab,
    /// nicht die übersprungene Vereinssaison. Bis zum Saisonwechsel also vier Monatsabschlüsse – Mai bis August (Sommerpause, im
    /// Finanzbuch der neuen Saison; der April fällt aus, R688S-01, <c>RunSummerBreak</c>) – statt elf (rot, wenn die Schleife ab
    /// Tag 0 nachrechnet). FILE20 → FILE21 bucht den April wegen der Ladeeigenheit (<c>GameState.LoadQuirk.cs</c>) mit.
    /// </summary>
    [Fact]
    public void WorldOnly_FirstSeason_ClosesSummerMonthsOnly()
    {
        var game = GameModeTests.Create(11, flag: false, globe: true, 5);
        var manager = game.Managers[0];
        var wages = FinanceRules.MonthlyWages(game, game.Clubs[manager.ClubIndex]);
        Assert.True(wages > 0);
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            game.Continue();
        }
        Assert.Equal(500_000, manager.Money); // bis zum Saisonende kein Monatsletzter in der Schleife
        game.StartNextSeason();
        Assert.Equal(-4 * wages, game.Finance(manager).Book.Current(FinanceCategory.Gehaelter));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel im Modus „nur Weltkugel“, passiv und aktiv (Freundschaftsspiele). Das
    /// Konto von Satz 0 ändert sich je Saison genau um die Buchungen des Finanzbuchs und sinkt (Gehälter ohne Einnahmen aus Spielen);
    /// der Manager wird nicht entlassen, sein Kader bleibt.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorldOnly_MultiSeason_ManagerAccountBooksDailyClose(bool active)
    {
        var game = GameModeTests.Create(11, flag: false, globe: true, 5);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        var squad = club.Players.ToList();
        var account = new ClubAccountProbe(game, manager);
        for (var change = 0; change < 4; change++)
        {
            game.Continue();
            game.AutoNominate();
            if (active)
                for (var opponent = NationalTeams.First; opponent <= NationalTeams.Last; opponent++)
                    if (opponent != NationalTeams.Germany)
                        game.PlayFriendly(opponent, home: opponent % 2 == 0);
            for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
            {
                Assert.True(guard < 100, "Saisonende nicht erreicht");
                game.Continue();
            }
            account.SeasonEnd();
            game.StartNextSeason();
            account.SeasonStart();
            Assert.False(manager.Dismissed);
            Assert.Equal(squad, club.Players);
        }
    }
}

/// <summary>
/// Konto eines Managers im Modus „nur Weltkugel“ (#732): Es ändert sich innerhalb einer Saison nur über das Finanzbuch
/// (<see cref="GameState.Book"/>) und sinkt mit dem Tagesabschluss von Satz 0 ([ORIGINAL] Datei 0x56512, 0x5569A). Ersetzt in den
/// Mehrsaisonproben des Modus 1 die frühere Prüfung „Vereinskonto unverändert“ (R15-E2 = C). Ausnahme: Begrenzung auf
/// −<see cref="FinanceRules.BalanceLimit"/> am Monatsabschluss (keine Buchung, Datei 0x3F10E).
/// </summary>
internal sealed class ClubAccountProbe
{
    private readonly GameState _game;
    private readonly Manager _manager;
    private long _base, _start;
    private bool _started;

    public ClubAccountProbe(GameState game, Manager manager)
    {
        (_game, _manager) = (game, manager);
        SeasonStart();
    }

    /// <summary>Nach <see cref="GameState.StartNextSeason"/>: Das Konto liegt unter dem Stand des vorigen Saisonstarts.</summary>
    public void SeasonStart()
    {
        if (_started && _manager.Money != -FinanceRules.BalanceLimit)
            Assert.True(_manager.Money < _start, $"Konto {_manager.Money} nicht unter {_start}: kein Tagesabschluss für Satz 0");
        _started = true;
        _start = _manager.Money;
        _base = _manager.Money - _game.Finance(_manager).Book.CurrentTotal;
    }

    /// <summary>Saisonende (vor <see cref="GameState.StartNextSeason"/>): Konto = Stand ohne Buchungen + Finanzbuch der Saison.</summary>
    public void SeasonEnd()
    {
        if (_manager.Money != -FinanceRules.BalanceLimit)
            Assert.Equal(_base, _manager.Money - _game.Finance(_manager).Book.CurrentTotal);
    }
}
