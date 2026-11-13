using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Aufstellung (Hauptmenü → Mannschaft → Aufstellung). Vermessen aus dem UI-Video (ca. 9:00):
/// links die Kaderliste NR ART NAME SP STÄRKEN TO 2m 5m STATUS E, rechts vier Blöcke mit je drei Stürmern
/// und zwei Verteidigern (Rückennummern auf kleinen Knöpfen aus kb\2) und der Torhüter (TW).
/// </summary>
/// <remarks>
/// [ORIGINAL] Bedienung wie beim Nationalteam (derselbe Code, Datei 0x40364; A010, Spur kader-start Runde 14 Teil 6):
/// Linksklick in einen Block bzw. den Torhüterkasten wählt die Reihe (Nummer in Farbe 0x10, Datei 0x40890–0x40943); ein
/// Linksklick auf einen belegten Platz leert ihn zusätzlich (Datei 0x40AA3–0x40D0D, danach nicht eingesetzt). Klick auf einen
/// Spieler der Liste: <see cref="Club.ClickPlayer"/> (Linksklick in die gewählte Reihe oder Aufgestellten heraus, Rechtsklick
/// einsetzen/herausnehmen). Rechtsklick außerhalb der Listenzeilen – auch auf einen Platz – verlässt den Bildschirm (Datei
/// 0x40D6B–0x40D78, Zeile 0x7F aus 0x41683). Höchstens 22 Eingesetzte („Schon 22 Mann / im Team !“), „Eingesetzt: n“ in
/// Farbe 16 (Datei 0x41D84).
/// [ORIGINAL] A364: NR (Kaderplatz + 1) und STATUS stehen nur bei Eingesetzten (Datei 0x42027–0x42031 bzw. 0x42281–0x422A7;
/// DOSBox <c>file34_3</c>: J.SCHULZ, DOBRYZINSKI, SANDNER ohne Nummer); Verletzte und Gesperrte zeigen den Status immer (A012).
/// [APPROX] Tasten A (Auto-Aufstellung) und C (leeren) – im Video nicht zu sehen. Unten rechts geht es zur Taktik.
/// </remarks>
public sealed class LineupScreen : Screen
{
    public static readonly Rect Window = new(1, 14, 240, 209);
    /// <summary>
    /// Grundlinie der Kopfzeile; die Linie liegt zwei Zeilen tiefer. [ORIGINAL] em.exe Datei 0x424DF–0x42525 (Text
    /// 0x30:0x831 auf der Grundlinie, Linie 0x30:0x995 bei Grundlinie + 2); Vorlage folge01_00528 (Spur pixel-verein, PV-02).
    /// </summary>
    public const int HeaderBaseline = 23;

    /// <summary>Oberkante der Kopfzeile in Mini (Großbuchstaben fünf Zeilen hoch, 19–23).</summary>
    public const int HeaderY = HeaderBaseline - 4;
    public const int FirstRowY = 29;
    public const int RowPitch = 6;
    public const int ListLeft = 3;
    public const int ListWidth = 235;
    public const int StatusY = 213;

    /// <summary>[ORIGINAL] Farbe von „Eingesetzt: n“ (Datei 0x41D84: 0x10), wie beim Nationalteam.</summary>
    public const byte DressedColor = 16;

    public static Rect BlockBox(int block) => new(246, 15 + 42 * block, 72, 38);
    public static readonly Rect GoalieBox = new(246, 183, 72, 38);

    /// <summary>Knopf eines Aufstellungsplatzes.</summary>
    public static Rect SlotButton(LineupSlot slot) => slot.Role switch
    {
        PlayerRole.Goalkeeper => new Rect(276, 203, TeamUi.SmallButtonWidth, TeamUi.SmallButtonHeight),
        PlayerRole.Defender => new Rect(267 + 18 * slot.Index, 35 + 42 * slot.Block, TeamUi.SmallButtonWidth, TeamUi.SmallButtonHeight),
        _ => new Rect(258 + 18 * slot.Index, 22 + 42 * slot.Block, TeamUi.SmallButtonWidth, TeamUi.SmallButtonHeight),
    };

