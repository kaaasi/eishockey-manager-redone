using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>Gemeinsame Bausteine der Mannschafts-Bildschirme (Aufstellung, Taktik, Verträge, Transfer, Training).</summary>
public static class TeamUi
{
    /// <summary>Textfarben der Spielerlisten je Art (Video: Torhüter hellblau, Abwehr gelb, Sturm orange).</summary>
    public static byte RoleColor(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => 30,
        PlayerRole.Defender => 24,
        _ => 9,
    };

    /// <summary>Kopfzeilen der Listen.</summary>
    public const byte HeaderColor = Colors.White;

    /// <summary>Linien unter der Kopfzeile und über der Statuszeile.</summary>
    public const byte LineColor = Colors.White;

    /// <summary>Markierung der Zeile unter der Maus (Video: dunkelrote Linien über und unter der Zeile).</summary>
    public const byte HoverColor = 17;

    /// <summary>Gedämpfte Beschriftung („1.“ an den Blöcken, „TW“).</summary>
    public const byte DimColor = 1;

    public const int SmallButtonWidth = 17;
    public const int SmallButtonHeight = 11;

    /// <summary>Kleiner grauer Knopf aus kb\2 (17 × 11) mit mittiger Beschriftung, z. B. Rückennummern der Aufstellung.</summary>
    public static void SmallButton(Surface s, Assets a, int x, int y, string text, bool pressed = false)
    {
        s.Blit(a.Image(2), new Rect(0, 179, SmallButtonWidth, SmallButtonHeight), x, y);
        if (pressed)
            s.Outline(new Rect(x, y, SmallButtonWidth, SmallButtonHeight), Colors.Red);
        if (text.Length > 0)
            s.TextCentered(a.Mini, x + SmallButtonWidth / 2 + 1, y + 3, text, Colors.White);
    }

    /// <summary>Umschalter unten rechts (kb\9, 72 × 16): in der Aufstellung zur Taktik, in der Taktik zurück.</summary>
    public static readonly Rect SwitchButton = new(246, 224, 72, 16);

    public static void DrawSwitch(Surface s, Assets a, bool toTactics) =>
        s.Blit(a.Image(9), new Rect(toTactics ? 72 : 0, 24, 72, 16), SwitchButton.X, SwitchButton.Y);

    /// <summary>Statuszeile unter einer Liste: Erklärung der Spalte bzw. Wert unter der Maus.</summary>
    public static void StatusLine(Surface s, Assets a, int centerX, int y, string? text)
    {
        if (!string.IsNullOrEmpty(text))
            s.TextCentered(a.Mini, centerX, y, text, DimColor); // Video: hellgraublau (Index 1)
    }

    /// <summary>Zeilenmarkierung wie im Original: Linien über und unter der Zeile.</summary>
    public static void HoverRow(Surface s, int x, int y, int width, int pitch)
    {
        s.HLine(x, y - 1, width, HoverColor);
        s.HLine(x, y + pitch - 1, width, HoverColor);
    }

    /// <summary>Füllfarbe der Dialoge (Vertragsverhandlung).</summary>
    public const byte DialogFill = 4;

    /// <summary>Untere und rechte Kante der Dialoge (Video: sehr dunkles Blau).</summary>
    public const byte DialogDark = 200;

    /// <summary>Schlagschatten der Dialoge, 2 px nach rechts und unten.</summary>
    public const byte DialogShadow = 6;

    /// <summary>
    /// Dialogfenster wie die Vertragsverhandlung im Video (ca. 10:25): Füllung 4, oben und links eine weiße Linie,
    /// unten und rechts eine dunkle, dazu 2 px Schlagschatten. Nicht der kb\2-Rahmen der großen Fenster.
    /// [APPROX] Der Schatten ist einfarbig; das Original dunkelt den Hintergrund vermutlich ab (im Video nicht trennbar).
    /// </summary>
    public static void DialogFrame(Surface s, Rect r)
    {
        s.Fill(new Rect(r.X + 2, r.Y + r.H, r.W, 2), DialogShadow);
        s.Fill(new Rect(r.X + r.W, r.Y + 2, 2, r.H), DialogShadow);
        s.Fill(r, DialogFill);
        s.HLine(r.X, r.Y, r.W - 1, Colors.White);
        s.VLine(r.X, r.Y, r.H - 1, Colors.White);
        s.HLine(r.X, r.Y + r.H - 1, r.W, DialogDark);
        s.VLine(r.X + r.W - 1, r.Y, r.H, DialogDark);
    }

