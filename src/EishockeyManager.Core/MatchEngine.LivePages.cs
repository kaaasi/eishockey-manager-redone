namespace EishockeyManager.Core;

/// <summary>Seite, die der Seitenwähler 0x3A88A nach einem Drittel zeigt (Spur live-verschraenkt, S04).</summary>
public enum LivePageKind
{
    /// <summary>Seite 7: Ligapaarungen 0x4E8A3(Liga, 2, Minute, 1).</summary>
    LeaguePairings,

    /// <summary>Seite 6: „DEB-Pokalspiele“ 0x32E23(0, Minute).</summary>
    DebCupGames,

    /// <summary>Seite 1: „Play-Off Spiele“ 0x32E23(2, Minute), davor die Live-Tore 0x5C8B → 0xCCB2 (Datei 0x3AA30–0x3AA84).</summary>
    PlayoffGames,

    /// <summary>Seite 2: Aufstiegsrunde 0x31CD5 (Live-Tore 0x3183C bzw. Tabelle 0x30852 bei Minute 60).</summary>
    PromotionRound,

    /// <summary>Seite 3: Europapokal-Gruppe 0x31CD5 (Live-Tore 0x3183C bzw. Tabelle 0x30852 bei Minute 60).</summary>
    EuropeanCupTable,

    /// <summary>Seite 8: WM-Gruppenpaarungen 0x2FC2F beider Gruppen (Live-Tore 0x3183C, Datei 0x3AC10–0x3AC65).</summary>
    WorldCupGroups,

    /// <summary>Seite 10: „WM-Endrundenspiele“ 0x32E23(3, Minute).</summary>
    WorldCupKnockout,
}

/// <summary>Eine Seite nach dem Drittel; <see cref="League"/> nur bei <see cref="LivePageKind.LeaguePairings"/>.</summary>
public readonly record struct LivePage(LivePageKind Kind, League League = League.Bundesliga);

/// <summary>
/// Seiten nach jedem Drittel eines Spieltags (S04: A292, A436, F068). [ORIGINAL] Nachbildung
/// <c>tools/reverse-engineering/live/drittelseiten.py</c> (<c>docs/analyse/live-spieltag.md</c> §8): Am Ende jedes Aufrufs der
/// Minutenschleife 0x37A62 (Datei 0x3858C–0x386CD: Minute 20, 40, 60, in der Verlängerung 65 und 70) und nach dem
/// Penaltyschießen (Datei 0x37297–0x372AC, Minute 70) ruft das Original den Seitenwähler 0x3A88A mit der Minute; welche Seite
/// erscheint, entscheiden die Meldungsmatrix und die Anzeigeschalter (Datei 0x3A8A3–0x3AA01).
/// <list type="bullet">
/// <item>Ligaspieltag: je Liga des Tages (Ligamaske, Liga 0 → 2) die Ligapaarungen, nach Minute 20/40 mit „Drittelpause“,
///   nach Minute 60 mit „Ergebnisse“ der Liga (Index 3 · Liga + 0 bzw. + 1).</item>
/// <item>Pokaltermine nach dem Wettbewerbscode DGROUP 0xD0C2: DEB-Pokal (6) nach 20/40 mit „Drittelpause 1.Liga“ (Index 0),
///   danach mit dem Schalter DEB-Pokal (Index 10); Play-offs und Aufstiegsrunde (1) „Play-Off Spiele“ mit Index 0 (20/40) bzw. 1
///   (ab 60), dazu bei Play-off-Spiel 0 oder 2 (DGROUP 0xCCA3) die Aufstiegsrunde mit Index 3 (Minute ≠ 60) bzw. 4 (60);
///   Europapokal (3), WM-Gruppe (8) und WM-Endrunde (10) mit ihrem Schalter (Index 11 bzw. 12), nach 20/40 zusätzlich mit
///   „Drittelpause 1.Liga“; Länder-Freundschaftsspiel (13) nie.</item>
/// <item>Mit dauerhaftem Eisfeld (DGROUP 0x5A55 = 1, Darstellung 2 mit Anzeige, Datei 0x37112/0x37227) erscheint keine Seite.</item>
/// </list>
/// Reine Abfrage ohne Zustand; die Seiten selbst zeichnet die Oberfläche.
/// </summary>
public static class LivePages
{
    /// <summary>[ORIGINAL] Drittelenden mit Seitenwähler: 20, 40, 60 und in der Verlängerung 65, 70 (Datei 0x37192–0x37276).</summary>
    public static bool IsPeriodEnd(int minute) => minute is 20 or 40 or 60 or 65 or 70;

