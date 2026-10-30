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
/// Spur finanzen-2 (#67) ändert die Spielregeln (Monatsabschluss, Zuschauer, Werbung, Startkonto) und damit den
/// Verlauf; die Abdrücke sind dort mit dem unveränderten Generator neu ermittelt. Sie bleiben Referenz dafür, dass
/// spätere Generator- oder Formatänderungen den Verlauf nicht verschieben.
/// Spur bundestrainer-modus4 (#123) ändert die Spielregeln im Modus 4: Der Verein des Probe-Managers steht unter den
/// ersten fünf der 1. Liga, er wird am 13. April Bundestrainer; WM-Ergebnisse und Pokal-Zufallsquelle verschieben sich.
/// Die synthetischen Abdrücke sind dort neu ermittelt (Gegenprobe ohne Ernennung: alte Werte unverändert).
/// Spur finanzen-nachpruefung (Runde 5) ändert Zuschauerformel (em.exe 0x3F63C) und Saisonprämien (0x55CD3, 0x2723C);
/// die Abdrücke sind mit dem unveränderten Generator neu ermittelt. Ursache je Fall (FN-01, #139, Tabelle in
/// docs/finanzen-rekonstruktion.md): synthetisch alle vier Europapokalsieg 1993 ohne Prämie (−100.000 DM); Originaldaten
/// aktiv ein Zug weniger aus FinanceRandom bei der Zuschauerteilung.
/// Spur finanzen-werbung (Runde 6, #132–#134) ändert Werbebewertung, Werbeangebote und den Tag des Monatsabschlusses;
/// alle sechs Abdrücke sind mit unverändertem Generator neu ermittelt. In allen sechs Fällen bleiben Haupt-, Pokal- und
/// Vertragszufallsquelle in jedem Schritt gleich (keine Spielergebnisse, Auslosungen oder Vertragsentscheidungen
/// verschoben); es ändern sich nur FinanceRandom (ab Tag 1: Bewertung nach jedem Tagesabschluss, 0x5569F) und daraus das
/// Konto (Tabelle in docs/finanzen-rekonstruktion.md).
/// Spur heimspiel-statistik (#136, „Weniger als neun Spieler“, em.exe 0x54420) ändert die drei passiven Abdrücke; die
/// aktiven verlängern Verträge und bleiben gleich. Ursache je Fall (Vergleichslauf mit Zustand und allen vier
/// Zufallsquellen nach jedem Schritt, Tabelle in docs/finanzen-rekonstruktion.md): Die Startverträge des passiven Managers
/// sind nach 1994 ausgelaufen, sein Verein hat 1995 keinen Spieler. Ab dem ersten Spiel 1995 zahlt er je Spiel
/// 200.000 DM (Strafen) statt Eintritt zu bekommen; die Zuschauerberechnung und damit ihr Zug aus FinanceRandom entfällt.
/// Haupt-, Pokal- und Vertragsquelle, Titel und Kader bleiben in jedem Schritt gleich; die Kontountergrenze der Probe
/// (10 Mio. DM) verhindert die Entlassung.
/// Mit der Spielwertung (#165, em.exe 0x36DC5) werden die Spiele dieses Vereins ab 1995 ohne Berechnung 0:2 bzw. 2:0
/// gewertet (Vergleich gegen 7833e2d): erste Differenz 1995, Tag 3 – das Ergebnis des ersten Spiels und der Stand der
/// Hauptquelle (Torzüge entfallen); danach Pokalquelle (Tag 14 bzw. 17, anderer Pokalverlauf), FinanceRandom und
/// Vertragsquelle (Saisonwechsel). Titel ab 1995 andere. Die drei aktiven Abdrücke bleiben gleich (ihr Kader bleibt
/// spielfähig, in keinem Schritt eine Differenz).
/// Spur boerse-start (#169) rechnet die Kurse nach em.exe (0x5784A, 90 Börsentage vor Spielbeginn) und zieht dafür anders
/// aus FinanceRandom. Alle sechs Abdrücke neu; Vergleichslauf gegen 15d0733 mit Zustand und allen vier Zufallsquellen
/// nach jedem Schritt: Haupt-, Pokal- und Vertragsquelle, Ergebnisse und Kader in jedem Schritt gleich. Es ändern sich
/// FinanceRandom ab dem ersten Schritt (1993, Tag 3) und daraus die Kontostände (Zuschauer, Werbung): synthetisch Seed 3
/// ab 1994 Tag 12, Seed 11 ab 1994 Tag 7 (vorher verdeckt die Kontountergrenze der Probe die Differenz), Originaldaten ab
/// 1993 Tag 3. Titel unverändert.
/// Danach (Entscheidung Nutzer, #188) rechnet die Börse erst nach dem Tagesabschluss aller Manager: Vergleich gegen
/// 801c732 wie oben – wieder nur FinanceRandom (ab 1993 Tag 3) und Konten (synthetisch ab 1994 Tag 12 bzw. 7,
/// Originaldaten ab 1993 Tag 10) verschoben.
/// </summary>
public class RngTwinProbeTests
{
    private const int SeasonChanges = 4;

    [Theory]
    [InlineData(false, 3, "846D463B02423F4A")]
    [InlineData(true, 3, "6D4B9A6331F462D3")]
    [InlineData(false, 11, "A6F8CEB952894F5E")]
    [InlineData(true, 11, "A0AF0802333A8B32")]
    public void FourSeasonChanges_FingerprintUnchangedBySerializableGenerator(bool active, int seed, string expected)
    {
        var game = TestGames.Create(seed);
        Assert.Equal(expected, Run(game, active));
    }

    [GameDataTheory]
    [InlineData(false, "53593E5794E25EB9")]
    [InlineData(true, "DD33816D93BB9252")]
    public void FourSeasonChanges_OriginalDatabase_FingerprintUnchanged(bool active, string expected)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = 42 });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 3);
        Assert.Equal(expected, Run(game, active));
    }

    /// <summary>Spielt <see cref="SeasonChanges"/> Saisonwechsel über <see cref="GameState.Continue"/> und bildet den Abdruck.</summary>
    internal static string Run(GameState game, bool active, Action<GameState>? afterStep = null, int changes = SeasonChanges,
        Action<GameState>? beforeChange = null)
    {
        for (var change = 0; change < changes; change++)
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
            beforeChange?.Invoke(game);
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
        var sb = Dump(game);
        sb.Append(game.Random.Next()).Append('|').Append(game.FinanceRandom.Next()).Append('|')
          .Append(game.CupRandom.Next()).Append('|').Append(game.ContractRandom.Next());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())))[..16];
    }

    /// <summary>Abdruck ohne Zufallsverbrauch (Spur „speichern“, Teil „format“): Zustand plus Zufallsabbild.</summary>
    internal static (string State, RngSnapshot Random) Snapshot(GameState game) =>
        (Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Dump(game).ToString())))[..16], game.CaptureRng());

    private static StringBuilder Dump(GameState game)
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
        return sb;
    }

    /// <summary>
    /// Nicht im Abdruck: <c>Player.Id</c> (Teil „ids“) gehört nur zum Spielstandformat und verändert den Spielverlauf
    /// nicht; so bleiben die auf <c>5f8e02a</c> ermittelten Abdrücke vergleichbar. Ebenso <c>Club.DressedCount</c>
    /// (Spur taktik-verein): abgeleitete Anzeige „Eingesetzt: n“, wirkt nicht auf den Spielverlauf – mit dieser Ausnahme
    /// stimmen alle sechs Abdrücke unverändert.
    /// </summary>
    private static readonly HashSet<string> Excluded = ["Id", "DressedCount"];

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