    /// <summary>Titel eines Dialogs (Video: beige, Index 10, mit dunklem Schatten).</summary>
    public static void DialogTitle(Surface s, Assets a, int centerX, int y, string text) =>
        Widgets.ShadowText(s, a.Flow, centerX, y, text, 10, 8);

    /// <summary>
    /// Knopf aus kb\2 in Dialogen: Ecken transparent, Beschriftung dunkel (200) mit hellem Schatten (1) – angepasst
    /// an das Video der Vertragsverhandlung. <see cref="Widgets.Button"/> bleibt unverändert.
    /// </summary>
    public static void DialogButton(Surface s, Assets a, int x, int y, string text)
    {
        s.Blit(a.Image(2), new Rect(17, 179, Widgets.ButtonWidth, Widgets.ButtonHeight), x, y, transparent: 0);
        var cx = x + Widgets.ButtonWidth / 2 + 2;
        var tx = cx - a.Mini.Measure(text) / 2;
        s.Text(a.Mini, tx + 1, y + 4, text, 1);
        s.Text(a.Mini, tx, y + 3, text, DialogDark);
    }

    /// <summary>Trennlinie zwischen den Einträgen der Transferliste (Video: beige, Index 93).</summary>
    public const byte EntryLineColor = 93;

    public const int ArrowWidth = 10;
    public const int ArrowHeight = 17;
    public const int CompactArrowHeight = 15;

    /// <summary>Beschriftung der Taktik (Video: weiß mit schwarzem Schatten).</summary>
    public const byte LabelColor = Colors.White;
    public const byte LabelShadow = Colors.Black;

    /// <summary>Schrift auf den Modus-Knöpfen der Taktik (Video: weiß mit Schatten 5).</summary>
    public const byte ButtonTextColor = Colors.White;
    public const byte ButtonTextShadow = 5;

    /// <summary>Wert in einem aktiven Feld (Video: hellgraublau).</summary>
    public const byte ActiveValueColor = 1;

    /// <summary>Wert in einem abgeschalteten Feld (Video: „0“ kaum sichtbar).</summary>
    public const byte InactiveValueColor = 3;

    /// <summary>Nicht gewählte Reiter: dunkle Schrift (6) mit hellem Schatten (2); der gewählte ist weiß.</summary>
    public const byte TabTextColor = 6;
    public const byte TabTextShadow = 2;

    /// <summary>Pfeilpaar ▲▼ aus kb\4 (10 × 17), z. B. neben den Modus-Knöpfen der Taktik.</summary>
    public static void Arrows(Surface s, Assets a, int x, int y) =>
        s.Blit(a.Image(4), new Rect(20, 0, ArrowWidth, ArrowHeight), x, y, transparent: 0);

    /// <summary>Kompaktes Pfeilpaar (10 × 15): obere Hälfte kb\4 Zeilen 0–7, untere Zeilen 10–16 (Video: Taktik-Zeilen).</summary>
    public static void CompactArrows(Surface s, Assets a, int x, int y)
    {
        s.Blit(a.Image(4), new Rect(20, 0, ArrowWidth, 8), x, y, transparent: 0);
        s.Blit(a.Image(4), new Rect(20, 10, ArrowWidth, 7), x, y + 8, transparent: 0);
    }

