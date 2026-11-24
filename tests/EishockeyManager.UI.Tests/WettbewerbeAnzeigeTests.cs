using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Runde 25, Spur wettbewerbe-anzeige: B10 (gemerkte Seite der Europapokal-Kacheln, A268/A269) und B17 (Reihenwerte der
/// Nationalteam-Mannschaftsfelder über 0x977F, A285/A419/A961).
/// </summary>
public class WettbewerbeAnzeigeTests
{
    private static GameState Synthetic()
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 8 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private sealed class Dummy(ScreenHost host) : Screen(host)
    {
        public override void Update(InputState input) { }
        public override void Draw(Surface s) { }
    }

    private static InputState Click(Rect r) => new() { MouseX = r.X + 5, MouseY = r.Y + 5, LeftClick = true };

    private static readonly InputState RightClick = new() { MouseX = 160, MouseY = 100, RightClick = true };

    /// <summary>
    /// B17 (A285, A419): [ORIGINAL] Die Mannschaftsfelder zeigen (K + T) / 2 aus 0x977F (Datei 0x3B65C, 0x3B664–0x3B691): Länder
    /// ohne Bundestrainer Teamwerte + 4 − 4 · Reihe, je auf 0 … 100 begrenzt. Erwartete Werte nach der Nachbildung
    /// `b17_reihenwerte_national.py` (docs/analyse/re-abweichungen.md B17). Land 55 (DGROUP 0x4C4E: K/T Abwehr 90/90, Sturm 95/93):
    /// Abwehr 94, 90, 86, Sturm 98, 94, 90. Vorher zeigte das Remake (K + T) / 2 ohne Reihenstufe: Abwehr 90, 90, 90, Sturm
    /// 94, 94, 94. Land 52 (Abwehr 97/103): 100, 98, 96 – vorher 99 in allen Reihen (Grenze 99).
    /// </summary>
    [Theory]
    [InlineData(55, false, new[] { 94, 90, 86 })]
    [InlineData(55, true, new[] { 98, 94, 90 })]
    [InlineData(52, false, new[] { 100, 98, 96 })]
    public void TeamPanel_LineValues_Of0x977F(int nation, bool attack, int[] expected)
    {
        var game = Synthetic();
        Assert.Equal(expected, Enumerable.Range(0, NationalCoach.Lines).Select(l => NationalWidgets.LineValue(game, nation, l, attack)));
    }

    /// <summary>
    /// B17 (A961): Land des Bundestrainers – die Anzeige nimmt denselben Weg 0x977F → 0x98F8 wie <see cref="GameState.NationalPanelLineValue"/>
    /// (über <see cref="MatchEngine.NationalSquad"/>) statt des Mittels der Aufstellung (<see cref="NationalLineup.LineValue"/>).
    /// </summary>
    [Fact]
    public void TeamPanel_CoachNation_UsesPanelLineValue()
    {
        var game = Synthetic();
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.AutoFormation();
        for (var line = 0; line < NationalCoach.Lines; line++)
            foreach (var attack in new[] { false, true })
                Assert.Equal(game.NationalPanelLineValue(coach.Nation, line, attack), NationalWidgets.LineValue(game, coach.Nation, line, attack));
        Assert.Contains(Enumerable.Range(0, NationalCoach.Lines),
            l => NationalWidgets.LineValue(game, coach.Nation, l, false) != coach.Lineup().LineValue(l, attack: false)
                || NationalWidgets.LineValue(game, coach.Nation, l, true) != coach.Lineup().LineValue(l, attack: true));
    }

    /// <summary>
    /// B10 (A269, A268): [ORIGINAL] „Tabelle Europapokal“ (DGROUP 0x5123) und „Europapokal“ der Paarungen (DGROUP 0x5126) merken je
    /// die zuletzt gezeigte Seite (Rückgabe des Seitenwählers 0x3A88A, Datei 0x45862 bzw. 0x458BB; Seiten 3/4/5 = Gruppe 1/2,
    /// Endrunde). Vorher öffneten beide immer Gruppe 1. Der Saisonwechsel setzt beide auf 3 zurück (Datei 0x5649D–0x564BA).
    /// </summary>
    [Fact]
    public void EuropeanCupTiles_RememberLastPage_PerTile_UntilSeasonChange()
    {
        var game = Synthetic();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new Dummy(host);
        var table = MenuEntries.All["Tabelle Europapokal"];
        var pairings = MenuEntries.All["Europapokal"];
        var before = StateFingerprint.Of(game);

        var first = Assert.IsType<EuropeanCupScreen>(table(host, menu));
        Assert.Equal(0, first.View);
        host.Show(first);
        first.Update(Click(EuropeanCupScreen.RightButton)); // Gruppe 1 → Endrunde (Seite 5)
        Assert.Equal(2, first.View);
        first.Update(RightClick);
        Assert.Same(menu, host.Current);

        Assert.Equal(2, Assert.IsType<EuropeanCupScreen>(table(host, menu)).View);
        var other = Assert.IsType<EuropeanCupScreen>(pairings(host, menu)); // eigenes Byte 0x5126
        Assert.Equal(0, other.View);
        host.Show(other);
        other.Update(Click(EuropeanCupScreen.LeftButton)); // Gruppe 1 → Gruppe 2 (Seite 4)
        Assert.Equal(1, other.View);
        other.Update(new InputState { Keys = { Key.Enter } });
        Assert.Equal(1, Assert.IsType<EuropeanCupScreen>(pairings(host, menu)).View);
        Assert.Equal(2, Assert.IsType<EuropeanCupScreen>(table(host, menu)).View);
        Assert.Equal(before, StateFingerprint.Of(game));

        game.Season++;
        Assert.Equal(0, Assert.IsType<EuropeanCupScreen>(table(host, menu)).View);
        Assert.Equal(0, Assert.IsType<EuropeanCupScreen>(pairings(host, menu)).View);
    }
}
