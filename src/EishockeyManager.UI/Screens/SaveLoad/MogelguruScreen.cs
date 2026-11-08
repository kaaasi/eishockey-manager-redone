namespace EishockeyManager.UI.Screens.SaveLoad;

/// <summary>
/// „Der kleine Mogelguru verabscheut Ihre Taten !“ – Bildschirm beim Laden eines Original-Spielstands mit falscher Prüfsumme
/// (Spur hok-import, Teil 3).
/// </summary>
/// <remarks>
/// [ORIGINAL] Texte em.exe DGROUP 0x2664 („SOLLTE ES IHNEN ETWA PASSIERT SEIN, DAß SIE OHNE JEGLICHE“), 0x269A („BÖSE ABSICHT
/// MIT EINEM MONITOR MANIPULIEREN WOLLTEN ?“), 0x26AF („DER KLEINE MOGELGURU“) und die folgende Zeile „VERABSCHEUT IHRE TATEN !“;
/// Fehlercode 5, Anzeige ab Datei 0x5CEFA (<c>docs/formate/spielstand-original.md</c>). Bild: Referenz-Repo
/// <c>original/spielstaende/bilder/kaputt_mogelguru.png</c> (KAPUTT.HOK, DOSBox): schwarzer Grund, Überschrift rot in zwei
/// Zeilen mit Winkel links oben, darunter zwei Zeilen kleine Schrift, zentriert.
/// Schrift der Überschrift newbold, der Zeilen darunter die kleine Schrift (Sichtvergleich mit dem Bild, Bildschirmfoto-Ablauf
/// <c>hok-import</c>). [APPROX] Lage nach dem DOSBox-Bild (640 × 480 halbiert) auf etwa 1 Pixel geschätzt, nicht per
/// Template-Matching vermessen; Farbe der kleinen Zeilen Weiß statt Hellblaugrau; Rückkehr mit Klick oder Taste zum
/// Laden-Dialog (Ablauf nach dem Bildschirm im Original nicht belegt).
/// </remarks>
public sealed class MogelguruScreen(ScreenHost host, Screen back) : Screen(host)
{
    /// <summary>Zeilen der Überschrift. [ORIGINAL] DGROUP 0x26AF ff.</summary>
    public static readonly string[] Title = ["DER KLEINE MOGELGURU", "VERABSCHEUT IHRE TATEN !"];

    /// <summary>Zeilen darunter. [ORIGINAL] DGROUP 0x2664, 0x269A.</summary>
    public static readonly string[] Text =
        ["SOLLTE ES IHNEN ETWA PASSIERT SEIN, DAß SIE OHNE JEGLICHE", "BÖSE ABSICHT MIT EINEM MONITOR MANIPULIEREN WOLLTEN ?"];

    public override void Update(InputState input)
    {
        if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
            Host.Show(back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Clear(Colors.Black);
        s.TextCentered(Assets.Bold, 166, 8, Title[0], Colors.Red);
        s.TextCentered(Assets.Bold, 160, 28, Title[1], Colors.Red);
        s.VLine(2, 2, 49, Colors.Red);
        s.HLine(2, 50, 149, Colors.Red);
        s.TextCentered(Assets.Mini, 160, 86, Text[0], Colors.White);
        s.TextCentered(Assets.Mini, 160, 96, Text[1], Colors.White);
    }
}
