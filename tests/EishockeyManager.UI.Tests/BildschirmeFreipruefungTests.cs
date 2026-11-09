using System.Reflection;
using System.Text;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Freiprüfung aller Bildschirme (Spur <c>pruefung-karte</c> Teil 1, offener Bereich aus #557, AGENTS.md §4): Jeder konkrete
/// <see cref="Screen"/> der UI-Assembly wird über Reflexion mit jedem seiner Konstruktoren erzeugt, geöffnet
/// (<c>Host.Show</c>), gezeichnet und einige Frames ohne Eingabe weiterlaufen gelassen (<c>Update</c>, #620); der Spielzustand
/// (gespeicherte Bytes und Feldabdruck) muss danach unverändert sein.
/// </summary>
/// <remarks>
/// Bekannte Abweichungen stehen mit Issue-Nummer in <see cref="Known"/>; der Test scheitert bei jeder neuen und bei jeder
/// nicht mehr auftretenden bekannten Abweichung (damit die Liste nicht veraltet). Konstruktoren, für die
/// <see cref="ScreenReflection"/> kein Argument bauen kann, stehen in <see cref="NotConstructible"/> – die Liste ist die
/// sichtbare Abdeckungslücke; der Test scheitert, wenn ein Eintrag hinzukommt oder wegfällt (#610, R13PK-02).
/// </remarks>
public class BildschirmeFreipruefungTests
{
    /// <summary>Bekannte Abweichungen: „Typ(Konstruktor)@Zustand“ → Issue.</summary>
    private static readonly Dictionary<string, string> Known = new(StringComparer.Ordinal);

    /// <summary>Nicht erzeugbare Konstruktoren „Signatur: Parameter“ (heute keine; #610).</summary>
    private static readonly HashSet<string> NotConstructible = new(StringComparer.Ordinal);

    /// <summary>Frames ohne Eingabe nach dem ersten Zeichnen (#620, R13GN-03).</summary>
    private const int UpdateFrames = 3;

    /// <summary>
    /// Einstiege mit Vorbedingung, die ohne passenden Zustand werfen (Schlüssel: Typ oder „Typ.Methode“ → Grund). Sie zählen als
    /// „nicht prüfbar in diesem Zustand“ und stehen im Bericht. Das Fenster „Saisonende“ prüft
    /// <c>BildschirmeOeffnenZustandTests.SeasonEndScreens_OpeningAndDrawingDoNotChangeGameState</c>; Kacheln prüft
    /// <c>BildschirmeOeffnenZustandTests.MenuTileScreens_OpeningAndDrawingDoNotChangeGameState</c> über <c>MenuEntries</c>.
    /// </summary>
    private static readonly Dictionary<string, string> Preconditions = new(StringComparer.Ordinal)
    {
        ["EishockeyManager.UI.Screens.SeasonEndScreen"] = "nur im Saisonende gültig",
        ["EishockeyManager.UI.Screens.SeasonEndSummaryScreen"] = "nur im Saisonende gültig",
        // #611 (R13PK-03): Open läuft nicht über MenuEntries, sondern aus MainMenuScreen (Kachel des Bundestrainers); mit einem
        // unbekannten Kachelnamen liefert es null. Gedeckt durch die direkte Prüfung der Bildschirme, die Open erzeugt
        // (NationalSquadScreen, FriendliesScreen, NationalLineupScreen und NationalLineupScreen.Tactics), und durch
        // NationalMenu_Open_AllCoachTiles_DoNotChangeGameState.
        ["EishockeyManager.UI.Screens.Competitions.NationalMenu.Open"] = "liefert für einen unbekannten Kachelnamen null",
    };

    /// <summary>Wie <see cref="Preconditions"/>, aber nur ohne ernannten Bundestrainer.</summary>
    private static readonly Dictionary<string, string> NeedsNationalCoach = new(StringComparer.Ordinal)
    {
        ["EishockeyManager.UI.Screens.Competitions.NationalLineupScreen.Tactics"] = "braucht einen ernannten Bundestrainer",
    };

    private static string? PreconditionReason(string type, string sig, StateKind kind)
    {
        foreach (var (k, reason) in Preconditions)
            if (type == k || sig.StartsWith(k + "(", StringComparison.Ordinal))
                return reason;
        if (kind != StateKind.NationalCoach)
            foreach (var (k, reason) in NeedsNationalCoach)
                if (sig.StartsWith(k + "(", StringComparison.Ordinal))
                    return reason;
        return null;
    }

    private enum StateKind { Start, MidSeason, NationalCoach }

    private static (ScreenDriver Driver, GameState Game, Screen Menu) Start(StateKind kind)
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        int[] clubs = [2, 14];
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        var game = NewGame.Create(d.Host.Assets.Database, setup, 5);
        d.Host.Game = game;
        var menu = new MainMenuScreen(d.Host);
        d.Host.Show(menu);
        d.Frame();
        if (kind == StateKind.MidSeason)
        {
            for (var i = 0; i < 6; i++)
            {
                KeepSquadHelper.KeepSquads(game);
                d.Advance();
                if (d.Host.Current is not MainMenuScreen)
                    d.Host.Show(new MainMenuScreen(d.Host));
            }
            menu = (MainMenuScreen)(d.Host.Current is MainMenuScreen m ? m : new MainMenuScreen(d.Host));
            d.Host.Show(menu);
            d.Frame();
        }
        if (kind == StateKind.NationalCoach)
            game.AppointNationalCoach(game.Managers[1]); // Bildschirme der Nationalmannschaft brauchen einen ernannten Bundestrainer
        game.CurrentManager = 1;
        return (d, game, menu);
    }

    private sealed record Result(string Key, string? Diff, string? Error);

    /// <summary>Öffnet und zeichnet alle erzeugbaren Bildschirme; liefert Abweichungen und nicht erzeugbare Konstruktoren.</summary>
    private static (List<Result> Findings, List<string> NotBuilt, List<string> Unresolved, int Opened) Run(StateKind kind)
    {
        var (d, game, menu) = Start(kind);
        var ctx = new ScreenReflection.Context(d.Host, game, menu);
        var findings = new List<Result>();
        var notBuilt = new List<string>();
        var unresolved = new List<string>();
        var opened = 0;
        var sources = ScreenReflection.AllScreenTypes()
            .SelectMany(type => ScreenReflection.Constructors(type).Select(c => (Type: type, Method: (MethodBase)c)))
            .Concat(ScreenReflection.Factories().Select(m => (Type: m.DeclaringType!, Method: (MethodBase)m)))
            .ToList();
        foreach (var (type, ctor) in sources)
        {
            {
                var sig = $"{type.FullName}.{(ctor.IsConstructor ? "ctor" : ctor.Name)}({string.Join(", ", ctor.GetParameters().Select(p => p.ParameterType.Name))})";
                var variants = ctor.GetParameters().Any(p => p.ParameterType == typeof(int) || p.ParameterType == typeof(bool)
                                                              || p.ParameterType == typeof(Player) || p.ParameterType.IsEnum) ? 3 : 1;
                for (var variant = 0; variant < variants; variant++)
                {
                    var args = ScreenReflection.BuildArguments(ctor, ctx, variant, out var missing);
                    if (args is null)
                    {
                        notBuilt.Add($"{sig}: {missing}");
                        unresolved.Add($"{sig}: {missing}");
                        break;
                    }
                    var key = $"{sig}#{variant}@{kind}";
                    game.CurrentManager = 1;
                    var bytes = SaveGame.Save(game);
                    var print = StateFingerprint.Of(game);
                    try
                    {
                        var screen = (Screen)(ctor.IsConstructor ? ((ConstructorInfo)ctor).Invoke(args) : ctor.Invoke(null, args))!;
                        d.Host.Show(screen);
                        d.Host.Draw(d.Surface);
                        d.Frames(UpdateFrames); // #620 (R13GN-03): auch Frames ohne Eingabe (Update) ändern keinen Zustand
                        opened++;
                    }
                    catch (Exception) when (PreconditionReason(type.FullName!, sig, kind) is not null)
                    {
                        notBuilt.Add($"{sig}: {PreconditionReason(type.FullName!, sig, kind)}");
                    }
                    catch (Exception e)
                    {
                        var inner = e is TargetInvocationException { InnerException: { } i } ? i : e;
                        findings.Add(new Result(key, null, $"{inner.GetType().Name}: {inner.Message}"));
                    }
                    var after = SaveGame.Save(game);
                    var afterPrint = StateFingerprint.Of(game);
                    if (!bytes.AsSpan().SequenceEqual(after) || print != afterPrint)
                        findings.Add(new Result(key, StateFingerprint.FirstDifference(print, afterPrint) ?? "Bytes weichen ab", null));
                    // Zustand wieder auf den Ausgangspunkt, damit eine Abweichung nicht die folgenden verfälscht.
                    game = Restore(d, bytes, game);
                    ctx = new ScreenReflection.Context(d.Host, game, menu);
                    d.Host.Show(menu);
                }
            }
        }
        return (findings, notBuilt, unresolved.Distinct().ToList(), opened);
    }

    private static GameState Restore(ScreenDriver d, byte[] bytes, GameState current)
    {
        // Gleicher Spielstand, falls nichts abwich; sonst aus den Bytes laden, damit Folgeprüfungen sauber starten.
        if (bytes.AsSpan().SequenceEqual(SaveGame.Save(current)))
            return current;
        var game = SaveGame.Load(bytes).Game!;
        d.Host.Game = game;
        return game;
    }

    /// <summary>Gegenprobe: Der Vergleich erkennt eine Zustandsänderung (hier: ein Tag Zeit), sonst wäre „keine Abweichung“ wertlos.</summary>
    [GameDataFact]
    public void Detector_SeesStateChange()
    {
        var (d, game, _) = Start(StateKind.Start);
        var bytes = SaveGame.Save(game);
        var print = StateFingerprint.Of(game);
        game.Continue();
        Assert.False(bytes.AsSpan().SequenceEqual(SaveGame.Save(game)));
        Assert.NotEqual(print, StateFingerprint.Of(game));
        Assert.NotNull(d);
    }

    [GameDataTheory]
    [InlineData(StateKind.Start)]
    [InlineData(StateKind.MidSeason)]
    [InlineData(StateKind.NationalCoach)]
    private void AllScreens_OpeningAndDrawingDoNotChangeGameState(StateKind kind)
    {
        var (findings, notBuilt, unresolved, opened) = Run(kind);
        var report = Environment.GetEnvironmentVariable("EM_FREIPRUEFUNG_BERICHT");
        if (!string.IsNullOrEmpty(report))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Zustand {kind}: {opened} geöffnet, {findings.Count} Abweichungen, {notBuilt.Count} nicht erzeugbar");
            foreach (var f in findings) sb.AppendLine($"ABW {f.Key}: {f.Diff ?? f.Error}");
            foreach (var n in notBuilt.Distinct()) sb.AppendLine($"NICHT {n}");
            File.AppendAllText(report, sb.ToString());
        }
        Assert.True(opened > 0);
        var neu = findings.Where(f => !Known.ContainsKey(f.Key)).Select(f => $"{f.Key}: {f.Diff ?? f.Error}").ToList();
        Assert.True(neu.Count == 0, "Neue Abweichungen:\n" + string.Join("\n", neu));
        // #610 (R13PK-02): veraltete bekannte Abweichungen und eine geänderte Abdeckungslücke fallen auf.
        var stale = Known.Keys.Where(k => k.EndsWith("@" + kind, StringComparison.Ordinal) && findings.All(f => f.Key != k)).ToList();
        Assert.True(stale.Count == 0, "Bekannte Abweichungen treten nicht mehr auf:\n" + string.Join("\n", stale));
        Assert.Equal(NotConstructible.Order(StringComparer.Ordinal), unresolved.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// #611 (R13PK-03): <c>NationalMenu.Open</c> mit allen Kachelnamen des Bundestrainer-Menüs öffnet einen Bildschirm, ohne
    /// den Spielzustand zu ändern (auch mit Frames ohne Eingabe).
    /// </summary>
    [GameDataFact]
    public void NationalMenu_Open_AllCoachTiles_DoNotChangeGameState()
    {
        var (d, game, menu) = Start(StateKind.NationalCoach);
        // Zug des Bundestrainers (sonst liefert Open null); im Spiel setzt ihn PassNationalTurn am Ende der Vereinszüge.
        typeof(GameState).GetField("_nationalTurn", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, true);
        game.CurrentManager = game.NationalCoach!.Manager;
        Assert.True(game.NationalCoachMenu);
        var opened = 0;
        foreach (var tile in EishockeyManager.UI.Screens.Competitions.NationalMenu.CoachTiles)
        {
            if (EishockeyManager.UI.Screens.Competitions.NationalMenu.Open(game, tile, d.Host, menu) is not { } screen)
                continue;
            var bytes = SaveGame.Save(game);
            var print = StateFingerprint.Of(game);
            d.Host.Show(screen);
            d.Host.Draw(d.Surface);
            d.Frames(UpdateFrames);
            Assert.True(bytes.AsSpan().SequenceEqual(SaveGame.Save(game)), $"Kachel {tile}: Bytes weichen ab");
            Assert.Equal(print, StateFingerprint.Of(game));
            opened++;
            d.Host.Show(menu);
        }
        Assert.True(opened >= 4, $"nur {opened} Kacheln geöffnet");
    }
}
