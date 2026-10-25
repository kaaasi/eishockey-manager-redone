using EishockeyManager.Formats;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EishockeyManager.Viewer;

/// <summary>
/// Meilenstein v0.1: zeigt alle Grafiken der DOS-Version an.
/// Pfeil links/rechts: Bild wechseln · 1–4: Zoom · P: Standardpalette erzwingen · Esc: Ende
/// </summary>
public sealed class AssetViewer : Game
{
    private const int ScreenWidth = 320;
    private const int ScreenHeight = 240;

    private readonly GraphicsDeviceManager _graphics;
    private readonly GameData _game;
    private readonly List<(int Number, VgaImage Image)> _images = [];
    private Palette _defaultPalette = Palette.Grayscale();
    private SpriteBatch? _spriteBatch;
    private Texture2D? _texture;
    private int _current;
    private int _zoom = 3;
    private bool _forceDefaultPalette;
    private KeyboardState _previousKeys;

    public AssetViewer(GameData game)
    {
        _game = game;
        _graphics = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
        Window.AllowUserResizing = false;
        ApplyZoom();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _defaultPalette = _game.LoadDefaultPalette();

        foreach (var number in _game.GraphicsNumbers())
        {
            var bytes = File.ReadAllBytes(_game.GraphicsPath(number));
            if (VgaImage.TryParse(bytes, out var image))
                _images.Add((number, image!));
        }

        ShowImage(0);
    }

    protected override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();
        bool Pressed(Keys key) => keys.IsKeyDown(key) && !_previousKeys.IsKeyDown(key);

        if (Pressed(Keys.Escape))
            Exit();
        if (Pressed(Keys.Right) || Pressed(Keys.Space))
            ShowImage(_current + 1);
        if (Pressed(Keys.Left))
            ShowImage(_current - 1);
        if (Pressed(Keys.Home))
            ShowImage(0);
        if (Pressed(Keys.P))
        {
            _forceDefaultPalette = !_forceDefaultPalette;
            ShowImage(_current);
        }
        for (var z = 1; z <= 4; z++)
        {
            if (Pressed(Keys.D0 + z))
            {
                _zoom = z;
                ApplyZoom();
            }
        }

        _previousKeys = keys;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(40, 40, 48));
        if (_texture is not null && _spriteBatch is not null)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_texture, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, _zoom, SpriteEffects.None, 0f);
            _spriteBatch.End();
        }
        base.Draw(gameTime);
    }

    private void ShowImage(int index)
    {
        if (_images.Count == 0)
        {
            Window.Title = "Eishockey Manager Redone – keine Grafiken gefunden";
            return;
        }

        _current = (index % _images.Count + _images.Count) % _images.Count;
        var (number, image) = _images[_current];

        var palette = _forceDefaultPalette || image.Palette is null ? _defaultPalette : image.Palette;
        var rgba = new VgaImage(image.Width, image.Height, image.Pixels).ToRgba(palette);

        _texture?.Dispose();
        _texture = new Texture2D(GraphicsDevice, image.Width, image.Height);
        _texture.SetData(rgba);

        var paletteInfo = image.Palette is null ? "Standardpalette" : _forceDefaultPalette ? "Standardpalette (erzwungen)" : "eigene Palette";
        Window.Title = $"Eishockey Manager Redone – {number}.vga ({image.Width}×{image.Height}, {paletteInfo}) [{_current + 1}/{_images.Count}]";
    }

    private void ApplyZoom()
    {
        _graphics.PreferredBackBufferWidth = ScreenWidth * _zoom;
        _graphics.PreferredBackBufferHeight = ScreenHeight * _zoom;
        if (GraphicsDevice is not null)
            _graphics.ApplyChanges();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _texture?.Dispose();
            _spriteBatch?.Dispose();
        }
        base.Dispose(disposing);
    }
}
