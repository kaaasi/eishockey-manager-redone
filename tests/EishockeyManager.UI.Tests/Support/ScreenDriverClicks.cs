using EishockeyManager.UI;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tests.Support;

/// <summary>Gemeinsame Klickhilfe der UI-Tests (Aufräumkonzept U9, vorher siebenmal kopiert).</summary>
public static class ScreenDriverClicks
{
    /// <summary>Klickt in die Mitte des Rechtecks.</summary>
    public static void Click(ScreenDriver d, Rect r) => d.Click(r.X + r.W / 2, r.Y + r.H / 2);
}
