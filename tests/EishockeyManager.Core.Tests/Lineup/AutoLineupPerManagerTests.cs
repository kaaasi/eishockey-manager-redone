using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur aufstellung (Runde 25), Teil 2 K07: „automatisch aufstellen“ je Manager (A936) und Kaderplatz +0x15 außerhalb der Aufstellung
/// (A939). Belege <c>docs/archiv/analyse/re-abweichungen.md</c> K07 (<c>k07_automatik.py</c>, <c>k07_sechster.py</c>).
/// </summary>
public class AutoLineupPerManagerTests
{
    /// <summary>random(a, b) liefert immer die untere Grenze: jeder Kaderplatz würfelt 0 und schreibt +0x02.</summary>
    private sealed class MinRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
        public override double NextDouble() => 0;
    }

    private static (GameState Game, Club Club) Game(int seed = 3)
    {
        var game = TestGames.Create(seed);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.True(club.ApplyAutoLineup());
        return (game, club);
    }

    /// <summary>
    /// A939 (Vorgabe K07 (2)): [ORIGINAL] 0x426BC schreibt +0x15 nur für Ausgewählte (Datei 0x42744–0x42777), die Verletzung löscht nur
    /// +0x0A (Datei 0x5C50); die Spielabrechnung kopiert +0x15 &amp; 0x7F (Datei 0x548A8–0x548E7). Aufgestellter Verteidiger der Reihe 2
    /// verletzt sich, Neuaufstellung, Wurf 0 → eingespielte Reihe 2. Vorher 0 (Spieler aus der Aufstellung genommen).
    /// </summary>
    [Fact]
    public void InjuredDefenderOfLine2_AfterRelineup_PlaysInLine2()
    {
        var (_, club) = Game();
        var defender = club.Lineup[LineupSlot.Defender(1, 0)]!;
        Assert.Equal(2, club.Lineup.PositionOf(defender));
        defender.InjuredGames = 3;

        club.RefillLineup();

        Assert.False(club.Lineup.Contains(defender));
        Assert.Equal(2, club.SlotLine(defender));
        SquadSlotRules.RollPlayedLines(club, new MinRandom());
        Assert.Equal(2, defender.PlayedLine);
    }

    /// <summary>
    /// A939 ohne „automatisch aufstellen“: die Bereinigung des Tageslaufs bzw. <see cref="Club.RefillLineup"/> nimmt den Verletzten aus dem
    /// Platz, sein +0x15 bleibt (Stürmer der Reihe 1: 0x81, eingespielt 1).
    /// </summary>
    [Fact]
    public void InjuredForward_ManualSetting_KeepsLine()
    {
        var (_, club) = Game();
        club.AutoLineupEnabled = false;
        var forward = club.Lineup[LineupSlot.Forward(0, 0)]!;
        forward.InjuredGames = 2;

        club.RefillLineup();

        Assert.Equal(0x81, club.SlotLine(forward));
        SquadSlotRules.RollPlayedLines(club, new MinRandom());
        Assert.Equal(1, forward.PlayedLine);
    }

    /// <summary>
    /// [ORIGINAL] Leeren von Hand schreibt +0x15 = 0 (Datei 0x40B69–0x40C98, <c>k07_sechster.py</c> a/b); Ersatz der Neuaufstellung 0
    /// (Datei 0x427FD–0x42816); ein neuer Kaderplatz 0 (memset Datei 0x42E3A).
    /// </summary>
    [Fact]
    public void ManualClear_Reserve_NewSlot_LineZero()
    {
        var (_, club) = Game();
        var defender = club.Lineup[LineupSlot.Defender(1, 0)]!;
        defender.InjuredGames = 3;
        club.RefillLineup();
        Assert.Equal(2, club.SlotLine(defender));

        // Geheilt und von Hand wieder eingesetzt, dann von Hand geleert: 0.
        defender.InjuredGames = 0;
        club.AutoLineupEnabled = false;
        foreach (var reserve in club.Dressed.Where(p => !club.Lineup.Contains(p)))
            Assert.Equal(0, club.SlotLine(reserve));
        if (club.Dressed.FirstOrDefault(p => !club.Lineup.Contains(p)) is { } spare)
            club.Undress(spare);
        var free =Lineup.SlotsOfLine(3, PlayerRole.Defender).First(s => club.Lineup[s] is null);
        Assert.Equal(LineupResult.Assigned, club.AssignToLineup(free, defender));
        club.ClearLineupSlot(free);
        Assert.Equal(0, club.SlotLine(defender));

        defender.LastLine = 3;
        defender.ApplyNewSlot(default, fromManager: true);
        Assert.Equal(0, defender.LastLine);
    }

    /// <summary>
    /// A939: Nicht Ausgewählte der Neuaufstellung behalten +0x15 ([ORIGINAL] <c>k07_automatik.py</c>: Platz 4 verletzt, vorher
    /// aufgestellt mit +0x15 = 2, danach +0x0A = 0, +0x15 = 2), die nächste Neuaufstellung mit dem Geheilten setzt ihn neu.
    /// </summary>
    [Fact]
    public void NextRelineup_HealedPlayer_GetsNewLine()
    {
        var (_, club) = Game();
        var defender = club.Lineup[LineupSlot.Defender(1, 0)]!;
        defender.InjuredGames = 3;
        club.RefillLineup();
        Assert.False(club.IsDressed(defender));
        Assert.Equal(2, club.SlotLine(defender));

        defender.InjuredGames = 0;
        Assert.True(club.ApplyAutoLineup());

        Assert.True(club.Lineup.Contains(defender));
        Assert.Equal(club.Lineup.PositionOf(defender), club.SlotLine(defender));
    }

    /// <summary>
    /// A936 (Vorgabe K07 (1)): [ORIGINAL] 0x426BC liest DGROUP 0x375 + 2 · Manager (Datei 0x426C4–0x426E4), das Abschalten in der Aufstellung
    /// schreibt es (Datei 0x40ADE, 0x40DE4). Die Einstellung gehört dem Manager: Wechselt er den Verein, kommt sie mit; der neue Verein
    /// stellt danach nicht mehr automatisch auf. Vorher am Verein (Feld fehlte am Manager).
    /// </summary>
    [Fact]
    public void Setting_BelongsToManager_MovesWithClubChange()
    {
        var (game, club) = Game();
        var manager = game.Managers[0];
        int? line = 0;
        club.ClickPlayer(club.Players.First(p => p.Available), ref line, right: false);
        Assert.False(manager.AutoLineupEnabled);
        Assert.False(club.AutoLineupEnabled);

        var other = game.Clubs.First(c => c.Index != club.Index && c.Index < 36);
        Assert.True(other.AutoLineupEnabled);
        manager.ClubIndex = other.Index;
        game.BindLineupOwners();

        Assert.False(other.AutoLineupEnabled);
        Assert.False(other.ApplyAutoLineup());

        // Umschalten über den neuen Verein schreibt den Manager.
        other.SetAutoLineup(true);
        Assert.True(manager.AutoLineupEnabled);
    }

    /// <summary>
    /// Befund #1016: Ein im laufenden Spiel hinzugekommener Manager ist an seinen Verein gebunden (<c>BindLineupOwners</c> in
    /// <c>AddManager</c>, A936). Ohne die Bindung schriebe das Umschalten nur das Vereinsfeld, und das nächste Laden setzte es auf den
    /// Wert des Managers zurück. Rückschaltprobe: Aufruf in <c>GameState.AddManager.cs</c> entfernen → rot.
    /// </summary>
    [Fact]
    public void AddManager_MidGame_BindsNewcomerToClub_SwitchSurvivesSaveLoad()
    {
        var game = TestGames.Create(3);
        TestSeason.PlayMatchDay(game);
        var manager = game.AddManager(new ManagerSetup { Name = "Neu", Head = 4, ClubIndex = 7 });
        var club = game.Clubs[manager.ClubIndex];
        Assert.True(manager.AutoLineupEnabled);

        club.SetAutoLineup(false);
        Assert.False(manager.AutoLineupEnabled); // ohne Bindung bliebe der Manager auf true

        var g = SaveGame.Load(SaveGame.Save(game)).Game!;
        var m = g.Managers.Single(x => x.Name == "Neu");
        Assert.False(g.Clubs[m.ClubIndex].AutoLineupEnabled);
        g.Clubs[m.ClubIndex].SetAutoLineup(true);
        Assert.True(m.AutoLineupEnabled);
    }

    /// <summary>Speichern und Laden halten beide Felder (Schema 64).</summary>
    [Fact]
    public void SaveLoad_KeepsManagerSettingAndLastLine()
    {
        var (game, club) = Game();
        var defender = club.Lineup[LineupSlot.Defender(1, 0)]!;
        defender.InjuredGames = 3;
        club.RefillLineup();
        club.AutoLineupEnabled = false;

        var loaded = SaveGame.Load(SaveGame.Save(game));

        Assert.True(loaded.Success, loaded.Error);
        var g = loaded.Game!;
        Assert.False(g.Managers[0].AutoLineupEnabled);
        var c = g.Clubs[club.Index];
        Assert.False(c.AutoLineupEnabled);
        Assert.Equal(2, c.SlotLine(c.Players.Single(p => p.Id == defender.Id)));
        c.SetAutoLineup(true);
        Assert.True(g.Managers[0].AutoLineupEnabled);
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5, vier Saisonwechsel, passiver und aktiver Manager): Die Einstellung bleibt am Manager und gleicht der
    /// seines Vereins; +0x15 außerhalb der Aufstellung steht nur bei nicht Eingesetzten (Ersatz der Neuaufstellung 0), ist eine gültige
    /// Reihe (Reihe 1–4, Bit 0x80 Sturm) und übersteht Speichern und Laden.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_SettingAndLastLineStayConsistent(bool active)
    {
        var game = TestGames.Create(3);
        var season = game.Season;
        var seasons = 0;
        var kept = 0;
        RngTwinProbeTests.Run(game, active, afterStep: g =>
        {
            var manager = g.Managers[0];
            var club = g.Clubs[manager.ClubIndex];
            Assert.Equal(manager.AutoLineupEnabled, club.AutoLineupEnabled);
            foreach (var p in club.Players.Where(p => !club.Lineup.Contains(p)))
            {
                Assert.Contains(p.LastLine & 0x7F, new[] { 0, 1, 2, 3, 4 });
                if (p.LastLine != 0)
                {
                    kept++;
                    Assert.False(club.IsDressed(p) && club.AutoLineupEnabled, $"{p.Name}: Ersatz mit +0x15 {p.LastLine}");
                }
            }
            if (g.Season == season)
                return;
            season = g.Season;
            seasons++;
            var loaded = SaveGame.Load(SaveGame.Save(g));
            Assert.True(loaded.Success, loaded.Error);
            var c = loaded.Game!.Clubs[club.Index];
            Assert.Equal(club.Players.Select(club.SlotLine), c.Players.Select(c.SlotLine));
            Assert.Equal(manager.AutoLineupEnabled, loaded.Game.Managers[0].AutoLineupEnabled);
        });
        Assert.Equal(4, seasons);
        Assert.True(kept > 0, "kein Spieler mit +0x15 außerhalb der Aufstellung");
    }
}
