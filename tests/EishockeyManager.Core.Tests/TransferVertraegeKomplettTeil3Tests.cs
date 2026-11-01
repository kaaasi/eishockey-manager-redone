using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Teil 3: unaufgeforderte Angebote für Spieler im Kader eines Managers (Kaderplatz
/// +0x09 Bit 0x40, Tagesablauf 0x07972–0x07AEF, Bieter 0x059B8, Höhe beim Anklicken 0x46E0E, Verkauf 0x46F3C). Vorher rot:
/// Das Remake kannte Angebote nur für Spieler auf der Transferliste.
/// </summary>
public class TransferVertraegeKomplettTeil3Tests
{
    /// <summary>random(lo, hi) liefert der Reihe nach die angegebenen Werte (Next(n) = Wert − lo).</summary>
    private sealed class Script(params int[] values) : Random
    {
        private readonly Queue<int> _values = new(values);
        public int Draws { get; private set; }

        public override int Next(int minValue, int maxValue)
        {
            Draws++;
            var v = _values.Dequeue();
            Assert.InRange(v, minValue, maxValue - 1);
            return v;
        }

        public override int Next(int maxValue) => Next(0, maxValue);
    }

    /// <summary>[ORIGINAL] 0x079DE: w = Wert / 10000; random(0, 200) &lt; w, dann random(0, 450) = 0 (zweiter Wurf nur danach).</summary>
    [Fact]
    public void Roll_FollowsTheDailyRule()
    {
        Assert.True(UnsolicitedOfferRules.Roll(900_000, new Script(89, 0)));
        Assert.False(UnsolicitedOfferRules.Roll(900_000, new Script(90))); // 90 < 90 nicht erfüllt, kein zweiter Wurf
        Assert.False(UnsolicitedOfferRules.Roll(900_000, new Script(0, 1)));
        var script = new Script(200);
        Assert.False(UnsolicitedOfferRules.Roll(9_999, script)); // w = 0
        Assert.Equal(1, script.Draws);
    }

    /// <summary>[ORIGINAL] 0x07A61: Europapokal-Gegner nur bei w &gt; 90 mit random(0, 10) = 0; sonst kein Wurf.</summary>
    [Fact]
    public void Foreign_OnlyForValuablePlayers()
    {
        var none = new Script();
        Assert.False(UnsolicitedOfferRules.Foreign(909_999, none)); // w = 90
        Assert.Equal(0, none.Draws);
        Assert.True(UnsolicitedOfferRules.Foreign(910_000, new Script(0)));
        Assert.False(UnsolicitedOfferRules.Foreign(910_000, new Script(1)));
    }

    /// <summary>[ORIGINAL] 0x46E0E: Wert · random(87, 125) bzw. random(112, 155) (Verein &gt; 0x3F), / 30000 · 300.</summary>
    [Fact]
    public void Amount_IsValueTimesFactor_InFullHundreds()
    {
        Assert.Equal(900_000 * 87 / 30000 * 300, UnsolicitedOfferRules.AmountThirds(900_000, false, new Script(87)));
        Assert.Equal(900_000 * 125 / 30000 * 300, UnsolicitedOfferRules.AmountThirds(900_000, false, new Script(125)));
        Assert.Equal(123_456 * 155 / 30000 * 300, UnsolicitedOfferRules.AmountThirds(123_456, true, new Script(155)));
        Assert.Equal(0, UnsolicitedOfferRules.AmountThirds(123_456, true, new Script(112)) % 300);
    }

    /// <summary>Befund 0x49332 (Markt-Angebot, Argument 0): Ablösesumme = Angebot − Angebot / 7 · Restlaufzeit (Drittel-DM).</summary>
    [Fact]
    public void MarketTransferFee_SubtractsSeventhPerContractYear()
    {
        Assert.Equal(700_000 - 100_000 * 2, UnsolicitedOfferRules.MarketTransferFeeThirds(700_000, 2));
        Assert.Equal(300_000 - 300_000 / 7, UnsolicitedOfferRules.MarketTransferFeeThirds(300_000, 1));
        Assert.Equal(300_000, UnsolicitedOfferRules.MarketTransferFeeThirds(300_000, 0));
    }

    /// <summary>
    /// [ORIGINAL] 0x059B8: Verein aus 0 … 42 (bzw. 64 … 149); Managerverein ohne zweiten Wurf verworfen; angenommen, wenn
    /// |Vereinsstärke − Spielerstärke| &lt; random(2, 10).
    /// </summary>
    [Fact]
    public void Bidder_PicksClubOfSimilarStrength()
    {
        int? Strength(int club) => club switch { 3 => null, 5 => 70, 7 => 52, _ => 0 };
        // Verein 3 (Manager) → nächster Versuch; Verein 5: |70 − 50| = 20 nicht &lt; 9; Verein 7: |52 − 50| = 2 &lt; 3.
        var script = new Script(3, 5, 9, 7, 3);
        Assert.Equal(7, UnsolicitedOfferRules.Bidder(50, foreign: false, Strength, script));
        Assert.Equal(5, script.Draws);
        Assert.Equal(64, UnsolicitedOfferRules.Bidder(0, foreign: true, _ => 0, new Script(64, 2)));
    }

    /// <summary>
    /// Saisonlauf: Spieler der Manager bekommen Angebote (Meldung im Posteingang); Anklicken löscht das Angebot und würfelt die
    /// Höhe, „Verkaufen“ bucht den Betrag ohne Abzug und gibt den Spieler ab. Vorher rot (kein Angebot).
    /// </summary>
    [Fact]
    public void Season_BringsUnsolicitedOffers_AndSellingPaysTheAmount()
    {
        var (game, offered) = PlayUntilOffer(7);
        var manager = game.Managers.First(m => game.Clubs[m.ClubIndex].Players.Contains(offered));
        var club = game.Clubs[manager.ClubIndex];
        var buyer = offered.UnsolicitedOffer!;
        Assert.DoesNotContain(game.Managers, m => m.ClubIndex == buyer.Index);
        Assert.Contains(game.Inbox(manager), m => m.Lines.Any(l => l.Contains("interessiert")));

        var money = manager.Money;
        var offer = game.OpenUnsolicitedOffer(manager, offered);
        Assert.NotNull(offer);
        Assert.Null(offered.UnsolicitedOffer); // Bit 0x40 ist mit dem Anklicken weg
        Assert.Null(game.OpenUnsolicitedOffer(manager, offered));
        Assert.Equal(0, offer.Amount % 100);
        Assert.True(offer.Amount > 0);

        Assert.Equal(TransferResult.Accepted, game.AcceptUnsolicitedOffer(offer));
        Assert.Equal(money + offer.Amount, manager.Money);
        Assert.DoesNotContain(offered, club.Players);
        Assert.False(club.Lineup.Contains(offered));
        if (buyer.Index < UnsolicitedOfferRules.FirstForeignClub)
            Assert.Contains(offered, buyer.Players);
        else
            Assert.Contains(offered, game.TransferMarket.FreeAgents);
        Assert.Equal(TransferResult.Impossible, game.AcceptUnsolicitedOffer(offer));
    }

    /// <summary>„Behalten“: das Angebot ist mit dem Anklicken verfallen, der Spieler bleibt, kein Geld.</summary>
    [Fact]
    public void Keeping_DropsTheOffer()
    {
        var (game, offered) = PlayUntilOffer(8);
        var manager = game.Managers.First(m => game.Clubs[m.ClubIndex].Players.Contains(offered));
        var money = manager.Money;
        Assert.NotNull(game.OpenUnsolicitedOffer(manager, offered));
        Assert.Equal(money, manager.Money);
        Assert.Contains(offered, game.Clubs[manager.ClubIndex].Players);
        Assert.Null(offered.UnsolicitedOffer);
    }

    /// <summary>Das Angebot übersteht Sichern und Laden (Schema 21) und zeigt auf denselben Verein.</summary>
    [Fact]
    public void Offer_SurvivesSaveAndLoad()
    {
        var (game, offered) = PlayUntilOffer(9);
        var saved = SaveGame.Save(game);
        var loaded = SaveGame.Load(saved);
        Assert.True(loaded.Success, loaded.Error);
        var copy = loaded.Game!.AllPlayers.Single(p => p.Id == offered.Id);
        Assert.Equal(offered.UnsolicitedOffer!.Index, copy.UnsolicitedOffer?.Index);
        Assert.Equal(saved, SaveGame.Save(loaded.Game!));
    }

    /// <summary>
    /// Mehrsaisonprobe (Kader, Geld): vier Saisonwechsel; aktiv verkauft der Manager jedes Angebot, passiv behält er es. Angebote
    /// gibt es nur für eigene, nicht geliehene Spieler; Sichern/Laden bleibt bytegleich.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_UnsolicitedOffers(bool active)
    {
        var game = TestGames.Create(23);
        var offers = 0;
        for (var change = 0; change < 4; change++)
        {
            for (var day = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; day++)
            {
                Assert.True(day < 1000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                {
                    TestSeason.KeepSquad(game, manager);
                    var club = game.Clubs[manager.ClubIndex];
                    foreach (var p in club.Players.Where(p => p.UnsolicitedOffer is not null).ToList())
                    {
                        Assert.False(p.OnLoan);
                        offers++;
                        var offer = game.OpenUnsolicitedOffer(manager, p)!;
                        if (active && club.Players.Count > 18)
                            Assert.Equal(TransferResult.Accepted, game.AcceptUnsolicitedOffer(offer));
                    }
                }
                game.Continue();
            }
            // Ein Angebot vom letzten Tag bleibt wie Bit 0x40 im Original über den Saisonwechsel stehen.
            Assert.All(game.Clubs.Where(c => game.Managers.All(m => m.ClubIndex != c.Index)).SelectMany(c => c.Players),
                p => Assert.Null(p.UnsolicitedOffer));
            game.StartNextSeason();
            var saved = SaveGame.Save(game);
            var loaded = SaveGame.Load(saved);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(saved, SaveGame.Save(loaded.Game!));
        }
        Assert.True(offers >= 1, "kein Angebot in vier Saisons");
    }

    /// <summary>Spielt Tag für Tag, bis ein Spieler eines Managers ein Angebot hat.</summary>
    private static (GameState Game, Player Player) PlayUntilOffer(int seed)
    {
        for (var s = seed; s < seed + 40; s++)
        {
            var game = TestGames.Create(s);
            for (var day = 0; day < 1000 && game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; day++)
            {
                foreach (var manager in game.Managers)
                    TestSeason.KeepSquad(game, manager);
                game.Continue();
                var offered = game.Managers.SelectMany(m => game.Clubs[m.ClubIndex].Players)
                    .FirstOrDefault(p => p.UnsolicitedOffer is not null);
                if (offered is not null)
                    return (game, offered);
            }
        }
        throw new InvalidOperationException("kein Angebot gefunden");
    }
}
