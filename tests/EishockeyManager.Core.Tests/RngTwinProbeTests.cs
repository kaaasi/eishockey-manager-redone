using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur „speichern“, Teil „rng“ – Mehrsaisonprobe: Die Umstellung der Zufallsquellen auf <see cref="GameRandom"/>
/// darf keinen Spielverlauf verändern. Die Abdrücke wurden auf <c>main</c> <c>5f8e02a</c> (noch mit
/// <see cref="System.Random"/>) ermittelt und müssen nach der Umstellung unverändert sein.
/// Spur finanzen-2 (#67) ändert die Spielregeln (Monatsabschluss, Zuschauer, Werbung, Startkonto) und damit den
/// Verlauf; die Abdrücke sind dort mit dem unveränderten Generator neu ermittelt. Sie bleiben Referenz dafür, dass
/// spätere Generator- oder Formatänderungen den Verlauf nicht verschieben.
/// Spur bundestrainer-modus4 (#123) ändert die Spielregeln im Modus 4: Der Verein des Probe-Managers steht unter den
/// ersten fünf der 1. Liga, er wird am 13. April Bundestrainer; WM-Ergebnisse und Pokal-Zufallsquelle verschieben sich.
/// Die synthetischen Abdrücke sind dort neu ermittelt (Gegenprobe ohne Ernennung: alte Werte unverändert).
/// Spur finanzen-nachpruefung (Runde 5) ändert Zuschauerformel (em.exe 0x3F63C) und Saisonprämien (0x55CD3, 0x2723C);
/// die Abdrücke sind mit dem unveränderten Generator neu ermittelt. Ursache je Fall (FN-01, #139, Tabelle in
/// docs/finanzen-rekonstruktion.md): synthetisch alle vier Europapokalsieg 1993 ohne Prämie (−100.000 DM); Originaldaten
/// aktiv ein Zug weniger aus FinanceRandom bei der Zuschauerteilung.
/// Spur finanzen-werbung (Runde 6, #132–#134) ändert Werbebewertung, Werbeangebote und den Tag des Monatsabschlusses;
/// alle sechs Abdrücke sind mit unverändertem Generator neu ermittelt. In allen sechs Fällen bleiben Haupt-, Pokal- und
/// Vertragszufallsquelle in jedem Schritt gleich (keine Spielergebnisse, Auslosungen oder Vertragsentscheidungen
/// verschoben); es ändern sich nur FinanceRandom (ab Tag 1: Bewertung nach jedem Tagesabschluss, 0x5569F) und daraus das
/// Konto (Tabelle in docs/finanzen-rekonstruktion.md).
/// Spur heimspiel-statistik (#136, „Weniger als neun Spieler“, em.exe 0x54420) ändert die drei passiven Abdrücke; die
/// aktiven verlängern Verträge und bleiben gleich. Ursache je Fall (Vergleichslauf mit Zustand und allen vier
/// Zufallsquellen nach jedem Schritt, Tabelle in docs/finanzen-rekonstruktion.md): Die Startverträge des passiven Managers
/// sind nach 1994 ausgelaufen, sein Verein hat 1995 keinen Spieler. Ab dem ersten Spiel 1995 zahlt er je Spiel
/// 200.000 DM (Strafen) statt Eintritt zu bekommen; die Zuschauerberechnung und damit ihr Zug aus FinanceRandom entfällt.
/// Haupt-, Pokal- und Vertragsquelle, Titel und Kader bleiben in jedem Schritt gleich; die Kontountergrenze der Probe
/// (10 Mio. DM) verhindert die Entlassung.
/// Mit der Spielwertung (#165, em.exe 0x36DC5) werden die Spiele dieses Vereins ab 1995 ohne Berechnung 0:2 bzw. 2:0
/// gewertet (Vergleich gegen 7833e2d): erste Differenz 1995, Tag 3 – das Ergebnis des ersten Spiels und der Stand der
/// Hauptquelle (Torzüge entfallen); danach Pokalquelle (Tag 14 bzw. 17, anderer Pokalverlauf), FinanceRandom und
/// Vertragsquelle (Saisonwechsel). Titel ab 1995 andere. Die drei aktiven Abdrücke bleiben gleich (ihr Kader bleibt
/// spielfähig, in keinem Schritt eine Differenz).
/// Spur kader-start (#168, em.exe 0x29DA4): Die Kaderspieler der Manager-Vereine bekommen Kondition/Technik aus der
/// Tabelle DGROUP 0x4B42 (Liga, Start-Stärke) statt aus der Vereinsstärke; alle sechs Abdrücke neu ermittelt. Ursache je
/// Fall (Vergleichslauf gegen 15d0733 mit Zustand und allen vier Zufallsquellen nach jedem Schritt, Tabelle in
/// docs/reverse-engineering.md, Abschnitt „Startwerte der Kaderspieler“): Nach dem Spielstart unterscheiden sich nur die
/// Spieler des Manager-Vereins (Werte, Gehalt); Computervereine, Pool und alle vier Zufallsquellen sind gleich. Erste
/// Folge ist in allen Fällen das Konto am Monatsende (Tag 21: Gehälter und Grundeinnahmen aus den Teamwerten);
/// danach weicht die Spielstärke um einen Punkt ab (Energie wirkt auf größere Spielerwerte) – synthetisch (Verein 2,
/// Basis 81 statt 65) Pokalquelle ab Tag 45 bzw. 56, Hauptquelle ab Tag 153 bzw. 163 der ersten Saison; Originaldaten
/// (Verein 42, Basis 31 wie bisher im Mittel) FinanceRandom 1994 Tag 28, Kader und Pool ab dem Saisonwechsel 1995.
/// Spur boerse-start (#169) rechnet die Kurse nach em.exe (0x5784A, 90 Börsentage vor Spielbeginn) und zieht dafür anders
/// aus FinanceRandom. Alle sechs Abdrücke neu; Vergleichslauf gegen 15d0733 mit Zustand und allen vier Zufallsquellen
/// nach jedem Schritt: Haupt-, Pokal- und Vertragsquelle, Ergebnisse und Kader in jedem Schritt gleich. Es ändern sich
/// FinanceRandom ab dem ersten Schritt (1993, Tag 3) und daraus die Kontostände (Zuschauer, Werbung): synthetisch Seed 3
/// ab 1994 Tag 12, Seed 11 ab 1994 Tag 7 (vorher verdeckt die Kontountergrenze der Probe die Differenz), Originaldaten ab
/// 1993 Tag 3. Titel unverändert.
/// Danach (Entscheidung Nutzer, #188) rechnet die Börse erst nach dem Tagesabschluss aller Manager: Vergleich gegen
/// 801c732 wie oben – wieder nur FinanceRandom (ab 1993 Tag 3) und Konten (synthetisch ab 1994 Tag 12 bzw. 7,
/// Originaldaten ab 1993 Tag 10) verschoben.
/// Spur spielberechnung (Teil 1, #186): alle sechs Abdrücke neu, weil der Abdruck alle einfachen Eigenschaften enthält
/// und <c>Player.GoalsAgainst</c>/<c>GoalieMinutes</c> neu sind (noch nicht gezählt, immer 0). Gegenprobe: mit beiden
/// Feldern in <c>Excluded</c> stimmen alle sechs alten Abdrücke unverändert; kein Spielverlauf ändert sich.
/// Spur spielberechnung (Teil 3, #186): alle sechs Abdrücke neu. Liga, Play-offs, Aufstiegsrunde, DEB-Pokal, Europapokal
/// und WM rechnen mit der Berechnung des Originals (<c>MatchEngine.PlayWithEvents</c>) statt mit dem Poisson-Modell: andere
/// Ergebnisse ab dem ersten Spieltag, anderer Verbrauch der Haupt- und Pokalquelle (Minutenzüge, Spielerzähler), damit
/// auch Tabellen, Titel, Finanzen, Transfers und Verträge; dazu die Spielerzähler im Abdruck (je Saison, nach dem Wechsel
/// 0). Keine Gegenprobe möglich, die den alten Verlauf erhält – die Spielberechnung ist der Zweck der Spur. Ermittelt auf
/// dem Stand des Branches (Basis <c>15d0733</c>); die Leitstelle zieht die Werte in der Integration nach.
/// Integration Runde 7: Sollwerte auf dem integrierten Stand neu ermittelt (kader-start, boerse-start, weltkugel-dosbox,
/// spielberechnung, Freundschaftsspiele über die neue Spielberechnung). Die Änderungen der einzelnen Spuren sind oben
/// begründet; der Integrationswert ist ihr Zusammenwirken (siehe docs/arbeitspakete.md, „Integration Runde 7“).
/// Review-Befunde R7-02 (#200, Angriffszuschlag lrandom(Angriff, 7)) und R7-03 (#201, Zähler des Nationalteams): neu
/// ermittelt; R7-02 ändert das Torbudget jedes Spiels und damit alle Ergebnisse und die Hauptquelle.
/// Spur spielerwert (Runde 8): Die Werbebewertung rechnet mit dem Spielerwert nach 0x49D36 statt „Kaufpreis · 3“; das ändert
/// die Bewertung und damit Werbeeinnahmen und Kontostände – drei synthetische Abdrücke neu (passiv Seed 3 und beide
/// Originaldaten-Abdrücke unverändert). Gegenprobe: mit „Kaufpreis · 3“ nur in der Werbebewertung stimmen alle alten Abdrücke.
/// Spur statistiken (Runde 8, #135): fünf Abdrücke neu, weil die Einzelprämien (bester Torhüter, Topscorer, Fair-Play je
/// 100.000 DM, em.exe 0x27184) die Kontostände ändern; der aktive Lauf mit Originaldaten bekommt keine und bleibt gleich.
/// Gegenprobe auf dem Branch: mit abgeschalteten Einzelprämien (und <c>LeaguePenaltyMinutes</c> in <c>Excluded</c>) alle
/// sechs alten Abdrücke unverändert – kein Spielverlauf und kein Zufallsverbrauch ändert sich. Basis <c>10eb6c5</c>.
/// Spur spielstaerke-manager (#204): alle sechs Abdrücke neu. Beim ersten Spiel zieht der Lader der Teamwerte (em.exe
/// 0x2EF1B) für alle 150 Vereine 11 Zahlen aus der Hauptquelle, die Computervereine spielen mit gestreuten Teamwerten,
/// eigener Form und Tagesform (0x4F527 nach jedem Ligaspiel, weitere Züge): andere Ergebnisse ab dem ersten Spieltag,
/// damit Tabellen, Titel, Finanzen und alle Zufallsquellen; dazu das neue Feld <c>Club.TeamValues</c> im Abdruck. Keine
/// Gegenprobe, die den alten Verlauf erhält (die Streuung ist der Zweck). Ermittelt auf dem Branch (Basis <c>10eb6c5</c>).
/// Integration Runde 8 (Leitstelle): alle sechs neu ermittelt auf dem Stand mit spielerwert, befunde-p3, tabellen-original,
/// nationalteam, statistiken und spielstaerke-manager. Die Spielverläufe folgen dem Lader (spielstaerke-manager); hinzu
/// kommen Werbebewertung, Einzelprämien und neue Felder (<c>LeaguePenaltyMinutes</c>, Taktik des Nationalteams) im Abdruck.
/// Spur spielstaerke-manager-2 (Basis <c>99fa995</c>): alle sechs Abdrücke neu. In Managerpaarungen rechnet der Managerverein
/// mit den Werten seiner Spieler (0x98F8 statt Teamwert aus der Datenbankstärke), beide Vereine verbrauchen je Minute
/// Energie (0x87F7), wechseln Reihen (0x8EF2/0x80CB), tauschen die Seiten (random(0, 1)) und folgen der Taktik in geraden
/// Minuten (0x939A, für beide Vereine); Strafen ziehen den Spieler 0x378C5 und zählen den Einsatz mit dem Managerwert
/// 0x974B. Andere Ergebnisse und Zufallszüge ab dem ersten Spiel des Managers, damit Tabellen, Titel, Finanzen und alle
/// Quellen. Paarungen ohne Manager sind unverändert (Gegenprobe: <c>SpielstaerkeManagerTests</c> und
/// <c>SpielberechnungMinutenTests</c> unverändert grün). Keine Gegenprobe, die den alten Verlauf erhält.
/// Integration Runde 8, Welle 3 (Leitstelle, R8W3I-01 #269): alle sechs neu – 0x80CB/0x939A lesen den Spielstand vor den
/// Toren der Minute; das ändert Ergebnisse jeder Managerpaarung und damit alle Verläufe.
/// Spur transfer-vertraege-komplett (Teil 1): die drei aktiven Abdrücke neu – Spieler, deren Vertrag am Saisonende ohne
/// Verhandlung endet, bringen die Ablöse Wert(Preis) / 2 (0x27DF3) aufs Konto; Gegenprobe ohne Ablöse: alte Werte, passive
/// Abdrücke unverändert.
/// Spur transfer-vertraege-komplett (Rest von Teil 2): die drei aktiven Abdrücke neu – Startgehalt der Managerkader und
/// Gehalt beim Kauf = Wert(Gehalt) (0x29EE4, 0x42F11), zieht keinen Zufall; die Verlängerungen (Forderung mindestens bisheriges
/// Gehalt) und die Konten ändern sich; vertragslose Spieler (<c>SignFreeAgents</c>) kommen mit 1 Jahr statt 2 (0x42CB1, +0x0B = 1).
/// Gegenprobe mit altem Startgehalt und alter Laufzeit: alte Werte, passive Abdrücke unverändert.
/// Spur transfer-vertraege-komplett (Teil 5): alle sechs Abdrücke neu – der Markt kommt aus dem Spielerpool (Listenwechsel
/// 0x494EF mit random(0, Managerzahl + 2) je Ligaspieltag statt 30 %/50 % aus Computerkadern; Pool beim Spielstart und
/// Saisonwechsel), damit anderer Verbrauch der Hauptquelle und andere Ergebnisse; Markt-Angebote schreiben Angebot − Angebot / 7
/// · Restlaufzeit gut (0x49332); auf der Liste stehen nur noch Vertragslose aus Managervereinen (<c>SignFreeAgents</c> holt
/// also nur eigene zurück). Die Datenbankspieler des Pools fehlen den Computerkadern; beim Saisonwechsel tauschen Pool und
/// Computerkader (jeder Name nur einmal, Entscheidung Nutzer 01.11.2026).
/// Spur spielberechnung-komplett, Teil 1 (#204): alle sechs neu – Wanderung der Teamwerte 0x9D1B beim ersten Spiel
/// (Spielstart, großer Schritt), zweimal je Woche (kleiner Schritt) und nach jedem Saisonwechsel (großer Schritt); andere
/// Teamwerte der Computervereine, damit andere Ergebnisse ab dem ersten Spieltag und mehr Züge aus der Hauptquelle. Keine
/// Gegenprobe, die den alten Verlauf erhält (die Wanderung ist der Zweck).
/// Spur spielberechnung-komplett, Teil 2 (R8W3I-03 bis -06): drei Abdrücke neu. Passiv Seed 3: das Nationalteam des
/// Bundestrainers zieht den Strafspieler 0x378C5 (Gegenprobe ohne diesen Zug: alter Abdruck). Aktiv Seed 3 und 11:
/// 0x378C5 zieht höchstens 150-mal, Strafen ohne Spieler erzeugen kein Ereignis, Managerwert 100 unter neun Spielern.
/// Spur spielberechnung-komplett, Teil 3 (0x38B8F, 0x386D1): alle sechs neu – je Minute der Managerpaarung ein Verletzungszug, je Abschluss die Schützenwahl
/// statt der Torverteilung 0x5028 nach dem Spiel für Managervereine. Keine Gegenprobe (geänderter Zufallsverbrauch ist der Zweck).
/// Spur spielberechnung-komplett, Teil 5 (0x3901B): alle sechs neu – K.-o.-Spiele des Minutenwegs, die nach der Verlängerung unentschieden
/// sind, entscheidet das Penaltyschießen des Originals (random(0, 1), dann random(2, 5) je Seite bis ungleich) statt eines
/// NextDouble-Zugs in KnockoutMatch.Play; danach andere Zufallsfolge. Teil 4 (Zähler) zieht keinen Zufall.
/// Integration Runde 9 (Leitstelle): Werte auf dem integrierten Stand aller fünf Spuren neu ermittelt; die Ursachen oben
/// wirken zusammen, dazu der Minutenweg der Pokalspiele ohne Manager bei eingeschaltetem Anzeigeschalter (DEB-Pokal an,
/// 0x3702E).
/// Fix #351 (Leitstelle): neu – Play-offs und Aufstiegsrunde ohne Manager über den schnellen Weg 0x3731F (Meldungsmatrix voreingestellt aus, 0x37058), anderer Zufallsverbrauch und andere Meister.
/// Spur geld, Teil 2 (Runde 11): neu – Werbebewertung mit dem Ø-Platz der letzten Spieltage (0x3F915), Angebote auch bei
/// laufendem Vertrag (0x3FB47, mehr Züge aus FinanceRandom) und Monatseinnahme aus den Vereinsfeldern (0x3E657); jede der
/// drei Regeln allein ändert schon einen Teil der Abdrücke (Gegenprobe je Regel im PR).
/// Spur geld, Teil 3 (Runde 11): neu – Jugendteams je Ligaspieltag nach 0x39DEC (Züge aus FinanceRandom an jedem Ligaspieltag
/// statt monatlich je Team ein NextDouble; Stärke, Startliga aus der Spielvorbereitung #327, Saisonwechsel 0x3A13B ohne Zufall).
/// Spur geld, Teil 6 (Runde 11): neu – Preis der Talentsuche an Monatsmitte und Monatsletztem (0x3EEAB: random(0, 1), bei ≠ 0
/// random(0, 10) aus FinanceRandom). Gegenprobe ohne diesen Zug: alle Abdrücke wie vorher; die Talentsuche selbst läuft in
/// den Proben nicht.
/// Spur saisonwechsel, Teil 1 (S-K1/S-K2, Datei 0x56238–0x5636D): alle neu – Oberliga-Nachrücker per random(36, 41) über die Plätze
/// ohne Liga (Wiederholung bei gleichem Zug) statt Zug aus allen Vereinen ohne Liga, anderer Zufallsverbrauch und andere Nachrücker;
/// ein Managerverein unter den letzten zwei bleibt in der Oberliga (Konto 400.000 DM) statt des nächstschlechteren Vereins.
/// Integration Runde 11 (geld + saisonwechsel): Abdrücke auf dem integrierten Stand neu ermittelt (beide Spuren ändern den
/// Zufallsverbrauch; kein weiterer Unterschied).
/// Spur kader-transfer, Teil 1 (A170, 0x29A3F): neu – der Managerverein in der Bundesliga hat 25 statt 20 Spieler (Kader nach Start-Liga), andere Kadererzeugung, Gehälter und Spielstärke. Keine Gegenprobe (geänderte Kadergröße ist der Zweck).
/// Spur kader-transfer, Teil 3 (0x058DC, 0x063BE): neu nur die aktiven Fälle – Verkäufe stärken den Käufer, Käufe vom Computermarkt würfeln den Sponsorzuschuss (anderer Zufallsverbrauch); die passiven Fälle bleiben.
/// Spur kader-transfer, Teil 4 (0x54E71, 0x072DD, 0x059B8): alle neu – der Markt würfelt Listenwechsel und Angebote täglich statt je Ligaspieltag, Bieter nach 0x059B8 (anderer Verbrauch der Hauptquelle ab dem ersten Tag).
/// Spur kader-transfer, Teil 4, Rest (0x4907A, 0x4981E, F113/A209): alle neu – neue Kaderplatz-Felder Tendenzwert und Steuerbyte
/// (<c>TrendScore</c>, <c>TrendControl</c>) stehen im Abdruck; die Tendenz der Markt-Plätze geht in Spielerwert und Preise ein,
/// Nationalspieler auf dem Markt bekommen Grundstärken aus der Ländertabelle. Die Hauptquelle zieht nicht mehr als vorher (Tendenz
/// aus <c>TrendRandom</c>). Gegenprobe mit beiden Feldern ausgenommen: die vier synthetischen Abdrücke unverändert, die zwei
/// mit Originaldatenbank auch ohne den Zweig A209 neu (Wirkung der Tendenz über den Spielerwert).
/// Spur kader-transfer, Teil 5 (A226, Datei 0x27A0C–0x27AA8): alle neu – neues Feld <c>ClubGamesCarry</c> im Abdruck; die Spiele
/// beim jetzigen Verein (+0x1C) laufen über den Saisonwechsel weiter und gehen ab der zweiten Saison in Spielerwert, Gehälter und
/// Preise ein. Die Hauptquelle zieht nicht mehr als vorher (Modus-4-Probe: Titel und Zufallsstände gleich, nur Konten neu).
/// Spur kader-transfer, Teil 5 (M-K7/M-K8, 0x057E3, 0x07C7E, 0x05521): alle neu – die Poolspieler gehen beim Spielstart (Zufall des
/// Kaderaufbaus) und beim Saisonwechsel (Hauptquelle: Drift 0x054D9, Auslandsrückholung 0x059B8, Zuteilung) in die Ligen der Manager;
/// andere Vereine der Markt-Plätze, andere Grundstärken und Preise ab der ersten Liste. Neue Felder <c>_poolTurn…</c> im Abdruck.
/// Spur kader-transfer, Teil 7 (0x07169, Datei 0x0754D/0x076FA): alle neu – Ruhestand und Verlängerungsangebote der Managerkader nach
/// dem Original (anderer Verbrauch der Vertragsquelle <c>ContractRandom</c>, weniger Angebote ohne Frist, Ruhestand über den Saisonwechsel).
/// Integration Runde 11 (geld + saisonwechsel + kader-transfer): Abdrücke auf dem integrierten Stand neu ermittelt (alle drei Spuren
/// ändern den Zufallsverbrauch; kein weiterer Unterschied).
/// Spur wettbewerbe-spiel, Teil 3 (A011 ff.): neu – Nationalteams spielen mit ihren eigenen Teamwerten (Kondition/Technik je Teil aus DGROUP 0x4C4E − 2, Form und Tagesform des Vereinsplatzes, Lader 0x2F0CC), das Land des Bundestrainers mit seinem Kader (0x98F8 mit Manager 5); andere WM-Ergebnisse und anderer Zufallsverbrauch nach der ersten WM.
/// Spur wettbewerbe-spiel, Teil 7 (A185, A186): neu – Kaderplatz +0x19 der Managerkader beim Spielstart, der Talente und der Marktplätze gewürfelt (random(3, 12) je Halbbyte, Datei 0x299F4/0x06F23/0x49803; eigene Quelle, Hauptquelle unverbraucht) statt 7/7; Über- und Unterzahl der Managerpaarungen rechnen damit, andere Ergebnisse und anderer Zufallsverbrauch ab dem ersten Managerspiel. Gegenprobe: ohne Würfe (alle 7/7) alte Werte.
/// Spur wettbewerbe-spiel, Teil 8 (A068, A089/A070): neu – Managervereine stellen mit der Einstellung „automatisch aufstellen“ (Startwert 1) beim Spielstart und täglich nach 0x426BC/0x42838 neu auf (statt Lücken stehen zu lassen bzw. nach Spielwert zu füllen), und die Feldspieler der Managerpaarung zählt je Minute 0x38A36 aus der Aufstellung; andere Aufstellungen und Ergebnisse ab dem ersten Managerspiel.
/// Integration Runde 11 (+ wettbewerbe-spiel): Abdrücke auf dem integrierten Stand neu ermittelt (alle vier Spuren ändern
/// Spielverlauf bzw. Zufallsverbrauch; dazu die Anschlüsse der Integration: Verletzungsart +0x17, Neuaufstellung je Managerzug, Bezugsbewertung nach der Startaufstellung).
/// </summary>
public class RngTwinProbeTests
{
    private const int SeasonChanges = 4;

