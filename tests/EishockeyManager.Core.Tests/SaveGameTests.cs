using System.Text;
using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur „speichern“, Teil „format“: Abnahmen 3 (Roundtrip), 4 (Zwillingsspiel), 5 (Vollständigkeit) und
/// 6 (Kennung und Version). Der Laden/Sichern-Dialog folgt in Teil „dialog“.
/// </summary>
public class SaveGameTests
{
    // ---------- Abnahme 5: Vollständigkeit ----------

    /// <summary>Jedes Feld von GameState und aller erreichbaren Zustandsklassen ist speicherbar; ausgenommen ist nichts.</summary>
    [Fact]
    public void SchemaCoverage_EveryStateFieldIsSaved() => Assert.Empty(SaveGame.CheckSchemaCoverage());

    /// <summary>
    /// Schemawächter: Ändert sich ein Zustandsfeld (neu, umbenannt, anderer Typ), ändert sich dieser Abdruck. Dann
    /// <see cref="SaveGame.SchemaVersion"/> erhöhen, eine Migration eintragen und den Abdruck hier nachführen.
    /// </summary>
    [Fact]
    public void SchemaFingerprint_MatchesSchemaVersion()
    {
        Assert.Equal(23, SaveGame.SchemaVersion);
        Assert.Equal(ExpectedSchemaFingerprint, SaveGame.SchemaFingerprint());
    }

    // Schema 11: Börse (9 → 10) und Torhüterzähler (10 → 11), Integration Runde 7. Integration Runde 8: Schema 12 Taktik des
    // Nationalteams (Spur nationalteam #192), 13 Club.LeaguePenaltyMinutes und SeasonEvent BestGoalie/TopScorer/FairPlay
    // (Spur statistiken), 14 Teamwerte des Laders (Spur spielstaerke-manager). Runde 8, Welle 3: 15 Club.SeriesCurrent,
    // Manager.SeriesRecords und Manager.PowerPlayCounts (Spur statistiken-verein), 16 Platzierungen der Weltmeisterschaften
    // (Spur nationalteam-2), 17 Verhandlungs- und Gebotssperre (Spur transfer-original-vertrag). Runde 9: 18 Rekorde,
    // Kopf-an-Kopf-Liste und Bilanz der Länder (Spur wettbewerbe-komplett, Gegner-Fenster).
    // 19 Spielstand-Optionen GameSetup.Options (Spur optionen-wirkung).
    // 20 Versicherungsprämie und Abschluss je Spieler (Spur transfer-vertraege-komplett, Teil 2), 21 unaufgefordertes Angebot
    // je Spieler (Teil 3), 22 Spielerpool des Transfermarkts (Teil 5).
    // 23 Kaufpreis und Verletzungsart je Spieler (Spur kader-transfer, Teil 2; vorläufig, Leitstelle nummeriert).
    private const string ExpectedSchemaFingerprint = "8418CCB1E2023372";

    // ---------- Abnahme 3: Roundtrip ----------

    [Fact]
    public void NewGame_RoundTripIsByteIdentical_AndSavingChangesNothing()
    {
        var game = TestGames.Create();
        var rng = game.CaptureRng();
        var first = SaveGame.Save(game);
        Assert.Equal(first, SaveGame.Save(game));
        Assert.Equal(rng, game.CaptureRng());
        AssertRoundTrip(game);
    }

