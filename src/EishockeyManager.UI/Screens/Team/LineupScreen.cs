using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Team;

/// <summary>
/// Aufstellung (Hauptmenü → Mannschaft → Aufstellung). Vermessen aus dem UI-Video (ca. 9:00):
/// links die Kaderliste NR ART NAME SP STÄRKEN TO 2m 5m STATUS E, rechts vier Blöcke mit je drei Stürmern
/// und zwei Verteidigern (Rückennummern auf kleinen Knöpfen aus kb\2) und der Torhüter (TW).
/// </summary>
/// <remarks>
/// Bedienung: Spieler anklicken, dann Platz (oder umgekehrt); Rechtsklick auf einen Platz leert ihn.
/// [ORIGINAL] Wie beim Nationalteam (derselbe Code, Datei 0x40364): Linksklick auf einen eingesetzten, nicht aufgestellten
/// Spieler nimmt ihn heraus, Rechtsklick auf einen nicht eingesetzten setzt ihn ein, höchstens 22 Eingesetzte
/// („Schon 22 Mann / im Team !“), „Eingesetzt: n“ in Farbe 16 (Datei 0x41D84). Regeln: <see cref="Club.MaxDressed"/>.
/// [APPROX] Linksklick auf nicht eingesetzte oder aufgestellte Spieler wählt sie aus; STATUS bleibt bei nicht
/// Eingesetzten leer (wie beim Nationalteam), NR zeigt weiter die Rückennummer.
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
    private Player? _selectedPlayer;
    private LineupSlot? _selectedSlot;
    private int _mouseX = -1, _mouseY = -1;
    private readonly PlayerInfoPopup _info = new();

    /// <summary>Spielerdaten-Fenster (rechte Maustaste auf einen Namen).</summary>
    public PlayerInfoPopup Info => _info;

    public LineupScreen(ScreenHost host, Screen back) : base(host)
    {
        _back = back;
        _message = new(() => host.Game?.ActiveManager.Name ?? "");
    }

    private Club Club => TeamUi.OwnClub(Host);

    public Player? SelectedPlayer => _selectedPlayer;

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

        switch (input.Text.ToUpperInvariant())
        {
            case "A":
                Club.AutoLineup();
                ClearSelection();
                return;
            case "C":
                Club.ClearLineup();
                ClearSelection();
                return;
        }

        if (input.Clicked(TeamUi.SwitchButton))
        {
            if (MenuEntries.TryCreate("Taktik", Host, _back, out var tactics))
                Leave(tactics!);
            return;
        }

        if (OnSlot(input.MouseX, input.MouseY, out var slot))
        {
            // [ORIGINAL] em.exe Datei 0x40B37–0x40B55: Klick auf einen belegten Platz, dessen Spieler Restdauer ≠ 0 und Status
            // gesperrt/verletzt hat → Kurzmeldung „Spieler ist“ / „noch gesperrt !“, keine Aktion (auch für Verletzte).
            // [APPROX] Nur Linksklick; der Rechtsklick leert den Platz weiter (Prüfung des Originals für die rechte Taste fehlt).
            // Im Remake nehmen Verletzung und Sperre den Spieler schon aus der Aufstellung heraus – der Fall tritt nur auf, wenn
            // der Status nach dem Aufstellen entsteht. Mit Automatik: erst deaktivieren (Datei 0x40ACF), dann diese Meldung.
            if (input.LeftClick && Club.Lineup[slot] is { } placed && (placed.InjuredGames > 0 || placed.SuspendedGames > 0))
            {
                Club.AutoLineupEnabled = false;
                ClearSelection();
                _message.ShowShort("Spieler ist", "noch gesperrt !");
                return;
            }
            if (input.RightClick)
            {
                Club.ClearLineupSlot(slot);
                ClearSelection();
            }
            else if (input.LeftClick)
            {
                if (_selectedPlayer is { } p)
                    TryAssign(slot, p);
                else
                    _selectedSlot = _selectedSlot == slot ? null : slot;
            }
            return;
        }

        var inList = Window.Contains(input.MouseX, input.MouseY);
        var listRow = inList ? RowAt(input.MouseY) : -1;
        var player = listRow >= 0 && listRow < rows.Count ? rows[listRow] : null;
        if (input.RightClick)
        {
            if (player is null)
                Leave(_back);
            else if (!Club.IsDressed(player))
                Report(Club.Dress(player), player);
            return;
        }
        if (!input.LeftClick || player is null)
            return;
        if (_selectedSlot is { } target)
            TryAssign(target, player);
        else if (Club.IsDressed(player) && !Club.Lineup.Contains(player))
        {
            Club.Undress(player);
            ClearSelection();
        }
        else
            _selectedPlayer = _selectedPlayer == player ? null : player;
    }

    private void Leave(Screen next)
    {
        Host.Show(next);
    }

    private void ClearSelection()
    {
        _selectedPlayer = null;
        _selectedSlot = null;
    }

    private void TryAssign(LineupSlot slot, Player player)
    {
        ClearSelection();
        Report(Club.AssignToLineup(slot, player), player);
    }

    /// <summary>
    /// Meldungen (Texte aus em.exe); „Schon 22 Mann / im Team !“ aus DGROUP 0x11D5. [ORIGINAL] Die Aufstellung ruft für alle Fälle die
    /// Kurzmeldung 0x0C0B8 – ohne Knopf, schließt von selbst. E2 (Spur kader-transfer Teil 4, docs/analyse/re-runde-12.md):
    /// Ein Gesperrter oder Verletzter aus der Kaderliste ergibt „Spieler ist“ / „Gesperrt.“ bzw. „Verletzt.“ (Datei 0x40E01–0x40E27,
    /// Zeile 2 aus DGROUP 0xB88/0xB84, gemeinsamer Aufruf 0x40EB9); Zeile 1 ist fester Text, kein Name. „noch gesperrt !“ gehört zum
    /// Klick auf den Platz (siehe <see cref="HandleInput"/>, Datei 0x40B55). Fall „Keine Reihe / ausgewählt !“ (Datei 0x40EB9, DGROUP 0x1221):
    /// ohne Entsprechung – das Remake wählt erst den Spieler oder den Platz und prüft erst bei der Zuordnung.
    /// </summary>
    private void Report(LineupResult result, Player player)
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
            if (i == hoverRow || p == _selectedPlayer)
                TeamUi.HoverRow(s, ListLeft + 3, y, ListWidth - 8, RowPitch);
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
            s.TextRight(mini, 231, y, p.Energy.ToString(), c);
        }
        TeamUi.StatusLine(s, Assets, 131, StatusY, HoverText(rows));

        for (var b = 0; b < Lineup.Blocks; b++)
        {
            Widgets.Frame(s, Assets, BlockBox(b), FrameStyle.Orange);
            s.Text(mini, 251, 33 + 42 * b, $"{b + 1}.", TeamUi.DimColor);
        }
        Widgets.Frame(s, Assets, GoalieBox, FrameStyle.Orange);
        s.TextCentered(mini, 282, 189, $"Eingesetzt: {dressed.Count}", DressedColor);
        s.Text(mini, 250, 202, "TW", TeamUi.DimColor);

        foreach (var slot in LineupSlot.All)
        {
            var r = SlotButton(slot);
            var player = club.Lineup[slot];
            TeamUi.SmallButton(s, Assets, r.X, r.Y, player?.Number.ToString() ?? "", pressed: slot == _selectedSlot);
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
