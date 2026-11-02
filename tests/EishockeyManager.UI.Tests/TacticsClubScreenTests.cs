using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur taktik-verein (Befunde aus #103) an den Vereinsbildschirmen mit Originaldaten: Farbe von „Eingesetzt“,
/// Grenze von 22 Eingesetzten, Taktik nur bei „SEMI“ änderbar.
/// </summary>
public class TacticsClubScreenTests
{
    private static int Count(Surface s, int x0, int y0, int x1, int y1, Func<byte, bool> match)
    {
        var n = 0;
        for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
                if (match(s.Pixels[y * Surface.Width + x]))
                    n++;
        return n;
    }

    private static (Testing.ScreenDriver Driver, GameState Game, Club Club) Start()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        return (d, game, game.Clubs[game.ActiveManager.ClubIndex]);
    }

    /// <summary>Regressionstest (b): „Eingesetzt: n“ in Farbe 16 (Datei 0x41D84), nicht 17.</summary>
    [GameDataFact]
    public void Lineup_DressedCount_InColour16()
    {
        var (d, _, _) = Start();
        d.OpenTile("Aufstellung");
        Assert.IsType<LineupScreen>(d.Host.Current);
        d.Move(300, 235);
        var s = d.Surface;
        Assert.True(Count(s, 250, 186, 316, 196, c => c == 16) > 20);
        Assert.Equal(0, Count(s, 250, 186, 316, 196, c => c == 17));
    }

    /// <summary>
    /// Regressionstest (c): Bei 22 Eingesetzten weist die Vereinsaufstellung einen weiteren ab („Schon 22 Mann / im
    /// Team !“); nach Herausnehmen eines Reservisten (Linksklick) geht es.
    /// </summary>
    [GameDataFact]
    public void Lineup_RefusesTwentyThirdDressed()
    {
        var (d, game, club) = Start();
        foreach (var donor in game.Clubs.Where(c => c != club && c.Players.Count > 0))
        {
            foreach (var p in donor.Players.Where(p => p.Available && p.Role == PlayerRole.Forward).ToList())
            {
                if (club.Players.Count >= 27)
                    break;
                donor.Lineup.Remove(p);
                donor.Players.Remove(p);
                club.Players.Add(p);
            }
        }
        // Teil 8 (A068): neu aufstellen wie nach dem Transferbildschirm (Datei 0x465F1 ruft 0x426BC); die Eingesetzten
        // der automatischen Aufstellung stehen fest und wachsen nicht von selbst mit dem Kader.
        club.ApplyAutoLineup();
        d.OpenTile("Aufstellung");
        var rows = club.SortedPlayers.ToList();
        int RowY(Player p) => LineupScreen.FirstRowY + rows.IndexOf(p) * LineupScreen.RowPitch + 2;

        // Ein Spieler außerhalb der 22 Eingesetzten der automatischen Aufstellung (0x426BC).
        Assert.Equal(Club.MaxDressed, club.DressedCount);
        var outside = club.Players.First(p => p.Available && !club.IsDressed(p) && p.Role != PlayerRole.Goalkeeper);
        var slot = outside.Role == PlayerRole.Forward ? LineupSlot.Forward(3, 0) : LineupSlot.Defender(3, 0);
        var button = LineupScreen.SlotButton(slot);
        Assert.Null(club.Lineup[slot]);
        d.Click(button.X + 5, button.Y + 5);
        d.Click(60, RowY(outside));
        Assert.Null(club.Lineup[slot]);
        d.Click(10, 10); // Meldung schließen

        var reserve = club.Dressed.First(p => !club.Lineup.Contains(p) && p != outside);
        d.Click(60, RowY(reserve));
        Assert.False(club.IsDressed(reserve));
        d.Click(button.X + 5, button.Y + 5);
        d.Click(60, RowY(outside));
        Assert.Same(outside, club.Lineup[slot]);
        Assert.Equal(Club.MaxDressed, club.DressedCount);

        // Rechtsklick auf einen nicht Eingesetzten: wieder abgewiesen, Bildschirm bleibt.
        d.RightClick(60, RowY(reserve));
        Assert.IsType<LineupScreen>(d.Host.Current);
        Assert.False(club.IsDressed(reserve));
    }

    /// <summary>Befund (a): Nur „SEMI“ ist änderbar (Datei 0x488F6); „MANUELL“ und „AUTOM.“ sperren die Felder.</summary>
    [GameDataFact]
    public void Tactics_OnlySemiEditable()
    {
        var (d, _, club) = Start();
        d.OpenTile("Taktik");
        var t = club.Tactics;
        var up = TacticsScreen.SituationArrows(ScoreSituation.Tied, attack: true);
        var mode = TacticsScreen.AttackArrows;

        d.Click(up.X + 4, up.Y + 2);
        Assert.Equal(6, t.Attack(0, ScoreSituation.Tied));
        d.Click(mode.X + 4, mode.Y + 2);
        Assert.Equal(TacticMode.Semi, t.AttackMode);
        d.Click(up.X + 4, up.Y + 2);
        Assert.Equal(7, t.Attack(0, ScoreSituation.Tied));
        d.Click(up.X + 4, up.Y + 2);
        Assert.Equal(7, t.Attack(0, ScoreSituation.Tied)); // Obergrenze 7
        d.Click(mode.X + 4, mode.Y + 2);
        Assert.Equal(TacticMode.Manual, t.AttackMode);
        d.Click(up.X + 4, up.Y + up.H - 2);
        Assert.Equal(7, t.Attack(0, ScoreSituation.Tied));
    }
}
