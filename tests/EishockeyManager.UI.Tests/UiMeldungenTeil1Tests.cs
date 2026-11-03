using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Runde 12, Spur <c>ui-meldungen</c> Teil 1: E3 (Entscheidung Nutzer 03.11.2026, C) – das Meldungsfenster schließt wie im Original
/// nur per Linksklick (em.exe Datei 0x4C246 wertet nur Mausereignis 1 aus, <c>docs/analyse/re-meldungen.md</c> §C); UM-3 Rest –
/// Meldungen mit mehr als vier Zeilen erscheinen im Fenster des Originals statt im früheren Rahmen.
/// Gegenbeispiele (vorher): Enter, Escape, Texteingabe und rechte Maustaste schlossen die Meldung; fünf Zeilen zeigten den alten
/// orangefarbenen Rahmen.
/// </summary>
public class UiMeldungenTeil1Tests
{
    private static readonly string[] Five = ["Achtung !", "Der Spielstand", "lässt sich nicht", "sichern.", "Vorgang abgebrochen."];

    public static TheoryData<string> NonLeftInputs => new() { "Enter", "Escape", "Text", "Rechtsklick" };

    private static InputState Input(string kind)
    {
        var input = new InputState { MouseX = 160, MouseY = 140 };
        switch (kind)
        {
            case "Enter":
                input.Keys.Add(Key.Enter);
                break;
            case "Escape":
                input.Keys.Add(Key.Escape);
                break;
            case "Text":
                input.Text = "j";
                break;
            default:
                input.RightClick = true;
                break;
        }
        return input;
    }

    [Theory]
    [MemberData(nameof(NonLeftInputs))]
    public void Message_WithButtons_IgnoresKeysAndRightClick(string kind)
    {
        var box = new MessageBox();
        box.Show("Die Firma", "Acme", "wird den Vertrag", "nicht verlängern.");
        Assert.True(box.Update(Input(kind))); // Eingabe verbraucht, damit der Bildschirm darunter nichts tut
        Assert.True(box.Visible);
        Assert.True(box.Update(new InputState { MouseX = 160, MouseY = 140, LeftClick = true }));
        Assert.False(box.Visible);
    }

    [Theory]
    [MemberData(nameof(NonLeftInputs))]
    public void Message_WithoutButtons_IgnoresKeysAndRightClick(string kind)
    {
        var box = new MessageBox();
        box.Show("Hans", "ist verletzt.");
        Assert.True(box.Update(Input(kind)));
        Assert.True(box.Visible);
    }

    [Theory]
    [MemberData(nameof(NonLeftInputs))]
    public void Message_MoreThanFourLines_IgnoresKeysAndRightClick(string kind)
    {
        var box = new MessageBox();
        box.Show(Five);
        Assert.True(box.Update(Input(kind)));
        Assert.Equal(Five[..4], box.Lines);
    }

    [Fact]
    public void Message_MoreThanFourLines_ShowsPagesOfFourLines_EachClosedByLeftClick()
    {
        var box = new MessageBox();
        box.Show(Five);
        Assert.Equal(Five[..4], box.Lines);
        Assert.True(box.Update(new InputState { MouseX = 5, MouseY = 5, LeftClick = true }));
        Assert.True(box.Visible);
        Assert.Equal(Five[4..], box.Lines);
        Assert.True(box.Update(new InputState { MouseX = 5, MouseY = 5, LeftClick = true }));
        Assert.False(box.Visible);
    }

    [GameDataFact]
    public void Message_MoreThanFourLines_DrawsOriginalWindow()
    {
        var assets = new Assets(TestGameData.Load());
        var five = new MessageBox(() => "Hans");
        five.Show(Five);
        var four = new MessageBox(() => "Hans");
        four.Show(Five[..4]);
        var a = new Surface();
        var b = new Surface();
        five.Draw(a, assets);
        four.Draw(b, assets);
        Assert.Equal(b.Pixels, a.Pixels); // erste Seite = Fenster 0x4C246 mit den ersten vier Zeilen
        Assert.NotEqual(0, a.Pixels[OriginalDialog.Window.Y * Surface.Width + OriginalDialog.Window.X]);
    }

    [Fact]
    public void Message_ButtonText_IsNaGutFromWrapper0x4BCCC()
    {
        // [ORIGINAL] Datei 0x4BCCC (Stub 0x120:0x4D) ruft 0x4C246 mit Zeile 5 = DGROUP 0x12D5 „Na gut.“ und Modus 0.
        Assert.Equal("Na gut.", MessageBox.ButtonText);
    }
}