    [Theory]
    [InlineData(false, 3, "CD8BEB27630AAC4E")]
    [InlineData(true, 3, "E51C7BCC704AA42D")]
    [InlineData(false, 11, "FC5639D844B4EEBD")]
    [InlineData(true, 11, "746F9251D819A9F6")]
    public void FourSeasonChanges_FingerprintUnchangedBySerializableGenerator(bool active, int seed, string expected)
    {
        var game = TestGames.Create(seed);
        Assert.Equal(expected, Run(game, active));
    }

    [GameDataTheory]
    [InlineData(false, "8D92AE74D1C12FBD")]
    [InlineData(true, "3E66FBA6BE529954")]
    public void FourSeasonChanges_OriginalDatabase_FingerprintUnchanged(bool active, string expected)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Probe", ClubIndex = 42 });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed: 3);
        Assert.Equal(expected, Run(game, active));
    }

    /// <summary>Spielt <see cref="SeasonChanges"/> Saisonwechsel über <see cref="GameState.Continue"/> und bildet den Abdruck.</summary>
    internal static string Run(GameState game, bool active, Action<GameState>? afterStep = null, int changes = SeasonChanges,
        Action<GameState>? beforeChange = null)
    {
        for (var change = 0; change < changes; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                    manager.Money = Math.Max(manager.Money, 10_000_000); // keine Entlassung
                if (active)
                    ExtendOffers(game);
                game.Continue();
                afterStep?.Invoke(game);
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            beforeChange?.Invoke(game);
            game.StartNextSeason();
            if (active)
                SignFreeAgents(game);
            afterStep?.Invoke(game);
        }
        return Fingerprint(game);
    }

    private static Club OwnClub(GameState game) => game.Clubs[game.Managers[0].ClubIndex];

    private static void ExtendOffers(GameState game)
    {
        foreach (var p in OwnClub(game).Players.Where(p => p.Notice == ContractNotice.Offered && p.ContractYears == 1).ToArray())
            Contracts.Negotiate(game, game.Managers[0], p, p.NoticeYears, Contracts.Demand(p) + 100, false, demandFilled: true);
    }

    private static void SignFreeAgents(GameState game)
    {
        var manager = game.Managers[0];
        while (OwnClub(game).Players.Count < 20
               && game.TransferMarket.Listings.FirstOrDefault(l => l.IsFreeAgent) is { } listing)
        {
            if (game.TransferMarket.Buy(game, manager, listing, 0, loan: false) != TransferResult.Accepted)
                break;
        }
    }

    /// <summary>
    /// Abdruck über Manager, Vereine, Kader, Pool, Laufbahnende und den Stand aller vier Zufallsquellen
    /// (je ein weiterer Wert, danach ist der Spielstand verbraucht).
    /// </summary>
    internal static string Fingerprint(GameState game)
    {
        var sb = Dump(game);
        sb.Append(game.Random.Next()).Append('|').Append(game.FinanceRandom.Next()).Append('|')
          .Append(game.CupRandom.Next()).Append('|').Append(game.ContractRandom.Next());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())))[..16];
    }

    /// <summary>Abdruck ohne Zufallsverbrauch (Spur „speichern“, Teil „format“): Zustand plus Zufallsabbild.</summary>
    internal static (string State, RngSnapshot Random) Snapshot(GameState game) =>
        (Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Dump(game).ToString())))[..16], game.CaptureRng());

    private static StringBuilder Dump(GameState game)
    {
        var sb = new StringBuilder();
        sb.Append(game.Season).Append('|').Append(game.Phase).Append('|').Append(game.CurrentDay).Append('\n');
        foreach (var manager in game.Managers)
            Dump(sb, manager);
        foreach (var club in game.Clubs)
        {
            Dump(sb, club);
            foreach (var p in club.Players)
                Dump(sb, p);
        }
        sb.Append("pool\n");
        foreach (var p in game.TransferMarket.FreeAgents)
            Dump(sb, p);
        sb.Append("retired\n");
        foreach (var p in game.RetiredPlayers)
            Dump(sb, p);
        return sb;
    }

    /// <summary>
    /// Nicht im Abdruck: <c>Player.Id</c> (Teil „ids“) gehört nur zum Spielstandformat und verändert den Spielverlauf
    /// nicht; so bleiben die auf <c>5f8e02a</c> ermittelten Abdrücke vergleichbar. Ebenso <c>Club.DressedCount</c>
    /// (Spur taktik-verein): abgeleitete Anzeige „Eingesetzt: n“, wirkt nicht auf den Spielverlauf – mit dieser Ausnahme
    /// stimmen alle sechs Abdrücke unverändert. Ebenso <c>Club.LeaguePenaltyMinutes</c> (Spur statistiken): Zähler für die
    /// Fair-Play-Prämie) steht im Kontostand. Ebenso <c>Player.InsurancePremium</c> und <c>Player.InsuranceLocked</c> (Spur
    /// transfer-vertraege-komplett, Teil 2): „versichert“ steht weiter als <c>Insured</c> im Abdruck, die Prämie wirkt über den
    /// Kontostand. Ebenso <c>Player.PurchasePrice</c>, <c>Player.InjuryKind</c> und <c>Player.InjuryName</c> (Spur kader-transfer,
    /// Teil 2): reine Anzeigefelder des Spielerdaten-Fensters und der Hinweiszeile, ohne Wirkung auf den Spielverlauf – mit
    /// dieser Ausnahme stimmen alle Abdrücke unverändert.
    /// </summary>
    private static readonly HashSet<string> Excluded =
        ["Id", "DressedCount", "LeaguePenaltyMinutes", "InsurancePremium", "InsuranceLocked", "PurchasePrice", "InjuryKind",
         "InjuryName"];

    /// <summary>
    /// Umbenannte Felder stehen unter ihrem alten Namen im Abdruck, damit die Abdrücke vergleichbar bleiben:
    /// <c>Player.NegotiationLock</c> (Spur transfer-original-vertrag, Sperre in Tagen) ersetzt <c>RejectedOfferDay</c>
    /// (Spieltag der letzten Ablehnung, sonst leer); 0 erscheint wie früher <c>null</c> als leerer Wert.
    /// </summary>
    private static readonly Dictionary<string, (string Name, Func<object?, object?> Value)> Renamed = new()
    {
        ["NegotiationLock"] = ("RejectedOfferDay", v => v is 0 ? null : v),
    };

    private static void Dump(StringBuilder sb, object value)
    {
        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.GetIndexParameters().Length == 0 && IsSimple(p.PropertyType) && !Excluded.Contains(p.Name))
                     .OrderBy(p => Renamed.TryGetValue(p.Name, out var r) ? r.Name : p.Name, StringComparer.Ordinal))
        {
            var (name, map) = Renamed.TryGetValue(property.Name, out var renamed) ? renamed : (property.Name, v => v);
            sb.Append(name).Append('=').Append(map(property.GetValue(value))).Append(';');
        }
        sb.Append('\n');
    }

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
    }
}
