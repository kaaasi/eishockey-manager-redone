using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// „WM Vorbereitung“ (Bundestrainer, Kachel „Training“ im Menü des Nationalteams): Freundschaftsspiele gegen die
/// anderen elf Nationalteams. Erster Klick auf ein Land wählt es, der zweite vereinbart das Spiel; es wird sofort
/// ausgetragen. Klick auf das Infofeld schaltet zwischen „Heim“ und „Auswärts“ um.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Overlay 0xF8 Eintrag 11, Datei 0x3B8F7); kein Videobeleg, daher kein Pixelvergleich:
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 12 × 12 Rahmenteile ab (16 | 2); Titel Bold zentriert 0 … 319, Grundlinie 21, Farbe 7,
///   Schatten 1; Linie (27 … 292 | 23) Farbe 7.</item>
/// <item>[ORIGINAL] Zwölf Länderfelder 42 × 42 ab (21 | 28), Schritt 43, vier je Reihe (Datei 0x3ACE3): Fläche Farbe 1,
///   Kante oben/links Farbe 23, rechts/unten Farbe 29, Flagge 38 × 38 bei (+2 | +2). Reihenfolge = Plätze der Länder
///   nach den Tauschen (<see cref="WorldCup.Order"/>); das eigene Land ist gesperrt.</item>
/// <item>[ORIGINAL] Acht Kästen für die vereinbarten Spiele bei (193 | 28 + 20 i) … (298 | 46 + 20 i)
///   (<see cref="NationalWidgets.Box"/>): Heim zentriert 197 … 255 und Ergebnis x = 273 (Grundlinie y + 8), „-“ x = 257,
///   Gast zentriert bei 253 (Grundlinie y + 14); Mini, Farbe 7, das Land des Bundestrainers 17.</item>
/// <item>[ORIGINAL] Infofeld (21 | 157) … (191 | 187): „Heim“ bzw. „Auswärts“ (Bold, zentriert 27 … 188, Grundlinie
///   179, Farbe 7, Schatten 0); über einem Land stattdessen „Gegner“, „Kosten“, „Kontostand“ (Mini, x = 27, Grundlinien
///   167/174/181, Farbe 0), „:“ bei x = 75, Werte bei x = 88.</item>
/// <item>[ORIGINAL] Nach der Wahl eines Landes unten die Mannschaftsfelder von Heim (x = 31) und Gast (x = 165).</item>
/// <item>[APPROX] Das Original setzt beim Öffnen die Energie (Kaderfeld 0x13) aller Kaderspieler auf 70 (Datei 0x3BB51);
///   Bildschirme ändern im Remake beim Öffnen keinen Zustand, das Remake setzt sie beim ersten Freundschaftsspiel
///   (<see cref="GameState.PlayFriendly"/>).</item>
/// </list>
/// </remarks>
public sealed class FriendliesScreen : Screen
{
    public static readonly Rect Window = new(16, 2, 288, 228);
    public const string Title = "WM Vorbereitung";
    /// <summary>[ORIGINAL] Infofeld (21 | 157) … (191 | 187).</summary>
    public static readonly Rect InfoBox = new(21, 157, 171, 31);

    /// <summary>[ORIGINAL] Klickfläche des Infofelds (Feld 20 der Trefferliste): x 21 … 194, y 157 … 187.</summary>
    public static readonly Rect InfoHit = new(21, 157, 174, 31);

    private readonly Screen _back;
    private readonly MessageBox _message;

    public FriendliesScreen(ScreenHost host, Screen back) : base(host)
    {
        _back = back;
        _message = new(() => host.Game?.ActiveManager.Name ?? "");
    }

    /// <summary>Heimspiel (Vorgabe) oder Auswärtsspiel des eigenen Landes.</summary>
    public bool Home { get; private set; } = true;

    /// <summary>Land unter dem Mauszeiger (nicht das eigene) oder null.</summary>
    public int? Hovered { get; private set; }

    /// <summary>Gewähltes Land (erster Klick) oder null.</summary>
    public int? Selected { get; private set; }

    public MessageBox Message => _message;

    /// <summary>Feld des Landes an Platz <paramref name="i"/> (0–11).</summary>
    public static Rect Tile(int i) => new(21 + 43 * (i % 4), 28 + 43 * (i / 4), 42, 42);

    /// <summary>Kasten des <paramref name="i"/>-ten Freundschaftsspiels (0–7).</summary>
    public static Rect Row(int i) => new(193, 28 + 20 * i, 106, 19);

    /// <summary>Länder in der Reihenfolge der Felder.</summary>
    public IReadOnlyList<int> Nations =>
        Host.Game?.WorldCup?.Order ?? Enumerable.Range(NationalTeams.First, WorldCup.Teams).ToList();

    private int? NationAt(int x, int y)
    {
        for (var i = 0; i < WorldCup.Teams; i++)
            if (new Rect(Tile(i).X, Tile(i).Y, 43, 43).Contains(x, y))
                return Nations[i];
        return null;
    }

