using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Laden/Sichern-Dialog (Kacheln „Laden“ und „Sichern“ unter „Gemischtes“, Spur speichern-dialog).
/// Der Ordner der Spielstände liegt im Temp-Ordner und wird am Ende gelöscht; echte Einstellungen bleiben unberührt.
/// </summary>
internal static class Speichern
{
    /// <summary>
    /// Bilder: Laden mit gefüllter Liste (Vorlage Referenzbild oberflaeche_0102: gleiche Namen, aber mit unserer Endung),
    /// leerer Sichern-Dialog, Rückfrage beim Überschreiben, Meldung bei fremder Datei, Spielvorbereitung mit Notizbuch.
    /// </summary>
    [Scenario("speichern")]
    private static void SaveLoadDialog(ScreenDriver d)
    {
        GameSettings.StorePath = null;
        GameSettings.Current = new GameSettings();
        var directory = Path.Combine(Path.GetTempPath(), "em-speichern-" + Guid.NewGuid().ToString("N"));
        SaveContext.For(d.Host).Directory = directory;
        try
        {
            d.StartNewGame();
            d.PlayMatchDay();

            // leerer Ordner
            d.OpenTile("Sichern"); d.Move(10, 235); d.Save("01_sichern_leer");
            d.Type("test1"); d.Move(10, 235); d.Save("02_name_getippt");
            d.Press(Key.Enter);
            if (d.Host.Current is not MainMenuScreen)
                throw new InvalidOperationException("Sichern hat den Dialog nicht verlassen.");

            // Liste wie im Referenzbild
            var data = SaveGame.Save(d.Host.Game!);
            foreach (var name in new[]
                     {
                         "AUTOSAVE", "BEN01", "TEST01", "TEST2", "TEST2A", "TRAIN01", "TRAIN03A", "TRAIN03B", "TRAIN04",
                         "TRAIN04A", "TRAIN04B", "TRAIN09", "TRAINING", "UEBER01",
                     })
                SaveFiles.WriteAtomic(SaveFiles.PathOf(directory, name), data);
            foreach (var folder in new[] { "HIGH", "KB", "TORE" })
                Directory.CreateDirectory(Path.Combine(directory, folder));
            foreach (var other in new[] { "EISHOCK1.PDF", "EM.EXE", "INFOFILE" })
                File.WriteAllBytes(Path.Combine(directory, other), []);
            // Spielstand in neuerer Version → Meldung
            File.WriteAllText(Path.Combine(directory, "NEUER.ems"),
                $"{{\"kennung\":\"{SaveGame.Identifier}\",\"schema\":{SaveGame.SchemaVersion + 1}}}");

            d.OpenTile("Laden"); d.Move(150, 120); d.Save("03_laden_liste");
            var neuer = Array.FindIndex([.. SaveFiles.List(directory)], e => e.FileName == "NEUER.ems");
            var (col, row) = (neuer / SaveLoadScreen.RowsPerColumn, neuer % SaveLoadScreen.RowsPerColumn);
            var r = SaveLoadScreen.EntryRect(col, row);
            d.Click(r.X + 5, r.Y + 3); d.Save("04_gewaehlt");
            d.Click(r.X + 5, r.Y + 3); d.Save("05_meldung_neuere_version");
            d.Press(Key.Enter); // Meldung schließen
            d.Press(Key.Escape);

            // Überschreiben
            d.OpenTile("Sichern");
            d.Type("test1"); d.Press(Key.Enter); d.Save("06_ueberschreiben_fragen");
            d.Press(Key.Escape);
            d.Press(Key.Escape);

            // Spielvorbereitung: Notizbuch öffnet den Laden-Dialog
            d.Host.Show(new TitleScreen(d.Host));
            d.Frame();
            d.Click(160, 120);
            d.Click(SetupScreen.LoadButton.X + 10, SetupScreen.LoadButton.Y + 10);
            d.Move(150, 120); d.Save("07_laden_aus_spielvorbereitung");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
