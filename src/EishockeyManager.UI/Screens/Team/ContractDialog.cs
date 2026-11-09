using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Fenster „Vertragsverhandlung“ (em.exe 0x4A1DB, Stubeintrag 0x118:0x75): Laufzeit, Gehalt, Versicherung, „Mach' ich!“ und
/// „Abbruch“. Gemeinsam für den Bildschirm „Verträge“ (Aufruf 0x40208), den Kauf (0x4786A) und das Saisonende (0x27D91).
/// Der Dialog hält nur die Eingaben; die Verhandlung führt der Aufrufer (<see cref="Contracts.Negotiate"/>,
/// <see cref="TransferMarket.Sign"/>, <see cref="GameState.NegotiateRenewal"/>).
/// </summary>
/// <remarks>
/// [ORIGINAL] Lage: Argument y des Dialogs (Datei 0x4A2F3: Titel bei y + 0x33 …): Verträge und Saisonende y = 80 für Zeilen
/// &lt; 12, sonst 0 (0x401D6, 0x27D74); der Kauf ruft immer mit y = 0 (Datei 0x4785D).
/// </remarks>
public sealed class ContractDialog
{
    public enum Outcome { None, Offer, Cancel }

    private enum Field { None, Years, Salary }

    private readonly Func<int, int?> _demand;
    private readonly Func<int> _insuranceOffer;
    private readonly Func<NegotiationResult?> _availability;
    private readonly int _baseYears, _baseSalary;
    private Field _edit;
    private string _editText = "";

    /// <param name="player">verhandelter Spieler.</param>
    /// <param name="rect">Rahmen (<see cref="ContractsScreen.DialogRect"/>, <see cref="Top"/>).</param>
    /// <param name="baseYears">Laufzeit ohne Eingabe.</param>
    /// <param name="baseSalary">Gehalt ohne Eingabe.</param>
    /// <param name="insurance">Versicherungsprämie beim Öffnen in DM (Kaderplatz +0x30), 0 = keine.</param>
    /// <param name="insuranceOffer">Prämie beim Einschalten (<see cref="Contracts.InsuranceOffer"/>).</param>
    /// <param name="demand">Forderung nach Eingabe der Laufzeit oder null (Level 5).</param>
    /// <param name="availability">Prüfung beim Anklicken von Laufzeit und Gehalt (0x4A692) oder null = frei.</param>
    public ContractDialog(Player player, Rect rect, int baseYears, int baseSalary, int insurance, Func<int> insuranceOffer,
        Func<int, int?> demand, Func<NegotiationResult?> availability)
    {
        Player = player;
        Rect = rect;
        _baseYears = baseYears;
        _baseSalary = baseSalary;
        Insurance = insurance;
        _insuranceOffer = insuranceOffer;
        _demand = demand;
        _availability = availability;
    }

    /// <summary>Lage beim Kauf: immer oben (y = 0, Datei 0x4785D).</summary>
    public static Rect Top => ContractsScreen.DialogRect(12);

    public Player Player { get; }

    public Rect Rect { get; }

    /// <summary>Eingegebene Laufzeit, 0 = keine Eingabe (wie im Original).</summary>
    public int Years { get; private set; }

    /// <summary>Gehalt im Feld, 0 = keine Eingabe.</summary>
    public int Salary { get; private set; }

    /// <summary>Versicherungsprämie im Fenster in DM, 0 = „keine“.</summary>
    public int Insurance { get; private set; }

    public bool Insured => Insurance > 0;

    /// <summary>Steht die vom Dialog gefüllte Forderung im Gehaltsfeld (nach Laufzeit, ohne spätere Gehaltseingabe)?</summary>
    public bool DemandFilled { get; private set; }

    /// <summary>Meldung nach einem abgewiesenen Klick auf Laufzeit oder Gehalt (der Aufrufer zeigt sie an).</summary>
    public NegotiationResult? Refusal { get; private set; }

