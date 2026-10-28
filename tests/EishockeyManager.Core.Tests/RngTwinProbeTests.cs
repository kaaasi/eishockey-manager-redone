using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur „speichern“, Teil „rng“ – Mehrsaisonprobe: Die Umstellung der Zufallsquellen auf <see cref="GameRandom"/>
/// darf keinen Spielverlauf verändern. Die Abdrücke wurden auf <c>main</c> <c>5f8e02a</c> (noch mit
/// <see cref="System.Random"/>) ermittelt und müssen nach der Umstellung unverändert sein.
/// </summary>
public class RngTwinProbeTests
{
    private const int SeasonChanges = 4;

    [Theory]
    [InlineData(false, 3, "DF2E4C728CDA887D")]
    [InlineData(true, 3, "960DCF6E91C20560")]
    [InlineData(false, 11, "9F1D7B8F600A0346")]
    [InlineData(true, 11, "65C75DC4E275DFC9")]
    public void FourSeasonChanges_FingerprintUnchangedBySerializableGenerator(bool active, int seed, string expected)
    {
        var game = TestGames.Create(seed);
        Assert.Equal(expected, Run(game, active));
    }

    [GameDataTheory]
    [InlineData(false, "C02392CC5FD90EBC")]
    [InlineData(true, "72FDBB9B29D7E2F5")]
    public void FourSeasonChanges_OriginalDatabase_FingerprintUnchanged(bool active, string expected)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = 42 });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 3);
        Assert.Equal(expected, Run(game, active));
    }

    /// <summary>Spielt <see cref="SeasonChanges"/> Saisonwechsel über <see cref="GameState.Continue"/> und bildet den Abdruck.</summary>
    internal static string Run(GameState game, bool active, Action<GameState>? afterStep = null)
    {
        for (var change = 0; change < SeasonChanges; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                    manager.Money = Math.Max(manager.Money, 10_000_000); // keine Entlassung
                if (active)
                    ExtendOffers(game);
                game.Continue();
                afterStep?.Invoke(game);
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            game.StartNextSeason();
            if (active)
                SignFreeAgents(game);
            afterStep?.Invoke(game);
        }
        return Fingerprint(game);
    }

    private static Club OwnClub(GameState game) => game.Clubs[game.Managers[0].ClubIndex];

    private static void ExtendOffers(GameState game)
    {
        foreach (var p in OwnClub(game).Players.Where(p => p.Notice == ContractNotice.Offered && p.ContractYears == 1).ToArray())
            Contracts.Offer(p, p.NoticeYears, Contracts.Demand(p) + 100, false, game.PlayedMatchDays, game.Random);
    }

    private static void SignFreeAgents(GameState game)
    {
        var manager = game.Managers[0];
        while (OwnClub(game).Players.Count < 20
               && game.TransferMarket.Listings.FirstOrDefault(l => l.IsFreeAgent) is { } listing)
        {
            if (game.TransferMarket.Buy(game, manager, listing, 0, loan: false) != TransferResult.Accepted)
                break;
        }
    }

    /// <summary>
    /// Abdruck über Manager, Vereine, Kader, Pool, Laufbahnende und den Stand aller vier Zufallsquellen
    /// (je ein weiterer Wert, danach ist der Spielstand verbraucht).
    /// </summary>
    internal static string Fingerprint(GameState game)
    {
        var sb = new StringBuilder();
        sb.Append(game.Season).Append('|').Append(game.Phase).Append('|').Append(game.CurrentDay).Append('\n');
        foreach (var manager in game.Managers)
            Dump(sb, manager);
        foreach (var club in game.Clubs)
        {
            Dump(sb, club);
            foreach (var p in club.Players)
                Dump(sb, p);
        }
        sb.Append("pool\n");
        foreach (var p in game.TransferMarket.FreeAgents)
            Dump(sb, p);
        sb.Append("retired\n");
        foreach (var p in game.RetiredPlayers)
            Dump(sb, p);
        sb.Append(game.Random.Next()).Append('|').Append(game.FinanceRandom.Next()).Append('|')
          .Append(game.CupRandom.Next()).Append('|').Append(game.ContractRandom.Next());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())))[..16];
    }

    /// <summary>
    /// Nicht im Abdruck: <c>Player.Id</c> (Teil „ids“) gehört nur zum Spielstandformat und verändert den Spielverlauf
    /// nicht; so bleiben die auf <c>5f8e02a</c> ermittelten Abdrücke vergleichbar.
    /// </summary>
    private static readonly HashSet<string> Excluded = ["Id"];

    private static void Dump(StringBuilder sb, object value)
    {
        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.GetIndexParameters().Length == 0 && IsSimple(p.PropertyType) && !Excluded.Contains(p.Name))
                     .OrderBy(p => p.Name, StringComparer.Ordinal))
            sb.Append(property.Name).Append('=').Append(property.GetValue(value)).Append(';');
        sb.Append('\n');
    }

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
    }
}
