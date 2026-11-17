using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-regeln</c>, Runde 22, Teil 3: Aufstellen bei voller Hälfte der Reihe (A683 Verein, A490 Nationalteam). [ORIGINAL]
/// Datei 0x412A1–0x41386 sucht die eigene Hälfte (Sturm 0–2, Abwehr 3–4), Datei 0x414D3–0x414ED dann die andere (3 − Hälfte).
/// Synthetisch, ohne Originaldaten.
/// </summary>
public class KaderRegelnTeil3Tests
{
    private static int s_nextId = 70_000;

    private static Player NewPlayer(PlayerRole role, string name)
    {
        var p = new Player { Name = name, Role = role, Condition = 70, Technique = 70, Form = 50, Age = 25, ContractYears = 2 };
        typeof(Player).GetProperty(nameof(Player.Id))!.SetValue(p, Interlocked.Increment(ref s_nextId));
        return p;
    }

    /// <summary>2 Torhüter, 8 Verteidiger, 12 Stürmer; „A“ stellt drei Blöcke auf (Reihe 4 leer), Einstellung danach aus.</summary>
    private static Club NewClub()
    {
        var club = new Club { Index = 3, Name = "Test", Strength = 70 };
        for (var i = 0; i < 2; i++) club.Players.Add(NewPlayer(PlayerRole.Goalkeeper, $"T{i}"));
        for (var i = 0; i < 8; i++) club.Players.Add(NewPlayer(PlayerRole.Defender, $"V{i}"));
        for (var i = 0; i < 12; i++) club.Players.Add(NewPlayer(PlayerRole.Forward, $"S{i}"));
        club.RenumberPlayers();
        club.AutoLineup();
        club.AutoLineupEnabled = false;
        return club;
    }

    /// <summary>Nicht aufgestellte Spieler der Art, eingesetzte Reservisten zuerst (sonst greift „Schon 22 Mann“ vor der Platzsuche).</summary>
    private static Player Free(Club club, PlayerRole role, int skip = 0) =>
        club.Players.Where(p => p.Role == role && !club.Lineup.Contains(p)).OrderBy(p => !club.IsDressed(p)).Skip(skip).First();

    /// <summary>Leert Reihe <paramref name="line"/> über die Oberfläche (Datei 0x40CF8: danach nicht eingesetzt).</summary>
    private static void ClearLine(Club club, int line)
    {
        foreach (var slot in Lineup.SlotsOfLine(line, PlayerRole.Forward).Concat(Lineup.SlotsOfLine(line, PlayerRole.Defender)))
            club.ClearLineupSlot(slot);
    }

