using EishockeyManager.Audio;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>Auftrag an den Abspieler (Argumente von 0x567C8, soweit sie die Anzeige betreffen).</summary>
/// <param name="Scene">Vom Spielkern gewählte Szene (<c>GoalSceneSelection</c>; der Abspieler zieht nichts).</param>
/// <param name="Goal">Art 1 = Tor: am Ende Torzahl der Seite + 1 und Kopfzeile Modus 2 (Datei 0x56DD9–0x56E11).</param>
/// <param name="Side">0 Heim, 1 Gast (Seite der Chance).</param>
/// <param name="HomeColor">Farbcode des Heimvereins (Vereinsfeld 0x3B), noch nicht entzerrt.</param>
/// <param name="Continuous">Modus 2 (Dauer-Eisfeld, <c>2 · [0x5A55]</c>): kein Kopfbild neu, keine Pause 6.</param>
public sealed record GoalSceneRequest(GoalScene Scene, bool Goal, int Side, int HallLevel, int HomeColor, int AwayColor,
    GoalSceneHeader Header, bool Continuous = false);

/// <summary>
/// Abspieler der Torszene 0x567C8 als Bildschirm (Spur torszenen, Teil C2; <c>docs/analyse/re-livespiel.md</c> §6, §11, §16).
/// <list type="bullet">
/// <item>[ORIGINAL] Modus (Datei 0x56804–0x56813, 0x56ABC, 0x56E14, 0x56E37): 0 = Einzelszene (Bilder laden, Palette, Szene,
///   Pause 6, Bilder freigeben, Live-Bildschirm neu); 1 = Dauer-Eisfeld aufbauen (laden, Kopfzeile, Eis; Rückgabe 1);
///   2 = Szene im Dauer-Eisfeld (ohne Laden, ohne Pause, Rückgabe 0); 3 = Dauer-Eisfeld abbauen. Die Torszene 0x53C57 ruft
///   0 oder 2 (<c>2 · [0x5A55]</c>, Datei 0x53FCE). Damit ist der Widerspruch §6/§11 aufgelöst: Pause 6 nur in Modus 0.</item>
/// <item>[APPROX] Der Speichermangel-Zweig von Modus 0 (freier Speicher unter 0x412F8 → Torszenen aus, Meldung
///   <c>DGROUP 0x13E5</c>, Datei 0x56816–0x56871) entfällt: das Remake hat keine Speichergrenze.</item>
/// <item>[ORIGINAL] Bildschleife (Datei 0x56CEA–0x56DBD): Bildtakt <c>48h:2C08h(1)</c> = ein BIOS-Takt (18,2 Hz), Bild
///   anwenden und zeichnen, Toneffekt s = Byte 3 ≠ 0 → laufenden Ton stoppen und <c>kb\(237 + s)</c> spielen; ist die Ausgabe
///   frei, Publikum <c>kb\237</c> neu (Datei 0x56D10–0x56D26). [APPROX] Ein Bild je 18,2-Hz-Takt, bei 60 Bildern je Sekunde
///   über einen Zähler verteilt; „Ton an“ (<c>DGROUP 0x4A44</c>) regelt hier die Stummschaltung der Ausgabe.</item>
/// <item>[ORIGINAL] Ende: bei Tor die Torzahl der Seite + 1 und Kopfzeile Modus 2, dann (nur Modus 0) Pause 6 = 6 / 3 = 2 Takte
///   (<c>50h:19Eh</c>, Datei 0x56E1D–0x56E29). Eingaben wertet der Abspieler nicht aus.</item>
/// </list>
/// </summary>
public sealed class GoalSceneScreen : Screen
{
    /// <summary>[ORIGINAL] BIOS-Takt (Bildtakt der Szene, <c>48h:2C08h(1)</c>).</summary>
    public const double TicksPerSecond = 18.2065;

    /// <summary>[ORIGINAL] Pause nach der Szene in Modus 0: <c>50h:19Eh(6)</c> = 2 Takte.</summary>
    public const int EndPauseTicks = 6 / 3;

    private readonly Action _finished;
    private readonly GoalSceneGraphics _graphics;
    private double _ticks;
    private int _pauseTicks;

    public GoalSceneScreen(ScreenHost host, GoalSceneRequest request, GoalSceneGraphics graphics, Action finished) : base(host)
    {
        Request = request;
        _graphics = graphics;
        _finished = finished;
        Animation = new GoalSceneAnimation(request.Scene);
        Header = request.Header;
        var (home, away) = GoalSceneGraphics.SeparateColors(request.HomeColor, request.AwayColor);
        Palette = graphics.PaletteFor(home, away);
    }

    public GoalSceneRequest Request { get; }
    public GoalSceneAnimation Animation { get; }

    /// <summary>Kopfzeile; nach einem Tor mit erhöhter Torzahl.</summary>
    public GoalSceneHeader Header { get; private set; }

    public Palette Palette { get; }

    /// <summary>Toneffekte in der Reihenfolge, in der die Szene sie auslöst (Byte 3 ≠ 0); für Proben.</summary>
    public List<int> Sounds { get; } = [];

    /// <summary>Szene und Endpause vorbei.</summary>
    public bool Done { get; private set; }

    /// <summary>Bildschirmbilder (60 Hz) je Szenenbild im Mittel.</summary>
    public static double FramesPerTick => ShortMessage.FramesPerSecond / TicksPerSecond;

    public override void Update(InputState input)
    {
        if (Done)
            return;
        _ticks += TicksPerSecond / ShortMessage.FramesPerSecond;
        while (_ticks >= 1 && !Done)
        {
            _ticks -= 1;
            Tick();
        }
    }

    /// <summary>Ein BIOS-Takt: nächstes Bild bzw. Ende und Pause.</summary>
    public void Tick()
    {
        if (Done)
            return;
        if (_pauseTicks > 0)
        {
            if (--_pauseTicks == 0)
                Finish();
            return;
        }
        if (Animation.Step() is { } sound)
        {
            PlaySound(sound);
            return;
        }
        if (Request.Goal)
            Header = Request.Side == 0 ? Header with { HomeGoals = Header.HomeGoals + 1 } : Header with { AwayGoals = Header.AwayGoals + 1 };
        if (Request.Continuous)
            Finish();
        else
            _pauseTicks = EndPauseTicks;
    }

    private void Finish()
    {
        Done = true;
        _finished();
    }

    private void PlaySound(int s)
    {
        var audio = GameAudio.Current;
        if (SoundEffects.FromSceneIndex(s) is { } effect)
        {
            Sounds.Add(s);
            audio.StopEffect();
            audio.PlayEffect(effect);
        }
        else if (audio.CurrentEffect is null)
            audio.PlayEffect(SoundEffect.Crowd);
    }

    public override void Draw(Surface surface)
    {
        surface.Palette = Palette;
        _graphics.DrawField(surface, Animation, Request.HallLevel);
        _graphics.DrawHeader(surface, Assets, Header, 0);
    }
}
