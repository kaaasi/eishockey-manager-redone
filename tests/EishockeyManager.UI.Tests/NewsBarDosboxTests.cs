using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.ManagerArea;

namespace EishockeyManager.UI.Tests;

/// <summary>ui-rest Teil 2 (A321): Nachrichtenleiste nach DOSBox-Sitzung 2, <c>file37a_1</c> (HEDOS interessiert an SCHIFFL).</summary>
public class NewsBarDosboxTests
{
    private static (int First, int Last)? Rows(Surface s, int x0, int x1, int y0, int y1)
    {
        int? first = null, last = null;
        for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
                if (s.Pixels[y * Surface.Width + x] == 0)
                {
                    first ??= y;
                    last = y;
                }
        return first is null ? null : (first.Value, last!.Value);
    }

    private static (int First, int Last)? Cols(Surface s, int x0, int x1, int y0, int y1)
    {
        int? first = null, last = null;
        for (var x = x0; x < x1; x++)
            for (var y = y0; y < y1; y++)
                if (s.Pixels[y * Surface.Width + x] == 0)
                {
                    first ??= x;
                    last = x;
                }
        return first is null ? null : (first.Value, last!.Value);
    }

    [GameDataFact]
    public void Message_ShowsDateAndTwoCenteredLines()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        game.Finance(me).Inbox.Clear();
        game.Notify(me, "EC HEDOS MÜNCHEN ist an", "SCHIFFL interessiert.");
        var s = new Surface();
        s.Palette = d.Host.Assets.Palette;
        NewsBar.Draw(s, d.Host.Assets, game);

        // Datum oben (Zeilen 208–212, x 135–181 beim Datum „6. Dez. 1993“), Text weiß ab Zeile 220
        // Die Messung am Bild gibt die Versalhöhe (5 Zeilen); Umlaut und Punkte reichen eine Zeile höher: Grundlinie prüfen.
        Assert.Equal(212, Rows(s, 100, 220, 203, 216)!.Value.Last);
        Assert.Equal(224, Rows(s, 90, 225, 217, 226)!.Value.Last);
        Assert.Equal(231, Rows(s, 90, 225, 226, 234)!.Value.Last);
        var l2 = Cols(s, 90, 225, 227, 232)!.Value;
        Assert.InRange((l2.First + l2.Last) / 2, 156, 160);
        Assert.Null(Rows(s, 285, 299, 218, 224)); // kein Zähler „1/1“
    }
}