    /// <summary>
    /// A683: Drei Stürmer in Reihe 3, ein vierter Stürmer kommt auf den ersten freien Verteidigerplatz; der fünfte auf den zweiten;
    /// ein sechster findet keinen Platz. Vorher: „Kein / Platz mehr !“ schon beim vierten.
    /// </summary>
    [Fact]
    public void ClubClick_ForwardHalfFull_GoesToDefenceHalf()
    {
        var club = NewClub();
        ClearLine(club, 2);
        int? line = 2;
        for (var i = 0; i < 3; i++)
            Assert.Equal(LineupResult.Assigned, club.ClickPlayer(Free(club, PlayerRole.Forward), ref line, right: false));
        var fourth = Free(club, PlayerRole.Forward);
        var fifth = Free(club, PlayerRole.Forward, 1);

        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(fourth, ref line, right: false));
        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(fifth, ref line, right: false));

        Assert.Same(fourth, club.Lineup[LineupSlot.Defender(2, 0)]);
        Assert.Same(fifth, club.Lineup[LineupSlot.Defender(2, 1)]);
        Assert.True(club.IsDressed(fourth));
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(Free(club, PlayerRole.Defender), ref line, right: false));

        // Herausnehmen aus dem fremden Platz wie aus jedem anderen (Datei 0x40F7C): Reihe bleibt die gewählte.
        line = 0;
        Assert.Equal(LineupResult.TakenOut, club.ClickPlayer(fourth, ref line, right: false));
        Assert.Equal(2, line);
        Assert.Null(club.Lineup[LineupSlot.Defender(2, 0)]);
    }

    /// <summary>A683: Umgekehrt kommt ein Verteidiger bei voller Abwehrhälfte in die Sturmhälfte.</summary>
    [Fact]
    public void ClubClick_DefenceHalfFull_GoesToForwardHalf()
    {
        var club = NewClub();
        ClearLine(club, 2);
        int? line = 2;
        for (var i = 0; i < 2; i++)
            Assert.Equal(LineupResult.Assigned, club.ClickPlayer(Free(club, PlayerRole.Defender), ref line, right: false));
        var third = Free(club, PlayerRole.Defender);

        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(third, ref line, right: false));

        Assert.Same(third, club.Lineup[LineupSlot.Forward(2, 0)]);
    }

    /// <summary>Volle Reihe (beide Hälften): weiter „Kein / Platz mehr !“, niemand wird verdrängt; die Torhüterreihe hat keine andere Hälfte.</summary>
    [Fact]
    public void ClubClick_FullLine_AndGoalkeeperLine_NoRoom()
    {
        var club = NewClub();
        int? line = 0;
        var before = LineupSlot.All.Select(s => club.Lineup[s]).ToList();
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(Free(club, PlayerRole.Forward), ref line, right: false));
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(Free(club, PlayerRole.Defender), ref line, right: false));
        line = Lineup.GoalkeeperLine;
        Assert.Equal(LineupResult.NoRoom, club.ClickPlayer(Free(club, PlayerRole.Goalkeeper), ref line, right: false));
        Assert.Equal(before, LineupSlot.All.Select(s => club.Lineup[s]).ToList());
    }

    /// <summary>A683: Rechtsklick im Spiel (Datei 0x40E63, gleicher Suchcode) weicht ebenso in die andere Hälfte aus.</summary>
    [Fact]
    public void InMatchClick_ForwardHalfFull_GoesToDefenceHalf()
    {
        var club = NewClub();
        ClearLine(club, 2);
        int? line = 2;
        for (var i = 0; i < 3; i++)
            Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(Free(club, PlayerRole.Forward), ref line, right: false));
        var fourth = Free(club, PlayerRole.Forward);

        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(fourth, ref line, right: false));

        Assert.Same(fourth, club.Lineup[LineupSlot.Defender(2, 0)]);
    }

    /// <summary>Suchreihenfolge: eigene Hälfte, dann die andere; Torhüterreihe nur der Torhüterplatz.</summary>
    [Fact]
    public void SearchSlots_OwnHalfThenOther()
    {
        Assert.Equal(
            [LineupSlot.Forward(1, 0), LineupSlot.Forward(1, 1), LineupSlot.Forward(1, 2), LineupSlot.Defender(1, 0), LineupSlot.Defender(1, 1)],
            Lineup.SearchSlots(1, PlayerRole.Forward));
        Assert.Equal(
            [LineupSlot.Defender(2, 0), LineupSlot.Defender(2, 1), LineupSlot.Forward(2, 0), LineupSlot.Forward(2, 1), LineupSlot.Forward(2, 2)],
            Lineup.SearchSlots(2, PlayerRole.Defender));
        Assert.Equal([LineupSlot.Goalkeeper], Lineup.SearchSlots(Lineup.GoalkeeperLine, PlayerRole.Goalkeeper));
    }

    /// <summary>A490: Nationalteam, derselbe Code – ein Verteidiger kommt bei voller Abwehrhälfte in die Sturmhälfte.</summary>
    [Fact]
    public void NationalClick_DefenceHalfFull_GoesToForwardHalf()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.ClearFormation();
        Player First(PlayerRole role, int skip = 0) => coach.Rows.Where(p => p.Role == role && p.Available).Skip(skip).First();
        int? line = 0;
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(First(PlayerRole.Defender), ref line, right: false));
        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(First(PlayerRole.Defender, 1), ref line, right: false));
        var third = First(PlayerRole.Defender, 2);
        var dressed = coach.DressedCount;

        Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(third, ref line, right: false));

        Assert.Same(third, coach.Formation[LineupSlot.Forward(0, 0)]);
        Assert.Equal(PlayerStatus.InTeam, coach.StatusOf(third));
        Assert.Equal(dressed + 1, coach.DressedCount);
    }
}
