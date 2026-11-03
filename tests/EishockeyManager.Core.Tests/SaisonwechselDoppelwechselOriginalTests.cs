using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Teil 5 (Gesamtreview #474, Bereich 2, D3): Doppelwechsel im Original, DOSBox-Lauf FILE16 → FILE16A → FILE17
/// (Referenz-Repo `original/spielstaende/`, Bilder `file16_…`, `file17_…`; docs/analyse/re-saisonwechsel.md, „Auswertung D3“).
/// SV BAYREUTH (Managerverein, 2. Liga, Konto am 29. April unter −666.666,67 DM) wird Erster der 2. Liga und Zweiter der
/// Aufstiegsrunde Bundesliga/2. Liga (`file16_6`: 1. Düsseldorfer EG, 2. SV Bayreuth, 3. EV Landshut, 4. ECD Sauerland).
/// Die Regeln des Remakes (<see cref="GameState.InsertForcedRelegation"/>, <see cref="GameState.RelegationExchange"/>,
/// <see cref="GameState.ResolveDoubleMoves"/>) werden auf diese Gruppen angewandt und mit den Ligen in FILE17 verglichen.
/// </summary>
public class SaisonwechselDoppelwechselOriginalTests
{
    private const int Liga = 12; // em.exe: Vereine 0–11 Bundesliga, 12–23 2. Liga, 24–35 Oberliga (Liga-Stufe = Index / 12)

    private static string[] ClubNames(string file)
    {
        var clubs = OriginalSaveGame.Read(File.ReadAllBytes(TransferOriginalTests.OriginalSave(file))).Block("far:9657");
        return Enumerable.Range(0, 3 * Liga)
            .Select(i => GameText.Decode(clubs.AsSpan(i * OriginalSaveGame.ClubRecordLength, 0x16)).Trim()).ToArray();
    }

    private static HashSet<string> LeagueOf(string[] names, int level) => names.Skip(level * Liga).Take(Liga).ToHashSet();

    /// <summary>Alle Reihenfolgen einer Gruppe (Plätze 1–4).</summary>
    private static IEnumerable<int[]> Permutations(int[] items) => items.Length <= 1
        ? [items]
        : items.SelectMany(x => Permutations(items.Where(y => y != x).ToArray()).Select(rest => (int[])[x, .. rest]));

    /// <summary>
    /// Remake-Austausch beider Gruppen nach dem Zwangsabstieg des Vereins <paramref name="forced"/> (2. Liga): neue Liga-Stufe je
    /// Vereinsindex aus FILE16A. Gruppe 2. Liga/Oberliga mit Einschub (Plätze <paramref name="lowerGroup"/>), dann Tausch
    /// Bundesliga/2. Liga (<paramref name="upperGroup"/>) und Auflösung des Doppelwechsels, wie <c>ApplyForcedRelegation</c>.
    /// </summary>
    private static int[] Predict(int[] upperGroup, int[] lowerGroup, int forced)
    {
        static int Level(int c) => c / Liga;
        var lower = lowerGroup.ToArray();
        GameState.InsertForcedRelegation(lower, 0, forced, c => Level(c) == 1);
        var record = new SeasonRecord { Season = 0, FinalTables = new Dictionary<League, IReadOnlyList<int>>() };
        var (up1, down1) = GameState.RelegationExchange(upperGroup, c => Level(c) == 0);
        var (up2, down2) = GameState.RelegationExchange(lower, c => Level(c) == 1);
        record.PromotedInto[League.Bundesliga].AddRange(up1);
        record.RelegatedInto[League.ZweiteLiga].AddRange(down1);
        record.PromotedInto[League.ZweiteLiga].AddRange(up2);
        record.RelegatedInto[League.Oberliga].AddRange(down2);
        GameState.ResolveDoubleMoves(record, upperGroup, c => Level(c) == 0);
        var moves = Enum.GetValues<League>().SelectMany(l => record.PromotedInto[l].Concat(record.RelegatedInto[l])).ToList();
        Assert.Equal(moves.Count, moves.Distinct().Count());
        return Enumerable.Range(0, 3 * Liga).Select(c => record.NextLeague(c, (League)Level(c)) is { } l ? (int)l : -1).ToArray();
    }

    /// <summary>
    /// FILE17: SV BAYREUTH in der Bundesliga, EV LANDSHUT (Bundesligist auf Platz 3, Paarpartner) zwei Ligen tiefer in der
    /// Oberliga, Ligen 12/12/12 – wie das Remake. Die Gruppe 2. Liga/Oberliga ist nicht fotografiert: Für jede Platzfolge geht
    /// Bayreuth hoch und Landshut in die Oberliga; mindestens eine Platzfolge ergibt genau die Bundesliga und 2. Liga aus FILE17.
    /// </summary>
    [GameDataFact]
    public void OriginalRecording_ForcedSecondLeagueClubPromoted_StaysUp_PartnerToOberliga()
    {
        var before = ClubNames("FILE16A.HOK");
        var after = ClubNames("FILE17.HOK");
        int Index(string name) => Array.IndexOf(before, name) is var i and >= 0 ? i : throw new Xunit.Sdk.XunitException(name);

        var bayreuth = Index("SV BAYREUTH");
        Assert.Equal(1, bayreuth / Liga);
        // file16_6: Gruppe Bundesliga/2. Liga nach Platz.
        int[] upper = [Index("DÜSSELDORFER EG"), bayreuth, Index("EV LANDSHUT"), Index("ECD SAUERLAND")];
        // file16_4: Plätze 11–12 der 2. Liga; Oberliga-Teilnehmer = die beiden Neuen der 2. Liga in FILE17 (file17_3).
        int[] lower = [Index("SC MEMMINGEN"), Index("EV LANDSBERG"), Index("ESC WOLFSBURG"), Index("SC RIESSERSEE")];

        Assert.Equal([12, 12, 12], Enumerable.Range(0, 3).Select(l => LeagueOf(after, l).Count).ToArray());
        Assert.Contains("SV BAYREUTH", LeagueOf(after, 0));
        Assert.Contains("EV LANDSHUT", LeagueOf(after, 2));
        Assert.Contains("SC MEMMINGEN", LeagueOf(after, 2));
        Assert.Contains("EV LANDSBERG", LeagueOf(after, 1));

        var matches = 0;
        foreach (var order in Permutations(lower))
        {
            var next = Predict(upper, order, bayreuth);
            Assert.Equal((int)League.Bundesliga, next[bayreuth]);
            Assert.Equal((int)League.Oberliga, next[Index("EV LANDSHUT")]);
            HashSet<string> Predicted(League l) => Enumerable.Range(0, 3 * Liga).Where(c => next[c] == (int)l).Select(c => before[c]).ToHashSet();
            if (Predicted(League.Bundesliga).SetEquals(LeagueOf(after, 0)) && Predicted(League.ZweiteLiga).SetEquals(LeagueOf(after, 1)))
                matches++;
        }
        Assert.True(matches > 0, "keine Platzfolge der Gruppe 2. Liga/Oberliga ergibt FILE17");
    }

    /// <summary>FILE16A → FILE17: Der Manager wechselt mit seinem Verein in die Bundesliga (Liga-Stufe 1 → 0), keine Entlassung.</summary>
    [GameDataFact]
    public void OriginalRecording_ManagerLeagueLevel_SecondLeagueToBundesliga()
    {
        var before = File.ReadAllBytes(TransferOriginalTests.OriginalSave("FILE16A.HOK"));
        var after = File.ReadAllBytes(TransferOriginalTests.OriginalSave("FILE17.HOK"));
        Assert.Equal("SV BAYREUTH", OriginalSaveGame.Read(before).Managers[0].Club);
        Assert.Equal("SV BAYREUTH", ClubNames("FILE17.HOK")[0]);
        Assert.DoesNotContain("SV BAYREUTH", ClubNames("FILE16A.HOK").Take(Liga));
        Assert.True(OriginalSaveGame.Read(after).ChecksumsValid);
    }
}
