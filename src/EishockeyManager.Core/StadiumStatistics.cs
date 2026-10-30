namespace EishockeyManager.Core;

/// <summary>
/// Zuschauerstatistik eines Managers für die laufende Saison (Spur heimspiel-statistik, #136): Verlauf, Summe, Rekord
/// und Minuskulisse mit Gegner, Zahl der Heimspiele. Grundlage des Bildschirms „Statistik Verein“ (wartet auf
/// <c>livespiel</c>); bis dahin nur im Core.
/// </summary>
/// <remarks>
/// Belege aus em.exe (Dateioffsets, Managersatz 0x1AF Byte, Felder als DGROUP-Offset bei Manager 0):
/// <list type="bullet">
/// <item>[ORIGINAL] Fortschreibung 0x54664–0x54760 nach jeder Zuschauerberechnung eines Ligaheimspiels: Verlauf
///   <c>[0x9759 + n]</c> = Zuschauer / 1000 (Byte), Summe <c>[0x97DE]</c> += Zuschauer, Rekord <c>[0x97E2]</c> mit Gegner
///   <c>[0x976F]</c> bei Rekord ≤ Zuschauer, Minuskulisse <c>[0x97E6]</c> mit Gegner <c>[0x9770]</c> bei Minuskulisse ≥
///   Zuschauer, danach Zähler <c>[0x9748]</c> + 1. Gleichstand übernimmt also den neuen Gegner.</item>
/// <item>[ORIGINAL] Nur Ligaspiele: Übergeben ein Pokal, die Play-Off-/Aufstiegsrunden oder die WM eine Teilnehmerzahl
///   (Argument 3 von 0x544C0, aus 0x36B5D Argument 2), springt 0x54658 an der Statistik vorbei; Eintritt wird trotzdem
///   gebucht (0x5464A, vor der Prüfung).</item>
/// <item>[ORIGINAL] Saisonwechsel 0x55A86–0x55ADB: Rekord 0, Zähler 0, Summe 0, Minuskulisse 99.999 (0x1869F, auch
///   beim Spielstart 0x29C2F). Gegner und alte Verlaufswerte bleiben stehen (der Zähler begrenzt den Verlauf).</item>
/// <item>[ORIGINAL] Anzeige „Schnitt“ 0x2DB79: Summe / Zähler (Ganzzahl), ohne Heimspiel ein Text statt einer Zahl.</item>
/// </list>
/// </remarks>
public sealed class StadiumStatistics
{
    /// <summary>[ORIGINAL] Startwert der Minuskulisse (0x29C35, 0x55ADB: 0x1869F).</summary>
    public const int InitialMinimum = 99_999;

    private readonly List<int> _curve = [];

    /// <summary>Zuschauer / 1000 je Heimspiel der Saison, in Spielreihenfolge.</summary>
    public IReadOnlyList<int> Curve => _curve;

    /// <summary>Heimspiele der Saison (Zähler 0x9748).</summary>
    public int Games { get; private set; }

    /// <summary>Summe der Zuschauer (0x97DE).</summary>
    public long Total { get; private set; }

    /// <summary>Rekordkulisse (0x97E2), 0 ohne Heimspiel.</summary>
    public int Record { get; private set; }

    /// <summary>Gegner beim Rekord (Vereinsindex, 0x976F), −1 = noch keiner.</summary>
    public int RecordOpponent { get; private set; } = -1;

    /// <summary>Minuskulisse (0x97E6), <see cref="InitialMinimum"/> ohne Heimspiel.</summary>
    public int Minimum { get; private set; } = InitialMinimum;

    /// <summary>Gegner bei der Minuskulisse (Vereinsindex, 0x9770), −1 = noch keiner.</summary>
    public int MinimumOpponent { get; private set; } = -1;

    /// <summary>Zuschauerschnitt der Saison (Summe / Heimspiele, abgerundet); null ohne Heimspiel.</summary>
    public int? Average => Games == 0 ? null : (int)(Total / Games);

    /// <summary>Ein Ligaheimspiel eintragen. [ORIGINAL] 0x54664–0x54760.</summary>
    public void Add(int spectators, int opponent)
    {
        _curve.Add(spectators / 1000);
        Total += spectators;
        if (Record <= spectators)
        {
            Record = spectators;
            RecordOpponent = opponent;
        }
        if (Minimum >= spectators)
        {
            Minimum = spectators;
            MinimumOpponent = opponent;
        }
        Games++;
    }

    /// <summary>Saisonwechsel. [ORIGINAL] 0x55A86–0x55ADB; die Gegner bleiben wie im Original stehen.</summary>
    public void NewSeason()
    {
        _curve.Clear();
        Games = 0;
        Total = 0;
        Record = 0;
        Minimum = InitialMinimum;
    }
}

public sealed partial class ManagerFinance
{
    /// <summary>Wird beim ersten Heimspiel angelegt (Spielstände bis Schema 6 haben keine Statistik).</summary>
    private StadiumStatistics? _homeStatistics;

    /// <summary>Zuschauerstatistik der laufenden Saison (<see cref="StadiumStatistics"/>).</summary>
    public StadiumStatistics HomeStatistics => _homeStatistics ??= new StadiumStatistics();
}

public sealed partial class GameState
{
    /// <summary>
    /// Spielabrechnung eines Spiels (Aufruf aus <see cref="SettleHomeMatch"/>). [ORIGINAL] 0x544C0 ff.: für jeden Manager
    /// des Heimvereins Zuschauer und Eintritt (0x5464A), bei Ligaspielen danach die Zuschauerstatistik (0x54664).
    /// Die Prüfung „Weniger als neun Spieler“ (0x54589/0x547AA) folgt in einem eigenen PR (Spur heimspiel-statistik).
    /// </summary>
    internal void SettleMatchGate(int home, int away, int tableMatchDay, int importance, bool leagueMatch)
    {
        foreach (var manager in Managers.Where(m => m.ClubIndex == home))
        {
            var finance = Finance(manager);
            var stadium = finance.Stadium;
            var spectators = Spectators(Clubs[home], Clubs[away], stadium, tableMatchDay, FinanceRandom, importance, manager.Level);
            var tickets = (long)spectators * stadium.TicketPrice;
            Book(manager, FinanceCategory.Eintritt, tickets);
            finance.LastHomeGame = (spectators, tickets);
            if (leagueMatch)
                finance.HomeStatistics.Add(spectators, away);
        }
    }

    /// <summary>Saisonwechsel der Zuschauerstatistik. [ORIGINAL] 0x55A86 (Saisonwechsel, vor der neuen Kostenstufe 0x563C0).</summary>
    internal void StartStadiumStatisticsSeason()
    {
        foreach (var manager in Managers)
            manager.FinanceData?.HomeStatistics.NewSeason();
    }
}
