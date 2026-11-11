using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Regressionstest (Befund aus #437, <c>ClubDressedTests.MultiSeason_DressedAndTactics(active: True, seed: 3)</c>,
/// „23 Eingesetzte“): nach einer Verletzung stellte <see cref="PlayerInjuries.RefillLineup"/> einen nicht eingesetzten
/// Spieler auf, der Verletzte blieb eingesetzt – bei Auswahl von Hand mehr als 22 Eingesetzte.
/// [ORIGINAL] Datei 0x5C50 (Verletzung 0x05B1A, Spiel und Training): der Verletzte verliert +0x0A (eingesetzt);
/// Neuaufstellung 0x426BC setzt höchstens 22 ein (Zähler [bp-9] ab 1, Vergleich mit 0x17 bei 0x427C0/0x4282C).
/// </summary>
public class RefillLineupDressedTests
{
    private static (GameState Game, Club Club) CreateFullRoster()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var club = game.Clubs[2];
        foreach (var donor in game.Clubs.Where(c => c != club && c.Players.Count > 0))
        {
            foreach (var p in donor.Players.Where(p => p.Available && p.Role != PlayerRole.Goalkeeper).ToList())
            {
                if (club.Players.Count >= 27)
                    break;
                donor.Lineup.Remove(p);
                donor.Players.Remove(p);
                club.Players.Add(p);
            }
        }
        // Teil 8 (A068): neu aufstellen wie nach dem Transferbildschirm (Datei 0x465F1 ruft 0x426BC).
        club.ApplyAutoLineup();
        Assert.Equal(Club.MaxDressed, club.DressedCount);
        return (game, club);
    }

    /// <summary>Auswahl von Hand mit 22 Eingesetzten; die stärksten nicht Eingesetzten wären die Kandidaten von FillGaps.</summary>
    private static List<Player> DressByHand(Club club)
    {
        // Alle eingesetzten Reservisten herausnehmen und die schwächsten übrigen einsetzen: FillGaps (bester Wert zuerst)
        // würde dann einen nicht Eingesetzten statt eines Reservisten wählen.
        var reserves = club.Dressed.Where(p => !club.Lineup.Contains(p)).ToList();
        foreach (var p in reserves)
            Assert.Equal(LineupResult.Undressed, club.Undress(p));
        var outsiders = club.Players.Where(p => !club.IsDressed(p) && p.Available)
            .OrderBy(TeamStrength.PlayerValue).ToList();
        var dressedReserves = new List<Player>();
        foreach (var p in outsiders)
        {
            if (club.DressedCount >= Club.MaxDressed)
                break;
            Assert.Equal(LineupResult.Dressed, club.Dress(p));
            dressedReserves.Add(p);
        }
        Assert.Equal(Club.MaxDressed, club.DressedCount);
        return dressedReserves;
    }

    [Fact]
    public void Injury_UndressesInjured_AndKeepsAtMostTwentyTwo()
    {
        var (_, club) = CreateFullRoster();
        var reserves = DressByHand(club);
        var injured = club.Lineup.Players.First(p => p.Role != PlayerRole.Goalkeeper
            && reserves.Any(r => r.Role == p.Role));
        var slot = club.Lineup.FindSlot(injured)!.Value;

        injured.InjuredGames = 3;
        PlayerInjuries.RefillLineup(club);

        Assert.False(club.IsDressed(injured));
        Assert.True(club.DressedCount <= Club.MaxDressed, $"{club.DressedCount} Eingesetzte");
        Assert.All(club.Lineup.Players, p => Assert.True(club.IsDressed(p)));
        // #738 (abgleich-fix, R15-E2 = C): Ohne „automatisch aufstellen“ rückt niemand nach – auch kein eingesetzter Reservist
        // ([ORIGINAL] 0x426BC endet ohne Einstellung sofort, Datei 0x426C4–0x426E4; FILE44 → FILE46 KÖLN 11 Aufgestellte).
        Assert.Null(club.Lineup[slot]);
        Assert.All(reserves, r => Assert.False(club.Lineup.Contains(r)));
    }

    [Fact]
    public void Injury_WithoutDressedReserve_FillsOnlyUpToTwentyTwo()
    {
        var (_, club) = CreateFullRoster();
        DressByHand(club);
        // Lücke von Hand (Spieler nicht mehr eingesetzt, 0x40CF8), dafür einen anderen einsetzen: 22 eingesetzt, ein Platz leer.
        var cleared = club.Lineup.Players.First(p => p.Role == PlayerRole.Forward);
        var gap = club.Lineup.FindSlot(cleared)!.Value;
        club.ClearLineupSlot(gap);
        Assert.Equal(LineupResult.Dressed, club.Dress(club.Players.First(p => !club.IsDressed(p) && p.Available
            && p.Role == PlayerRole.Defender)));
        Assert.Equal(Club.MaxDressed, club.DressedCount);

        var injured = club.Lineup.Players.First(p => p.Role == PlayerRole.Defender);
        injured.InjuredGames = 3;
        PlayerInjuries.RefillLineup(club);

        Assert.False(club.IsDressed(injured));
        Assert.True(club.DressedCount <= Club.MaxDressed, $"{club.DressedCount} Eingesetzte");
        Assert.All(club.Lineup.Players, p => Assert.True(club.IsDressed(p)));
    }
}
