using EishockeyManager.Formats;

namespace EishockeyManager.UI;

/// <summary>Lädt und puffert die Originaldateien für die Oberfläche.</summary>
public sealed class Assets
{
    private readonly Dictionary<int, VgaImage> _images = [];
    private ClubDatabase? _database;

    public Assets(GameData game)
    {
        Game = game;
        Palette = game.LoadDefaultPalette();

        var exe = Path.Combine(game.RootDirectory, "em.exe");
        Mini = BitmapFont.LoadFromExe(exe, FontKind.Mini);
        Flow = BitmapFont.LoadFromExe(exe, FontKind.Flow);
        Bold = BitmapFont.LoadFromExe(exe, FontKind.NewBold);
    }

    public GameData Game { get; }

    /// <summary>Standardpalette kb\21.</summary>
    public Palette Palette { get; }

    /// <summary>Font 6 px – fast alle Texte der Oberfläche.</summary>
    public BitmapFont Mini { get; }

    /// <summary>Font 8 px.</summary>
    public BitmapFont Flow { get; }

    /// <summary>Font 20 px – Überschriften.</summary>
    public BitmapFont Bold { get; }

    public ClubDatabase Database => _database ??= ClubDatabase.Load(Game);

    public VgaImage Image(int number)
    {
        if (!_images.TryGetValue(number, out var image))
        {
            image = VgaImage.Load(Game.GraphicsPath(number));
            _images[number] = image;
        }
        return image;
    }
}

/// <summary>Häufig benutzte Farben der Standardpalette.</summary>
public static class Colors
{
    public const byte Black = 0;
    public const byte White = 7;
    public const byte Yellow = 24;
    public const byte Red = 18;

    /// <summary>Hintergrund der Menükacheln (mintgrün). Das Original färbt diesen Index zur Laufzeit um.</summary>
    public const byte TileBackground = 30;

    /// <summary>Füllfarbe der grau-blauen Fenster.</summary>
    public const byte PanelFill = 2;
}
