using System.Reflection;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur transfer-original-vertrag (Runde 8, Welle 3): Pixelvergleich des Verhandlungsfensters (0x4A1DB, Vorlage
/// <c>oberflaeche_1030</c>: SCHMEISSER, 19 Jahre, 2 Jahre, 1.500 DM, keine Versicherung) und der Transferliste (0x465F1,
/// Vorlage <c>oberflaeche_1102</c>: die sechs Spieler des Videos, Maus auf LAPORTE „aus FRANKREICH“). Die Spieler werden mit
/// den im Bild lesbaren Werten nachgebaut; die Liste wird dafür im Ablauf (nicht im Spiel) über Reflexion ersetzt.
/// </summary>
internal static class TransferVertrag
{
    [Scenario("transfer-vertrag")]
    private static void Run(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];

        // Verhandlungsfenster: erster Torhüter wie im Video
        var keeper = club.Players.First(p => p.Role == PlayerRole.Goalkeeper);
        var schmeisser = new Player
        {
            Name = "SCHMEISSER", Role = PlayerRole.Goalkeeper, Age = 19, Condition = 33, Technique = 30, Form = 41,
            ContractYears = 2, MonthlySalary = 1500, Number = keeper.Number, Energy = 61,
        };
        club.Players[club.Players.IndexOf(keeper)] = schmeisser;
        club.Lineup.Remove(keeper);
        d.OpenTile("Verträge");
        var rows = TeamUi.Rows(club);
        var row = rows.ToList().IndexOf(schmeisser);
        d.Click(60, ContractsScreen.FirstRowY + row * ContractsScreen.RowPitch + 2);
        var insurance = ContractsScreen.InsuranceField(ContractsScreen.DialogRect(row));
        d.Move(insurance.X + 5, insurance.Y + 3); // Zeiger wie im Video auf „keine“
        d.Save("01_verhandlung_oberflaeche_1030");
        d.Press(Key.Escape);
        d.Press(Key.Escape);

        // Transferliste wie im Video (Preise exakt nach Teil 1, TransferOriginalTests.Price_MatchesVideo_Exactly)
        var france = game.Clubs.First(c => c.Name == "FRANKREICH");
        var donor = game.Clubs.First(c => c.Index != club.Index && c.League is not null);
        var video = new (string Name, PlayerRole Role, int C, int T, int F, int Age, PlayerTrend Trend, Club From)[]
        {
            ("LANGE", PlayerRole.Goalkeeper, 25, 37, 48, 26, PlayerTrend.Constant, donor),
            ("TROJAN", PlayerRole.Defender, 37, 31, 55, 23, PlayerTrend.Constant, donor),
            ("HANFT", PlayerRole.Defender, 16, 20, 53, 18, PlayerTrend.Constant, donor),
            ("J.REIL", PlayerRole.Defender, 47, 40, 47, 23, PlayerTrend.Rising, donor),
            ("IBELHERR", PlayerRole.Forward, 21, 31, 54, 31, PlayerTrend.Falling, donor),
            ("LAPORTE", PlayerRole.Forward, 76, 79, 47, 18, PlayerTrend.Constant, france),
        };
        var listings = (List<TransferListing>)typeof(TransferMarket)
            .GetField("_listings", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(game.TransferMarket)!;
        listings.Clear();
        foreach (var v in video)
        {
            var player = new Player
            {
                Name = v.Name, Role = v.Role, Condition = v.C, Technique = v.T, Form = v.F, Age = v.Age, Trend = v.Trend,
            };
            listings.Add(new TransferListing { Player = player, From = v.From, Price = TransferMarket.Price(player) });
        }
        d.OpenTile("Transfer");
        var laporte = TransferScreen.EntryBuy(5);
        d.Move(laporte.X + 26, laporte.Y + 3); // Zeiger wie im Video auf dem Kaufpreis
        d.Save("02_transferliste_oberflaeche_1102");
    }
}
