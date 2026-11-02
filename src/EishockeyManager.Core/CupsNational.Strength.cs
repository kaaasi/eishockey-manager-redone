namespace EishockeyManager.Core;

/// <summary>
/// Spielstärke der Nationalteams (Spur wettbewerbe-spiel, Teil 3; Inventur P2 Nr. 2). Fundstellen:
/// <c>docs/spielberechnung.md</c> §7, §13 und §20.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Nationalteams sind Vereinsplätze 52–63 wie alle anderen: Die Spielberechnung liest ihre Vereinsfelder
///   0x17–0x1D über 0x368B1/0x977F (Datei 0x368B1, 0x977F), es gibt keine eigene Stärkeformel. Der Lader setzt Kondition
///   und Technik je Teil aus DGROUP 0x4C4E − 2 ohne Streuung (Datei 0x2F0CC–0x2F120), Form 0x1C/0x1D = random(45, 55) und
///   Tagesform 0x17 = random(47, 53) wie bei den Vereinen (Datei 0x2EFE0, 0x2F093).</item>
/// <item>[ORIGINAL] Das Land des Bundestrainers hat Vereinsfeld 0x16 = 6 (Manager 5 + 1, Datei 0x55494) und spielt wie ein
///   Managerverein: Reihenwerte aus den eingesetzten Spielern seines Kaders (0x98F8 mit Manager 5, Kaderplätze DGROUP
///   0x5DC3 + 5 · 0x618), Technik + Länderspiele / 8 (höchstens 99), Taktik 0x939A, Reihenwechsel 0x8EF2/0x91FF mit drei
///   bzw. mit vierter Reihe vier Reihen, Energie 0x87F7 (<see cref="MatchEngine.NationalLiveTeam"/>).</item>
/// <item>[ORIGINAL] 0x952C mit Manager 5 schreibt die Vereinsfelder 0x18–0x1D des Landes aus der Aufstellung des
///   Bundestrainers (Tageslauf 0x54F9C–0x54FBF jeden Tag, nach Freundschaftsspielen 0x555D6); diese Werte zeigen
///   „Stärke:“ und die Stärketabelle (<see cref="NationalTeamValues"/>).</item>
/// <item>[ORIGINAL] Die Taktik-Automatik (Spielvorbereitung 0x8365) vergleicht die doppelten Stärken 0x368B1(Verein, 1, 0,
///   0, 1) beider Länder (Datei 0x83AD/0x83CA, Grenze 0x19 Datei 0x840E; <see cref="NationalMatchStrength"/>).</item>
/// </list>
/// </remarks>
public sealed partial class GameState
{
    /// <summary>
    /// Teamwerte eines Nationalteams (Vereinsfelder 0x18–0x1D) und seine Tagesform 0x17.
    /// <list type="bullet">
    /// <item>[ORIGINAL] Land des Bundestrainers mit eigener Aufstellung: 0x952C mit Manager 5 (Kondition, Technik, Form je
    ///   Teil aus den Reihen 0–2 ohne Spiel, <see cref="MatchEngine.LineupTeamValues"/>). [APPROX] Aus der aktuellen
    ///   Aufstellung statt dem Stand des letzten Tageslaufs (zwischen Tageslauf und Anzeige bzw. Spiel ändert sich die
    ///   Aufstellung im Remake nur durch den Nutzer); ohne eigene Aufstellung die Länderwerte.</item>
    /// <item>Sonst [ORIGINAL] Kondition und Technik aus <see cref="NationalRatings"/> (DGROUP 0x4C4E − 2 bzw. die
    ///   ausgewürfelten Werte Deutschlands), Form und Tagesform aus dem Lader bzw. der Wanderung
    ///   (<see cref="MatchEngine.TeamValuesOf"/> des Vereinsplatzes). [APPROX] Die Form, die 0x554CF für Deutschland mit
    ///   auswürfelt, verwirft das Remake (kein Feld im Spielstand); die Wanderung 0x9D1B zieht sie zweimal je Woche ohnehin
    ///   neu.</item>
    /// </list>
    /// </summary>
    public (PartValues Defense, PartValues Attack, int DayForm) NationalTeamValues(int nation)
    {
        var loaded = MatchEngine.TeamValuesOf(Clubs[nation]);
        if (NationalCoach is { } coach && coach.Nation == nation && coach.Formation.Count > 0)
        {
            var (defense, attack, _) = MatchEngine.LineupTeamValues(coach.Formation);
            return (defense, attack, loaded.DayForm);
        }
        var (cd, td, ca, ta) = NationalRatings(nation);
        return (new PartValues(cd, td, loaded.DefenseForm), new PartValues(ca, ta, loaded.AttackForm), loaded.DayForm);
    }

    /// <summary>
    /// Spielstärke eines Nationalteams vor dem Spiel. [ORIGINAL] 0x368B1(Verein, 1, 0, 0, 1) wie in der Spielvorbereitung
    /// 0x8365 (Datei 0x83AD/0x83CA): Gewichtung 1, Minute 0, gleich viele Feldspieler, Reihe 0 auf dem Eis, Energie der
    /// Spielvorbereitung; beim Land des Bundestrainers aus dessen Kader (0x98F8 mit Manager 5, + Länderspiele / 8).
    /// [APPROX] Einsatz 0x26 (wirkt über die Kondition): der Einsatz der Taktik beim Stand 0:0 gegen einen gleich starken
    /// Gegner; das Original liest den zuletzt gesetzten Wert.
    /// </summary>
    public int NationalMatchStrength(int nation)
    {
        var team = MatchEngine.NationalLiveTeam(this, nation, 0, 0, managerMatch: true);
        return OriginalMinutes.Strengths(team, team, 0).Normal;
    }
}
