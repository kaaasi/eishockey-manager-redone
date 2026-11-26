namespace EishockeyManager.Core;

/// <summary>
/// Schaden einer Woche am Stadion: ein Ausbaubereich eine Stufe tiefer bzw. <see cref="Places"/> Sitz- oder Stehplätze gesperrt.
/// <see cref="Project"/> = Projektnummer 0–10 wie im Original (0–8 Bereiche, 9 Sitz-, 10 Stehplätze).
/// </summary>
public sealed record StadiumDamage(int Project, int Places)
{
    /// <summary>
    /// Meldung (zwei Zeilen). [ORIGINAL] Texte DGROUP 0xD10–0xD2E, 0x4F19 (Endung „r“/„m“), Bereichsnamen 0x484, Platznamen 0x4A8,
    /// Zahl über 0x0BAB2 mit DGROUP 0x38D = 0 (Tausenderpunkt), Datei 0x3D0C4–0x3D141, 0x3D1CF–0x3D1ED, 0x3D27A–0x3D303
    /// (tools/reverse-engineering/tagesabschluss/stadionschaeden.py Fall 8). [APPROX] Die Mindestbreite DGROUP 0x374 der Zahlenausgabe
    /// (fehlende Stellen als „%“) hängt vom zuletzt gezeichneten Bildschirm ab und ist hier nicht nachgelesen; die Zahl steht ohne Auffüllen.
    /// </summary>
    public string[] Lines => Project switch
    {
        Stadium.SeatsProject or Stadium.StandingProject =>
        [
            "Der DEB hat die Sicherheit Ihres Stadions gerügt !",
            $"{Places.ToString("#,0", StadiumDamageRules.Thousands)} {(Project == Stadium.SeatsProject ? "Sitzplätze" : "Stehplätze")} wurden gesperrt.",
        ],
        (int)StadiumFeature.Sitzkomfort => ["Ihre Fans sind einfach zu stürmisch...", "Der Sitzkomfort muß leider etwas schlechter beurteilt werden."],
        (int)StadiumFeature.Zustand => ["Ihre Fans sind einfach zu stürmisch...", "Das schlägt sich auch auf den Allgemeinzustand des Stadions nieder."],
        _ =>
        [
            "Der Zahn der Zeit nagt auch an IHREM Stadion...",
            $"Er knabberte etwas an Ihre{((StadiumFeature)Project == StadiumFeature.Kino ? "m" : "r")} {Stadium.Features[Project].Label}.",
        ],
    };
}

/// <summary>Wöchentliche Stadionschäden des Originals (em.exe Datei 0x3CFA0, Spur tagesabschluss Teil 2, R26-E1 = B).</summary>
public static class StadiumDamageRules
{
    internal static readonly System.Globalization.NumberFormatInfo Thousands = new() { NumberGroupSeparator = "." };

    /// <summary>
    /// Würfelt den Schaden einer Woche und trägt ihn ins Stadion ein; null ohne Schaden. <paramref name="levelValue"/> = „5 − Level“
    /// (DGROUP 0xED8), <paramref name="league"/> = Liga-Stufe des Managers (DGROUP 0x9746 + 0x1AF · m, 0 = Bundesliga).
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] Datei 0x3CFA0–0x3D315 (Aufruf zuerst in 0x3D316, Datei 0x3D330; Nachbildung
    /// <c>tools/reverse-engineering/tagesabschluss/stadionschaeden.py</c>), alle Würfe random(a, b) mit beiden Grenzen:
    /// <list type="number">
    /// <item>random(0, 2·d + 8) ≠ 0 → kein Schaden (Datei 0x3CFC5, d = „5 − Level“).</item>
    /// <item>Sonst je Projekt p = 0 … 10 der Reihe nach: Chancenwurf random(0, N) &lt; 20 · (5 − d) mit N = 5000 für die Bereiche
    ///   (Datei 0x3D21E), 2500 für Sitzkomfort und Zustand (p = 3, 4; Datei 0x3D173), 3000 für die Plätze (p = 9, 10; Datei
    ///   0x3D003). Kühlanlage und Verkehrsanbindung (p = 2, 5) nur bei d = 0, sonst ohne Wurf (Datei 0x3D1F0–0x3D203).</item>
    /// <item>Trifft der Chancenwurf, Ligawurf random(1, 3) &gt; Liga (Datei 0x3D247/0x3D19C/0x3D02C), erst danach die Prüfung Stufe
    ///   bzw. Plätze ≠ 0; scheitert eins, geht es mit dem nächsten Projekt weiter.</item>
    /// <item>Bereich: Stufe − 1. Plätze: random(Plätze / 20, Plätze / 6) (Datei 0x3D083), gesperrt ((r + 150) / 100) · 100.
    ///   Dann Meldung und Ende – höchstens ein Schaden je Woche (Datei 0x3D149, 0x3D1ED, 0x3D303 → 0x3D312).</item>
    /// </list>
    /// Kosten bucht der Schaden nicht; eine laufende Baustelle setzt bei Fertigstellung die Zielstufe bzw. Zielplätze (0x3D316).
    /// </remarks>
    public static StadiumDamage? Draw(Stadium stadium, Random random, int levelValue, int league)
    {
        ArgumentNullException.ThrowIfNull(stadium);
        if (SquadSetup.Between(random, 0, 2 * levelValue + 8) != 0)
            return null;
        var chance = 20 * (5 - levelValue);
        for (var p = 0; p < Stadium.ProjectCount; p++)
        {
            if (p is 2 or 5 && levelValue != 0)
                continue;
            var range = p switch
            {
                >= Stadium.SeatsProject => 3000,
                3 or 4 => 2500,
                _ => 5000,
            };
            if (SquadSetup.Between(random, 0, range) >= chance)
                continue;
            if (SquadSetup.Between(random, 1, 3) <= league)
                continue;
            if (p < Stadium.FeatureCount)
            {
                var feature = (StadiumFeature)p;
                if (stadium.Level(feature) == 0)
                    continue;
                stadium.SetLevel(feature, stadium.Level(feature) - 1);
                return new StadiumDamage(p, 0);
            }
            var places = p == Stadium.SeatsProject ? stadium.Seats : stadium.Standing;
            if (places == 0)
                continue;
            var closed = (SquadSetup.Between(random, places / 20, places / 6) + 150) / 100 * 100;
            // [APPROX] Das Original zieht im 16-Bit-Wort ab (Datei 0x3D0B1–0x3D0C0) und liefe unter 100 Plätzen über; das Remake
            // hält bei 0 (erst nach sehr vielen Schäden ohne Ausbau erreichbar).
            var left = Math.Max(0, places - closed);
            if (p == Stadium.SeatsProject)
                stadium.Seats = left;
            else
                stadium.Standing = left;
            return new StadiumDamage(p, closed);
        }
        return null;
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Stadionschäden der Woche eines Managers (Datei 0x3CFA0) mit Meldung, zu Beginn seines Stadionbaus 0x3D316 (Datei 0x3D330).
    /// </summary>
    private void WeeklyStadiumDamage(Manager manager)
    {
        var damage = StadiumDamageRules.Draw(Finance(manager).Stadium, Random, TrainingRules.LevelValue(manager.Level),
            (int)Finance(manager).CostLeague);
        if (damage is not null)
            Notify(manager, damage.Lines);
    }
}
