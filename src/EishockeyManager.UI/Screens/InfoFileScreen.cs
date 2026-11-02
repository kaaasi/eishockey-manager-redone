namespace EishockeyManager.UI.Screens;

/// <summary>Eine Seite des Infofiles: Zeichen mit x und Grundlinie, wie sie em.exe Datei 0x432EC setzt.</summary>
public sealed record InfoFilePage(IReadOnlyList<(int X, int Baseline, byte Code)> Glyphs);

/// <summary>
/// Infofile „Neueste Informationen zum Eishockey-Manager“ (Spur ui-meldungen, Teil 2, M-K5): verstecktes Feld im
/// Hauptmenü, Linksklick auf Pixel (319 | 0). Zeigt die Datei <c>infofile</c> seitenweise; ein Klick blättert, nach der
/// letzten Seite geht es zurück. Das Öffnen ändert keinen Spielzustand.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] em.exe Datei 0x432EC (Regel in <c>docs/analyse/re-meldungen.md</c> §D), Aufruf nur aus dem Hauptmenü
///   Datei 0x453E5–0x453F8 (Maus-x = 319, Maus-y = 0).</item>
/// <item>[ORIGINAL] Seitenaufbau: Hintergrund kb\22, Kopfzeile Schrift 3 (Mini) zentriert, Grundlinie 7, Linie y 9, Text ab
///   x 0, Grundlinie 19, Zeilenabstand 8, Seitenende bei Grundlinie &gt; 230 (27 Zeilen), „TASTE!“ ab x 275, Grundlinie 238.
///   Zeichen: LF neue Zeile, TAB (x &lt; 285) auf das nächste Vielfache von 20, „#“ Seitenende, x &gt; 310 neue Zeile, CR wie ein
///   Zeichen; Latin-1 ä Ä ü Ü ß ö Ö werden auf die Spielzeichen umgesetzt (ö wie Ö), CP437-Umlaute des Originalfiles
///   treffen die Tabelle nicht und gehen unverändert an den Font (dort leer).</item>
/// <item>[APPROX] Schatten der Texte schwarz, 1 Pixel rechts unten (wie bei den übrigen Texten der Oberfläche; „Schatten 0“ im
///   Original); Bildaufbau kein Video belegt (Funktion im Video nicht gezeigt). Dateigröße-Prüfung („weniger als 21 000 Byte
///   frei“) entfällt. Rechte Maustaste blättert wie die linke (Warten 0xB908 akzeptiert beide).</item>
/// </list>
/// </remarks>
public sealed class InfoFileScreen : Screen
{
    public const int MaxBytes = 20000;
    public const string FileName = "infofile";
    public const string Title = "Neueste Informationen zum Eishockey-Manager";
    public const string Prompt = "TASTE!";
    public const string MissingText = "KEIN INFOFILE VORHANDEN !";

    /// <summary>Verstecktes Feld im Hauptmenü. [ORIGINAL] 0x453E5–0x453F8.</summary>
    public static bool IsTrigger(InputState input) => input.LeftClick && input.MouseX == 319 && input.MouseY == 0;

    private const int FirstBaseline = 19, LineHeight = 8, LastBaseline = 230, RightLimit = 310, TabLimit = 285;

    private readonly Screen _back;
    private readonly IReadOnlyList<InfoFilePage>? _pages;
    private int _page;

    public InfoFileScreen(ScreenHost host, Screen back) : base(host)
    {
        _back = back;
        var path = Path.Combine(host.Assets.Game.RootDirectory, FileName);
        if (File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            var buffer = new byte[MaxBytes];
            var length = stream.Read(buffer, 0, MaxBytes);
            _pages = Layout(buffer.AsSpan(0, length), code => host.Assets.Mini[code].Width);
        }
    }

    public int PageCount => _pages?.Count ?? 0;
    public int Page => _page;

    public override void Update(InputState input)
    {
        if (!input.LeftClick && !input.RightClick)
            return;
        _page++;
        if (_pages is null || _page >= _pages.Count)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        var a = Assets;
        if (_pages is null)
        {
            _back.Draw(s);
            Widgets.ShadowText(s, a.Mini, 160, 100 - TextTop, MissingText, Colors.White, Colors.Black);
            return;
        }
        s.Blit(a.Image(MainMenuScreen.Background), 0, 0);
        Widgets.ShadowText(s, a.Mini, 160, 7 - TextTop, Title, Colors.White, Colors.Black);
        s.HLine(0, 9, Surface.Width, Colors.Black);
        var page = _pages[Math.Min(_page, _pages.Count - 1)];
        foreach (var (x, baseline, code) in page.Glyphs)
        {
            Glyph(s, a, code, x + 1, baseline - TextTop + 1, Colors.Black);
            Glyph(s, a, code, x, baseline - TextTop, Colors.White);
        }
        Widgets.ShadowText(s, a.Mini, 275 + a.Mini.Measure(Prompt) / 2, 238 - TextTop, Prompt, Colors.White, Colors.Black);
    }

    /// <summary>Mini: Oberkante 4 Pixel über der Grundlinie (wie <see cref="OriginalDialog"/>).</summary>
    private const int TextTop = 4;

    private static void Glyph(Surface s, Assets a, byte code, int x, int y, byte color)
    {
        var glyph = a.Mini[code];
        for (var gy = 0; gy < a.Mini.Height; gy++)
            for (var gx = 0; gx < glyph.Width; gx++)
                if (glyph[gx, gy])
                    s.Set(x + gx, y + gy, color);
    }

    /// <summary>Umsetzung Latin-1 → Spielzeichen. [ORIGINAL] 0x43596, Tabellen DGROUP 0x512E / 0x5136.</summary>
    public static byte Translate(byte b) => b switch
    {
        0xE4 => (byte)'{',
        0xC4 => (byte)'[',
        0xFC => (byte)'}',
        0xDC => (byte)']',
        0xDF => (byte)'~',
        0xF6 or 0xD6 => (byte)'\\',
        _ => b,
    };

    /// <summary>Seitenaufbau nach Datei 0x432EC. <paramref name="width"/> liefert die Zeichenbreite des Fonts.</summary>
    public static IReadOnlyList<InfoFilePage> Layout(ReadOnlySpan<byte> data, Func<byte, int> width)
    {
        var pages = new List<InfoFilePage>();
        var glyphs = new List<(int, int, byte)>();
        int x = 0, baseline = FirstBaseline;

        void EndPage()
        {
            pages.Add(new InfoFilePage(glyphs.ToArray()));
            glyphs.Clear();
            x = 0;
            baseline = FirstBaseline;
        }

        foreach (var raw in data)
        {
            if (raw == '#')
            {
                EndPage();
                continue;
            }
            if (raw == '\n')
            {
                x = 0;
                baseline += LineHeight;
                if (baseline > LastBaseline)
                    EndPage();
                continue;
            }
            if (raw == '\t' && x < TabLimit)
            {
                x = (x / 20 + 1) * 20;
                continue;
            }
            var code = Translate(raw);
            glyphs.Add((x, baseline, code));
            x += width(code);
            if (x > RightLimit)
            {
                x = 0;
                baseline += LineHeight;
                if (baseline > LastBaseline)
                    EndPage();
            }
        }
        // [ORIGINAL] Datei 0x432EC (re-meldungen.md §D Schritt 4): Nach jedem Seitenende folgt ein neuer Seitenkopf; am Dateiende steht
        // „TASTE!“ auf der aktuellen Seite – auch wenn sie leer ist (Datei endet nach „#“ oder genau nach der 27. Zeile; R11U-04).
        EndPage();
        return pages;
    }
}
