using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.SaveLoad;

/// <summary>
/// Automatisches Speichern („Automatisch speichern“ in den Einstellungen).
/// </summary>
/// <remarks>
/// <para>
/// <b>Zeitpunkt [ORIGINAL]</b> (em.exe, Overlay-Funktion bei Dateioffset 0x44E22 – nach Deutung das Hauptmenü zu einem
/// Spieltag; sie liest <c>[DGROUP 0x828 + Tag]</c> und zeigt unter anderem die Meldung „Sie können nicht
/// mehr weiterspielen …“): Gleich beim Aufruf, vor jeder Eingabe des Tages, prüft sie den Byte-Zähler
/// <c>DGROUP 0x5148</c> (Anfangswert 1). Steht er auf 10, ruft sie die Speicherfunktion (<c>0140:005C</c>, dieselbe
/// wie für „ERROR.HOK“) mit dem Namen <c>autosave.hok</c> (<c>DGROUP 0x5231</c>) auf und setzt ihn auf 0. Danach zählt
/// er um 1 hoch, aber nur wenn der Manager am Zug <c>DGROUP 0xA70E == 0</c> ist (der erste Manager). Der Zähler ist
/// kein Teil des Spielstands. Folge: Der erste Spielstand entsteht beim 10. Hauptmenü-Aufruf des ersten Managers, danach
/// alle 10 Aufrufe; der Stand ist der des Tages, bevor der Manager etwas tut.
/// </para>
/// <para>
/// <b>Abbildung [APPROX]:</b> Ein „Hauptmenü-Aufruf“ ist hier jeder neue Zug (Saison, Tag, Manager) im Hauptmenü; die
/// Rückkehr aus einer Kachel zählt nicht. „Erster Manager“ ist der erste nicht entlassene Manager (das Original prüft
/// Nummer 0 und würde nach dessen Entlassung nicht mehr sichern). Die Datei heißt <c>AUTOSAVE.ems</c> im Spielstandordner.
/// Nicht gesichert wird, wenn die Saison oder das Spiel zu Ende ist (dort ist kein Hauptmenü, der Stand wäre nicht
/// spielbar). Ein Fehler beim Schreiben wird nicht gemeldet (Original unbekannt), nur in <see cref="SaveContext.LastAutoSaveError"/> festgehalten.
/// </para>
/// <para>
/// Gespeichert wird <b>nie während</b> <see cref="GameState.Continue"/>: Der Aufruf kommt aus dem Hauptmenü, also zwischen
/// zwei Aufrufen von <c>Continue()</c>, an einem Haltepunkt mit vollständigem Zustand. Der Spielzustand ändert sich dabei nicht.
/// </para>
/// </remarks>
public static class AutoSave
{
    /// <summary>Anfangswert des Zählers. [ORIGINAL] Byte in der Datei em.exe an DGROUP 0x5148.</summary>
    public const int InitialCounter = 1;

    /// <summary>Zählerstand, bei dem gespeichert wird. [ORIGINAL] <c>cmp byte [0x5148], 0xA</c>.</summary>
    public const int Interval = 10;

    /// <summary>
    /// Das Hauptmenü ist sichtbar geworden. Zählt den Zug und schreibt, wenn der Zähler bei <see cref="Interval"/> steht
    /// und <see cref="SaveContext.AutoSaveEnabled"/> gilt. Liefert <c>true</c>, wenn dabei eine Datei geschrieben wurde.
    /// </summary>
    public static bool OnMainMenu(ScreenHost host)
    {
        if (host.Game is not { } game)
            return false;
        var context = SaveContext.For(host);

        var turn = (game, game.Season, game.Day, game.CurrentManager);
        if (context.LastTurn == turn)
            return false; // derselbe Zug, z. B. zurück aus einer Kachel
        context.LastTurn = turn;

        var written = false;
        if (context.AutoSaveCounter == Interval)
        {
            written = context.AutoSaveEnabled && Write(context, game);
            context.AutoSaveCounter = 0;
        }
        if (IsFirstActiveManager(game))
            context.AutoSaveCounter++;
        return written;
    }

    private static bool IsFirstActiveManager(GameState game) =>
        game.CurrentManager == game.Managers.ToList().FindIndex(m => !m.Dismissed);

    private static bool Write(SaveContext context, GameState game)
    {
        context.LastAutoSaveError = null;
        if (context.Directory is not { } directory)
            return false;
        if (game.Phase is SeasonPhase.SeasonOver or SeasonPhase.GameOver)
            return false;
        try
        {
            var data = SaveGame.Save(game);
            SaveFiles.WriteAtomic(SaveFiles.PathOf(directory, SaveFiles.AutoSaveBaseName), data);
            context.AutoSaveWrites++;
            return true;
        }
        catch (Exception e) when (e is IOException or InvalidOperationException)
        {
            context.LastAutoSaveError = e.Message;
            return false;
        }
    }
}
