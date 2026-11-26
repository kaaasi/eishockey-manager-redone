using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>
/// Seiten nach einem Drittel im Live-Spiel (S04: A292, A436, F068; <c>docs/analyse/live-spieltag.md</c> §8). Welche Seiten
/// erscheinen, sagt <see cref="LivePages.After"/>; hier entstehen die Bildschirme mit der Minute und den laufenden Ständen.
/// <list type="bullet">
/// <item>[ORIGINAL] Laufende Stände: Vor den Seiten schreibt das Original die Stände des Termins in die Ergebnistabellen
///   (0x369C7(2, Pokal), Datei 0x38598/0x38616; Play-off 0x5C8B → 0xCCB2 Datei 0x3AA30–0x3AA84; WM-Gruppen 0x3183C Datei
///   0x3AC21), die Seiten lesen sie wie gespielte Ergebnisse. Das Remake setzt den Stand aus dem Bericht der Paarung
///   (<see cref="LiveMatch.Report"/>) in die Zeile des Termins: DEB-Pokal in die Spalte des laufenden Termins (x wie zuvor nach den
///   vergangenen Terminen, Nachbildung <c>pokalseiten.py</c>: 207/243 beim zweiten Termin), Play-off in die Spalte des laufenden
///   Spiels, WM-Endrunde und WM-Gruppen an die Stelle des Ergebnisses.</item>
/// <item>[ORIGINAL] Marken (C07, <c>c07_marken.py</c>): die erste setzt 0x32AA3 erst nach den Seiten der Minute 60 (Datei 0x37211),
///   also ein Punkt ab Minute 65 bei Verlängerung; nach dem Penaltyschießen zwei Punkte und der Stand mit den Treffern
///   (wie <see cref="CupMatch"/>).</item>
/// <item>[ORIGINAL] Ligapaarungen (Seite 7, 0x4E8A3 Modus 2, Teil 3b): <see cref="LeaguePairingsScreen"/> mit Minute, laufender
///   Stand je Paarung des Termins (0x369C7(2, 0), Datei 0x38598).</item>
/// <item>[ORIGINAL] Aufstiegsrunde (Seite 2) und Europapokal (Seite 3 bzw. 5 in der Endrunde, Code DGROUP 0xD0C2 = 3 bzw. 5,
///   Datei 0x31490–0x314A1): 0x31CD5 mit Merker „im Spiel“ [bp+0xA] und Terminzahl (Datei 0x3AAA4–0x3ABCE, Nachbildung
///   <c>aufstieg_europapokal.py</c>; spur live-seiten-rest): <see cref="PromotionRoundScreen"/> bzw. <see cref="EuropeanCupScreen"/>
///   mit <see cref="GroupLive"/>. Die Aufstiegsrunde ruft der Spieltag zweimal (Datei 0x38699–0x386C5: Seite 2 mit Gruppe 1 und 2),
///   gefiltert mit Zeile 3 · Gruppe + (Minute = 60) der Meldungsmatrix (Datei 0x3A8C5–0x3A8F5): Gruppe 0 „2.Liga“ (Index 3/4,
///   <see cref="LivePages.After"/>), Gruppe 1 „Oberliga“ (Index 6/7, hier in <see cref="For"/>).</item>
/// </list>
/// </summary>
public static class LivePageScreens
{
    /// <summary>Seiten nach <paramref name="minute"/> des Termins <paramref name="day"/> (siehe <see cref="LivePages.After"/>).</summary>
    /// <param name="permanentRink">Läuft das Dauer-Eisfeld (DGROUP 0x5A55)? Ohne Angabe Darstellung 2; nach einem angezeigten
    /// Penaltyschießen ist es abgebaut (Spur torszene-dauer, T5/T7).</param>
    public static IReadOnlyList<LivePage> For(GameState game, LiveMatchDay day, int minute, bool afterShootout = false,
        bool? permanentRink = null)
    {
        if (day.Matches.Count == 0)
            return [];
        var first = day.Matches[0];
        var playoffGame = PlayoffGame(game);
        // [APPROX] DGROUP 0x5A55 = 1 nur mit Anzeige und Darstellung 2 (Datei 0x37112, 0x37227); ob die Torszene 0x567C8 dabei
        // abbricht (Datei 0x37168), bleibt unberücksichtigt.
        var rink = permanentRink ?? game.Options.LiveMode == GameOptions.PermanentRinkLiveMode;
        var pages = LivePages.After(game.Options, minute, first.Competition,
            day.Matches.Where(m => m.Competition == MatchCompetition.League && m.Home.League is not null).Select(m => m.Home.League!.Value),
            worldCupKnockout: first.Competition == MatchCompetition.WorldCup && first.KnockOut,
            playoffGame: playoffGame,
            permanentRink: rink,
            afterShootout: afterShootout).ToList();
        // [ORIGINAL] Datei 0x386AD–0x386C5: nach Gruppe 0 (0x3A88A(2, 1, Minute)) die zweite Gruppe 0x3A88A(2, 2, Minute) mit Index
        // 3 · 2 + (Minute = 60) – „Drittelpause Oberliga“ bzw. „Ergebnisse Oberliga“ (Datei 0x3A8C5–0x3A8F5).
        if (first.Competition is MatchCompetition.Playoff or MatchCompetition.Relegation && !afterShootout && !rink
            && playoffGame is 0 or 2 && LivePages.IsPeriodEnd(minute) && game.Options.Reports[6 + (minute == 60 ? 1 : 0)])
            pages.Add(new LivePage(LivePageKind.PromotionRound, League.Oberliga));
        return pages;
    }