    /// <summary>
    /// Sichern → laden → sichern ergibt an jedem Tag zweier Saisons dieselben Bytes – darunter unmittelbar vor und
    /// nach einer Pokalrunde, am letzten Haltepunkt vor der WM, unmittelbar vor und nach dem Saisonwechsel und mit offener Frist.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryDayOfTwoSeasons_RoundTripIsByteIdentical(bool active)
    {
        var game = TestGames.Create(11);
        var seen = new HashSet<string>();
        var cupMatches = 0;
        RngTwinProbeTests.Run(game, active, Check, changes: 2, beforeChange: g =>
        {
            Assert.Equal(SeasonPhase.SeasonOver, g.Phase);
            seen.Add("vor dem Saisonwechsel");
            AssertRoundTrip(g);
        });
        string[] required = active
            ? ["Pokalrunde", "vor der WM", "nach dem Saisonwechsel", "offene Frist", "vor dem Saisonwechsel"]
            : ["Pokalrunde", "nach dem Saisonwechsel", "offene Frist", "vor dem Saisonwechsel"];
        Assert.Equal(required, required.Where(seen.Contains));

        void Check(GameState g)
        {
            // Aktiv: Der Manager ist Bundestrainer, damit „und tschüß…“ an den WM-Tagen anhält.
            if (active && g.NationalCoach is null && g.Phase == SeasonPhase.Running)
            {
                g.AppointNationalCoach(g.Managers[0]);
                g.AutoNominate();
            }
            var matches = g.DebCup?.Matches.Count() ?? 0;
            if (matches > cupMatches)
                seen.Add("Pokalrunde"); // der vorige Tag war unmittelbar vor, dieser unmittelbar nach der Runde
            cupMatches = matches;
            // „und tschüß…“ spielt die WM in einem Schritt durch; letzter Haltepunkt davor ist das Ende der
            // Vereinssaison mit ausgeloster WM und (aktiv) nominiertem Kader des Bundestrainers.
            if (g.Phase == SeasonPhase.ClubSeasonOver && g.WorldCup is { Winner: null }
                && (!active || g.NationalCoach?.Squad.Count == NationalCoach.MaxSquad))
                seen.Add("vor der WM");
            if (g.CurrentDay == 0 && g.Phase == SeasonPhase.Running && g.Season > GameState.FirstSeason)
            {
                seen.Add("nach dem Saisonwechsel");
                cupMatches = 0;
            }
            if (g.Managers.Any(m => g.Inbox(m).Any(x => x.IsQuestion))
                || g.Clubs.SelectMany(c => c.Players).Any(p => p.Notice == ContractNotice.Offered))
                seen.Add("offene Frist");
            AssertRoundTrip(g);
        }
    }

    internal static GameState AssertRoundTrip(GameState game)
    {
        var first = SaveGame.Save(game);
        var loaded = SaveGame.Load(first);
        Assert.True(loaded.Success, loaded.Error);
        var second = SaveGame.Save(loaded.Game!);
        if (!first.AsSpan().SequenceEqual(second))
            Assert.Fail(FirstDifference(first, second));
        Assert.Equal(RngTwinProbeTests.Snapshot(game), RngTwinProbeTests.Snapshot(loaded.Game!));
        return loaded.Game!;
    }

    private static string FirstDifference(byte[] a, byte[] b)
    {
        var i = 0;
        while (i < a.Length && i < b.Length && a[i] == b[i])
            i++;
        var from = Math.Max(0, i - 200);
        return $"Unterschied ab Byte {i}:\n{Encoding.UTF8.GetString(a, from, Math.Min(a.Length - from, 400))}\n---\n"
               + Encoding.UTF8.GetString(b, from, Math.Min(b.Length - from, 400));
    }

    // ---------- Abnahme 4: Zwillingsspiel ----------

    public static TheoryData<bool, int, int> Twins => new()
    {
        { false, 3, 5 }, { true, 3, 5 }, { false, 11, 40 }, { true, 11, 40 }, { true, 3, 0 },
    };

    /// <summary>
    /// Gesichert + geladen + 4 Saisonwechsel über <see cref="GameState.Continue"/> = ununterbrochen gespielt:
    /// Abdruck (Konten, Finanzbuch, Tabellen, Kader, Titel, Statistik, Zufall) und Spielstanddatei identisch.
    /// </summary>
    [Theory]
    [MemberData(nameof(Twins))]
    public void TwinGame_LoadedPlaysExactlyLikeUninterrupted(bool active, int seed, int steps) =>
        AssertTwin(TestGames.Create(seed), active, steps);

    /// <summary>
    /// Zwilling am letzten Haltepunkt vor der WM (Ende der Vereinssaison), der Manager ist Bundestrainer mit Kader.
    /// Die WM selbst läuft in einem einzigen <see cref="GameState.Continue"/>, mittendrin gibt es keinen Haltepunkt.
    /// </summary>
    [Fact]
    public void TwinGame_SavedBeforeWorldCup_AsNationalCoach()
    {
        var game = TestGames.Create(3);
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var steps = 0;
        for (; game.Phase != SeasonPhase.ClubSeasonOver; steps++)
        {
            Assert.True(steps < 500, "WM nicht erreicht");
            game.Continue();
        }
        AssertTwin(game, active: true, steps: 0);
    }

