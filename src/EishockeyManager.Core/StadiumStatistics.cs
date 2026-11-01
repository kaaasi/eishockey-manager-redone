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
    /// <summary>[ORIGINAL] 0x9698: Mit weniger als neun Aufgestellten gilt eine Mannschaft als nicht spielfähig.</summary>
    public const int MinimumPlayers = 9;

    /// <summary>[ORIGINAL] 0x5447D–0x544B2: Abzug 200.000 DM (DGROUP 0x79D/0x7A1: · 3 / 1 = 600.000 Drittel).</summary>
    public const long ShortHandedPenalty = 200_000;

    /// <summary>[ORIGINAL] Meldung aus DGROUP 0x54E9 (Aufruf 0x54462).</summary>
    public const string ShortHandedMessage = "Weniger als neun Spieler!";

    /// <summary>
    /// Nicht spielfähig: weniger als <see cref="MinimumPlayers"/> Spieler aufgestellt. [ORIGINAL] 0x952C/0x9690: gezählt
    /// werden über 0x149D:0x89 (Datei 0x388F6) die Kaderplätze mit Aufstellungsposition (+0x15) und „eingesetzt“
    /// (+0x0A); bei weniger als neun setzt das Original den Managerwert 0x974B auf 100, sonst auf 20–40.
    /// [APPROX] Im Remake zählen Aufgestellte immer als eingesetzt (<see cref="Club.Dressed"/>); verletzte oder
    /// gesperrte Aufgestellte zählen nicht mit, weil sie im Remake nicht spielen. Ob das Original sie vorher aus der
    /// Aufstellung nimmt, ist nicht belegt.
    /// </summary>
    public bool IsShortHanded(Club club) => club.Lineup.Players.Count(p => p.Available) < MinimumPlayers;

    /// <summary>
    /// Spielwertung bei weniger als neun Spielern (#165), vor den Spielen eines Tages (Aufruf aus <c>ProcessDay</c>):
    /// markiert die Vereine nicht entlassener Manager, die nicht spielfähig sind (<see cref="IsShortHanded"/>).
    /// [ORIGINAL] Spieltagsablauf 0x36DC5–0x36DE9 (Liga) und 0x36F9C ff. (Spiele mit Teilnehmerzahl): Liefert die
    /// Spielabrechnung 0x544C0 1 (Heimmanager) bzw. 2 (Gastmanager), trägt 0x36D2A das Spiel ohne Spielberechnung als
    /// 0:2 bzw. 2:0 ein. Zwischen Markierung und Abrechnung ändert sich die Aufstellung nur durch Verletzungen im Spiel
    /// (0x38B8F, danach <see cref="PlayerInjuries.RefillLineup"/>); [APPROX] die Abrechnung prüft dann den Stand nach dem
    /// Spiel (im Original stellt 0x426BC nur mit der Einstellung DGROUP 0x375 neu auf, nicht geprüft).
    /// [APPROX] Sind beide Vereine Managervereine und beide nicht spielfähig, prüft das Original die Manager in
    /// Indexreihenfolge; das Remake wertet dann immer 0:2 (Heimverein zuerst).
    /// </summary>
    internal void MarkShortHandedClubs()
    {
        foreach (var club in Clubs)
            club.ForfeitsMatch = false;
        foreach (var manager in ActiveManagers)
            Clubs[manager.ClubIndex].ForfeitsMatch = IsShortHanded(Clubs[manager.ClubIndex]);
    }

    /// <summary>
    /// Spielabrechnung eines Spiels für die Manager beider Vereine (Aufruf aus <see cref="SettleHomeMatch"/>).
    /// [ORIGINAL] 0x544C0 ff., Schleife über die Manager in Indexreihenfolge:
    /// <list type="bullet">
    /// <item>Manager des Heimvereins nicht spielfähig (<see cref="IsShortHanded"/>): Meldung „Weniger als neun Spieler!“,
    ///   200.000 DM Abzug (0x54420), keine Zuschauer, kein Eintritt, keine Statistik; die Abrechnung endet (0x54595).</item>
    /// <item>sonst Zuschauer und Eintritt (0x5464A); bei Ligaspielen die Zuschauerstatistik (0x54664).</item>
    /// <item>Manager des Gastvereins nicht spielfähig: dieselbe Strafe (0x547B6); die Abrechnung endet.</item>
    /// </list>
    /// [APPROX] Die Meldung kommt in den Posteingang statt als Fenster, der Abzug steht im Finanzbuch unter „Strafen“
    /// (das Original zieht ihn ohne Buchungsart vom Konto ab). Die Spielwertung 0:2 bzw. 2:0 des Originals (Datei
    /// 0x36DC5–0x36DE9, #165) setzt <see cref="MarkShortHandedClubs"/> vor den Spielen des Tages.
    /// [APPROX] Entlassene Manager (<see cref="Manager.Dismissed"/>, Entlassung selbst ist [APPROX]) prüft die Abrechnung
    /// nicht mehr: Sie führen keinen Verein, zahlen also keine Strafe; Eintritt wie bisher.
    /// </summary>
    internal void SettleMatchGate(int home, int away, int tableMatchDay, int importance, bool leagueMatch)
    {
        foreach (var manager in Managers)
        {
            if (manager.ClubIndex == home)
            {
                if (!manager.Dismissed && IsShortHanded(Clubs[home]))
                {
                    PenalizeShortHanded(manager);
                    return;
                }
                var finance = Finance(manager);
                var stadium = finance.Stadium;
                var spectators = Spectators(Clubs[home], Clubs[away], stadium, tableMatchDay, FinanceRandom, importance, manager.Level);
                var tickets = (long)spectators * stadium.TicketPrice;
                Book(manager, FinanceCategory.Eintritt, tickets);
                finance.LastHomeGame = (spectators, tickets);
                if (leagueMatch)
                    finance.HomeStatistics.Add(spectators, away);
            }
            if (manager.ClubIndex == away && !manager.Dismissed && IsShortHanded(Clubs[away]))
            {
                PenalizeShortHanded(manager);
                return;
            }
        }
    }

    private void PenalizeShortHanded(Manager manager)
    {
        Notify(manager, ShortHandedMessage);
        Book(manager, FinanceCategory.Strafen, -ShortHandedPenalty);
    }

    /// <summary>Saisonwechsel der Zuschauerstatistik. [ORIGINAL] 0x55A86 (Saisonwechsel, vor der neuen Kostenstufe 0x563C0).</summary>
    internal void StartStadiumStatisticsSeason()
    {
        foreach (var manager in Managers)
            manager.FinanceData?.HomeStatistics.NewSeason();
    }
}

public sealed partial class Club
{
    /// <summary>
    /// Managerverein heute nicht spielfähig (weniger als neun Spieler, <see cref="GameState.MarkShortHandedClubs"/>):
    /// <see cref="MatchEngine.PlayWithEvents"/> (und <see cref="MatchEngine.Play"/>) wertet sein Spiel ohne Berechnung
    /// 0:2 bzw. 2:0. Wird vor den Spielen jedes Tages neu gesetzt und im Spielstand mitgesichert (seit Schema 9,
    /// <c>SaveGame.Migration8.cs</c>; Kommentar berichtigt, #179).
    /// </summary>
    internal bool ForfeitsMatch { get; set; }
}
