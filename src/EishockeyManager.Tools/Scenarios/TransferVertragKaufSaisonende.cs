using System.Reflection;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur transfer-vertraege-komplett, Teil 1: Pixelvergleich der neuen Lagen des Verhandlungsfensters.
/// 1. Nach einem angenommenen Kauf (0x4786A, y = 0) über der Transferliste, Vorlage <c>karriere_01924</c>: RUSSLER, 25 Jahre,
///    4 Jahre, 5.125 DM, keine Versicherung, Zeiger auf „Mach' ich!“.
/// 2. Am Saisonende nach „Erhaltene Prämien“ (0x27D91, Zeile &lt; 12 → y = 80), Vorlage <c>karriere_03555</c>: A.ONNEN,
///    27 Jahre, 1 Jahr, 6.480 DM, keine Versicherung.
/// Laufzeit und Gehalt im Fenster werden im Ablauf (nicht im Spiel) über Reflexion auf die Werte des Videos gesetzt.
/// </summary>
internal static class TransferVertragKaufSaisonende
{
    [Scenario("transfer-vertrag-kauf-saisonende")]
    private static void Run(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        game.ActiveManager.Money = 5_000_000;

        // 1. Kauf: ein Spieler des Computermarkts, im Ablauf auf die Werte des Videos gesetzt
        var listings = (List<TransferListing>)typeof(TransferMarket)
            .GetField("_listings", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(game.TransferMarket)!;
        var market = listings.First(l => l.Seller is null && !l.IsFreeAgent && l.Player.Role == PlayerRole.Forward);
        listings.RemoveAll(l => l != market);
        var russler = market.Player;
        typeof(Player).GetProperty(nameof(Player.Name))!.SetValue(russler, "RUSSLER"); // init-only, nur im Ablauf
        russler.Age = 25;
        d.OpenTile("Transfer");
        var screen = (TransferScreen)d.Host.Current!;
        var buy = TransferScreen.EntryBuy(0);
        d.Click(buy.X + 5, buy.Y + 2);
        d.Click(TransferDialog.AmountField.X + 5, TransferDialog.AmountField.Y + 2);
        d.Type((listings[0].Price * 2).ToString());
        d.Press(Key.Enter);
        d.Press(Key.Enter); // über 130 % nimmt der Computermarkt an (0x4991D)
        var contract = screen.Contract ?? throw new InvalidOperationException("Verhandlungsfenster fehlt");
        SetTerms(contract, 4, 5125);
        d.Move(contract.AcceptButton.X + 30, contract.AcceptButton.Y + 4);
        d.Save("01_kauf_karriere_01924");
        d.Click(contract.CancelButton.X + 5, contract.CancelButton.Y + 3);
        d.Click(10, 10);
        d.Press(Key.Escape);

        // 2. Saisonende
        for (var guard = 0; game.Phase != SeasonPhase.SeasonOver && guard < 40000; guard++)
        {
            game.ActiveManager.Money = Math.Max(game.ActiveManager.Money, 5_000_000);
            d.Advance();
        }
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        foreach (var p in club.Players)
        {
            p.ContractYears = 3;
            p.Notice = ContractNotice.None;
        }
        var rows = TeamUi.Rows(club).ToList();
        var onnen = rows.Take(12).Last(p => p is not null && !p.OnLoan)!;
        onnen.ContractYears = 1;
        typeof(Player).GetProperty(nameof(Player.Name))!.SetValue(onnen, "A.ONNEN"); // init-only, nur im Ablauf
        onnen.Age = 26; // wird beim Saisonwechsel 27
        ContractsScreen? contracts = null;
        for (var guard = 0; contracts is null && guard < 200; guard++)
        {
            d.Press(Key.Enter);
            contracts = d.Host.Current as ContractsScreen;
        }
        var dialog = contracts?.Dialog ?? throw new InvalidOperationException("Verhandlung am Saisonende fehlt");
        SetTerms(dialog, 1, 6480);
        d.Move(160, dialog.Rect.Y + 20); // Zeiger wie im Video rechts am Fensterrand
        d.Save("02_saisonende_karriere_03555");
    }

    private static void SetTerms(ContractDialog dialog, int years, int salary)
    {
        typeof(ContractDialog).GetProperty(nameof(ContractDialog.Years))!.SetValue(dialog, years);
        typeof(ContractDialog).GetProperty(nameof(ContractDialog.Salary))!.SetValue(dialog, salary);
    }
}
