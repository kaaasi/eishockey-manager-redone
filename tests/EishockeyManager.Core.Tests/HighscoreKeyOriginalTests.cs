using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonende-status, Befund #126: Ist der Schlüssel 0x7530 („nur Weltkugel“) im Original erreichbar?
/// <para>
/// <b>Ergebnis: ja – der Befund trifft nicht zu.</b> Das Issue las die Bedingung ab Datei 0x289A3 und übersah den Vergleich
/// unmittelbar davor. [ORIGINAL] em.exe, Funktion der Spielvorbereitung (Spielstart-Overlay), mit <c>[bp-0x57]</c> = Knopfbits
/// (Bit 0 Fahne, Bit 1 Weltkugel; dieselbe Variable bildet bei 0x29838 den Modus):
/// </para>
/// <code>
/// 0x2899D  cmp byte [bp-0x57],2     ; nur Weltkugel?
/// 0x289A1  jz  0x289C5              ; ja → [bp-0x42] = 1 (Liste zulässig), ohne Blick auf Level, Dauer, Liga, Stärke
/// 0x289A3  test byte [bp-0x57],1    ; Fahne gesetzt? sonst keine Liste
/// 0x289A9  cmp byte [0x118],2 ; jng ; Level &gt; 2
/// 0x289B0  cmp byte [0x11B],1 ; jz  ; Stärke = Mittelfeld → zulässig
/// 0x289B7  cmp [0x119],0 ; cmp [0x11A],0  ; sonst: Dauer = eine Saison und Startliga = Bundesliga
/// 0x289CC  mov [bp-0x42],ax         ; Zulässigkeit (0 oder 1)
/// 0x28A2C  [bp-0x42] = 0 → [0x256] = 0 (keine Bestenliste)
/// 0x28A34  cmp byte [bp-0x57],2 → [0x256] = 0x7530 (0x28A3A)
/// 0x28A42  sonst [0x256] = Level + 4·Dauer + 32·Liga + 128·Stärke − 2
/// </code>
/// <para>Diese Tabelle prüft das Remake gegen die Abfolge im Original über alle Kombinationen.</para>
/// </summary>
public class HighscoreKeyOriginalTests
{
    /// <summary>Schlüssel DGROUP 0x256 nach dem Ablauf im Original; null = 0 (keine Bestenliste).</summary>
    private static int? OriginalKey(int bits, int level, int duration, int league, int strength)
    {
        var eligible = bits == 2
            || ((bits & 1) != 0 && level > 2 && (strength == 1 || (duration == 0 && league == 0)));
        if (!eligible)
            return null;
        return bits == 2 ? 0x7530 : level + 4 * duration + 32 * league + 128 * strength - 2;
    }

    private static GameSetup Setup(int bits, int level, GameDuration duration, League league, StartStrength strength)
    {
        var setup = new GameSetup { Duration = duration };
        setup.Managers.Add(new ManagerSetup { Name = "A", Level = level, StartLeague = league, StartStrength = strength });
        if ((bits & 1) == 0)
            Assert.True(setup.ToggleClubCompetitions());
        if ((bits & 2) == 0)
            Assert.True(setup.ToggleEuropeAndWorldCup());
        Assert.Equal(bits, setup.ModeBits);
        return setup;
    }

    [Fact]
    public void Key_MatchesOriginalFlow_ForEveryCombination()
    {
        var checkedCases = 0;
        var withList = 0;
        for (var bits = 0; bits <= 3; bits++)
            for (var level = 0; level <= 5; level++)
                foreach (var duration in Enum.GetValues<GameDuration>())
                    foreach (var league in Enum.GetValues<League>())
                        foreach (var strength in Enum.GetValues<StartStrength>())
                        {
                            var expected = OriginalKey(bits, level, (int)duration, (int)league, (int)strength);
                            var setup = Setup(bits, level, duration, league, strength);
                            Assert.True(expected == HighscoreRules.Key(setup),
                                $"bits {bits}, Level {level}, {duration}, {league}, {strength}: Original {expected?.ToString() ?? "keine Liste"}, Remake {HighscoreRules.Key(setup)?.ToString() ?? "keine Liste"}");
                            checkedCases++;
                            if (expected is not null)
                                withList++;
                        }
        Assert.Equal(4 * 6 * 4 * 3 * 3, checkedCases);
        Assert.True(withList > 0);
    }

    /// <summary>Im Modus „nur Weltkugel“ gilt immer der Schlüssel 0x7530 = 30000 – unabhängig von allen anderen Einstellungen.</summary>
    [Theory]
    [InlineData(0, GameDuration.EineSaison, League.Bundesliga, StartStrength.Mittelfeld)]
    [InlineData(1, GameDuration.DreiSaisons, League.Oberliga, StartStrength.Abstiegskampf)]
    [InlineData(5, GameDuration.Unbegrenzt, League.ZweiteLiga, StartStrength.OberesDrittel)]
    public void WorldOnly_AlwaysHasList_WithKey30000(int level, GameDuration duration, League league, StartStrength strength)
    {
        var setup = Setup(2, level, duration, league, strength);
        Assert.Equal(GameMode.WorldOnly, setup.Mode);
        Assert.Equal(0x7530, HighscoreRules.Key(setup));
        Assert.Equal(HighscoreRules.WorldOnlyKey, HighscoreRules.Key(setup));
    }

    /// <summary>Gegenprobe: „nur Fahne“ und beide Knöpfe nutzen die gewöhnliche Formel, ohne Knopf (bits 0) gibt es keine Liste.</summary>
    [Fact]
    public void ClubModes_UseFormula_NoButtonHasNoList()
    {
        Assert.Equal(3 + 4 * 3 + 32 * 0 + 128 * 1 - 2, HighscoreRules.Key(Setup(1, 3, GameDuration.Unbegrenzt, League.Bundesliga, StartStrength.Mittelfeld)));
        Assert.Equal(3 + 4 * 3 + 32 * 0 + 128 * 1 - 2, HighscoreRules.Key(Setup(3, 3, GameDuration.Unbegrenzt, League.Bundesliga, StartStrength.Mittelfeld)));
        Assert.Null(HighscoreRules.Key(Setup(0, 5, GameDuration.EineSaison, League.Bundesliga, StartStrength.Mittelfeld)));
    }
}