    /// <summary>Liga der Aufstiegsseite: Gruppe 0 (2. Liga → „Aufstieg  1.Liga“) oder Gruppe 1 (Oberliga → „Aufstieg  2.Liga“).</summary>
    public static League PromotionLeague(LivePage page) => page.League == League.Oberliga ? League.Oberliga : League.ZweiteLiga;

    /// <summary>
    /// [ORIGINAL] Ansicht der Europapokalseite: Gruppe 1 (Seite 3, Code 3) in den Gruppenterminen, Endrunde (Seite 5, Code 5) ab dem
    /// siebten Termin (DGROUP 0x7AF &gt; 5, Datei 0x31490–0x314A1).
    /// </summary>
    public static int EuropeanCupView(GameState game) =>
        SeasonCalendar.DaysOf(DayEvents.EuropeanCup).Count(d => d < game.CurrentDay) >= EuropeanCup.GroupDates ? 2 : 0;

    /// <summary>[ORIGINAL] Spiel der laufenden Play-off-Runde (DGROUP 0xCCA3): Termin des Tages modulo fünf.</summary>
    public static int PlayoffGame(GameState game)
    {
        var days = SeasonCalendar.DaysOf(DayEvents.Playoffs);
        var index = days.Count(d => d < game.CurrentDay);
        return index % Playoffs.DatesPerRound;
    }

    /// <summary>Stand einer Paarung aus Sicht von <paramref name="left"/> oder null, wenn der Termin sie nicht enthält.</summary>
    public static DebCupResult? Score(LiveMatchDay day, int left, int right, int minute, bool afterShootout)
    {
        foreach (var m in day.Matches)
        {
            bool leftIsHome;
            if (m.Home.Index == left && m.Away.Index == right)
                leftIsHome = true;
            else if (m.Home.Index == right && m.Away.Index == left)
                leftIsHome = false;
            else
                continue;
            var r = m.Report;
            var (home, away, dots) = (r.HomeGoals, r.AwayGoals, minute > 60 && r.Overtime ? 1 : 0);
            if (afterShootout && r.ShootoutGoals is { } shoot && r.HomeGoals == r.AwayGoals)
                (home, away, dots) = (home + shoot.Home, away + shoot.Away, 2);
            return leftIsHome ? new DebCupResult(home, away, dots) : new DebCupResult(away, home, dots);
        }
        return null;
    }

