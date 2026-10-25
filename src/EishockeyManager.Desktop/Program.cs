using EishockeyManager.Desktop;
using EishockeyManager.Formats;

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

using var window = new DesktopGame(game);
window.Run();
return 0;