    private readonly Screen _back;
    private readonly MessageBox _message;
    private (string Line1, string Line2)? _queued;
    /// <summary>[ORIGINAL] Farbe der Nummer der gewählten Reihe (Datei 0x4090A, sonst 2 Datei 0x408D3), wie beim Nationalteam.</summary>
    public const byte SelectedLineColor = 0x10;

    private int? _selectedLine;
    private int _mouseX = -1, _mouseY = -1;
    private readonly PlayerInfoPopup _info = new();

    /// <summary>Spielerdaten-Fenster (rechte Maustaste auf einen Namen).</summary>
    public PlayerInfoPopup Info => _info;

    public LineupScreen(ScreenHost host, Screen back) : base(host)
    {
        _back = back;
        _message = new(() => host.Game?.ActiveManager.Name ?? "");
    }

    /// <summary>Aufstellung mit eigenem Weg zur Taktik (Eingriff im Spiel, <see cref="Live.LiveIntervention"/>).</summary>
    public LineupScreen(ScreenHost host, Screen back, Func<Screen> tactics) : this(host, back) => _tactics = tactics;

    private readonly Func<Screen>? _tactics;

    /// <summary>
    /// Kennwert „im Spiel“ (DGROUP 0x2D6 + Manager über 0x48E28; Spur livespiel-ui-2 B3): Regeln aus
    /// <see cref="Club.ClickPlayerInMatch"/> und <see cref="Club.ClearLineupSlotInMatch"/>, Kopfzeile 0x40290.
    /// </summary>
    public bool InMatch { get; init; }

    private Club Club => TeamUi.OwnClub(Host);

    /// <summary>Gewählte Reihe 0–3 bzw. <see cref="Lineup.GoalkeeperLine"/>, null = keine (DGROUP [bp−9] = 0xFF beim Aufbau).</summary>
    public int? SelectedLine => _selectedLine;

    /// <summary>Zeilen der offenen Meldung (leer, wenn keine offen ist).</summary>
    public IReadOnlyList<string> MessageLines => _message.Lines;

    /// <summary>Ist die offene Meldung eine Kurzmeldung (0x0C0B8)?</summary>
    public bool MessageIsShort => _message.Short is not null;

    public static int RowAt(int y) => y < FirstRowY - 1 ? -1 : (y - FirstRowY + 1) / RowPitch;


    public override void Update(InputState input)
    {
        _mouseX = input.MouseX;
        _mouseY = input.MouseY;
        if (_message.Update(input))
            return;
        // [ORIGINAL] Die zweite Meldung folgt, sobald die erste weg ist (em.exe Datei 0x40ACF–0x40B55: erst „ist deaktiviert !“, dann die Sperre).
        if (_queued is { } next && !_message.Visible)
        {
            _queued = null;
            _message.ShowShort(next.Line1, next.Line2);
            return;
        }
        if (_info.Update(input))
            return;

        var wasAuto = Club.AutoLineupEnabled;
        HandleInput(input);
        // [ORIGINAL] em.exe Datei 0x40ADE (Integration Runde 11, wettbewerbe-spiel Teil 8): Ein Eingriff von Hand schaltet die
        // Einstellung „automatisch aufstellen“ ab und meldet „Auto-Aufstellung / ist deaktiviert !“ (DGROUP 0x124D; Texte
        // Datei 0x1AE4B/0x1AE5C). [APPROX] Trifft der Eingriff mit einer anderen Meldung zusammen, gewinnt diese Meldung.
        // [ORIGINAL] Kommt eine zweite Meldung hinzu (Datei 0x40B55, 0x40EB9), steht „ist deaktiviert !“ vorn, die zweite danach.
        if (wasAuto && !Club.AutoLineupEnabled)
        {
            if (_message.Short is { } second)
                _queued = (second.Line1, second.Line2);
            _message.ShowShort("Auto-Aufstellung", "ist deaktiviert !");
        }
    }

