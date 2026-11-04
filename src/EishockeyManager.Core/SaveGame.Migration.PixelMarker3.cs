using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur pixel-marker, Teil 3 (Runde 12; Schema 35 → 36, nummeriert von der Spur, die Leitstelle prüft in der
/// Integration): Nachrichtenspeicher wie im Original (R-K6, em.exe Datei 0x4B101/0x4B127/0x4B270; Entscheidung Nutzer D2 = C) und
/// Wegfall des Feldes „gelesen“ je Nachricht (<c>Message.Read</c>; D8 = A, #537).
/// <list type="bullet">
/// <item><c>Read</c> entfällt (das Original kennt es nicht; die Leiste richtet sich nach <c>Manager._newMessage</c>).</item>
/// <item><c>DaysLeft</c> (Restlaufzeit einer Nachricht ohne Spieler, Datei 0x54F07–0x54F87): [APPROX] ältere Nachrichten beginnen mit
/// 3 Tagen und altern danach wie neue.</item>
/// <item><c>Player</c> (Poolspieler der Nachricht): [APPROX] ältere Angebote kennen ihren Spieler nicht (null) und verhalten sich
/// wie Meldungen ohne Spieler; sie fallen nach drei Tagen aus dem Speicher.</item>
/// </list>
/// Ältere Posteingänge mit mehr als 24 Nachrichten bleiben unverändert; neue Nachrichten entfallen, bis der Speicher unter 24 sinkt.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigratePixelMarker3(JsonObject root)
    {
        Visit(root);
        return root;

        static void Visit(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject obj:
                    if (obj.ContainsKey("Read") && obj.ContainsKey("Lines") && obj.ContainsKey("Kind"))
                    {
                        obj.Remove("Read");
                        obj["DaysLeft"] ??= GameState.MessageLifetimeDays;
                        if (!obj.ContainsKey("Player"))
                            obj["Player"] = null;
                        SortFields(obj);
                    }
                    foreach (var child in obj.Select(f => f.Value).ToList())
                        Visit(child);
                    break;
                case JsonArray array:
                    foreach (var child in array.ToList())
                        Visit(child);
                    break;
            }
        }
    }
}
