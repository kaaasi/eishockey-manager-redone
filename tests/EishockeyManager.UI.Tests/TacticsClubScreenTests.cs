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
        // #716 R14G4-08: fester Sollwert 22 (Datei 0x40E4D, Vergleich mit 0x16) statt Club.MaxDressed; Gegenprobe Konstante 23 → rot.
        Assert.Equal(22, club.DressedCount);
        var outside = club.Players.First(p => p.Available && !club.IsDressed(p) && p.Role != PlayerRole.Goalkeeper);
        var slot = outside.Role == PlayerRole.Forward ? LineupSlot.Forward(3, 0) : LineupSlot.Defender(3, 0);
        // Reihe 4 wählen (Klick in den Block, Datei 0x40890), dann den Spieler (Datei 0x40E2A vor der Reihenlogik).
        var block = LineupScreen.BlockBox(3);
        Assert.Null(club.Lineup[slot]);
        d.Click(block.X + 3, block.Y + 5);
        d.Click(60, RowY(outside));
        Assert.Null(club.Lineup[slot]);
        d.Frames(200); // erst „ist deaktiviert !“ (Datei 0x40DE4), dann „Schon 22 Mann“ – beide schließen von selbst; Integration Runde 11 (ui-meldungen, K2): Kurzmeldung 0x0C0B8 ohne Knopf, schließt von selbst (Klick wirkt nicht)

        // Rechtsklick (außerhalb der Namensspalte) nimmt einen Reservisten heraus (Datei 0x40F59; vorher Linksklick).
        var reserve = club.Dressed.First(p => !club.Lineup.Contains(p) && p != outside);
        d.RightClick(150, RowY(reserve));
        Assert.False(club.IsDressed(reserve));
        // Integration Runde 11 (ui-meldungen, K2): Der Eingriff schaltet die automatische Aufstellung ab und meldet das als Kurzmeldung
        // (Datei 0x40ADE -> 0x0C0B8); sie nimmt keine Eingabe an, bis sie von selbst schließt.
        d.Frames(100);
        d.Click(60, RowY(outside)); // Reihe 4 bleibt gewählt: erster freier Platz seiner Art (Datei 0x412A1)
        Assert.Same(outside, club.Lineup[slot]);
        Assert.Equal(22, club.DressedCount);

        // Rechtsklick auf einen nicht Eingesetzten: wieder abgewiesen, Bildschirm bleibt.
        d.RightClick(150, RowY(reserve));
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

        // Startwert 0 (A180, kader-start Runde 14; vorher 6 aus der ausgeglichenen Zeile).
        d.Click(up.X + 4, up.Y + 2);
        Assert.Equal(0, t.Attack(0, ScoreSituation.Tied));
        d.Click(mode.X + 4, mode.Y + mode.H - 2);
        Assert.Equal(TacticMode.Semi, t.AttackMode);
        d.Click(up.X + 4, up.Y + 2);
        Assert.Equal(1, t.Attack(0, ScoreSituation.Tied));
        for (var i = 0; i < 7; i++)
            d.Click(up.X + 4, up.Y + 2);
        Assert.Equal(7, t.Attack(0, ScoreSituation.Tied)); // Obergrenze 7
        d.Click(mode.X + 4, mode.Y + mode.H - 2);
        Assert.Equal(TacticMode.Manual, t.AttackMode);
        d.Click(up.X + 4, up.Y + up.H - 2);
        Assert.Equal(7, t.Attack(0, ScoreSituation.Tied));
    }

    /// <summary>
    /// A373 (Review #643 R14RD1-01): Modusknöpfe: untere Hälfte = nächster Wert, obere = voriger, an den Enden kein Umlauf
    /// (em.exe Datei 0x487A8–0x487B8, 0x48BD2–0x48C25).
    /// </summary>
    [GameDataFact]
    public void Tactics_ModeArrows_LowerHalfIsNext_NoWrap()
    {
        var (d, _, club) = Start();
        d.OpenTile("Taktik");
        var t = club.Tactics;
        var line = TacticsScreen.LineChangeArrows;
        Assert.Equal(LineChangeMode.Automatic, t.LineChange);

        d.Click(line.X + 4, line.Y + 2); // obere Hälfte am Anfang: unverändert
        Assert.Equal(LineChangeMode.Automatic, t.LineChange);
        d.Click(line.X + 4, line.Y + line.H - 2);
        Assert.Equal(LineChangeMode.TwoLinesOnly, t.LineChange);
        d.Click(line.X + 4, line.Y + 2);
        Assert.Equal(LineChangeMode.Automatic, t.LineChange);

        var last = Enum.GetValues<LineChangeMode>()[^1];
        for (var i = 0; i < 10; i++)
            d.Click(line.X + 4, line.Y + line.H - 2);
        Assert.Equal(last, t.LineChange); // Ende: kein Umlauf
    }

    /// <summary>R14UR-09 / A373: Torwart-Pfeil oben links – untere Hälfte = nächster Wert, obere = voriger, kein Umlauf.</summary>
    [GameDataFact]
    public void Tactics_GoalieArrows_LowerHalfIsNext_NoWrap()
    {
        var (d, _, club) = Start();
        d.OpenTile("Taktik");
        var t = club.Tactics;
        var arrows = TacticsScreen.GoalieArrows;
        var values = Enum.GetValues<GoalieMode>();
        t.Goalie = values[0];

        d.Click(arrows.X + 4, arrows.Y + 2); // obere Hälfte am Anfang: unverändert (kein Umlauf)
        Assert.Equal(values[0], t.Goalie);
        d.Click(arrows.X + 4, arrows.Y + arrows.H - 2); // untere Hälfte: nächster Wert
        Assert.Equal(values[1], t.Goalie);
        d.Click(arrows.X + 4, arrows.Y + arrows.H - 2); // am Ende: kein Umlauf
        Assert.Equal(values[^1], t.Goalie);
        d.Click(arrows.X + 4, arrows.Y + 2); // obere Hälfte: voriger Wert
        Assert.Equal(values[^2], t.Goalie);
    }
}