    [GameDataTheory]
    [InlineData(false, 25)]
    [InlineData(true, 25)]
    public void TwinGame_OriginalDatabase(bool active, int steps)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = 42 });
        AssertTwin(NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 3), active, steps);
    }

    private static void AssertTwin(GameState game, bool active, int steps)
    {
        for (var i = 0; i < steps; i++)
            game.Continue();
        var twin = AssertRoundTrip(game);

        var played = RngTwinProbeTests.Run(game, active);
        var loaded = RngTwinProbeTests.Run(twin, active);
        Assert.Equal(played, loaded);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(twin));
    }

    // ---------- Abnahme 6: Kennung und Version ----------

    [Theory]
    [InlineData("")]
    [InlineData("Hallo")]
    [InlineData("{}")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"kennung\":\"ANDERES SPIEL\",\"schema\":1}")]
    [InlineData("{\"kennung\":42,\"schema\":1}")]
    public void ForeignFile_IsRejectedWithMessage(string text)
    {
        var result = SaveGame.Load(Encoding.UTF8.GetBytes(text));
        Assert.False(result.Success);
        Assert.Null(result.Game);
        Assert.Equal("Die Datei ist kein Spielstand dieses Spiels.", result.Error);
    }

    [Fact]
    public void BinaryFile_IsRejectedWithMessage()
    {
        var result = SaveGame.Load([0x48, 0x4F, 0x4B, 0x00, 0xFF, 0x13, 0x37]);
        Assert.False(result.Success);
        Assert.Equal("Die Datei ist kein Spielstand dieses Spiels.", result.Error);
    }

    [Fact]
    public void NewerSchema_IsRejectedBeforeReading()
    {
        // Höhere Version mit sonst unlesbarem Inhalt: Die Version wird vor allem anderen geprüft.
        var text = $"{{\"kennung\":\"{SaveGame.Identifier}\",\"schema\":{SaveGame.SchemaVersion + 1},\"spiel\":\"neu\"}}";
        var result = SaveGame.Load(Encoding.UTF8.GetBytes(text));
        Assert.False(result.Success);
        Assert.Contains("neueren Version", result.Error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("\"1\"")]
    public void InvalidSchema_IsRejected(string schema)
    {
        var result = SaveGame.Load(Encoding.UTF8.GetBytes($"{{\"kennung\":\"{SaveGame.Identifier}\",\"schema\":{schema}}}"));
        Assert.False(result.Success);
        Assert.Equal("Der Spielstand hat keine gültige Version.", result.Error);
    }

    /// <summary>Beschädigte Dateien: Meldung statt Ausnahme oder Teilzustand; der laufende Spielstand bleibt unverändert.</summary>
    [Theory]
    [InlineData("truncate")]
    [InlineData("\"Age\": ", "\"Alter\": ")]
    [InlineData("\"Age\": 1", "\"Age\": \"x\"")]
    [InlineData("\"Goals\": 0,\n", "")]
    [InlineData("\"$spieler\": 1", "\"$spieler\": 99999")]
    [InlineData("\"$verein\": 2", "\"$verein\": 999")]
    [InlineData("\"Role\": \"Goalkeeper\"", "\"Role\": \"Schiedsrichter\"")]
    [InlineData("\"$ref\": ", "\"$ref\": 1")]
    public void DamagedFile_IsRejected_RunningGameUnchanged(string find, string? replace = null)
    {
        var running = TestGames.Create();
        TestSeason.PlayMatchDay(running);
        var before = SaveGame.Save(running);
        var text = Encoding.UTF8.GetString(before);
        text = find == "truncate" ? text[..(text.Length / 2)] : ReplaceFirst(text, find, replace!);

        var result = SaveGame.Load(Encoding.UTF8.GetBytes(text));

        Assert.False(result.Success, "beschädigte Datei wurde geladen");
        Assert.NotNull(result.Error);
        Assert.Equal(before, SaveGame.Save(running));
    }

    /// <summary>„$type“ darf nur Zustandsklassen aus Core und Formats nennen, nie fremde Typen.</summary>
    [Fact]
    public void ForeignTypeName_IsRejected()
    {
        var game = TestGames.Create();
        game.Continue();
        var text = Encoding.UTF8.GetString(SaveGame.Save(game));
        Assert.Contains("\"$type\": \"", text);
        var index = text.IndexOf("\"$type\": \"", StringComparison.Ordinal) + 10;
        var end = text.IndexOf('"', index);
        text = text[..index] + "System.IO.FileInfo" + text[end..];
        var result = SaveGame.Load(Encoding.UTF8.GetBytes(text));
        Assert.False(result.Success);
        Assert.Contains("Unbekannter Typ", result.Error);
    }

    /// <summary>
    /// #79 (S71-01): Ein Verein, der in der Datei fehlt (<c>null</c>), wird mit Meldung abgelehnt – vorher lief das
    /// Laden in eine <see cref="NullReferenceException"/> in <c>ValidatePlayerIdentity()</c>.
    /// </summary>
    [Fact]
    public void NullClub_IsRejectedWithMessage()
    {
        var root = JsonNode.Parse(SaveGame.Save(TestGames.Create()))!;
        root["spiel"]!["Clubs"]!["items"]![0] = null;

        var result = SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString()));

        Assert.False(result.Success);
        Assert.Contains("darf nicht leer sein", result.Error);
    }

    /// <summary>
    /// #79: <c>null</c> an beliebiger Stelle eines Objekts oder einer Sammlung führt nie zu einer Ausnahme, sondern
    /// zu einer Meldung (oder ist an dieser Stelle zulässig und wird geladen). Feste Stichprobe über die ganze Datei.
    /// </summary>
    [Fact]
    public void NullAnywhere_NeverThrows()
    {
        var game = TestGames.Create();
        TestSeason.PlayMatchDay(game);
        var text = Encoding.UTF8.GetString(SaveGame.Save(game));
        var count = Positions(JsonNode.Parse(text)!).Count;
        Assert.True(count > 1000, $"nur {count} Stellen");

        var random = new Random(79);
        var rejected = 0;
        foreach (var index in Enumerable.Range(0, 150).Select(_ => random.Next(count)).Distinct())
        {
            var root = JsonNode.Parse(text)!;
            var (parent, key) = Positions(root)[index];
            if (parent is JsonObject obj)
                obj[(string)key] = null;
            else
                parent.AsArray()[(int)key] = null;

            SaveLoadResult result;
            try
            {
                result = SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString()));
            }
            catch (Exception e)
            {
                Assert.Fail($"Stelle {parent.GetPath()}/{key}: {e.GetType().Name}: {e.Message}");
                return;
            }
            if (!result.Success)
                rejected++;
            else
                SaveGame.Save(result.Game!); // was geladen wurde, muss auch wieder speicherbar sein
        }
        Assert.True(rejected > 0);

        // Alle Objekte und Sammlungen unterhalb der vier Abschnitte, als (Elternknoten, Schlüssel).
        static List<(JsonNode Parent, object Key)> Positions(JsonNode root)
        {
            var result = new List<(JsonNode, object)>();
            Walk(root);
            return result;

            void Walk(JsonNode node)
            {
                if (node is JsonObject obj)
                    foreach (var (name, child) in obj)
                    {
                        if (child is JsonObject or JsonArray)
                        {
                            if (node != root)
                                result.Add((node, name));
                            Walk(child);
                        }
                    }
                else if (node is JsonArray array)
                    for (var i = 0; i < array.Count; i++)
                        if (array[i] is JsonObject or JsonArray)
                        {
                            result.Add((node, i));
                            Walk(array[i]!);
                        }
            }
        }
    }

    // ---------- #81 (S71-03): Vollständigkeit über Writer und Reader ----------

    /// <summary>
    /// Für <b>jeden</b> erreichbaren Zustandstyp – auch solche, die im Beispielspielstand nie vorkommen – schreibt der
    /// Writer genau die Felder des Schemas, und der Reader liest diese Probe ohne Meldung zurück. Vorher prüfte der
    /// Vollständigkeitstest nur, ob die Feldtypen unterstützt werden; ein ausgelassenes Feld fiel nur im Roundtrip
    /// und nur bei tatsächlich belegten Typen auf.
    /// </summary>
    [Fact]
    public void CoverageProbe_WriterAndReaderCoverEveryFieldOfEveryType()
    {
        var probe = SaveGame.CoverageProbe();

        // Jedes Feld des Schemas (Zeilen „Typ.Feld: Feldtyp“; nicht „Schnittstelle : Klasse“) wird genau einmal geschrieben.
        var schemaFields = SaveGame.SchemaDescription()
            .Where(line => line.Contains(": ", StringComparison.Ordinal) && !line.Contains(" : ", StringComparison.Ordinal))
            .Select(line => line[..line.IndexOf(": ", StringComparison.Ordinal)])
            .ToHashSet(StringComparer.Ordinal);
        var written = probe.SelectMany(p => p.Value.WrittenFields.Select(f => $"{p.Key}.{f}")).ToList();
        Assert.Equal(written.Count, written.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(schemaFields.Order(StringComparer.Ordinal), written.Order(StringComparer.Ordinal));

        foreach (var (type, result) in probe)
            Assert.True(result.ReaderError is null, $"{type}: {result.ReaderError}");
    }

    [Fact]
    public void Save_RefusesBrokenPlayerIdentity()
    {
        var game = TestGames.Create();
        var own = game.Clubs[game.Managers[0].ClubIndex];
        own.Players.Add(PlayerFactory.CreateTalent("OHNE ID", PlayerRole.Forward, 50, new Random(1)));
        Assert.Throws<InvalidOperationException>(() => SaveGame.Save(game));
    }

    private static string ReplaceFirst(string text, string find, string replace)
    {
        var index = text.IndexOf(find, StringComparison.Ordinal);
        Assert.True(index >= 0, $"„{find}“ nicht in der Datei");
        return text[..index] + replace + text[(index + find.Length)..];
    }
}