    private void HandleInput(InputState input)
    {
        var rows = TeamUi.Rows(Club);
        // [ORIGINAL] em.exe 0x400E9–0x4010D bzw. 0x4084E–0x40872: rechte Maustaste (Ereignis 3) über der Namensspalte
        // (34 < x < 96) einer Zeile der Hinweiszeilen-Rechnung 0x41683 öffnet das Spielerdaten-Fenster 0x041AC
        // (Spur statistiken-verein; Sichtbarkeit über PlayerDataContext.Create).
        if (input.RightClick && input.MouseX is > 0x22 and < 0x60 && Host.Game is { } game
            && PlayerHintLine.Row(PlayerListLayout.Lineup(rows.Count), input.MouseY) is var hit and >= 0 && hit < rows.Count
            && Statistics.PlayerStatisticsScreen.OpenPlayerData(_info, game, rows[hit]))
            return;
        if (input.Pressed(Key.Escape))
        {
            Leave(_back);
            return;
        }

        // [APPROX] Die Remake-Tasten A und C gibt es im Spiel nicht (sie würden die Regeln „im Spiel“ umgehen).
        switch (InMatch ? "" : input.Text.ToUpperInvariant())
        {
            case "A":
                Club.AutoLineup();
                return;
            case "C":
                Club.ClearLineup();
                return;
        }

        if (input.LeftClick && TeamUi.SwitchButton.Contains(input.MouseX, input.MouseY))
        {
            if (_tactics is not null)
                Leave(_tactics());
            else if (MenuEntries.TryCreate("Taktik", Host, _back, out var tactics))
                Leave(tactics!);
            return;
        }

        var inList = Window.Contains(input.MouseX, input.MouseY);
        var listRow = inList ? RowAt(input.MouseY) : -1;
        var player = listRow >= 0 && listRow < rows.Count ? rows[listRow] : null;
        if (player is null)
        {
            // [ORIGINAL] Datei 0x40D6B–0x40D78: Rechtsklick außerhalb der Listenzeilen verlässt den Bildschirm.
            if (input.RightClick)
                Leave(_back);
            else if (input.LeftClick)
                ClickLines(input.MouseX, input.MouseY);
            return;
        }
        if (!input.LeftClick && !input.RightClick)
            return;
        var line = _selectedLine;
        var result = InMatch
            ? Club.ClickPlayerInMatch(player, ref line, right: input.RightClick)
            : Club.ClickPlayer(player, ref line, right: input.RightClick);
        _selectedLine = line;
        Report(result);
    }

    /// <summary>
    /// Linksklick rechts neben der Liste. [ORIGINAL] Datei 0x40890–0x40943: ein Block bzw. der Torhüterkasten wählt die Reihe.
    /// Datei 0x4095E–0x40D0D: Klick auf einen belegten Platz – Verletzter oder Gesperrter mit Restdauer (+0x0D ≠ 0, +0x09 &amp; 3 ≠ 0):
    /// Kurzmeldung „Spieler ist“ / „noch gesperrt !“ (Datei 0x40B37–0x40B55, auch für Verletzte); sonst Platz leeren, der Spieler
    /// ist nicht mehr eingesetzt (Datei 0x40CF8–0x40D0D). Beides schaltet „automatisch aufstellen“ ab (Datei 0x40ACF–0x40ADE); ein
    /// leerer Platz ändert nichts (Datei 0x40AC4). Im Remake nehmen Verletzung und Sperre den Spieler schon aus der Aufstellung –
    /// die Meldung tritt nur auf, wenn der Status nach dem Aufstellen entsteht.
    /// </summary>
    private void ClickLines(int x, int y)
    {
        if (LineAt(x, y) is { } line)
            _selectedLine = line;
        if (!OnSlot(x, y, out var slot) || Club.Lineup[slot] is not { } placed)
            return;
        if (placed.InjuredGames > 0 || placed.SuspendedGames > 0)
        {
            Club.AutoLineupEnabled = false;
            _message.ShowShort("Spieler ist", "noch gesperrt !");
            return;
        }
        if (InMatch)
            Club.ClearLineupSlotInMatch(slot);
        else
            Club.ClearLineupSlot(slot);
    }

    /// <summary>Reihe unter der Maus: Block 0–3 oder Torhüterkasten (<see cref="Lineup.GoalkeeperLine"/>).</summary>
    public static int? LineAt(int x, int y)
    {
        for (var b = 0; b < Lineup.Blocks; b++)
            if (BlockBox(b).Contains(x, y))
                return b;
        return GoalieBox.Contains(x, y) ? Lineup.GoalkeeperLine : null;
    }

    private void Leave(Screen next)
    {
        Host.Show(next);
    }

