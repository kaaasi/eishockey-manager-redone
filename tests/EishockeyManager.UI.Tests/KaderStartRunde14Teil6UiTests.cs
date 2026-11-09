using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur kader-start Runde 14 Teil 6 (E14-7): Vereinsaufstellung wie im Original (derselbe Bildschirm wie beim Nationalteam,
/// em.exe Datei 0x40364). A364: Spalte NR nur bei eingesetzten Spielern (Datei 0x42027–0x42031, DOSBox <c>file34_3</c>).
/// Reihenlogik wie A010: Auswahl nach Reihe (Datei 0x40890–0x40943), Klick auf einen Spieler der Liste (Datei 0x40D8D–0x414C6),
/// Klick auf einen Platz (Datei 0x40AA3–0x40D65), Rechtsklick außerhalb der Liste (Datei 0x40D6B–0x40D78).
/// </summary>
public class KaderStartRunde14Teil6UiTests
{
    private static (ScreenDriver Driver, LineupScreen Screen, Club Club) Open()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        var screen = Assert.IsType<LineupScreen>(d.Host.Current);
        var club = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex];
        club.AutoLineupEnabled = false;
        return (d, screen, club);
    }

    private static int RowY(Club club, Player p) =>
        LineupScreen.FirstRowY + TeamUi.Rows(club).ToList().IndexOf(p) * LineupScreen.RowPitch + 2;

    /// <summary>Linksklick auf eine Zeile (Namensspalte).</summary>
    private static void ClickRow(ScreenDriver d, Club club, Player p) => d.Click(60, RowY(club, p));

    /// <summary>Rechtsklick auf eine Zeile außerhalb der Namensspalte (dort öffnet er die Spielerdaten, Datei 0x40855–0x40872).</summary>
    private static void RightClickRow(ScreenDriver d, Club club, Player p) => d.RightClick(150, RowY(club, p));

    /// <summary>Klick in den Block bzw. Torhüterkasten neben die Platzknöpfe: wählt nur die Reihe.</summary>
    private static void SelectLine(ScreenDriver d, int line)
    {
        var box = line == NationalCoach.GoalkeeperLine ? LineupScreen.GoalieBox : LineupScreen.BlockBox(line);
        d.Click(box.X + 3, box.Y + 5);
    }

    private static void ClickSlot(ScreenDriver d, LineupSlot slot)
    {
        var b = LineupScreen.SlotButton(slot);
        d.Click(b.X + 5, b.Y + 5);
    }

    private static Player Reserve(Club club, PlayerRole role) =>
        TeamUi.Rows(club).First(p => p.Role == role && p.Available && club.IsDressed(p) && !club.Lineup.Contains(p));

    /// <summary>Pixel einer Farbe in der Spalte NR (rechtsbündig bis x = 17) einer Listenzeile.</summary>
    private static int NumberPixels(ScreenDriver d, Club club, Player p)
    {
        d.Move(0, 0);
        var y0 = RowY(club, p) - 2;
        var color = TeamUi.RoleColor(p.Role);
        var count = 0;
        for (var y = y0; y < y0 + LineupScreen.RowPitch - 1; y++)
            for (var x = LineupScreen.ListLeft + 2; x <= 17; x++)
                if (d.Surface.Pixels[y * Surface.Width + x] == color)
                    count++;
        return count;
    }

    /// <summary>
    /// [ORIGINAL] A364: NR steht nur bei eingesetzten Spielern (Datei 0x42027: +0x0A = 0 → Spalte leer; DOSBox <c>file34_3</c>:
    /// J.SCHULZ, DOBRYZINSKI, SANDNER ohne Nummer). Vorher zeigte das Remake die Rückennummer immer.
    /// </summary>
    [GameDataFact]
    public void A364_NumberOnlyForDressedPlayers()
    {
        var (d, _, club) = Open();
        var reserve = Reserve(club, PlayerRole.Defender);
        Assert.True(NumberPixels(d, club, reserve) > 0);

        Assert.Equal(LineupResult.Undressed, club.Undress(reserve));
        Assert.Equal(0, NumberPixels(d, club, reserve));
        var placed = club.Lineup[LineupSlot.Forward(0, 0)]!;
        Assert.True(NumberPixels(d, club, placed) > 0);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x40E77: Linksklick auf einen nicht Aufgestellten ohne gewählte Reihe → „Keine Reihe / ausgewählt !“,
    /// nichts ändert sich. Vorher nahm der Linksklick einen Reservisten heraus.
    /// </summary>
    [GameDataFact]
    public void LeftClickOnReserve_WithoutLine_ShowsNoLineSelected()
    {
        var (d, screen, club) = Open();
        var reserve = Reserve(club, PlayerRole.Defender);
        ClickRow(d, club, reserve);
        Assert.Equal(["Keine Reihe", "ausgewählt !"], screen.MessageLines);
        Assert.True(club.IsDressed(reserve));
        Assert.False(club.Lineup.Contains(reserve));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x40890–0x40943: Klick in einen Block wählt die Reihe; Datei 0x412A1–0x415B2: Linksklick setzt den Spieler
    /// auf den ersten freien Platz seiner Art in der Reihe und setzt ihn ein.
    /// </summary>
    [GameDataFact]
    public void SelectLine_ThenLeftClick_PlacesInFirstFreeSlotOfLine()
    {
        var (d, screen, club) = Open();
        var p = Reserve(club, PlayerRole.Forward); // eingesetzt, nicht aufgestellt: Datei 0x41287 setzt ihn auf einen Platz
        SelectLine(d, 3);
        Assert.Equal(3, screen.SelectedLine);
        ClickRow(d, club, p);
        Assert.Same(p, club.Lineup[LineupSlot.Forward(3, 0)]);
        Assert.True(club.IsDressed(p));
    }

    /// <summary>[ORIGINAL] Datei 0x414BC–0x414C6: Reihe voll → „Kein / Platz mehr !“, niemand wird verdrängt.</summary>
    [GameDataFact]
    public void FullLine_ShowsNoRoom_AndChangesNothing()
    {
        var (d, screen, club) = Open();
        var p = Reserve(club, PlayerRole.Defender);
        var before = LineupSlot.All.Select(s => club.Lineup[s]).ToList();
        SelectLine(d, 0);
        ClickRow(d, club, p);
        Assert.Equal([ShortMessageTexts.NoRoom.Line1, ShortMessageTexts.NoRoom.Line2], screen.MessageLines);
        Assert.Equal(before, LineupSlot.All.Select(s => club.Lineup[s]).ToList());
        Assert.True(club.IsDressed(p));
    }

    /// <summary>[ORIGINAL] Datei 0x40E8F–0x40EB9: Torhüter in einen Feldblock → „Der darf da / nicht rein !“.</summary>
    [GameDataFact]
    public void GoalkeeperIntoFieldLine_IsRejected()
    {
        var (d, screen, club) = Open();
        var gk = TeamUi.Rows(club).First(p => p.Role == PlayerRole.Goalkeeper && !club.Lineup.Contains(p));
        SelectLine(d, 3);
        ClickRow(d, club, gk);
        Assert.Equal(["Der darf da", "nicht rein !"], screen.MessageLines);
        Assert.False(club.Lineup.Contains(gk));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x40F7C–0x411FE: Klick auf einen Aufgestellten nimmt ihn heraus und wählt seine Reihe; Linksklick: nicht
    /// mehr eingesetzt (Datei 0x4116F), Rechtsklick: weiter eingesetzt (Datei 0x41208).
    /// </summary>
    [GameDataFact]
    public void ClickOnPlacedPlayer_TakesOut_LeftUndresses_RightKeepsDressed()
    {
        var (d, screen, club) = Open();
        var left = club.Lineup[LineupSlot.Forward(1, 0)]!;
        ClickRow(d, club, left);
        Assert.False(club.Lineup.Contains(left));
        Assert.False(club.IsDressed(left));
        Assert.Equal(1, screen.SelectedLine);

        var right = club.Lineup[LineupSlot.Defender(2, 1)]!;
        RightClickRow(d, club, right);
        Assert.False(club.Lineup.Contains(right));
        Assert.True(club.IsDressed(right));
        Assert.Equal(2, screen.SelectedLine);
    }

    /// <summary>[ORIGINAL] Datei 0x40F45–0x40F59 und 0x411FE–0x41208: Rechtsklick nimmt einen Reservisten heraus bzw. setzt ein.</summary>
    [GameDataFact]
    public void RightClick_TogglesDressedForUnplaced()
    {
        var (d, _, club) = Open();
        var reserve = Reserve(club, PlayerRole.Forward);
        RightClickRow(d, club, reserve);
        Assert.False(club.IsDressed(reserve));
        RightClickRow(d, club, reserve);
        Assert.True(club.IsDressed(reserve));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x40AA3–0x40D0D: Linksklick auf einen belegten Platz leert ihn (nicht mehr eingesetzt) und wählt die Reihe
    /// (Datei 0x40890). Datei 0x40D6B–0x40D78: Rechtsklick außerhalb der Liste – auch auf einen Platz – verlässt den Bildschirm.
    /// </summary>
    [GameDataFact]
    public void Slot_LeftClickClears_RightClickLeaves()
    {
        var (d, screen, club) = Open();
        var slot = LineupSlot.Defender(1, 0);
        var p = club.Lineup[slot]!;
        ClickSlot(d, slot);
        Assert.Null(club.Lineup[slot]);
        Assert.False(club.IsDressed(p));
        Assert.Equal(1, screen.SelectedLine);

        var other = LineupScreen.SlotButton(LineupSlot.Forward(0, 0));
        var placed = club.Lineup[LineupSlot.Forward(0, 0)];
        d.RightClick(other.X + 5, other.Y + 5);
        Assert.IsNotType<LineupScreen>(d.Host.Current);
        Assert.Same(placed, club.Lineup[LineupSlot.Forward(0, 0)]);
    }
}
