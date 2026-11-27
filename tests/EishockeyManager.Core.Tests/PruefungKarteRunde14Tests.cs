using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>pruefung-karte</c> Runde 14, Teil 2: gesammelte P3 aus den Reviews von #688 (R688S2-03, R688N-01).
/// </summary>
public class PruefungKarteRunde14Tests
{

    /// <summary>Tage, deren Tageswechsel zwischen zwei Halten lag.</summary>
    private static IEnumerable<int> DaysBetween(int from, int to) => Enumerable.Range(from + 1, Math.Max(0, to - from));

    /// <summary>
    /// R688N-01 (Rückmutation 3): Ein verletzter eigener Spieler auf der Transferliste (außerhalb des Kaders) heilt im Hauptweg am
    /// Takttag mit Zähler ≡ 0 (mod 7) über den Markt-Teil von 0x07169 (Datei 0x072A0–0x072CB). Rot ohne die Heilung in
    /// <c>DailyRunMarket</c>: <c>HealUnmanaged</c> lässt Managervereine aus.
    /// </summary>
    [Fact]
    public void ListedInjuredOwnPlayer_HealsOnTheHealingTick()
    {
        var game = TestGames.Create(3);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        Assert.True(club.ApplyAutoLineup());
        var p = club.Players.First(q => q.Role == PlayerRole.Defender && !q.OnLoan && TransferMarket.CanLeave(club, q));
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));
        p.InjuredGames = 3;

        var healingTicks = 0;
        for (var guard = 0; healingTicks < 2; guard++)
        {
            Assert.True(guard < 200, "kein Heiltag erreicht");
            var from = game.CurrentDay;
            game.Continue();
            Assert.Contains(game.TransferMarket.OwnListingsOutsideSquad(club), l => l.Player == p); // Vorbedingung: noch gelistet
            healingTicks += DaysBetween(from, game.CurrentDay)
                .Count(d => GameState.IsDailyRunDay(d) && PlayerInjuries.IsHealingDay(DayCounter.Of(d)));
        }
        Assert.Equal(3 - healingTicks, p.InjuredGames);
    }

    /// <summary>
    /// R688N-01 (Rückmutationen 1 und 2): Mit „automatisch aufstellen“ stellt der Hauptweg nur am Takttag (Ende von 0x07169, Datei
    /// 0x07C07) und an Tagen mit Ereignis (Datei 0x54D9E) neu auf. Über <c>Continue()</c> ist das nicht sichtbar (jeder Halt ist ein
    /// Tag mit Ereignis), daher ruft der Test die beiden Schritte des Tageswechsels direkt auf: Takt bzw. Ereignistag stellen die
    /// gestörte Aufstellung wieder her, andere Tage lassen sie stehen. Rot, wenn täglich aufgestellt wird bzw. die Neuaufstellung
    /// am Ende von 0x07169 fehlt.
    /// </summary>
    [Fact]
    public void AutoLineup_OnlyOnTickOrEventDays()
    {
        var game = TestGames.Create(3);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        Assert.True(club.ApplyAutoLineup());
        var days = Enumerable.Range(1, 100).ToList();
        var tick = days.First(d => GameState.IsDailyRunDay(d) && !game.IsListChangeDay(d));
        var noTick = days.First(d => !GameState.IsDailyRunDay(d));
        var eventDay = days.First(game.IsListChangeDay);
        var quiet = days.First(d => !game.IsListChangeDay(d));

        void Step(string name, int day, bool restores)
        {
            club.Lineup.Clear(LineupSlot.Goalkeeper); // Störung ohne Abschalten der Einstellung
            typeof(GameState).GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(game, [day]);
            if (restores)
                Assert.NotNull(club.Lineup[LineupSlot.Goalkeeper]);
            else
                Assert.Null(club.Lineup[LineupSlot.Goalkeeper]);
            Assert.True(club.ApplyAutoLineup());
        }

        Step("MainPathEventDayLineups", quiet, restores: false);
        Step("MainPathEventDayLineups", eventDay, restores: true);
        Step("MainPathDailyRun", noTick, restores: false);
        Step("MainPathDailyRun", tick, restores: true);
    }
}
