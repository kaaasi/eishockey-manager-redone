using System.Text;
using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UiKey = EishockeyManager.UI.Key;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace EishockeyManager.Desktop;

/// <summary>
/// Fenster für das Spiel. Die Oberfläche rendert in einen 320×240-Palettenpuffer, der hier
/// ganzzahlig skaliert und mittig dargestellt wird (F11 = Vollbild).
/// </summary>
public sealed class DesktopGame : Game
{
    private const int DefaultScale = 3;

    private static readonly (XnaKeys Xna, UiKey Ui)[] KeyMap =
    [
        (XnaKeys.Enter, UiKey.Enter), (XnaKeys.Escape, UiKey.Escape), (XnaKeys.Back, UiKey.Backspace),
        (XnaKeys.Space, UiKey.Space), (XnaKeys.Up, UiKey.Up), (XnaKeys.Down, UiKey.Down),
        (XnaKeys.Left, UiKey.Left), (XnaKeys.Right, UiKey.Right),
        (XnaKeys.PageUp, UiKey.PageUp), (XnaKeys.PageDown, UiKey.PageDown),
    ];

    private readonly GraphicsDeviceManager _graphics;
    private readonly ScreenHost _host;
    private readonly Surface _surface = new();
    private readonly InputState _input = new();
    private readonly byte[] _rgba = new byte[Surface.Width * Surface.Height * 4];
    private readonly StringBuilder _typed = new();
    private SpriteBatch? _batch;
    private Texture2D? _texture;
    private MouseState _previousMouse;
    private KeyboardState _previousKeys;
    private Rectangle _target;

    public DesktopGame(GameData game)
    {
        _host = new ScreenHost(new Assets(game));
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = Surface.Width * DefaultScale,
            PreferredBackBufferHeight = Surface.Height * DefaultScale,
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "Eishockey Manager Redone";
        Window.TextInput += (_, e) =>
        {
            if (!char.IsControl(e.Character))
                _typed.Append(e.Character);
        };
    }

    protected override void LoadContent()
    {
        _batch = new SpriteBatch(GraphicsDevice);
        _texture = new Texture2D(GraphicsDevice, Surface.Width, Surface.Height);
        _host.Show(new TitleScreen(_host));
    }

    protected override void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var keys = Keyboard.GetState();
        bool Pressed(XnaKeys k) => keys.IsKeyDown(k) && !_previousKeys.IsKeyDown(k);

        if (Pressed(XnaKeys.F11))
        {
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            _graphics.ApplyChanges();
        }

        UpdateTarget();
        _input.Reset();
        if (_target.Width > 0)
        {
            _input.MouseX = (mouse.X - _target.X) * Surface.Width / _target.Width;
            _input.MouseY = (mouse.Y - _target.Y) * Surface.Height / _target.Height;
        }
        _input.LeftClick = IsActive && mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
        _input.RightClick = IsActive && mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;
        _input.Wheel = Math.Sign(mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue);
        foreach (var (xna, ui) in KeyMap)
            if (Pressed(xna))
                _input.Keys.Add(ui);
        _input.Text = _typed.ToString();
        _typed.Clear();

        _host.Update(_input);
        if (_host.ExitRequested)
            Exit();

        _previousMouse = mouse;
        _previousKeys = keys;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _host.Draw(_surface);
        _surface.ToRgba(_rgba);
        _texture!.SetData(_rgba);

        GraphicsDevice.Clear(Color.Black);
        _batch!.Begin(samplerState: SamplerState.PointClamp);
        _batch.Draw(_texture, _target, Color.White);
        _batch.End();
        base.Draw(gameTime);
    }

    /// <summary>Größte ganzzahlige Vergrößerung, die ins Fenster passt, mittig.</summary>
    private void UpdateTarget()
    {
        var w = GraphicsDevice.PresentationParameters.BackBufferWidth;
        var h = GraphicsDevice.PresentationParameters.BackBufferHeight;
        var scale = Math.Max(1, Math.Min(w / Surface.Width, h / Surface.Height));
        var tw = Surface.Width * scale;
        var th = Surface.Height * scale;
        _target = new Rectangle((w - tw) / 2, (h - th) / 2, tw, th);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _texture?.Dispose();
            _batch?.Dispose();
        }
        base.Dispose(disposing);
    }
}