    /// <summary>
    /// Knopf aus kb\2 (57 × 12) auf beliebige Größe gestreckt: Ränder bleiben, die Mitte wird wiederholt.
    /// Gedrückt (<paramref name="pressed"/>) = vertieftes Wertfeld. Ecken (Farbe 0) bleiben transparent.
    /// </summary>
    public static void StretchedButton(Surface s, Assets a, Rect r, bool pressed = false)
    {
        const int left = 2, right = 3, top = 2, bottom = 3, w = Widgets.ButtonWidth, h = Widgets.ButtonHeight;
        var image = a.Image(2);
        var sy0 = pressed ? 191 : 179;
        for (var ty = 0; ty < r.H; ty++)
        {
            var sy = ty < top ? ty : ty >= r.H - bottom ? h - (r.H - ty) : top;
            for (var tx = 0; tx < r.W; tx++)
            {
                var sx = tx < left ? tx : tx >= r.W - right ? w - (r.W - tx) : left;
                var c = image.Pixels[(sy0 + sy) * image.Width + 17 + sx];
                if (c != 0)
                    s.Set(r.X + tx, r.Y + ty, c);
            }
        }
    }

    /// <summary>Beschriftung mit Schatten (Taktik).</summary>
    public static void Label(Surface s, Assets a, int x, int y, string text, byte color = LabelColor, byte shadow = LabelShadow)
    {
        s.Text(a.Mini, x + 1, y + 1, text, shadow);
        s.Text(a.Mini, x, y, text, color);
    }

    public static void LabelCentered(Surface s, Assets a, int centerX, int y, string text, byte color = LabelColor,
        byte shadow = LabelShadow) =>
        Label(s, a, centerX - a.Mini.Measure(text) / 2, y, text, color, shadow);

    /// <summary>Kurzbezeichnung der Statusspalte.</summary>
    public static string StatusText(PlayerStatus status) => status switch
    {
        PlayerStatus.InTeam => "IM TEAM",
        PlayerStatus.Injured => "VERL.",
        PlayerStatus.Suspended => "GESP.",
        _ => "RESERVE",
    };

    public static string TrendText(PlayerTrend trend) => trend switch
    {
        PlayerTrend.Falling => "Fallend.",
        PlayerTrend.Rising => "Steigend.",
        _ => "Konstant.",
    };

    /// <summary>Verein des aktiven Managers.</summary>
    public static Club OwnClub(ScreenHost host) => host.Game!.Clubs[host.Game.ActiveManager.ClubIndex];

    /// <summary>Kader in Listenreihenfolge (Torhüter, Abwehr, Sturm; je nach Rückennummer).</summary>
    public static IReadOnlyList<Player> Rows(Club club) => club.SortedPlayers.ToList();
}

/// <summary>
/// Meldungsfenster über einem Bildschirm (z. B. „Der darf da nicht rein !“) als Baustein <see cref="OriginalDialog"/>
/// (Integration Runde 11, ui-meldungen, A375, M-K3).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] em.exe Datei 0x4C246 (<c>docs/analyse/re-meldungen.md</c> §C): Fenster ab (75 | 107), Überschrift = Name des
///   Managers am Zug, „!“-Felder, Knöpfe nur bei Zeile 3 oder 4, ein Linksklick schließt (<see cref="OriginalDialog.Update"/>).</item>
/// <item>[ORIGINAL] <see cref="ShowShort"/>: Kurzmeldung Datei 0x0C0B8 ohne Knopf und ohne Eingabe, schließt nach Takten von selbst
///   (<see cref="ShortMessage"/>); sie gilt für die Texte, die im Original über 0x0C0B8 laufen (Aufrufstellen in der Aufstellung
///   0x40AF3, 0x40B55, 0x40EB9, in der Transferliste 0x46A4D ff., bei den Freundschaftsspielen 0x3C2C5).</item>
/// <item>[APPROX] Mit drei und vier Zeilen zeigt das Original Knöpfe, deren Text der Aufrufer (Zeile 4/5) bestimmt; welche Texte die
///   Aufrufer der Verhandlungs-, Werbe- und Spielmodus-Meldungen übergeben, ist nicht belegt, daher steht „Na gut.“ als Zeile 5
///   (wie bei der Bundestrainer-Warnung, DGROUP Zeile 5). Mehr als vier Zeilen kennt 0x4C246 nicht; solche Meldungen
///   (Spielstand-Fehler, Spielmodus) behalten den früheren Rahmen. Komfort: Taste und rechte Maustaste schließen ebenfalls.</item>
/// </list>
/// </remarks>
public sealed class MessageBox
{
    /// <summary>[APPROX] Beschriftung der Knöpfe, wenn der Aufrufer keine Zeile 5 übergibt.</summary>
    public const string ButtonText = "Na gut.";