    public Rect YearsField => ContractsScreen.YearsField(Rect);
    public Rect SalaryField => ContractsScreen.SalaryField(Rect);
    public Rect InsuranceField => ContractsScreen.InsuranceField(Rect);
    public Rect AcceptButton => ContractsScreen.AcceptButton(Rect);
    public Rect CancelButton => ContractsScreen.CancelButton(Rect);

    /// <summary>Knopf, über dem die linke Maustaste gerade gedrückt ist (Darstellung „gedrückt“), sonst null.</summary>
    public Rect? PressedButton { get; private set; }

    public Outcome Update(InputState input)
    {
        Refusal = null;
        PressedButton = PressedAt(input, AcceptButton, CancelButton);
        if (_edit != Field.None)
        {
            foreach (var ch in input.Text)
                if (char.IsAsciiDigit(ch) && _editText.Length < (_edit == Field.Years ? 1 : 7))
                    _editText += ch;
            if (input.Pressed(Key.Backspace) && _editText.Length > 0)
                _editText = _editText[..^1];
            if (input.Pressed(Key.Enter))
            {
                CommitEdit();
                return Outcome.None;
            }
            if (input.Pressed(Key.Escape))
            {
                _edit = Field.None;
                return Outcome.None;
            }
            if (input.LeftClick)
                CommitEdit(); // Klick daneben übernimmt die Eingabe und wirkt weiter
            else
                return Outcome.None;
        }

        if (input.Pressed(Key.Escape) || input.RightClick || input.Clicked(CancelButton))
            return Outcome.Cancel;
        if (input.Clicked(YearsField) || input.Clicked(SalaryField))
        {
            if (_availability() is { } refusal)
            {
                Refusal = refusal;
                return Outcome.None;
            }
            _edit = input.Clicked(YearsField) ? Field.Years : Field.Salary;
            _editText = "";
        }
        else if (input.Clicked(InsuranceField))
        {
            // [ORIGINAL] 0x4AA74–0x4AAD4: nur ohne Abschluss (+0x2C Bit 31) und nicht bei Verletzung; ein → Wert(Gehalt) / 2.
            if (Contracts.CanChangeInsurance(Player))
                Insurance = Insured ? 0 : _insuranceOffer();
        }
        else if (input.Clicked(AcceptButton) || input.Pressed(Key.Enter))
            return Outcome.Offer;
        return Outcome.None;
    }

    /// <summary>
    /// Eingabe übernehmen. [ORIGINAL] Laufzeit (0x4A7C5): leere Eingabe = bisherige Laufzeit ohne Forderung, sonst Forderung ins
    /// Gehaltsfeld (nicht im Level 5) und Merker „Forderung steht im Feld“; Gehalt (0x4A9F5): leere Eingabe = bisheriges Gehalt,
    /// Merker gelöscht. Werte über 4 Jahre bleiben stehen und scheitern erst bei „Mach' ich!“.
    /// </summary>
    private void CommitEdit()
    {
        var value = 0;
        var empty = _editText.Length == 0 || !int.TryParse(_editText, out value);
        if (_edit == Field.Years && empty)
        {
            Years = _baseYears; // 0x4A808: leere Eingabe, keine Forderung, Merker gelöscht
            DemandFilled = false;
        }
        else if (_edit == Field.Years)
        {
            Years = Math.Max(value, Contracts.MinYears);
            if (_demand(Years) is { } demand)
                Salary = demand;
            DemandFilled = true;
        }
        else if (_edit == Field.Salary)
        {
            Salary = empty ? _baseSalary : Math.Max(value, Contracts.MinSalary); // Video: 0 DM wird zu 1 DM
            DemandFilled = false;
        }
        _edit = Field.None;
    }