    /// <summary>Bildschirme einer Seite in der Reihenfolge des Originals (WM-Gruppen: Gruppe 1, dann 2; Datei 0x3AC29–0x3AC65).</summary>
    public static IReadOnlyList<Screen> Create(ScreenHost host, Screen back, LiveMatchDay day, LivePage page, int minute,
        bool afterShootout = false)
    {
        var game = host.Game!;
        DebCupResult? ScoreOf(int left, int right) => Score(day, left, right, minute, afterShootout);
        // [ORIGINAL] Gruppenseiten zeigen Tore ohne Marken (0x31CD5, Datei 0x3255D–0x325D0).
        DebCupResult? LiveScore(int left, int right) => ScoreOf(left, right) is { } r ? r with { Dots = 0 } : null;
        switch (page.Kind)
        {
            case LivePageKind.LeaguePairings:
            {
                var normal = new LeaguePairingsScreen(host, back, page.League);
                if (normal.Page is not { } pairings)
                    return [];
                var rows = pairings.Rows.Select(r => ScoreOf(r.Home, r.Away) is { } live
                    ? r with { Result = (live.Left, live.Right) }
                    : r).ToList();
                // [ORIGINAL] Datei 0x4EEA4: die vorige Ligaseite der Folge hat Ergebnisse gezeichnet → Breite 2 (siehe LiveHeader).
                var previous = For(game, day, minute, afterShootout).TakeWhile(p => p != page)
                    .Any(p => p.Kind == LivePageKind.LeaguePairings);
                var header = LeaguePairingsScreen.LiveHeader(page.League, normal.Day, previous, minute);
                return [new LeaguePairingsScreen(host, back, page.League, new PairingsPage(header, rows), liveMinute: minute)];
            }
            case LivePageKind.DebCupGames when DebCupOverview.Of(game) is { } overview:
            {
                var groups = overview.Groups.Select(g => g with
                {
                    Rows = g.Rows.Select(r => ScoreOf(r.LeftClub, r.RightClub) is { } live
                        ? r with { Results = [.. r.Results, live] }
                        : r).ToList(),
                }).ToList();
                return [new DebCupGamesScreen(host, back, overview with { Groups = groups }, liveMinute: minute)];
            }
            case LivePageKind.PlayoffGames when PlayoffOverview.Of(game) is { } overview:
            {
                var sections = overview.Sections.Select(s => s with
                {
                    Rows = s.Rows.Select(r => ScoreOf(r.Left, r.Right) is { } live
                        ? r with { Results = [.. r.Results, live] }
                        : r).ToList(),
                }).ToList();
                return [new PlayoffGamesScreen(host, back, sections, liveMinute: minute)];
            }
            case LivePageKind.WorldCupKnockout when game.WorldCup is { } wm:
            {
                var sections = WorldCupKnockoutScreen.Of(wm).Select(s => s with
                {
                    Rows = s.Rows.Select(r => ScoreOf(r.Left, r.Right) is { } live
                        ? r with { LeftGoals = live.Left, RightGoals = live.Right, Dots = live.Dots }
                        : r).ToList(),
                }).ToList();
                return [new WorldCupKnockoutScreen(host, back, sections, liveMinute: minute)];
            }
            case LivePageKind.WorldCupGroups when game.WorldCup is { } wm:
            {
                var stage = CupPairingsScreen.StartDay(game);
                var screens = new List<Screen>();
                for (var g = 0; g < Math.Min(2, wm.Groups.Count); g++)
                {
                    var group = wm.Groups[g];
                    var rows = group.Pairings(stage).Select(p =>
                    {
                        if (ScoreOf(p.Home, p.Away) is { } live)
                            return new CupPairingRow(p.Home, p.Away, live.Left, live.Right);
                        var done = group.Results.FirstOrDefault(m => m.Home == p.Home && m.Away == p.Away);
                        return new CupPairingRow(p.Home, p.Away, done?.HomeGoals ?? 0, done?.AwayGoals ?? 0);
                    }).ToList();
                    screens.Add(new CupPairingsScreen(host, back, g, stage, rows, liveMinute: minute));
                }
                return screens;
            }
            case LivePageKind.PromotionRound:
                return [new PromotionRoundScreen(host, back, PromotionLeague(page), new GroupLive(minute, game.CurrentDay, LiveScore))];
            case LivePageKind.EuropeanCupTable:
                return [new EuropeanCupScreen(host, back, EuropeanCupView(game), live: new GroupLive(minute, game.CurrentDay, LiveScore))];
            default:
                return [];
        }
    }
}
