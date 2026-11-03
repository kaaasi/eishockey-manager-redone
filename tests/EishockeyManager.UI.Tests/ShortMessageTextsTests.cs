using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur ui-meldungen Teil 3 (UM-4): Bausteine der Kurzmeldungen 0x0C0B8, deren Aufrufer in fremden Bildschirmen sitzen
/// (<c>docs/analyse/re-runde-12.md</c> „UM-4“). Vorher gab es die Texte im Remake nicht als Kurzmeldung.
/// </summary>
public class ShortMessageTextsTests
{
    public static TheoryData<string, string, string> Texts => new()
    {
        { nameof(ShortMessageTexts.BuilderOffended), "Die Baufirma", "ist noch beleidigt..." },   // Datei 0x3DE9F
        { nameof(ShortMessageTexts.PlayerRetires), "Dieser Spieler hört", "am Saisonende auf." }, // Datei 0x46DEF
        { nameof(ShortMessageTexts.WrongVersion), "Spielstand aus", "falscher Version !" },       // Datei 0x5C559
        { nameof(ShortMessageTexts.SixSkaters), "Schon 6 Feldspieler", "auf dem Eis !" },        // Datei 0x40EF7
        { nameof(ShortMessageTexts.NoRoom), "Kein", "Platz mehr !" },                            // Datei 0x414C6
    };

    private static ShortMessageText Get(string name) =>
        (ShortMessageText)typeof(ShortMessageTexts).GetField(name)!.GetValue(null)!;

    /// <summary>[ORIGINAL] Genau diese Zeilen, kein Knopf, keine Eingabe, schließt nach 75 Takten (25 BIOS-Ticks) von selbst.</summary>
    [Theory]
    [MemberData(nameof(Texts))]
    public void Message_HasTheOriginalLines_AndClosesByItself(string name, string line1, string line2)
    {
        var text = Get(name);
        Assert.Equal(new ShortMessageText(line1, line2), text);
        var message = text.Create("LetsShow");
        Assert.Equal(line1, message.Line1);
        Assert.Equal(line2, message.Line2);
        Assert.Equal(75, message.Mode);
        var frames = 0;
        // Klicks und Tasten schließen nicht (Modus ≥ 2, keine Eingabe).
        while (message.Update(new InputState { LeftClick = true, RightClick = true, Keys = { Key.Escape } }))
            frames++;
        Assert.Equal(ShortMessage.FramesFor(75), frames);
        Assert.True(message.Closed);
    }

    /// <summary>[ORIGINAL] Bedingung 0x46DEF: nur ein angekündigter Ruhestand (Bit 0x80), nicht die bloße Absage.</summary>
    [Fact]
    public void AnnouncedRetirement_OnlyForRetiringNotice()
    {
        var player = new Player { Name = "TEST", Role = PlayerRole.Goalkeeper };
        foreach (var notice in Enum.GetValues<ContractNotice>())
        {
            player.Notice = notice;
            Assert.Equal(notice == ContractNotice.Retiring, ShortMessageTexts.AnnouncedRetirement(player));
        }
    }

    /// <summary>[ORIGINAL] Bedingung 0x3DE9F liest nur den Zustand der Baufirma (neues Stadion: nicht beleidigt).</summary>
    [Fact]
    public void BuilderOffended_ReadsTheStadium()
    {
        var stadium = new Stadium();
        Assert.False(ShortMessageTexts.IsBuilderOffended(stadium, StadiumProject.Sitzplaetze, default));
    }
}