    private readonly Func<string>? _title;
    private string[] _lines = [];
    private ShortMessage? _short;

    /// <param name="title">Name des Managers am Zug (Überschrift); ohne Angabe „An Alle:“.</param>
    public MessageBox(Func<string>? title = null) => _title = title;

    public bool Visible => _lines.Length > 0 || _short is { Closed: false };

    /// <summary>Angezeigte Zeilen (leer, wenn keine Meldung offen ist).</summary>
    public IReadOnlyList<string> Lines => _short is { Closed: false } ? [_short.Line1, _short.Line2] : _lines;

    /// <summary>Offene Kurzmeldung (0x0C0B8), sonst null.</summary>
    public ShortMessage? Short => _short is { Closed: false } ? _short : null;

    public void Show(params string[] lines)
    {
        _short = null;
        _lines = lines;
    }

    /// <summary>Kurzmeldung ohne Knopf, die nach der Zeit des Originals von selbst schließt (Datei 0x0C0B8, zwei Zeilen).</summary>
    public void ShowShort(string line1, string line2)
    {
        _lines = [];
        _short = new ShortMessage(_title?.Invoke() ?? "", line1, line2);
    }

    public void Close()
    {
        _lines = [];
        _short = null;
    }

    /// <summary>Verarbeitet Eingaben, solange die Meldung sichtbar ist. Liefert true, wenn sie die Eingabe verbraucht hat.</summary>
    public bool Update(InputState input)
    {
        if (_short is { } shortMessage)
        {
            if (shortMessage.Update(input))
                return true;
            _short = null; // abgelaufen; die Eingabe dieses Bildes gilt wieder dem Bildschirm
            return false;
        }
        if (_lines.Length == 0)
            return false;
        if (Fits)
        {
            var dialog = Dialog("");
            dialog.Update(input);
            if (dialog.Closed || input.RightClick || input.Keys.Count > 0 || input.Text.Length > 0)
                Close();
        }
        else if (input.LeftClick || input.RightClick || input.Keys.Count > 0 || input.Text.Length > 0)
            Close();
        return true;
    }

    /// <summary>Zeichnet die offene Meldung an der festen Stelle des Originals (75 | 107).</summary>
    public void Draw(Surface s, Assets a)
    {
        if (_short is { Closed: false } shortMessage)
        {
            shortMessage.Draw(s, a);
            return;
        }
        if (_lines.Length == 0)
            return;
        if (Fits)
        {
            Dialog(_title?.Invoke() ?? "").Draw(s, a);
            return;
        }
        // [APPROX] Mehr als vier Zeilen: früherer Rahmen, Lage und Größe nicht aus dem Video belegt.
        var width = Math.Max(110, _lines.Max(l => a.Mini.Measure(l)) + 24);
        var height = 22 + 8 * _lines.Length;
        var r = new Rect(160 - width / 2, 120 - height / 2, width, height);
        Widgets.Frame(s, a, r, FrameStyle.Orange);
        for (var i = 0; i < _lines.Length; i++)
            s.TextCentered(a.Mini, 160, r.Y + 11 + 8 * i, _lines[i], Colors.White);
    }

    private bool Fits => _lines.Length <= 4;

    private OriginalDialog Dialog(string title) => _lines.Length switch
    {
        1 => new OriginalDialog(OriginalDialogKind.Message, title, _lines[0]),
        2 => new OriginalDialog(OriginalDialogKind.Message, title, _lines[0], _lines[1]),
        3 => new OriginalDialog(OriginalDialogKind.Message, title, _lines[0], _lines[1], _lines[2], "", ButtonText),
        _ => new OriginalDialog(OriginalDialogKind.Message, title, _lines[0], _lines[1], _lines[2], _lines[3], ButtonText),
    };
}
