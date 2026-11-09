using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.SaveLoad;

/// <summary>Ein Manager im Infofeld: Name, Verein und – außer im Modus „nur Weltkugel“ – die Liga.</summary>
public sealed record SaveInfoManager(string Name, string Club, string? League);

/// <summary>
/// Inhalt der Spielstand-Info des Speichern/Laden-Dialogs (Rechtsklick auf einen Spielstand, em.exe Datei 0x5B661):
/// Jahr, nächster Spieltag und je Manager Name, Verein und Liga.
/// </summary>
/// <remarks>
/// [ORIGINAL] Original-Spielstände (<c>*.HOK</c>): aus dem unverschlüsselten Kopf (<see cref="OriginalSaveHeader"/>); Liga nur,
/// wenn Eintragsbyte +1 = 0 (Datei 0x5B8A3–0x5B9A5), Name aus DGROUP 0x9EC + 4 · Liga („Bundesliga“, „Zweite Liga“, „Oberliga“).
/// [APPROX] Remake-Spielstände (<c>*.ems</c>) haben keinen solchen Kopf; die Felder kommen aus dem geladenen Spielstand
/// (Kalenderjahr des Tages, Spieltag = gespielte Spieltage + 1 wie das Kopfbyte 0x7A9, Verein in Großbuchstaben wie im Kopf).
/// </remarks>
public sealed record SaveInfo(int Year, int MatchDay, IReadOnlyList<SaveInfoManager> Managers)
{
    private static readonly string[] LeagueNames = ["Bundesliga", "Zweite Liga", "Oberliga"];

    /// <summary>Zeile 2: „Jahr 1994, 1.Spieltag“ ([ORIGINAL] Datei 0x5B7ED–0x5B8A0; Texte DGROUP 0x2852, 0x1B2D).</summary>
    public string DateLine => $"Jahr {Year}, {MatchDay}.Spieltag";

    /// <summary>Liest die Info aus den Bytes einer Datei; null, wenn es kein Spielstand ist („Kein Eishockey Manager Spielstand“).</summary>
    public static SaveInfo? Read(byte[] data, SaveEntryKind kind) =>
        kind == SaveEntryKind.Original ? FromOriginal(data) : FromRemake(data);

    public static SaveInfo? FromOriginal(byte[] data)
    {
        if (OriginalSaveHeader.TryRead(data) is not { } header)
            return null;
        var managers = header.Managers
            .Select(m => new SaveInfoManager(m.Name, m.Club, m.OnlyWorldMode ? null : LeagueName(m.StartLeague)))
            .ToList();
        return new SaveInfo(header.Year, header.MatchDay, managers);
    }

    public static SaveInfo? FromRemake(byte[] data)
    {
        if (SaveGame.Load(data).Game is not { } game)
            return null;
        var worldOnly = game.Setup.Mode == GameMode.WorldOnly;
        var managers = game.Managers
            .Select(m =>
            {
                var club = game.Clubs[m.ClubIndex];
                return new SaveInfoManager(m.Name, club.Name.ToUpperInvariant(), worldOnly || club.League is not { } league ? null : LeagueName((int)league));
            })
            .ToList();
        return new SaveInfo(SeasonCalendar.Year(game.Day, game.Season), game.PlayedMatchDays + 1, managers);
    }

    private static string LeagueName(int league) => LeagueNames[Math.Clamp(league, 0, LeagueNames.Length - 1)];
}
