using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>
/// Finanzbuch: Einnahmen und Ausgaben je Kategorie für die laufende und die vorige Saison
/// (Klick auf Bankbild oder Kontostand im Bildschirm Finanzen). Klick oder Taste = zurück.
/// </summary>
/// <remarks>
/// [APPROX] Das Finanzbuch ist in keinem Video zu sehen; Fenster im Stil der übrigen Bildschirme
/// (oranger Rahmen über dem Finanzbildschirm), Kategorien laut Anleitung.
/// </remarks>
public sealed class FinanceBookScreen : Screen
{
    public static readonly Rect Window = new(30, 14, 260, 212);
    private const int FirstRowY = 52;
    private const int RowPitch = 8;
    private const int LabelX = 44;
    private const int CurrentRight = 210;
    private const int PreviousRight = 276;
    private const byte HeaderColor = 32;

    private readonly Screen _back;

    public FinanceBookScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    public override void Update(InputState input)
    {
        if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        _back.Draw(s);
        var game = Host.Game!;
        var book = game.Finance(game.ActiveManager).Book;
        var mini = Assets.Mini;

        Widgets.Frame(s, Assets, Window, FrameStyle.Orange, Colors.PanelFill);
        Widgets.ShadowText(s, Assets.Flow, 160, 24, "FINANZBUCH", 11, 198);

        s.Text(mini, LabelX, 40, "KATEGORIE", HeaderColor);
        s.TextRight(mini, CurrentRight, 40, $"{game.Season}/{(game.Season + 1) % 100:00}", HeaderColor);
        s.TextRight(mini, PreviousRight, 40, "VORSAISON", HeaderColor);
        s.HLine(LabelX - 4, 47, Window.W - 2 * (LabelX - Window.X - 4), 1);

        var y = FirstRowY;
        foreach (var category in Enum.GetValues<FinanceCategory>())
        {
            s.Text(mini, LabelX, y, FinanceBook.Label(category), Colors.White);
            Amount(s, CurrentRight, y, book.Current(category));
            Amount(s, PreviousRight, y, book.Previous(category));
            y += RowPitch;
        }

        s.HLine(LabelX - 4, y, Window.W - 2 * (LabelX - Window.X - 4), 1);
        y += 4;
        s.Text(mini, LabelX, y, "Summe", Colors.Yellow);
        Amount(s, CurrentRight, y, book.CurrentTotal);
        Amount(s, PreviousRight, y, book.PreviousTotal);
    }

    private void Amount(Surface s, int right, int y, long amount) =>
        s.TextRight(Assets.Mini, right, y, GameState.FormatMoney(amount), amount < 0 ? Colors.Red : Colors.White);
}
