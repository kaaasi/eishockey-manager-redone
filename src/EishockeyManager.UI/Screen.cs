using EishockeyManager.Core;

namespace EishockeyManager.UI;

public enum Key
{
    Enter,
    Escape,
    Backspace,
    Space,
    Up,
    Down,
    Left,
    Right,
    PageUp,
    PageDown,
}

/// <summary>Eingaben eines Frames, bereits auf das 320×240-Raster umgerechnet.</summary>
public sealed class InputState
{
    public int MouseX { get; set; } = -1;
    public int MouseY { get; set; } = -1;

    /// <summary>Linke Maustaste wurde in diesem Frame gedrückt.</summary>
    public bool LeftClick { get; set; }

    /// <summary>Linke Maustaste ist gedrückt (auch gehalten), z. B. zum Überstreichen im Training.</summary>
    public bool LeftDown { get; set; }

    /// <summary>Rechte Maustaste wurde in diesem Frame gedrückt.</summary>
    public bool RightClick { get; set; }

    /// <summary>Rechte Maustaste ist gedrückt (auch gehalten), z. B. für die Spielstand-Info des Speichern-Dialogs (em.exe 0x5B661).</summary>
    public bool RightDown { get; set; }

    /// <summary>Mausrad: positiv = nach oben.</summary>
    public int Wheel { get; set; }

    public List<Key> Keys { get; } = [];

    /// <summary>Getippte Zeichen (Texteingabe).</summary>
    public string Text { get; set; } = "";

    public bool Pressed(Key key) => Keys.Contains(key);

    public bool Clicked(Rect r) => LeftClick && r.Contains(MouseX, MouseY);

    public bool RightClicked(Rect r) => RightClick && r.Contains(MouseX, MouseY);

    public bool Hover(Rect r) => r.Contains(MouseX, MouseY);

    public void Reset()
    {
        LeftClick = LeftDown = RightClick = RightDown = false;
        Wheel = 0;
        Keys.Clear();
        Text = "";
    }
}

public abstract class Screen
{
    protected Screen(ScreenHost host) => Host = host;

    protected ScreenHost Host { get; }
    protected Assets Assets => Host.Assets;

    /// <summary>Wird aufgerufen, wenn der Bildschirm sichtbar wird.</summary>
    public virtual void Enter() { }

    /// <summary>Verarbeitet die Eingaben eines Frames.</summary>
    public abstract void Update(InputState input);

    /// <summary>Zeichnet den Bildschirm komplett neu.</summary>
    public abstract void Draw(Surface surface);
}

/// <summary>Verwaltet den aktiven Bildschirm und den Spielstand.</summary>
public sealed class ScreenHost
{
    public ScreenHost(Assets assets) => Assets = assets;

    public Assets Assets { get; }

    public Screen? Current { get; private set; }

    public GameState? Game { get; set; }

    /// <summary>Gesetzt, wenn das Programm beendet werden soll.</summary>
    public bool ExitRequested { get; private set; }

    /// <summary>Zufallsquelle für neue Spiele (fester Startwert für reproduzierbare Abläufe möglich).</summary>
    public int? Seed { get; set; }

    public void Show(Screen screen)
    {
        Current = screen;
        screen.Enter();
    }

    public void Exit() => ExitRequested = true;

    public void Update(InputState input) => Current?.Update(input);

    public void Draw(Surface surface) => Current?.Draw(surface);
}
