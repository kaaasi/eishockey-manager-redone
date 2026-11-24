using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

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
        // B07: Text wie die Meldung des Originals („$ ist an $^interessiert.^“, Datei 0x19787), ohne Umbruch des Remakes.
        game.Notify(me, UnsolicitedOfferRules.MessageText(game.Clubs.First(c => c.Name == "EC HEDOS MÜNCHEN"), new Player { Name = "SCHIFFL", Role = Formats.PlayerRole.Forward }));
        var s = new Surface();
        s.Palette = d.Host.Assets.Palette;
        NewsBar.Draw(s, d.Host.Assets, game);

        // Datum oben (Zeilen 208–212, x 135–181 beim Datum „6. Dez. 1993“), Text weiß ab Zeile 220
        // Die Messung am Bild gibt die Versalhöhe (5 Zeilen); Umlaut und Punkte reichen eine Zeile höher: Grundlinie prüfen.
        Assert.Equal(212, Rows(s, 100, 220, 203, 216)!.Value.Last);
        Assert.Equal(224, Rows(s, 90, 225, 217, 226)!.Value.Last);
        Assert.Equal(231, Rows(s, 90, 225, 226, 234)!.Value.Last);
        // R14UR-07: Zeile 1 x 95–221, Zeile 2 x 133–183 (`file37a_1`), exakt
        Assert.Equal((95, 221), Cols(s, 90, 225, 217, 226)!.Value);
        Assert.Equal((133, 183), Cols(s, 90, 225, 227, 232)!.Value);
        Assert.Null(Rows(s, 90, 225, 232, 237)); // keine dritte Zeile
        Assert.Null(Rows(s, 285, 299, 218, 224)); // kein Zähler „1/1“
    }

    [GameDataFact]
    public void LongMessage_ShowsOnlyTwoLines_NothingOnTheBottomEdge()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        game.Finance(me).Inbox.Clear();
        var empty = new Surface();
        empty.Palette = d.Host.Assets.Palette;
        NewsBar.Draw(empty, d.Host.Assets, game); // Leiste ohne Nachricht: Hintergrund der unteren Zeilen
        game.Notify(me, "Ein Werbepartner mit Verlängerungsoption bittet Sie", "an den Verhandlungstisch, bevor die Saison beginnt und alles zu spät ist.");
        var s = new Surface();
        s.Palette = d.Host.Assets.Palette;
        NewsBar.Draw(s, d.Host.Assets, game);
        Assert.NotNull(Rows(s, 90, 225, 227, 232)); // zweite Zeile da
        // dritte Zeile (Grundlinie 238, Rand der Leiste y 234–236) wird nicht gezeichnet: Zeilen 234–236 wie ohne Nachricht (232–233 können Unterlängen der zweiten Zeile tragen)
        for (var y = 234; y <= 236; y++)
            for (var x = 3; x < 317; x++)
                Assert.True(empty.Pixels[y * Surface.Width + x] == s.Pixels[y * Surface.Width + x], $"Pixel ({x} | {y})");
    }

    private static Surface Draw(ScreenDriver d)
    {
        var s = new Surface();
        s.Palette = d.Host.Assets.Palette;
        NewsBar.Draw(s, d.Host.Assets, d.Host.Game!);
        return s;
    }

    /// <summary>
    /// B07 (A779): Das Original bricht nicht um – jede Zeile der Nachricht steht ungeteilt zentriert (Datei 0x4B6F0–0x4B76D,
    /// Trennung nur an „^“). Eine einzeilige Nachricht belegt nur die erste Textzeile. Vorher rot: Umbruch auf 128 Pixel
    /// verteilte sie auf zwei Zeilen.
    /// </summary>
    [GameDataFact]
    public void SingleLine_IsNotWrapped()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        game.Finance(me).Inbox.Clear();
        game.Notify(me, "EC HEDOS MÜNCHEN ist an SCHIFFL interessiert.");
        var s = Draw(d);
        var mini = d.Host.Assets.Mini;
        var width = mini.Measure("EC HEDOS MÜNCHEN ist an SCHIFFL interessiert.");
        var cols = Cols(s, 3, 299, 217, 226)!.Value;
        Assert.InRange(cols.Last - cols.First + 1, width - 2, width);
        Assert.Null(Rows(s, 20, 299, 227, 234)); // keine zweite Zeile
    }

    /// <summary>
    /// B07 (A778, A780): Kein Hinweis „(Klicken)“ bei einer Frage und kein Zähler „n/m“ bei mehreren Nachrichten – die Schleife
    /// Datei 0x4B6F0–0x4B76D zeichnet nur Datum und Zeilen (`file37a_1`, `file46_6`). Rechts neben den Zeilen (x 240–298) bleibt die
    /// Leiste wie ohne Nachricht. Vorher rot: Hinweis rechts unten, Zähler „2/2“.
    /// </summary>
    [GameDataFact]
    public void Question_WithSeveralMessages_ShowsNoHintAndNoCounter()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        game.Finance(me).Inbox.Clear();
        var empty = Draw(d);
        game.Notify(me, "Kurz.");
        game.Finance(me).Inbox.Add(new Message
        {
            Kind = MessageKind.LoanRequest, Day = game.Day, Lines = ["Kurz.", "Frage ?"], Borrower = 0, Amount = 10_000,
        });
        Assert.Equal(2, game.Inbox(me).Count);
        Assert.True(game.Inbox(me)[^1].IsQuestion);
        var s = Draw(d);
        for (var y = 202; y <= 236; y++)
            for (var x = 240; x < 299; x++)
                Assert.True(empty.Pixels[y * Surface.Width + x] == s.Pixels[y * Surface.Width + x], $"Pixel ({x} | {y})");
    }
}