    public override void Update(InputState input)
    {
        if (_message.Update(input))
            return;
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (Host.Game is not { NationalCoach: { } coach } game)
        {
            if (input.LeftClick)
                Host.Show(_back);
            return;
        }

        // Datei 0x3BE17 / 0x3C2E4: ein neues Land unter der Maus zeigt dessen Infos und hebt die Wahl auf; außerhalb
        // der Länder (auch über dem eigenen) gilt wieder „Heim“/„Auswärts“, die Wahl verfällt.
        var nation = NationAt(input.MouseX, input.MouseY);
        if (nation == coach.Nation)
            nation = null;
        if (nation != Hovered)
        {
            Hovered = nation;
            Selected = null;
        }

        if (!input.LeftClick)
            return;
        if (nation is not { } opponent)
        {
            if (InfoHit.Contains(input.MouseX, input.MouseY))
                Home = !Home;
            return;
        }
        if (Selected != opponent)
        {
            Selected = opponent;
            return;
        }
        switch (game.PlayFriendly(opponent, Home))
        {
            case FriendlyResult.TooMany:
                // [ORIGINAL] Datei 0x3C2C5: Kurzmeldung 0x0C0B8 (ohne Knopf, schließt von selbst; Integration Runde 11, ui-meldungen, K2).
                // [APPROX] „Sie haben nicht / genug Geld !“ darunter hat keinen eigenen Aufruf 0x0C0B8; es bleibt ein Meldungsfenster.
                _message.ShowShort("Nicht mehr als", "8 Freundschaftsspiele");
                break;
            case FriendlyResult.NotEnoughMoney:
                _message.Show("Sie haben nicht", "genug Geld !");
                break;
        }
        Selected = null;
        Hovered = null;
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);
        CompetitionWidgets.Centered(s, Assets.Bold, 0, 319, 21, Title, Colors.White, 1);
        s.HLine(27, 23, 292 - 27 + 1, Colors.White);

        if (Host.Game is not { } game)
            return;
        var nations = Nations;
        for (var i = 0; i < nations.Count; i++)
        {
            var t = Tile(i);
            NationalWidgets.FillInclusive(s, t.X, t.Y, t.X + 41, t.Y + 41, 1);
            s.HLine(t.X, t.Y, 42, 23);
            s.VLine(t.X, t.Y, 42, 23);
            s.VLine(t.X + 41, t.Y + 1, 41, 29);
            s.HLine(t.X + 1, t.Y + 41, 40, 29);
            Widgets.Logo(s, Assets, game.Clubs[nations[i]].LogoGraphic, t.X + 2, t.Y + 2);
        }

        var coach = game.NationalCoach;
        var mini = Assets.Mini;
        for (var i = 0; i < NationalCoach.MaxFriendlies; i++)
        {
            var r = Row(i);
            NationalWidgets.Box(s, r.X, r.Y, r.Right - 1, r.Bottom - 1);
            if (coach is null || i >= coach.Friendlies.Count)
                continue;
            var f = coach.Friendlies[i];
            byte Color(int club) => club == coach.Nation ? (byte)17 : Colors.White;
            int x = r.X + 4, y = r.Y + 4;
            CompetitionWidgets.Centered(s, mini, x, x + 58, y + 4, game.Clubs[f.Home].Name, Color(f.Home), null);
            CompetitionWidgets.Left(s, mini, x + 60, y + 4, "-", Colors.White);
            CompetitionWidgets.Centered(s, mini, x + 56, x + 56, y + 10, game.Clubs[f.Away].Name, Color(f.Away), null);
            CompetitionWidgets.Left(s, mini, x + 76, y + 4,
                CompetitionWidgets.Number(f.HomeGoals, 2) + ":" + CompetitionWidgets.Number(f.AwayGoals, 2), Colors.White);
        }

        NationalWidgets.Box(s, InfoBox.X, InfoBox.Y, InfoBox.Right - 1, InfoBox.Bottom - 1);
        NationalWidgets.FillInclusive(s, 27, 160, 188, 184, 1);
        if (Hovered is { } hovered && coach is not null)
        {
            string[] labels = ["Gegner", "Kosten", "Kontostand"];
            string[] values =
            [
                game.Clubs[hovered].Name,
                GameState.FormatMoney(NationalCoach.FriendlyCost(hovered, Home)),
                GameState.FormatMoney(game.NationalAccount), // Konto des Bundestrainers (Spur finanzen-2)
            ];
            for (var k = 0; k < 3; k++)
            {
                CompetitionWidgets.Left(s, mini, 27, 167 + 7 * k, labels[k], Colors.Black);
                CompetitionWidgets.Left(s, mini, 75, 167 + 7 * k, ":", Colors.Black);
                CompetitionWidgets.Left(s, mini, 88, 167 + 7 * k, values[k], Colors.Black);
            }
        }
        else
            CompetitionWidgets.Centered(s, Assets.Bold, 27, 188, 179, Home ? "Heim" : "Auswärts", Colors.White, Colors.Black);

        if (Selected is { } selected && coach is not null)
        {
            var (home, away) = Home ? (coach.Nation, selected) : (selected, coach.Nation);
            NationalWidgets.TeamPanel(s, Assets, game, home, 0);
            NationalWidgets.TeamPanel(s, Assets, game, away, 1);
        }

        _message.Draw(s, Assets);
    }
}