    public void Draw(Surface s, Assets assets)
    {
        var mini = assets.Mini;
        var p = Player;
        var d = Rect;
        TeamUi.DialogFrame(s, d);
        TeamUi.DialogTitle(s, assets, 84, d.Y + 5, "Vertragsverhandlung");
        s.HLine(5, d.Y + 14, 159, Colors.White);
        s.TextCentered(mini, 80, d.Y + 18, $"{p.Name}, {p.Age} Jahre alt", Colors.White); // Video: Mitte bei x = 79–80

        const byte label = Colors.White;
        s.Text(mini, 7, d.Y + 27, "Vertragsdauer", label);
        s.Text(mini, 7, d.Y + 35, "Gehalt / Monat", label);
        s.Text(mini, 7, d.Y + 43, "Versicherung", label);
        for (var i = 0; i < 3; i++)
            s.Text(mini, 77, d.Y + 27 + 8 * i, ":", Colors.White);

        DrawValue(s, mini, Field.Years, d.Y + 27, Core.Player.YearsText(Years != 0 ? Years : _baseYears));
        DrawValue(s, mini, Field.Salary, d.Y + 35, ContractsScreen.FormatMoney(Salary != 0 ? Salary : _baseSalary));
        // [ORIGINAL] 0x4AAD4–0x4AB1C: +0x30 = 0 → „keine“, sonst der Betrag mit „ DM“ (0x38:0x2D8).
        s.Text(mini, 81, d.Y + 43, Insured ? ContractsScreen.FormatMoney(Insurance) : "keine", Colors.White);

        Button(s, assets, AcceptButton, "Mach' ich!", PressedButton == AcceptButton);
        Button(s, assets, CancelButton, "Abbruch", PressedButton == CancelButton);
    }

    /// <summary>Knopf unter der gedrückten linken Maustaste (<see cref="Button"/>).</summary>
    public static Rect? PressedAt(InputState input, params Rect[] buttons) =>
        input.LeftDown ? buttons.Cast<Rect?>().FirstOrDefault(r => r!.Value.Contains(input.MouseX, input.MouseY)) : null;

    /// <summary>
    /// Knopf der Dialoge mit Zustand. [ORIGINAL] 0x120:0x75 (Datei 0x4C196) legt den Knopf an und zeichnet ihn mit Zustand 0;
    /// das fünfte Argument (0x12 bei „Mach' ich!“ und „Verkaufen“, sonst 1) liest die Funktion nicht. Die Warteschleife
    /// 0x120:0x6B (0x4BEBD) zeichnet den Knopf unter der gedrückten Maustaste mit Zustand 1 (kb\2 ab (17 | 179 + 12 · Zustand),
    /// 0x4BFF2) und beim Loslassen wieder mit Zustand 0; die Beschriftung bleibt gleich. Der hellere „Mach' ich!“ im Video ist
    /// also der gedrückte Knopf.
    /// </summary>
    internal static void Button(Surface s, Assets assets, Rect r, string text, bool pressed)
    {
        TeamUi.DialogButton(s, assets, r.X, r.Y, text);
        if (!pressed)
            return;
        s.Blit(assets.Image(2), new Rect(17, 179 + 12, Widgets.ButtonWidth, Widgets.ButtonHeight), r.X, r.Y, transparent: 0);
        var tx = r.X + Widgets.ButtonWidth / 2 + 1 - assets.Mini.Measure(text) / 2; // wie TeamUi.DialogButton
        s.Text(assets.Mini, tx + 1, r.Y + 4, text, 1);
        s.Text(assets.Mini, tx, r.Y + 3, text, TeamUi.DialogDark);
    }

    private void DrawValue(Surface s, BitmapFont mini, Field field, int y, string text)
    {
        if (_edit != field)
        {
            s.Text(mini, 81, y, text, Colors.White);
            return;
        }
        var x = s.Text(mini, 81, y, _editText, Colors.White);
        s.Fill(new Rect(x + 1, y, 3, 5), 1); // Eingabemarke wie im Video
    }
}
