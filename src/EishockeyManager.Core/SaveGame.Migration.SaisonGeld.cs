using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur saison-geld (Runde 25, Aufräumkonzept K11; Schema 64 → 65): <c>Player._roleValue</c> (Rollenwert Pool +0x18,
/// <see cref="Player.RoleValue"/>). [APPROX] Ältere Stände kennen den gewürfelten Wert nicht: <see cref="Player.NoRoleValue"/>, es gilt
/// die Mitte der Spanne je Art (0 / 33 / 83, wie bisher die Torwürfe); ein neuer Poolaufbau würfelt ihn neu.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateSaisonGeld(JsonObject root)
    {
        if (root["spieler"] is JsonArray players)
        {
            foreach (var player in players.OfType<JsonObject>())
            {
                player["_roleValue"] ??= Player.NoRoleValue;
                SortFields(player);
            }
        }
        return root;
    }
}