    /// <summary>
    /// Seiten nach <paramref name="minute"/> für einen Termin des Wettbewerbs <paramref name="competition"/>.
    /// <paramref name="leagues"/>: Ligen des Ligaspieltags; <paramref name="worldCupKnockout"/>: WM-Endrunde statt Gruppe;
    /// <paramref name="playoffGame"/>: Spiel der laufenden Play-off-Runde (DGROUP 0xCCA3, 0-basiert); <paramref name="permanentRink"/>:
    /// DGROUP 0x5A55; <paramref name="afterShootout"/>: Aufruf nach dem Penaltyschießen (Datei 0x372A7: nur die Seite des
    /// Wettbewerbscodes, also ohne Aufstiegsrunde).
    /// </summary>
    public static IReadOnlyList<LivePage> After(GameOptions options, int minute, MatchCompetition competition,
        IEnumerable<League>? leagues = null, bool worldCupKnockout = false, int playoffGame = 0, bool permanentRink = false,
        bool afterShootout = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        var pages = new List<LivePage>();
        if (permanentRink || !IsPeriodEnd(minute))
            return pages;
        var intermission = minute is 20 or 40; // [ORIGINAL] Datei 0x3A8A3–0x3A8B3: Merker [bp−4]
        var report = options.Reports;
        switch (competition)
        {
            case MatchCompetition.League:
                foreach (var league in (leagues ?? []).Distinct().Order())
                {
                    // [ORIGINAL] Datei 0x3A8C5–0x3A8F5: Index 3 · Liga + (Minute = 60)
                    if (report[3 * (int)league + (minute == 60 ? 1 : 0)])
                        pages.Add(new LivePage(LivePageKind.LeaguePairings, league));
                }
                break;
            case MatchCompetition.DebCup:
                // [ORIGINAL] Datei 0x3A9A7–0x3A9D5: nach 20/40 nur „Drittelpause 1.Liga“, sonst der Schalter DEB-Pokal
                if (intermission ? report[0] : options.ShowDebCup)
                    pages.Add(new LivePage(LivePageKind.DebCupGames));
                break;
            case MatchCompetition.Playoff or MatchCompetition.Relegation:
                // [ORIGINAL] Datei 0x3862E–0x386B6: Seite 1, bei Spiel 0 oder 2 auch Seite 2; Index nach Datei 0x3A8C5–0x3A8E9
                if (report[minute >= 60 ? 1 : 0])
                    pages.Add(new LivePage(LivePageKind.PlayoffGames));
                if (!afterShootout && playoffGame is 0 or 2 && report[3 + (minute == 60 ? 1 : 0)])
                    pages.Add(new LivePage(LivePageKind.PromotionRound));
                break;
            case MatchCompetition.EuropeanCup:
                // [ORIGINAL] Datei 0x3A92A–0x3A9A4: vor Minute 60 „Drittelpause 1.Liga“, immer der Schalter Europapokal
                if (options.ShowEuropeanCup && (minute >= 60 || report[0]))
                    pages.Add(new LivePage(LivePageKind.EuropeanCupTable));
                break;
            case MatchCompetition.WorldCup:
                // [ORIGINAL] Datei 0x3A8F7–0x3A927: Schalter WM-Spiele, nach 20/40 zusätzlich „Drittelpause 1.Liga“
                if (options.ShowWorldCup && (!intermission || report[0]))
                    pages.Add(new LivePage(worldCupKnockout ? LivePageKind.WorldCupKnockout : LivePageKind.WorldCupGroups));
                break;
        }
        return pages;
    }
}
