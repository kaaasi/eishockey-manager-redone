using EishockeyManager.Formats;
using EishockeyManager.Viewer;

GameData game;
try
{
    game = GameData.Locate(args.FirstOrDefault());
}
catch (DirectoryNotFoundException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

using var viewer = new AssetViewer(game);
viewer.Run();
return 0;
