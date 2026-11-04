namespace EishockeyManager.Core;

/// <summary>
/// Rekorde eines Teams im Gegner-Fenster (em.exe 0x128:0x2A, Datei 0x4D385): je Team acht Wörter in DGROUP 0xBD0F +
/// 0x10 · Verein + 2 · Art und der Gegner dazu in DGROUP 0xBB0F + 8 · Verein + Art.
/// </summary>
public enum TeamRecordKind
{
    /// <summary>„Höchster H-Sieg“: größte Differenz Heimtore − Gasttore als Heimteam (Wort = Heim · 256 + Gast).</summary>
    HomeWin = 0,

    /// <summary>„Höchste H-Niederl.“ – das Original trägt sie nie ein (siehe <see cref="TeamRecords.Update"/>).</summary>
    HomeLoss = 1,

    /// <summary>„Erzielte H-Tore“: meiste eigene Tore als Heimteam (Wort = Tore · 256).</summary>
    HomeGoalsFor = 2,

    /// <summary>„Kassierte H-Tore“: meiste Gegentore als Heimteam (Wort = Tore).</summary>
    HomeGoalsAgainst = 3,

    /// <summary>„Höchster A-Sieg“ – das Original trägt ihn nie ein (siehe <see cref="TeamRecords.Update"/>).</summary>
    AwayWin = 4,

    /// <summary>„Höchste A-Niederl.“: größte Differenz Heimtore − Gasttore als Gastteam (Wort = Heim · 256 + Gast).</summary>
    AwayLoss = 5,

    /// <summary>„Erzielte A-Tore“: meiste eigene Tore als Gastteam (Wort = Tore).</summary>
    AwayGoalsFor = 6,

    /// <summary>„Kassierte A-Tore“: meiste Gegentore als Gastteam (Wort = Tore · 256).</summary>
    AwayGoalsAgainst = 7,
}

/// <summary>Arten der Spiele, die im Original über die Ergebniseintragung 0x50283 laufen (Wettbewerbscode DGROUP 0xD0C2).</summary>
public enum NationalMatchKind
{
    /// <summary>WM-Gruppenspiel (Code 8): Kopf-an-Kopf-Liste, Rekorde, Serien und Bilanz.</summary>
    WorldCupGroup,

    /// <summary>WM-Endrunde (Code 10, Datei 0x31017 → 0x128:0x5C): nur die Kopf-an-Kopf-Liste.</summary>
    WorldCupKnockout,

    /// <summary>Freundschaftsspiel des Bundestrainers (Code 0xD, Datei 0x30390 → 0x128:0x5C): nur die Kopf-an-Kopf-Liste.</summary>
    Friendly,
}

/// <summary>Rechenregeln der Rekorde (Spur wettbewerbe-komplett, Teil 2).</summary>
public static class TeamRecords
{
    /// <summary>[ORIGINAL] Tabellengröße 64 Teams (Vereine 0–51, Länder 52–63).</summary>
    public const int Teams = 64;

    public const int Kinds = 8;

    public static int Index(int club, TeamRecordKind kind) => Kinds * club + (int)kind;

    /// <summary>
    /// [ORIGINAL] Datei 0x51133 (Argumente Verein, Art, a, b, Modus, s, t, Gegner): alter Wert H = Wort / 256, L = Wort mod 256;
    /// verglichen werden a · s − b · t und H · s − L · t als <b>vorzeichenlose</b> 16-Bit-Zahlen (<c>jna</c>/<c>jnc</c>); Modus 0
    /// übernimmt einen echt größeren, Modus ≠ 0 einen echt kleineren Wert. Dann Wort = (a · s) · 256 + b · t' mit t' = 1 bei
    /// t &lt; 0, sonst t, und Gegner = <paramref name="opponent"/>. Weil ein leerer Eintrag 0 ist und jede Niederlagen-Differenz
    /// vorzeichenlos größer, bleiben die Arten mit Modus ≠ 0 (H-Niederlage, A-Sieg) im Original immer leer.
    /// </summary>
    public static void Update(int[] words, int[] opponents, int club, TeamRecordKind kind, int a, int b, bool lowest, int s, int t, int opponent)
    {
        var i = Index(club, kind);
        var old = words[i];
        var high = old >> 8 & 0xFF;
        var low = old & 0xFF;
        var candidate = (ushort)(a * s - b * t);
        var current = (ushort)(high * s - low * t);
        if (lowest ? candidate >= current : candidate <= current)
            return;
        var factor = t < 0 ? 1 : t;
        words[i] = (ushort)((a * s << 8) + b * factor);
        opponents[i] = opponent;
    }

