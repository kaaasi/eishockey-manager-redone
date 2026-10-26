namespace EishockeyManager.Formats;

/// <summary>Spieler aus der Datenbank (nur Name und Ausländer-Kennzeichen).</summary>
public sealed record DatabasePlayer(string Name, bool IsForeigner, PlayerRole Role);

public enum PlayerRole
{
    Goalkeeper,
    Defender,
    Forward,
}

/// <summary>Verein oder Nationalmannschaft aus der Datenbank.</summary>
/// <param name="Strength">Erster Stärkewert (= <see cref="ClubStrengths.Defense"/>), wie bisher als Vereinsstärke genutzt.</param>
/// <param name="Strengths">Alle drei Stärkewerte unverändert aus der Datei (Befund A01).</param>
public sealed record DatabaseClub(
    int Index,
    string Name,
    int Strength,
    int LogoNumber,
    short ColorCode,
    IReadOnlyList<DatabasePlayer> Players,
    ClubStrengths Strengths = default)
{
    /// <summary>Grafik des Vereinslogos (kb\80–143), oder null für Vereine ohne Logo.</summary>
    public int? LogoGraphic => LogoNumber >= 0 ? 80 + LogoNumber : null;

    public ClubCategory Category => Index switch
    {
        < 12 => ClubCategory.FirstLeague,
        < 24 => ClubCategory.SecondLeague,
        < 36 => ClubCategory.Oberliga,
        < 48 => ClubCategory.OberligaReserve,
        < 52 => ClubCategory.Empty,
        < 64 => ClubCategory.NationalTeam,
        _ => ClubCategory.EuropeanOpponent,
    };
}

/// <summary>
/// Die drei Stärkewerte eines Vereins (Datenbank 0x016C + 3 · Index), Rohwerte unverändert.
/// Bedeutung belegt am Lader in em.exe (Datei 0x2EF1B, siehe docs/formate/dateiformate.md):
/// </summary>
/// <param name="Defense">Byte 0: Basis für Kondition und Technik der Abwehr (Vereinsfelder 0x18 / 0x1A).</param>
/// <param name="Attack">Byte 1: Basis für Kondition und Technik des Sturms (Vereinsfelder 0x19 / 0x1B).</param>
/// <param name="Third">Byte 2: vom Lader nicht gelesen, kein anderer Leser gefunden – Bedeutung offen.</param>
public readonly record struct ClubStrengths(byte Defense, byte Attack, byte Third);

public enum ClubCategory
{
    FirstLeague,
    SecondLeague,
    Oberliga,
    OberligaReserve,
    Empty,
    NationalTeam,
    EuropeanOpponent,
}

/// <summary>
/// Vereins- und Spielerdatenbank kb\23.vga (36 648 Bytes, trotz Endung kein Bild).
///
/// | Offset | Größe    | Inhalt |
/// |--------|----------|--------|
/// | 0x0000 | 150 × 2  | Farbcode je Verein (hi, lo als sbyte → hi·256 + lo) |
/// | 0x012C | 64 × 1   | Logo: (b + 0xB0) &amp; 0x3F → kb\(80 + Logo).vga |
/// | 0x016C | 150 × 3  | Stärken je Verein: Abwehr, Sturm, dritter Wert (Bedeutung offen); nur bei den Europapokal-Gegnern verschieden |
/// | 0x04AE | 64 × 523 | Vereine mit Kader: Name[23] + 25 × (Name[19] + Ausländer[1]) |
/// | 0x876E | 86 × 23  | Namen der Europapokal-Gegner (ohne Kader) |
///
/// Kaderplätze: 0–1 Torhüter, 2–11 Verteidiger, 12–24 Stürmer. Leere Plätze beginnen mit 0x00.
/// </summary>
public sealed class ClubDatabase
{
    public const int FileLength = 36648;
    public const int TotalClubs = 150;
    public const int ClubsWithSquad = 64;
    public const int SquadSize = 25;
    public const int NameLength = 23;
    public const int PlayerNameLength = 19;
    public const int PlayerRecordLength = 20;
    public const int SquadRecordLength = NameLength + SquadSize * PlayerRecordLength; // 523

    public const int ColorOffset = 0x0000;
    public const int LogoOffset = 0x012C;
    public const int StrengthOffset = 0x016C;
    public const int SquadOffset = 0x04AE;
    public const int EuropeanNamesOffset = 0x876E;

    private ClubDatabase(IReadOnlyList<DatabaseClub> clubs) => Clubs = clubs;

    public IReadOnlyList<DatabaseClub> Clubs { get; }

    public DatabaseClub this[int index] => Clubs[index];

    public static PlayerRole RoleForSlot(int slot) => slot switch
    {
        < 2 => PlayerRole.Goalkeeper,
        < 12 => PlayerRole.Defender,
        _ => PlayerRole.Forward,
    };

    public static ClubDatabase Load(GameData game) => Parse(File.ReadAllBytes(game.GraphicsPath(23)));

    public static ClubDatabase Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < FileLength)
            throw new InvalidDataException($"Datenbank zu kurz ({data.Length} statt {FileLength} Bytes).");

        var clubs = new List<DatabaseClub>(TotalClubs);
        for (var i = 0; i < TotalClubs; i++)
        {
            var color = (short)((sbyte)data[ColorOffset + i * 2] * 256 + (sbyte)data[ColorOffset + i * 2 + 1]);
            var at = StrengthOffset + i * 3;
            var strengths = new ClubStrengths(data[at], data[at + 1], data[at + 2]);

            string name;
            var players = new List<DatabasePlayer>();
            var logo = -1;

            if (i < ClubsWithSquad)
            {
                var record = data.Slice(SquadOffset + i * SquadRecordLength, SquadRecordLength);
                name = GameText.Decode(record[..NameLength]);
                logo = (data[LogoOffset + i] + 0xB0) & 0x3F;
                for (var slot = 0; slot < SquadSize; slot++)
                {
                    var p = record.Slice(NameLength + slot * PlayerRecordLength, PlayerRecordLength);
                    if (p[0] == 0)
                        continue; // leerer Platz – das Spiel vergibt einen zufälligen Datenbanknamen
                    players.Add(new DatabasePlayer(
                        GameText.Decode(p[..PlayerNameLength]),
                        p[PlayerNameLength] != 0,
                        RoleForSlot(slot)));
                }
            }
            else
            {
                var offset = EuropeanNamesOffset + (i - ClubsWithSquad) * NameLength;
                name = GameText.Decode(data.Slice(offset, NameLength));
            }

            clubs.Add(new DatabaseClub(i, name, strengths.Defense, logo, color, players, strengths));
        }

        return new ClubDatabase(clubs);
    }
}
