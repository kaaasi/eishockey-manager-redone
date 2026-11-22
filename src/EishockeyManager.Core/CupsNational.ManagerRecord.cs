namespace EishockeyManager.Core;

/// <summary>
/// Managersatz des Bundestrainers (Manager 5, DGROUP 0x965B + 5 · 0x1AF) für die Minutenrechnung (Spur national, Teil 1;
/// A424, A097).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Das Land des Bundestrainers hat Vereinsfeld 0x16 = 6 (Manager 5 + 1, Datei 0x55494). Die Zähler der Über- und
///   Unterzahl schreibt die Minutenrechnung in den Satz 0x94AC + 0x1AF · Vereinsfeld 0x16 (<c>docs/spielberechnung.md</c> §18),
///   für das Land also in DGROUP 0x97D2 + 5 · 0x1AF = Satz des Bundestrainers. Der Satz 5 hat dieselbe Form wie die Sätze der
///   Vereinsmanager (<see cref="Manager.PowerPlayCounts"/>).</item>
/// <item>[ORIGINAL] Der Managerwert DGROUP 0x974B + 5 · 0x1AF entsteht wie bei den Vereinsmanagern in 0x952C mit Manager 5
///   (Tageslauf Datei 0x54F9C–0x54FBF, an WM-Tagen nach dem Menü Datei 0x555D6) aus der Aufstellung des Bundestrainers
///   (<see cref="MatchEngine.ManagedTeamValues"/>); die Verletzungen im Spiel 0x38B8F lesen ihn für Satz 5, solange der Verein
///   des Bundestrainers DGROUP 0x9EDA ≤ 0x3F ist (Datei 0x38BE3–0x38BF0) – also mit Amt (Land 54), ohne Amt steht dort 0xFD
///   (Datei 0x5580E).</item>
/// <item>[APPROX] Ein neues Amt beginnt mit leeren Zählern; das Remake legt den Bundestrainer je Saison neu an. [ORIGINAL] Das
///   Original leert den Satz 5 weder bei der Ernennung noch beim Saisonwechsel: FILE19 <c>1,0,0,1,3,5</c> → FILE20
///   <c>1,0,0,90,3,5</c> → FILE21 (14.04.1995, neues Amt, Kader leer) <c>1,0,0,90,3,5</c> (Review #937 P3-1, #938). Zurückgestellt
///   (Leitstelle 22.11.2026): Die Zähler werden nirgends gelesen und nicht exportiert; ein Übertrag über den Saisonwechsel
///   (wie <c>_nationalAutoLineupSaved</c>) braucht ein Spielstandfeld samt Schema und gehört zum Export der Zähler (Spur
///   hok-export, Restlücke).</item>
/// </list>
/// </remarks>
public sealed partial class NationalCoach
{
    /// <summary>[ORIGINAL] Satz des Bundestrainers in den Verletzungen 0x38B8F (Schleife Datei 0x38BC0–0x38BF0: Manager 0 … 5).</summary>
    public const int ManagerRecord = 5;

    /// <summary>Über-/Unterzahl-Zähler des Satzes 5 (DGROUP 0x97D2 + 5 · 0x1AF, je Art ein Wort; <see cref="PowerPlay"/>).</summary>
    public int[] PowerPlayCounts { get; private set; } = new int[PowerPlay.Count];
}
