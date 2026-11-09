namespace EishockeyManager.Core;

/// <summary>
/// Tageszähler DGROUP 0x3B7/0x3B9 des Originals zu einem Tag des Remakes (Review #674, R14NT4-01/-03, #675).
/// </summary>
/// <remarks>
/// [ORIGINAL] Spiel- und Saisonstart setzen den Zähler auf [0x6AF] (Datei 0x43A93, 0x565A7). [0x6AF] ist 0: DGROUP-Startwert
/// (Datei 0x16DD0 + 0x6AF), Leser nur Datei 0x07C3B, 0x43A93 und 0x565A7, kein Schreiber. Das Original zählt nach den Spielen
/// jedes Tages weiter (Hauptweg Datei 0x55204, WM-Schleife Datei 0x55670, Modus 1 bis zur WM Datei 0x54CF7); im Menü und bei den
/// Spielen von Tag d steht er auf d + 1. Belege (<c>emtool hok dump … 03B7</c>, Referenz-Repo <c>original/spielstaende/</c>):
/// FILE01 10.09.1993 (Tag 0) = 1, FILE24 24.09. (Tag 14) = 15, FILE25 27.09. (Tag 17) = 18, FILE13 31.01.1994 (Tag 143) = 144,
/// FILE18 14.04. (Tag 216) = 217, FILE48 16.04. = 219, FILE20 28.04. (Tag 230) = 231, FILE14 10.09.1994 (neue Saison) = 1.
/// <para>
/// Was das Original nach dem Weiterzählen prüft (Talentsuche, kleine Wanderung, Tagesablauf 0x07169 im Hauptweg Datei
/// 0x55253–0x552D3; Heilung und Stadionbau in Schritt (d) der WM-Schleife), rechnet das Remake im Tageslauf vor den Spielen des
/// Folgetags (<c>OnDayManager(e)</c>), also mit dem Zähler <see cref="Of"/>(e) = e + 1. Schritt (c) der WM-Schleife prüft vor dem
/// Weiterzählen den Zähler der Spiele des Vortags e − 1, also <see cref="Of"/>(e − 1) = e.
/// </para>
/// <para>
/// Ausnahme: Nach dem HOK-Import eines im Menü des Markierungstags gesicherten Stands steht der Zähler bis zum Saisonwechsel einen
/// höher (Ladeeigenheit, <c>GameState.LoadCounterOffset</c>, <c>GameState.LoadQuirk.cs</c>); die Spielregeln fragen deshalb
/// <c>GameState.Counter</c>.
/// </para>
/// </remarks>
public static class DayCounter
{
    /// <summary>Zählerstand DGROUP 0x3B7 im Menü und bei den Spielen des Remake-Tags <paramref name="day"/>.</summary>
    public static int Of(int day) => day + 1;
}
