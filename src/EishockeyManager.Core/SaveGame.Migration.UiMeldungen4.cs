using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur ui-meldungen, Teil 4 (Schema 32 → 33, vorläufig; die Leitstelle nummeriert in der Integration):
/// Kennzeichen „neue Nachricht“ je Manager (<c>_newMessage</c>, DGROUP 0x5A50), Faktor der Gehaltsforderung des Zugs
/// (<c>_contractTurnFactor</c>, DGROUP 0xE1A4) und seine Zufallsquelle (<c>_turnRandom</c>).
/// [APPROX] Ältere Stände kennen das Kennzeichen nicht: nicht gesetzt, die Leiste erscheint mit der nächsten Nachricht (der
/// Klick auf den Managerkopf zeigt sie jederzeit). Der Faktor ist 0 (noch nicht gezogen, es gilt der frühere abgeleitete Wert
/// bis zum nächsten Managerzug); die Zufallsquelle fehlt (null) und beginnt beim ersten Zug.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateUiMeldungen4(JsonObject root)
    {
        if (root["spiel"] is JsonObject game)
        {
            if (!game.ContainsKey("_turnRandom"))
                game["_turnRandom"] = null;
            game["_contractTurnFactor"] ??= 0;
            SortFields(game);
        }
        if (root["manager"] is not JsonArray managers)
            return root;
        foreach (var manager in managers.OfType<JsonObject>())
        {
            manager["_newMessage"] ??= false;
            SortFields(manager);
        }
        return root;
    }
}