    /// <summary>
    /// [ORIGINAL] Ergebnis eines Spiels in die Rekorde beider Teams (Aufrufe in 0x50283 bei Code 7 und 8, Datei 0x503A4–0x50438,
    /// 0x50680–0x506BA, 0x50AD0–0x50B07): Tore (H 2, A 7, H 3, A 6) immer; bei Heimsieg H-Sieg des Heimteams und
    /// A-Niederlage des Gastes (Modus 0, s = t = 1); bei Auswärtssieg H-Niederlage des Heimteams und A-Sieg des Gastes
    /// (Modus 1). Unentschieden zählt nur bei den Toren.
    /// </summary>
    public static void Enter(int[] words, int[] opponents, int home, int away, int hg, int ag)
    {
        Update(words, opponents, home, TeamRecordKind.HomeGoalsFor, hg, ag, false, 1, 0, away);
        Update(words, opponents, away, TeamRecordKind.AwayGoalsAgainst, hg, ag, false, 1, 0, home);
        Update(words, opponents, home, TeamRecordKind.HomeGoalsAgainst, hg, ag, false, 0, -1, away);
        Update(words, opponents, away, TeamRecordKind.AwayGoalsFor, hg, ag, false, 0, -1, home);
        if (hg > ag)
        {
            Update(words, opponents, home, TeamRecordKind.HomeWin, hg, ag, false, 1, 1, away);
            Update(words, opponents, away, TeamRecordKind.AwayLoss, hg, ag, false, 1, 1, home);
        }
        else if (hg < ag)
        {
            Update(words, opponents, home, TeamRecordKind.HomeLoss, hg, ag, true, 1, 1, away);
            Update(words, opponents, away, TeamRecordKind.AwayWin, hg, ag, true, 1, 1, home);
        }
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x4F5A8 (Manager, Verein, Seite, lo, hi): zehn Bytes je Manager und Gegner, Byte = hi · 16 + lo (jedes
    /// Ergebnis in Halbbytes). Länder (Verein ≥ erstes Land) füllen eine Liste: erst die geraden Plätze 0, 2 … 8, dann die
    /// ungeraden 1 … 9; ist alles voll, rückt die Folge 0, 2, 4, 6, 8, 1, 3, 5, 7, 9 um eins nach vorn und der neue Wert
    /// steht auf Platz 9. Vereine haben je Seite fünf Plätze (gerade = Seite 0, ungerade = Seite 1), voll → nach vorn.
    /// </summary>
    public static void EnterHeadToHead(int[] list, int offset, bool nation, int side, int lo, int hi) =>
        MeetingHistory.RecordAt(list, offset, nation, side, lo, hi); // ein Schreiber für Liga und Länder (Integration Runde 9)
}

/// <summary>
/// Spielstand – Daten des Gegner-Fensters für Länder (Spur wettbewerbe-komplett, Teil 2; em.exe 0x128:0x2A).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Rekorde DGROUP 0xBD0F/0xBB0F (<see cref="TeamRecords.Enter"/>), Kopf-an-Kopf-Liste DGROUP 0xAB7B + 0x280 ·
///   Manager + 10 · Verein (<see cref="TeamRecords.EnterHeadToHead"/>, beim Spielstart 0xFF, Datei 0x43726), Bilanzzeichen
///   DGROUP 0x825E (acht Zeichen, jedes Spiel schiebt nach links, 0x4F4F4). Geschrieben aus 0x50283: Kopf-an-Kopf für jedes
///   Spiel eines Teams mit Manager (Vereinsfeld 0x16) und Deutschlands (DGROUP 0x6AC → Platz 0), Rekorde, Serien und
///   Bilanz nur bei Code 7 (Liga) und 8 (WM-Gruppe).</item>
/// <item>[ORIGINAL] Die WM-Auslosung 0x35EDD tauscht diese Sätze mit den Ländern (Datei 0x365B6–0x36799); im Remake behalten
///   die Länder ihren Index, ein Tausch entfällt.</item>
/// <item>Rekorde der Vereine: Ligaspiele (Code 7) über <see cref="EnterLeagueRecords"/> aus <see cref="PlayNextMatchDay"/>, für das
///   Vereinsfenster (<c>ClubInfoWindow</c>, Spur ui-meldungen Teil 2; Anschluss Integration Runde 12). [ORIGINAL] Pokal-, Play-off-
///   und Aufstiegsrundenspiele schreiben keine Rekorde (wie die Serien): 0x50283 prüft den Wettbewerbscode DGROUP 0xD0C2 nur auf
///   7 und 8 (Datei 0x5030D–0x5031B), DEB-Pokal ist 6 (0x35E7C), Play-offs und Aufstiegsrunden 1 (0x31AB7); Probe
///   <c>ClubRecordsSeasonTests</c> (ui-rest Teil 2). [APPROX] Bilanzzeichen der Vereine werden nicht geführt (das
///   Vereinsfenster rechnet die Bilanz aus <see cref="Results"/>).</item>
/// <item>[APPROX] Bilanzzeichen nur der Länder, als Art 1 = Niederlage, 2 = Unentschieden, 3 = Sieg (Heim/Gast spielt für die
///   Anzeige keine Rolle). [ORIGINAL] Der Saisonwechsel leert sie (em.exe Datei 0x55EBA–0x55EE0, siehe
///   <see cref="ClearNationalForm"/>); Rekorde und Kopf-an-Kopf bleiben.</item>
/// </list>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>[ORIGINAL] Plätze der Kopf-an-Kopf-Liste: Manager 0 … 5 (0x280 Byte je Manager).</summary>
    public const int HeadToHeadSlots = 6;

    /// <summary>[ORIGINAL] Zehn Ergebnisse je Manager und Gegner.</summary>
    public const int HeadToHeadLength = 10;

    /// <summary>[ORIGINAL] Leerer Platz (Spielstart füllt 0xFF, Datei 0x43726).</summary>
    public const int HeadToHeadEmpty = 0xFF;

    /// <summary>[ORIGINAL] Länge der Bilanz (DGROUP 0x825E, acht Zeichen).</summary>
    public const int NationalFormLength = 8;

    private readonly int[] _teamRecords = new int[TeamRecords.Teams * TeamRecords.Kinds];
    private readonly int[] _teamRecordOpponents = new int[TeamRecords.Teams * TeamRecords.Kinds];
    private readonly int[] _headToHead = Enumerable.Repeat(HeadToHeadEmpty, HeadToHeadSlots * TeamRecords.Teams * HeadToHeadLength).ToArray();
    private readonly int[] _nationalForm = new int[(NationalTeams.Last - NationalTeams.First + 1) * NationalFormLength];

    /// <summary>
    /// Leert die Bilanzzeichen beim Saisonwechsel. [ORIGINAL] em.exe Datei 0x55EBA–0x55EE0 in der Schleife 0x55E39–0x55EF8
    /// über alle 64 Teams: <c>strcpy(0x825E + 0x50 · Team + 9 · Hälfte, DGROUP 0x5503 "--------")</c>. Rekorde (0xBD0F) und
    /// Kopf-an-Kopf (0xAB7B) schreibt die Schleife nicht (R9W-D01, #328).
    /// </summary>
    internal void ClearNationalForm() => Array.Clear(_nationalForm);

    /// <summary>Rekordwort (0 = keine Eintragung) und Gegner eines Teams.</summary>
    public (int Word, int Opponent) TeamRecord(int club, TeamRecordKind kind)
    {
        var i = TeamRecords.Index(club, kind);
        return (uint)club < TeamRecords.Teams ? (_teamRecords[i], _teamRecordOpponents[i]) : (0, 0);
    }

    /// <summary>Zehn Bytes der Kopf-an-Kopf-Liste (Platz <paramref name="slot"/>, Gegner <paramref name="club"/>).</summary>
    public IReadOnlyList<int> HeadToHead(int slot, int club) =>
        (uint)slot < HeadToHeadSlots && (uint)club < TeamRecords.Teams
            ? new ArraySegment<int>(_headToHead, (slot * TeamRecords.Teams + club) * HeadToHeadLength, HeadToHeadLength)
            : Enumerable.Repeat(HeadToHeadEmpty, HeadToHeadLength).ToArray();

    /// <summary>Bilanz eines Landes: acht Felder, das letzte ist das jüngste Spiel (0 = leer, 1 = N, 2 = U, 3 = S).</summary>
    public IReadOnlyList<int> NationalForm(int nation) =>
        nation is >= NationalTeams.First and <= NationalTeams.Last
            ? new ArraySegment<int>(_nationalForm, (nation - NationalTeams.First) * NationalFormLength, NationalFormLength)
            : new int[NationalFormLength];

    /// <summary>
    /// Ergebnis eines Länderspiels eintragen (0x50283 für Länder). Kopf-an-Kopf über <see cref="RecordMeeting"/> (Deutschland =
    /// DGROUP 0x6AC → Platz 0, Heim: Seite 0 mit lo = Heimtore, hi = Gasttore; Gast: Seite 1 mit lo = Gasttore, hi = Heimtore);
    /// nur bei WM-Gruppenspielen Rekorde, Serien und Bilanz.
    /// [APPROX] Länder haben im Remake keinen Manager (Vereinsfeld 0x16), es zählt nur Deutschland.
    /// </summary>
    public void EnterNationalResult(int home, int away, int hg, int ag, NationalMatchKind kind)
    {
        RecordMeeting(home, away, hg, ag); // ein Schreiber 0x50283 → 0x4F5A8 für alle Wettbewerbe (Spur wettbewerbe-spiel, Teil 2)
        if (kind != NationalMatchKind.WorldCupGroup)
            return;
        TeamRecords.Enter(_teamRecords, _teamRecordOpponents, home, away, hg, ag);
        CountSeries(Clubs[home], Clubs[away], hg, ag);
        ShiftForm(home, hg, ag);
        ShiftForm(away, ag, hg);
    }

    /// <summary>
    /// [ORIGINAL] Rekorde eines Ligaspiels (0x50283 bei Code 7, Datei 0x503A4–0x50438, 0x50680–0x506BA, 0x50AD0–0x50B07): beide
    /// Vereine über <see cref="TeamRecords.Enter"/>. Aufruf je Spiel aus <see cref="PlayNextMatchDay"/> (Integration Runde 12).
    /// </summary>
    internal void EnterLeagueRecords(int home, int away, int hg, int ag) =>
        TeamRecords.Enter(_teamRecords, _teamRecordOpponents, home, away, hg, ag);

    private static bool IsNation(int club) => club is >= NationalTeams.First and <= NationalTeams.Last;

    private void ShiftForm(int nation, int goalsFor, int goalsAgainst)
    {
        if (!IsNation(nation))
            return;
        var offset = (nation - NationalTeams.First) * NationalFormLength;
        Array.Copy(_nationalForm, offset + 1, _nationalForm, offset, NationalFormLength - 1);
        _nationalForm[offset + NationalFormLength - 1] = goalsFor > goalsAgainst ? 3 : goalsFor == goalsAgainst ? 2 : 1;
    }

    /// <summary>
    /// Bilanz eines Landes in den WM-Gruppenspielen der Saison: Spiele, Siege, Niederlagen, Tore, Gegentore. [ORIGINAL] Das
    /// Original liest die Tabellenfelder DGROUP 0x825A + 0x50 · Verein (Heim + Auswärts), die nur Code 7/8 füllt und der
    /// Saisonwechsel (Datei 0x55E3D–0x55EF8, alle 64 Teams) löscht; im Remake die Gruppenergebnisse der laufenden WM.
    /// </summary>
    public (int Games, int Wins, int Losses, int GoalsFor, int GoalsAgainst) NationalGroupRecord(int nation)
    {
        int games = 0, wins = 0, losses = 0, goalsFor = 0, goalsAgainst = 0;
        foreach (var m in WorldCup?.Groups.SelectMany(g => g.Results) ?? [])
        {
            if (m.Home != nation && m.Away != nation)
                continue;
            var (f, a) = m.Home == nation ? (m.HomeGoals, m.AwayGoals) : (m.AwayGoals, m.HomeGoals);
            games++;
            goalsFor += f;
            goalsAgainst += a;
            if (f > a)
                wins++;
            else if (f < a)
                losses++;
        }
        return (games, wins, losses, goalsFor, goalsAgainst);
    }

    /// <summary>
    /// „Stärke:“ eines Landes. [ORIGINAL] Datei 0x4D316 (cs:0x26): Summe der Vereinsfelder 0x18 + k, 0x1A + k, 0x1C + k
    /// (k = 0, 1: Kondition, Technik und Form von Abwehr und Sturm) / 6 – im Remake <see cref="NationalTeamValues"/>.
    /// [ORIGINAL] Mit Bundestrainer schreibt 0x952C (Manager 5) die Werte Deutschlands jeden Tag aus dessen Aufstellung
    /// (Tageslauf 0x54F9C–0x54FBF; Spur wettbewerbe-spiel, Teil 3, A019).
    /// </summary>
    public int NationalInfoStrength(int nation)
    {
        var (defense, attack, _) = NationalTeamValues(nation);
        return (defense.Condition + attack.Condition + defense.Technique + attack.Technique + defense.Form + attack.Form) / 6;
    }
}