    /// <summary>
    /// Meldungen (Texte aus em.exe); „Schon 22 Mann / im Team !“ aus DGROUP 0x11D5. [ORIGINAL] Die Aufstellung ruft für alle Fälle die
    /// Kurzmeldung 0x0C0B8 – ohne Knopf, schließt von selbst. E2 (Spur kader-transfer Teil 4, docs/analyse/re-runde-12.md):
    /// Ein Gesperrter oder Verletzter aus der Kaderliste ergibt „Spieler ist“ / „Gesperrt.“ bzw. „Verletzt.“ (Datei 0x40E01–0x40E27,
    /// Zeile 2 aus DGROUP 0xB88/0xB84, gemeinsamer Aufruf 0x40EB9); Zeile 1 ist fester Text, kein Name. „noch gesperrt !“ gehört zum
    /// Klick auf den Platz (siehe <see cref="ClickLines"/>, Datei 0x40B55). „Keine Reihe / ausgewählt !“ (DGROUP 0x1221),
    /// „Der darf da / nicht rein !“ (DGROUP 0x1229, Datei 0x40E77–0x40EB9) und „Kein / Platz mehr !“ (Datei 0x414C6) wie beim
    /// Nationalteam (A010).
    /// </summary>
    private void Report(LineupResult result)
    {
        switch (result)
        {
            case LineupResult.Injured:
                _message.ShowShort("Spieler ist", "Verletzt.");
                break;
            case LineupResult.Suspended:
                _message.ShowShort("Spieler ist", "Gesperrt.");
                break;
            case LineupResult.WrongRole:
                _message.ShowShort("Der darf da", "nicht rein !");
                break;
            case LineupResult.RosterFull:
                _message.ShowShort("Schon 22 Mann", "im Team !");
                break;
            case LineupResult.NoLineSelected:
                _message.ShowShort("Keine Reihe", "ausgewählt !");
                break;
            case LineupResult.NoRoom:
                _message.ShowShort(ShortMessageTexts.NoRoom.Line1, ShortMessageTexts.NoRoom.Line2);
                break;
        }
    }

    private static bool OnSlot(int x, int y, out LineupSlot slot)
    {
        foreach (var s in LineupSlot.All)
        {
            if (SlotButton(s).Contains(x, y))
            {
                slot = s;
                return true;
            }
        }
        slot = default;
        return false;
    }

    public override void Draw(Surface s)
    {
        var mini = Assets.Mini;
        var club = Club;
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        if (InMatch && Host.Game is { } game)
            Live.LiveIntervention.DrawHeader(s, Assets, game);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);

        const byte h = TeamUi.HeaderColor;
        s.Text(mini, 6, HeaderY, "NR", h);
        s.Text(mini, 18, HeaderY, "ART", h);
        s.Text(mini, 38, HeaderY, "NAME", h);
        s.Text(mini, 95, HeaderY, "SP", h);
        s.Text(mini, 109, HeaderY, "STÄRKEN", h);
        s.Text(mini, 146, HeaderY, "TO", h);
        s.Text(mini, 159, HeaderY, "2m", h);
        s.Text(mini, 172, HeaderY, "5m", h);
        s.Text(mini, 186, HeaderY, "STATUS", h);
        s.Text(mini, 227, HeaderY, "E", h);
        s.HLine(6, HeaderBaseline + 2, 227, TeamUi.LineColor);
        s.HLine(6, 210, 227, TeamUi.LineColor);

