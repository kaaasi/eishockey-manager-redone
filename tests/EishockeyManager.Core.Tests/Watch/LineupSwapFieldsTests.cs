using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zusehen-rest Teil 3 (#908, A858) an der DOSBox-Aufnahme `a093`: FILE71 (Kontrollstand), FILE72 (FILE71 mit Reihentausch
/// bei EC HEDOS MÜNCHEN, Manager am Zug 1, ohne Spiel gesichert), FILE73 (Spieltag 1 danach), FILE74 (FILE71 geladen, gespielt).
/// </summary>
public class LineupSwapFieldsTests
{
    private const string Munich = "HEDOS";

    /// <summary>
    /// A858 (rot vor dem Fix: Felder blieben 68 70 69 69 55 48): Nach dem Tausch LUTZ ↔ SCHUSTER, BIRK ↔ VOLLAND schreibt das
    /// Verlassen der Aufstellung 0x952C(Manager, 0) (Datei 0x41513–0x4151A) – die Felder stehen genau auf denen von FILE72.
    /// </summary>
    [GameDataFact]
    public void A858_LeavingLineupAfterSwap_FieldsLikeFile72()
    {
        var game = Import("FILE71");
        var club = Club(game);
        // #930: Der Import von FILE72 bildet das Laden nach und schreibt die Felder neu (68 69 55 / 70 69 48); der Sollwert dieses
        // Tests sind die gesicherten Rohbytes 0x18–0x1D von FILE72: 69 70 69 70 55 48 (Fundstelle: A858, Datei 0x41513).
        var file72 = (new PartValues(69, 69, 55), new PartValues(70, 70, 48));
        Assert.NotEqual(file72, MatchEngine.ClubFieldValues(club));

        Swap(club);
        game.OnLineupLeft(club);

        Assert.Equal(file72, MatchEngine.ClubFieldValues(club));
    }

    /// <summary>
    /// #930 (Review #929 P3-1, rot vor dem Fix: 69 69 55 / 70 70 48): Der Import von FILE72 bildet das Laden nach. 0x375 ≠ 0x376
    /// (Manager 1: 0/1) – der Schleifenkopf stellt neu auf (0x426BC) und schreibt dann 0x952C(1, 0) (Datei 0x54DAA): die
    /// Vereinsfelder stehen auf den Werten der neuen Aufstellung, 68 69 55 / 70 69 48.
    /// </summary>
    [GameDataFact]
    public void Import_File72_LikeLoading_WritesClubFieldsAfterRelineup()
    {
        var club = Club(Import("FILE72"));

        Assert.Equal((new PartValues(68, 69, 55), new PartValues(70, 69, 48)), MatchEngine.ClubFieldValues(club));
        Assert.Equal(MatchEngine.ClubLineupTeamValues(club) is var (d, a, _) ? (d, a) : default, MatchEngine.ClubFieldValues(club));
    }

    /// <summary>A858: Für einen Computerverein rechnet das Ereignis nichts (0x952C nur mit dem Managersatz [0xA70E]).</summary>
    [GameDataFact]
    public void A858_ComputerClub_FieldsUnchanged()
    {
        var game = Import("FILE71");
        var other = game.Clubs.First(c => c.Players.Count > 0 && game.Managers.All(m => m.ClubIndex != c.Index));
        var before = MatchEngine.ClubFieldValues(other);
        foreach (var p in other.Lineup.Block(0))
            p.Technique = Math.Max(1, p.Technique - 30);
        game.OnLineupLeft(other);
        Assert.Equal(before, MatchEngine.ClubFieldValues(other));
    }

    /// <summary>
    /// #908, Import = Laden (Lesart gegen Review #906 P3-1, Beleg im PR): FILE72 trägt den Tausch und 0x375/0x376 von Manager 1 =
    /// 0/1. Laden setzt den Tageslauf am Schleifenkopf fort; am Tag mit Ereignis (Tagesindex 1, Markierung 7) holt er für jeden
    /// Managersatz 0x375 := 0x376 zurück und stellt mit 0x426BC neu auf (Datei 0x54D84–0x54D9E), erst danach beginnt der Zug am
    /// gesicherten Manager (Datei 0x54E3F–0x54E48). Nach dem Laden gilt also „automatisch aufstellen“, der Tausch ist verworfen –
    /// wie beim Bundestrainer (FILE47 „beim erneuten Laden wieder eingesetzt“) und FILE24 → FILE25.
    /// </summary>
    [GameDataFact]
    public void Import_File72_LikeLoading_DiscardsSwap()
    {
        var club = Club(Import("FILE72"));
        Assert.True(club.AutoLineupEnabled);
        Assert.Equal(0, club.Lineup.FindSlot(Player(club, "LUTZ"))!.Value.Block);
        Assert.Equal(1, club.Lineup.FindSlot(Player(club, "SCHUSTER"))!.Value.Block);
    }

    /// <summary>
    /// #908: FILE73 ist die Fortsetzung ohne erneutes Laden. Der Spieltag sichert 0x376 := 0x375 und stellt 0x375 auf 0 (Datei
    /// 0x54FCE–0x54FE8), später holt der Tageslauf 0x375 := 0x376 zurück (Datei 0x54D84/0x552F9); FILE73 hat 0x375/0x376 von
    /// Manager 1 = 0/0 und den Tausch (LUTZ 2A, SCHUSTER 1A, VOLLAND 1S, BIRK 2S). Nach einem Laden von FILE72 hätte der
    /// Schleifenkopf 0x375 = 1 gesetzt, 0x54FDC also 0x376 = 1 gesichert – FILE73 zeigte 1/1 und die Automatik.
    /// Das Remake ohne Laden: FILE71, Tausch von Hand (Automatik aus), Verlassen, bis zum Halt von FILE73 spielen – Tausch und
    /// Einstellung bleiben wie im Original. Sollwerte bleiben die Reihen aus FILE73; ein Spieler, den der Zufall des Remakes am
    /// Spieltag verletzt oder sperrt, wird nur auf „nicht aufgestellt“ geprüft (R21-D2: seit dem Wurf von A093 vor jedem Spiel,
    /// Datei 0x5482D, verschiebt sich der Zufallsstrom und VOLLAND verletzt sich mit Saat 1 – im Original nicht).
    /// </summary>
    [GameDataFact]
    public void ContinueWithoutLoading_File71SwapToFile73_KeepsSwap()
    {
        var game = Import("FILE71");
        var club = Club(game);
        Swap(club);
        club.AutoLineupEnabled = false; // Eingriff von Hand (Datei 0x40ADE/0x40DE4), FILE72: 0x375 + 2 · 1 = 0
        game.OnLineupLeft(club);
        var target = Import("FILE73");
        var original = Club(target);
        Assert.False(original.AutoLineupEnabled);

        Assert.Null(HokCompare.PlayTo(game, target));

        Assert.False(club.AutoLineupEnabled);
        var checkedNames = 0;
        foreach (var name in new[] { "LUTZ", "SCHUSTER", "BIRK", "VOLLAND" })
        {
            var expected = original.Lineup.FindSlot(Player(original, name))!.Value.Block;
            var player = Player(club, name);
            if (player.InjuredGames > 0 || player.SuspendedGames > 0)
            {
                // Zufall des Remakes (Verletzung oder Sperre am Spieltag, im Original nicht gezogen): Der Spieler verlässt im Remake
                // die Aufstellung ([APPROX] Squad.Lineup.cs) – kein Aufschluss über den Tausch, aber auch kein Ersatz durch die
                // Automatik an seiner Stelle.
                Assert.Null(club.Lineup.FindSlot(player));
                continue;
            }
            Assert.Equal(expected, club.Lineup.FindSlot(player)?.Block);
            checkedNames++;
        }
        // Ein zurückgenommener Tausch fiele schon an einem Paar auf (LUTZ/SCHUSTER bzw. BIRK/VOLLAND tauschen die Reihe).
        Assert.True(checkedNames >= 3, $"nur {checkedNames} der getauschten Spieler spielberechtigt");
    }

    private static void Swap(Club club)
    {
        club.Lineup.Assign(club.Lineup.FindSlot(Player(club, "SCHUSTER"))!.Value, Player(club, "LUTZ"));
        club.Lineup.Assign(club.Lineup.FindSlot(Player(club, "VOLLAND"))!.Value, Player(club, "BIRK"));
        Assert.Equal(1, club.Lineup.FindSlot(Player(club, "LUTZ"))!.Value.Block);
        Assert.Equal(0, club.Lineup.FindSlot(Player(club, "VOLLAND"))!.Value.Block);
    }

    private static Player Player(Club club, string name) => club.Players.Single(p => p.Name.Contains(name));

    private static Club Club(GameState game) => game.Clubs.Single(c => c.Name.Contains(Munich));

    private static GameState Import(string file) =>
        HokCompare.Import(OriginalImportTests.OriginalSave(file + ".HOK"), ClubDatabase.Load(TestGameData.Load()), 1);
}