        var rows = TeamUi.Rows(club);
        var dressed = club.Dressed.ToHashSet();
        var hoverRow = Window.Contains(_mouseX, _mouseY) ? RowAt(_mouseY) : -1;
        for (var i = 0; i < rows.Count; i++)
        {
            var p = rows[i];
            var y = FirstRowY + i * RowPitch;
            var c = TeamUi.RoleColor(p.Role);
            if (i == hoverRow)
                TeamUi.HoverRow(s, ListLeft + 3, y, ListWidth - 8, RowPitch);
            // [ORIGINAL] A364: NR (Kaderplatz + 1) nur bei Eingesetzten (Datei 0x42027–0x42031: +0x0A = 0 → leer; DOSBox file34_3).
            if (dressed.Contains(p))
                s.TextRight(mini, 17, y, p.Number.ToString(), c);
            s.Text(mini, 18, y, p.RoleShort, c);
            s.Text(mini, 36, y, p.Name, c);
            s.TextRight(mini, 102, y, p.Games.ToString(), c);
            s.TextRight(mini, 116, y, p.Condition.ToString(), c);
            s.TextRight(mini, 128, y, p.Technique.ToString(), c);
            s.TextRight(mini, 140, y, p.Form.ToString(), c);
            s.TextRight(mini, 152, y, p.Role == PlayerRole.Goalkeeper ? "-" : p.Goals.ToString(), c);
            s.TextRight(mini, 165, y, p.Penalties2.ToString(), c);
            s.TextRight(mini, 178, y, p.Penalties5.ToString(), c);
            if (dressed.Contains(p) || p.InjuredGames > 0 || p.SuspendedGames > 0)
                s.Text(mini, 185, y, TeamUi.StatusText(club.StatusOf(p)), c);
            // [ORIGINAL] Spalte E = Belastung · 10 / 15, Farbe 0x10 über 130 wie in der Nationalaufstellung (Datei 0x42335–0x4236D;
            // A184, Integration Runde 12, R12I-01).
            s.TextRight(mini, 231, y, TrainingRules.ExhaustionDisplay(p).ToString(),
                TrainingRules.ExhaustionWarning(p) ? Competitions.NationalLineupScreen.ExhaustionColor : c);
        }
        TeamUi.StatusLine(s, Assets, 131, StatusY, HoverText(rows));

        for (var b = 0; b < Lineup.Blocks; b++)
        {
            Widgets.Frame(s, Assets, BlockBox(b), FrameStyle.Orange);
            s.Text(mini, 251, 33 + 42 * b, $"{b + 1}.", b == _selectedLine ? SelectedLineColor : TeamUi.DimColor);
        }
        Widgets.Frame(s, Assets, GoalieBox, FrameStyle.Orange);
        s.TextCentered(mini, 282, 189, $"Eingesetzt: {dressed.Count}", DressedColor);
        s.Text(mini, 250, 202, "TW", _selectedLine == Lineup.GoalkeeperLine ? SelectedLineColor : TeamUi.DimColor);

        foreach (var slot in LineupSlot.All)
        {
            var r = SlotButton(slot);
            var player = club.Lineup[slot];
            TeamUi.SmallButton(s, Assets, r.X, r.Y, player?.Number.ToString() ?? "", pressed: false);
        }

        TeamUi.DrawSwitch(s, Assets, toTactics: true);
        _message.Draw(s, Assets);
        _info.Draw(s, Assets);
    }

    /// <summary>
    /// Text der Statuszeile für die Spalte bzw. den Platz unter der Maus. [ORIGINAL] Listenzeilen: Hinweiszeile
    /// em.exe 0x41683, siehe <see cref="PlayerHintLine"/> (Aufruf 0x40845: Ursprung 7 | 29, Maske 0xF0C7, Rand 0xE9).
    /// </summary>
    private string? HoverText(IReadOnlyList<Player> rows)
    {
        if (OnSlot(_mouseX, _mouseY, out var slot))
            return Club.Lineup[slot] is { } sp ? $"Name: {sp.Name} ({sp.Age} Jahre)" : null;
        var layout = PlayerListLayout.Lineup(rows.Count);
        var row = PlayerHintLine.Row(layout, _mouseY);
        var column = PlayerHintLine.Column(layout, _mouseX);
        if (row < 0 || column < 0)
            return null;
        var p = rows[row];
        return PlayerHintLine.Text(p, column, new PlayerHintInfo(Club.IsDressed(p), LineOf(p)), showExhaustion: true);
    }

    /// <summary>Reihe (1–4), in der der Spieler aufgestellt ist, sonst 0 (Kaderplatz +0x15 &amp; 0x7F).</summary>
    private int LineOf(Player p)
    {
        foreach (var slot in LineupSlot.All)
            if (Club.Lineup[slot] == p)
                return slot.Block + 1;
        return 0;
    }
}

/// <summary>Anmeldung im Hauptmenü.</summary>
internal static class LineupMenuEntries
{
    [MenuEntry("Aufstellung")]
    private static Screen Open(ScreenHost host, Screen back) => new LineupScreen(host, back);
}
