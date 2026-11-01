# Transfermarkt nach em.exe (Spur `transfer-original`)

Overlay 0x118 (Datei 0x46000–0x4ADE1). Beträge im Original in **Drittel-DM** (Eingabe · 3 über DGROUP 0x79D/0x7A1,
Datei 0x4727B; Anzeige / 3 über 0x99F:0x2D8). Der Transfermarkt ist Kaderplatz-Satz 4 (DGROUP 0x7623 = 0x5DC3 + 4 · 0x618);
eigene Spieler auf dem Markt stehen dort mit Besitzer = Manager (Pool +0x1A). Umsetzung: `Core/Transfer.Original.cs`
(`TransferRules`), angeschlossen in `Core/Transfer.cs`; Tests `TransferOriginalTests`.

## Belegt und umgesetzt (Teil 1)

| Regel | Fundstelle | Inhalt |
|---|---|---|
| Listenpreis „(K)“ | Start 0x29EB4, Anzeige 0x4628B | +0x2C = Spielerwert 0x118:0x70 Art 0; Anzeige +0x2C (ohne Bit 31) / 3 DM |
| Leihgebühr „(L)“ | 0x462EB | (+0x2C / 3) / 3 DM, also Kaufpreis in DM / 3 |
| Annahme eines Gebots (Computermarkt) | 0x4991D–0x49A3A, gerufen 0x472C8 | P = +0x2C (bei Leihe / 3); Gebot &lt; P · random(75, 85) / 100 → abgelehnt; &gt; P · random(110, 130) / 100 → angenommen; sonst angenommen, wenn random(70, 120) &lt; Gebot · 100 / P |
| Reihenfolge | 0x472C8 vor 0x474F0 | Annahme vor der Geldprüfung |
| Gebot an anderen Manager | 0x47546–0x475DB | Wert frisch (bei Leihe / 3); unter 60 % oder über 140 % „Dieser Betrag ist nicht erlaubt (unfair) !“; sonst Rückfrage an den Verkäufer „Nehmen Sie das Angebot (… DM) für … an ?“ (DGROUP 0xD98) |
| Angebot für eigenen Spieler auf dem Markt (Tageswurf) | 0x072DD–0x073C2 | w = Wert / 10000; random(0, 180) &lt; w und random(0, 2) ≠ 0 und keine offene Meldung (Pool +0x1E) → +0x09 Bit 0x80, Verein in +0x16 (0x238:0x1838) |
| Höhe dieses Angebots | 0x47347–0x473DF | V = Wert (mit Angebot: · random(95, 100) / 100 bzw. · 1,6); V + random(V/20, V/10) − V/15; · random(85, 115) / 30000 · 300 |
| random(a, b) | 0xC037 (0x99F:0x847) | 16-Bit-Grenzen, bei b &lt; a getauscht, rand() mod (hi + 1 − lo) + lo |
| Schalter DGROUP 0x4860 | 0x6FD7, 0x72FD, 0x79FE | nur gelesen, nie beschrieben, in em.exe 0 → Zweige „sofort Angebot“ entfallen |

Gegenprobe: alle 20 Markt-Plätze der Original-Spielstände FILE03/05/07 (Preis +0x2C und Gehalt +0x28 = Spielerwert) und
die sechs Preise der Transferliste im Video `oberflaeche_1102` (exakt mit einer Tendenz im Startbereich 35–65).

## Belegt und umgesetzt (Teil 2: Vertragsverhandlung, Sperren)

Umsetzung: `Core/Transfer.Contract.cs` (`ContractRules`), `Core/Player.Contract.cs` (`Contracts.Negotiate`, `Availability`,
`DemandAfterYears`), Gebotssperre in `Core/Transfer.cs`; Ansicht `UI/Screens/Team/ContractsScreen.cs`; Tests
`TransferOriginalVertragTests`, `TransferOriginalVertragScreenTests`.

Stubeinträge Ov 0x118 (Stub-Segment 0x14C7, Datei 0x16A70): 0x2A → 0x465F1 Transferliste, 0x43 → 0x48E28, 0x4D → 0x4907A,
**0x5C → 0x494EF** (Wechsel der Liste), 0x70 → 0x49D36 Spielerwert, **0x75 → 0x4A1DB** Vertragsverhandlung.

| Regel | Fundstelle | Inhalt |
|---|---|---|
| Aufrufer der Verhandlung | 0x4786A (Kauf), 0x27D91 (Saisonende 0x26F20), 0x40208 (Bildschirm „Verträge“ 0x3FFE0) | `f(Kaderplatz far*, y)`; Ergebnis 1 = Vertrag steht |
| Prüfung beim Anklicken von Laufzeit/Gehalt | 0x4A692–0x4A770 | +0x0C ≠ 0 „… ist leider von &lt;Verein&gt; nur ausgeliehen.“; +0x18 Bit 0x80 „… wird sich mit Ablauf des Vertrages zur Ruhe setzen.“; +0x18 ≠ 0 und Tageszähler 0x3B7 &lt; 233 „… ist im Moment nicht verhandlungsbereit.“ |
| Angebot des Spielers | 0x4A3AF, 0x4AD7D | +0x18 &gt; 99: beim Öffnen +0x0B = +0x18 − 100, +0x18 = 0; die Meldung (Pool +0x1E) wird beim Schließen gelöscht |
| Forderung nach Eingabe der Laufzeit | 0x4A81B–0x4A9F2 | S = Wert(Gehalt) · [0xE1A4] / 100; Rest = 100 − 100 · nächster Spieltag / 44; U = (Jahre − 1) · 100 + Rest; S = S · ((U / 6 + 122) · 8 / 10 + 4) / 100; nach dem Saisonende · (152 − Alter) / 100 (Alter aus Kaderplatz 3, DI = 3 nach der Zeichenschleife); mindestens bisheriges Gehalt; im Feld nur bei 0xED8 ≠ 0 (Level &lt; 5) |
| Faktor [0xE1A4] | 0x44E3F | random(85, 115) zu Beginn jedes Managerzugs |
| „Mach' ich!“ | 0x4AB66–0x4AD7A | &gt; 4 Jahre „SOOOO lange verpflichtet sich kein Eishockey-Spieler !“; Forderung im Feld (Merker, nach Laufzeit ohne spätere Gehaltseingabe) → angenommen; keine Eingabe bzw. gleiche Laufzeit und mindestens gleiches Gehalt → angenommen; kürzer „So dumm ist … leider nicht...“; sonst 0x49A3B, abgelehnt „… ist nicht an Ihrem Angebot interessiert.“ |
| Annahme | 0x49A3B–0x49D35 | gleiche Laufzeit: mehr Gehalt ja, weniger nein; kürzer: &gt; 120 % ja, ≤ 105 % nein; D = Wert(Gehalt) · random(85, 115) / 100 · random(W − 6, W + 4) / 100 mit W = (U / 6 + 110) · 8 / 10, nach dem Saisonende · random(132 − Alter, 152 − Alter) / 100; D ≥ 100.000 nein; sonst ja, wenn Gehalt &gt; D |
| Sperre nach gescheiterter Verhandlung | 0x4AD3E, Tagesablauf 0x76E5–0x76F6 | +0x18 = random(10, 18), täglich − 1 (Abbruch sperrt nicht) |
| Angebot des Spielers verfällt | Tagesablauf 0x767A–0x76E3 | +0x18 &gt; 99: random(0, 5) = 0 und Meldung offen → +0x18 = random(9, 17), Meldung gelöscht |
| Versicherung | 0x4AA74–0x4AAC3, 0x4AB73 | Umschalten nur ohne +0x2C Bit 31 und ohne Verletzung (+0x09 &amp; 3 = 2); Prämie +0x30 = Wert(Gehalt) / 2; bei Abschluss im Dialog +0x2C Bit 31 (danach nicht mehr kündbar) |
| Saisonende | 0x27D05–0x27DF3 | +0x0B = 0: +0x18 = 101, +0x0B = 1, Gehalt = Wert(Gehalt) · (147 − Alter) / 100, dann Verhandlung; scheitert sie: „… kehrt Ihrem Verein den Rücken...“ |
| Gebotssperre | 0x472F4, 0x47140, 0x47B01, Tagesablauf 0x071CC | abgelehntes Gebot vom Computermarkt: **Kaderplatz** +0x1A \|= 0x80 \| 1 &lt;&lt; Manager (nicht Pool +0x1A, das ist der Besitzer); Anklicken mit beiden Bits „Angebot wurde bereits abgelehnt.“; täglich (nur im Zug von Manager 0) hebt random(0, 2) = 0 die Sperre auf, wenn allein Manager 0 gesperrt ist |
| Reihenfolge Gebot an einen Manager | 0x474F0 vor 0x47546 | erst Geld, dann Unfair-Grenze (R8-C02) |
| Wechsel der Liste | 0x494EF (Stub 0x5C) | gerufen beim Saisonstart (0x43A9F) und täglich mit random(0, Managerzahl + 2) = 0 (0x54E71): Markt-Plätze leeren, aus dem Pool (Besitzer 6) Spieler 1–179 bzw. mit random(0, 5) = 0 180–199 ziehen, Form random(45, 55), Preis/Gehalt = Wert, +0x19 = random(3, 12) · 16 + random(3, 12) |

Abbildung im Remake: Tageszähler &gt; 232 = Phase `SeasonOver`; [0xE1A4] je Saison und Tag aus eigenem Startwert
(`GameState.ContractTurnFactor`, `[APPROX]`); Zufall der Verhandlung aus `ContractRandom`; Kaderplatz 3 = vierter Spieler
in `Club.Players` (`[APPROX]`).

## Belegt und umgesetzt (Spur `transfer-vertraege-komplett`, Teil 1: Kauf und Saisonende)

Umsetzung: `Core/Transfer.Purchase.cs` (`TransferMarket.Bid`/`Sign`, `TransferPurchase`), `Core/Transfer.SeasonEnd.cs`
(`StartNextSeasonWithRenewals`, `NegotiateRenewal`, `CancelRenewal`, `ResolveRenewals`), gemeinsamer Kern
`Contracts.NegotiateTerms`; Dialog `UI/Screens/Team/ContractDialog.cs` (Verträge, Kauf, Saisonende); Tests
`TransferVertraegeKomplettTests`, `TransferVertraegeKomplettScreenTests`.

| Regel | Fundstelle | Inhalt |
|---|---|---|
| Kauf: Verhandlung nach der Annahme | 0x4781B–0x4786A | neuer Kaderplatz (0x108:0x5C), +0x2C = Gebot (Bit 31 bleibt), dann `0x4A1DB(Platz, Zeile, y = 0)`; Geld und Wechsel erst danach (0x47945–0x47A3B) |
| Kauf: Verhandlung scheitert | 0x47874 → 0x47A3E | Platz frei (0x108:0x48), „Der Transfer / findet nicht statt.“ (DGROUP 0x1201/0x1205); auch bei „Abbruch“ |
| Leihe ohne Verhandlung | 0x47851–0x4785B | Verhandlung nur bei +0x11 = 0 (kein eigener Spieler) und ohne Leihe; Leihe: +0x0B = 1, +0x0C = Verein + 0x80, altes Gehalt (0x47918–0x47941) |
| Saisonende: Herunterzählen | 0x274A7, 0x27500 | im Saisonende 0x26F20: Alter +1 (nur im Zug von Manager 0), Laufzeit +0x0B − 1 |
| Saisonende: Angebot | 0x27D05–0x27D70 | +0x0B = 0 → +0x18 = 101 (Angebot 1 Jahr), +0x0B = 1, Gehalt = Wert(Gehalt) · (147 − Alter) / 100 |
| Saisonende: Verhandlung | 0x27D74–0x27D91 | `0x118:0x75(Platz, Zeile, y)` mit y = 80 für Zeile &lt; 12, sonst 0 (wie „Verträge“ 0x401D6) |
| Saisonende: bleibt | 0x27D99, 0x27EA7–0x27EDF | Ergebnis 1 und +0x0B ≠ 0 → „… bleibt Ihnen auch / die nächste Saison erhalten“ (DGROUP 0x139D/0x13A1) |
| Saisonende: geht | 0x27DAD–0x27E9D | „… kehrt Ihrem Verein / den Rücken...“ (0x138D/0x1391); Ablöse = Wert(Preis) / 2 aufs Konto, „Sie erhalten eine / Ablösesumme von / … DM“ (0x1395/0x1399, „ DM“ 0x14C9); Platz frei |

Abbildung im Remake: Der wartende Spieler bleibt mit Laufzeit 0 und dem Angebot „1 Jahr“ (`ContractNotice.Offered`,
`NoticeYears` = 1) im Kader – der Zustand +0x0B = 0/+0x18 = 101, ohne neues Spielstandsfeld. Solange solche Spieler warten,
gilt „nach dem Saisonende“ (`AfterSeasonEnd`). Der Bildschirm nach „Erhaltene Prämien“ (`ContractsScreen.ForSeasonEnd`) führt
die Verhandlungen; was offen bleibt, endet am ersten Tag der neuen Saison wie „Abbruch“; ohne Oberfläche
(`StartNextSeason`) sofort.

`[APPROX]`: Reihenfolge (Original je Manager Prämien, dann Verträge; Remake erst alle Prämien); Saisonzähler (Tore, Spiele) sind
beim Rechnen von Gehalt und Ablöse schon zurückgesetzt; Ablöse als Transfereinnahme gebucht (das Original schreibt sie ohne
Buchungsaufruf aufs Konto);
Liste hinter dem Fenster am Saisonende = Bildschirm „Verträge“ (Original 0x108:0x43; Abweichung im Pixelvergleich unten belegt).

**Befund für `finanzen-rest`:** Nach einem Kauf vom Computermarkt ruft das Original 0x18:0x223E (Datei 0x063BE, Fenster „Nehmen
Sie einen Sponsor- / zuschuß von … an ?“) mit dem Kaufpreis (Datei 0x47A18–0x47A34); im Remake fehlt das.

## Belegt und umgesetzt (Spur `transfer-vertraege-komplett`, Teil 2: Versicherung, Sperre)

Umsetzung: `Core/Player.Contract.cs` (`Player.InsurancePremium`, `Player.InsuranceLocked`, `Contracts.InsuranceOffer`,
`CanChangeInsurance`, `ApplyInsurance`), `Core/Transfer.Insurance.cs` (`GameState.PayInsurance`), Dialog
`UI/Screens/Team/ContractDialog.cs`; Migration 17 → 18 (vorläufig); Tests `TransferVertraegeKomplettTeil2Tests`,
`TransferVertraegeKomplettTeil2ScreenTests`.

| Regel | Fundstelle | Inhalt |
|---|---|---|
| Prämie | 0x4AA9C–0x4AABF | Einschalten im Fenster: +0x30 = Wert(Gehalt) (0x9D36 Art 1) / 2; Ausschalten: 0 (0x4AAC5) |
| Umschalten gesperrt | 0x4AA74–0x4AA90 | nicht bei +0x2C Bit 31 (Abschluss) und nicht bei Verletzung (+0x09 &amp; 3 = 2) |
| Anzeige | 0x4AAD4–0x4AB1C | +0x30 = 0 → „keine“, sonst Betrag mit „ DM“ |
| Abschluss | 0x4AB73–0x4AB8D | „Mach' ich!“ mit neu eingeschalteter Versicherung → Bit 31, **vor** der Prüfung des Ergebnisses |
| Gescheitert / Abbruch | 0x4AD39–0x4AD76 | +0x30, Laufzeit und Gehalt wie beim Öffnen; Bit 31 bleibt bei einer gescheiterten Verhandlung; „Abbruch“ springt an Bit 31 vorbei (0x4AB6E) |
| Kosten | 0x3E8DC | monatlich +0x30 mit den Gehältern (schon bisher über `FinanceRules.MonthlyInsurance`) |
| Leistung Verletzung | 0x7089–0x7168 | +0x30 · 4 aufs Konto, „NAME ist verletzt.“ / „Ein Fall für die Versicherung: Sie zahlt Ihnen … DM“ (DGROUP 0x134D–0x1359); Aufrufe 0x7542 (Tagesablauf), 0x38D6A (Spiel, 0x20:0xBA9) |
| Leistung Laufbahnende | 0x2772B–0x277DE | im Saisonende bei +0x0D = 0xFF: +0x30 · 60, „NAME ist invalide.“ (0x135D) |
| Sperre über den Saisonwechsel (R8W3I-09) | 0x274E8–0x27539 | +0x18 bleibt stehen; nur der Durchlauf ohne Halt (DGROUP 0x1DB) setzt 0 (0x27534) |

`[APPROX]`: ganze DM statt Drittel-DM (Prämie abgerundet, mindestens 1 DM); Leistung als Einnahme der Kategorie Versicherung
gebucht (Original ohne Buchungsaufruf, 0x7147); Stände vor Schema 18 und Aufrufer, die nur „versichert“ setzen, bekommen die
bisherige Prämie (10 % des Gehalts, `Contracts.LegacyInsurancePercent`), ebenso die Rückkehr eines verliehenen Spielers ohne
bekannte Prämie. Beim Kauf bleibt ein vorhandener Abschluss des Spielers stehen (+0x2C Bit 31 bleibt, 0x4781B); scheitert die
Verhandlung nach dem Kauf, wird der neue Platz verworfen und mit ihm der Abschluss.

## Belegt und umgesetzt (Spur `transfer-vertraege-komplett`, Rest von Teil 2: Start- und Wechselgehalt)

Umsetzung: `Contracts.ValueSalary` und `Contracts.PurchaseYears` (`Core/Player.Contract.cs`), `GameState.ApplyStartSalaries`
(`Core/Transfer.StartSalary.cs`, Anschluss in `GameState.InitializeManagerSystems`), Kauf in `TransferMarket.Bid`; Tests
`TransferVertraegeKomplettTeil2bTests` (Goldtest aus FILE01, FILE09, FILE10 und FILE07).

| Regel | Fundstelle | Inhalt |
|---|---|---|
| Startgehalt | 0x29EB4–0x29EF3 (0x28165) | je Kaderplatz des Managers [0xA70E]: +0x2C = Wert(Preis), +0x28 = Wert(Gehalt); Computervereine haben kein Gehaltsfeld |
| Kauf: neuer Kaderplatz | 0x477CD–0x477EC → 0x42CB1 | Aufruf `0x108:0x5C` (= Datei 0x42CB1) mit Laufzeit 1 (+0x0B, 0x42E75); +0x28 = Wert(Gehalt) (0x42F11), bei +0x0C = 99 zusätzlich · random(90, 115) / 100 (0x42F1F); danach +0x2C = Gebot (0x4781B) |
| Listenwechsel | 0x4975C, 0x497A1 (0x494EF) | +0x2C = Wert(Preis), +0x28 = Wert(Gehalt) eines neuen Kaderplatzes (Teil 3) |
| Beleg Spielstände | FILE01, FILE09 (2 Manager, Level 5), FILE10 | alle Kaderplätze der Manager: +0x28 = Wert(Gehalt) aus den gespeicherten Eingängen, Vertrag 2 Jahre |
| Beleg Kauf | FILE07 | DAFFNER: +0x28 = Wert(Gehalt) = 9.900 DM, +0x0B = 1 |

Wert(Gehalt) ist ein Vielfaches von 300 Drittel-DM; das Remake speichert es ohne Rundungsverlust als ganze DM. Bei den Stärken
der Bundesliga liegen die Startgehälter damit bei 11.000–18.000 DM (FILE01: Kölner EC), in der Oberliga um 1.000 DM.

`[APPROX]`: Tore und Spiele gehen mit den Zählern des Remakes ein (wie bei allen Aufrufen des Spielerwerts); den Startpreis
(+0x2C) führt das Remake nicht als Feld. Computervereine, Leihen, Talente und Verlängerungen der Computervereine behalten die
Forderung des Remakes (`Contracts.Demand`).

## Belegt und umgesetzt (Spur `transfer-vertraege-komplett`, Teil 3: unaufgeforderte Angebote, Knöpfe)

Umsetzung: `Core/Transfer.Unsolicited.cs` (`UnsolicitedOfferRules`, `GameState.RollUnsolicitedOffers`, `OpenUnsolicitedOffer`,
`AcceptUnsolicitedOffer`), Feld `Player.UnsolicitedOffer` (Schema 19, vorläufig), Anschluss täglich in `AnnounceContractEnds`;
Angebotsfenster `TransferDialog.ForUnsolicited` im Bildschirm „Transfer“; Tests `TransferVertraegeKomplettTeil3Tests`,
`TransferVertraegeKomplettTeil3ScreenTests`.

| Regel | Fundstelle | Inhalt |
|---|---|---|
| Tageswurf | 0x07972–0x07AEF (Tagesablauf 0x07169, Zug des Managers [0xA70E]) | nur bis zum Saisonende (0x3B7 &lt; 233); je Kaderplatz ohne Leihe (+0x0C) und ohne Rücktritt (+0x18 Bit 0x80): w = Wert(Preis) / 10000; random(0, 200) &lt; w, random(0, 450) = 0, keine offene Meldung (Pool +0x1E) → +0x09 \|= 0x40 |
| Bietender Verein | 0x07A61–0x07A8F → 0x059B8 (0x238:0x1838) | Europapokal-Gegner, wenn w &gt; 90 und random(0, 10) = 0; Verein c = random(0, 42) bzw. random(64, 149), Managerverein (+0x16 ≠ 0) verworfen; Vereinsstärke = (Felder 0x18–0x1B + 100) / 6; angenommen nach mehr als 300 Versuchen oder wenn \|Vereinsstärke − s\| &lt; random(2, 10 · t) (t = 1, ab Versuch 100 2, ab 200 3); s = Mittel der Grundstärken des Pooleintrags |
| Meldung | 0x07A93–0x07AE3 (0x14CF:0x39 = 0x4B2F6) | „&lt;Verein&gt; ist an &lt;Spieler&gt; interessiert.“ (Vorlage DGROUP 0x29B7) |
| Anklicken | 0x46E04–0x46ED2 (Transferliste, eigener Kader) | Spieler mit +0x18 Bit 0x80: „Dieser Spieler hört am Saisonende auf.“; mit Bit 0x40: V = Wert(Preis), Angebot = V · random(87, 125) bzw. bei Verein &gt; 0x3F random(112, 155) / 30000 · 300; Bit 0x40 und Meldung werden **vor** der Antwort gelöscht |
| Angebotsfenster | 0x491F9 (Argument 4 = 1) | Rahmen 0x490DE (2 … 0xA8 \| 0x1C … 0x63), Titel „Manager“ + Name, „ANGEBOT von &lt;Verein&gt; :“, Betrag, „ABLÖSESUMME :“ „keine“, Knöpfe „Behalten“ (0x0B \| 0x53) und „Verkaufen“ (0x5A \| 0x53) |
| Verkaufen | 0x46F3C–0x46FCF | Konto + Angebot (ohne Abzug), Kaderplatz entfernt (0x108:0x48), Pool +0x1D = Verein; Verein &lt; 0x40 nimmt ihn auf (0x18:0x175C), sonst Besitzer 6 |
| Knöpfe | 0x4C196 (0x120:0x75), 0x4BEBD (0x120:0x6B), 0x4BFF2 | das fünfte Argument (0x12 bei „Mach' ich!“/„Verkaufen“, sonst 1) wird nicht gelesen; Zustand 1 (kb\\2 ab (17 \| 191)) nur, solange die linke Maustaste über dem Knopf gedrückt ist |

Damit ist die offene Frage zum Verhandlungsfenster geklärt: Der hellere „Mach' ich!“ im Video ist der gedrückte Knopf (Zustand 1),
kein anderes Argument. Das Remake zeichnet Knöpfe der Verhandlungs- und Angebotsfenster unter gedrückter Maustaste ebenso.

`[APPROX]`: Zufall des Tageswurfs aus einer je Saison und Tag abgeleiteten Quelle (wie `ContractTurnFactor`), Höhe beim Anklicken aus
`ContractRandom`; „offene Meldung“ = noch nicht angeklicktes Angebot; Rücktritt = Ankündigung `Retiring`. Das Original übergibt
dem Bieter die **Nummer des Kaderplatzes** statt der Poolnummer (0x07A81), misst also die Grundstärken eines fremden
Pooleintrags; das Remake nimmt die Stärke des Spielers. Vereinsstärke aus Stärke und Teamwerten des Laders (`ClubTeamValues`).
Ein voller Computerkader, ein Nationalteam oder ein Europapokal-Gegner gibt den Spieler in den Pool der Vertragslosen. Das
Angebotsfenster ist im Video nicht zu sehen; Lage nur aus dem Code (Grundlinien wie beim Verhandlungsfenster umgerechnet).

**Neuer Befund (nicht umgesetzt):** Dasselbe Fenster zeigt bei einem Angebot für einen Spieler **auf dem Markt** (Bit 0x80,
Aufruf 0x473FF mit Argument 0) unter „ABLÖSESUMME :“ Angebot − Angebot / 7 · Restlaufzeit (+0x0B) und schreibt diesen Rest gut
(0x49332–0x49371, danach 0x4748A). Das Remake bucht den vollen Betrag und zeigt eine Rückfrage (`UnsolicitedOfferRules.MarketTransferFeeThirds`
ist vorbereitet). Umstellung ändert Geld in fremden Tests (`TransferTests`, `TransferOriginalTests` …) → Teil 4 bzw. Entscheidung.

**Listenwechsel 0x494EF (nicht umgesetzt, Entscheidung nötig):** Der Wechsel räumt alle Markt-Plätze (Satz 4, 10 Plätze) ohne
Managerspieler, zieht aus dem **Pool von 200 Spielern** (Besitzer 6; Nummer random(1, 179), mit random(0, 5) = 0 random(180, 199),
bis zu 1500 Versuche) neue Spieler, würfelt Form (+0x17 = random(45, 55)) und Grundstärken nach dem Verein (0x4981E), legt den
Kaderplatz an (0x108:0x5C) und setzt +0x2C = Wert(Preis), +0x28 = Wert(Gehalt) (0x4975C/0x497A1), +0x19 = random(3, 12) · 16 +
random(3, 12). Computervereine haben im Original keine Spieler; das Remake zieht Markt-Spieler aus den Kadern der Computervereine.
Eine treue Umsetzung braucht den Pool als eigenes Modell (Datenbank, Spielstand, Kader der Computervereine) – Entscheidung Nutzer.

## Noch offen (`[APPROX]` im Code, weitere Teile)

- **Antwort des verkaufenden Managers** (Hot-Seat-Rückfrage 0x47608–0x47743) fehlt; das Remake nimmt ab dem geforderten
  Betrag an, darunter bis 85 % mit sinkender Wahrscheinlichkeit.
- **Bietender Verein** für Spieler auf dem Markt (Bit 0x80): 0x059B8 ist jetzt belegt (Teil 3), für den Markt aber noch nicht
  angeschlossen; Pflege der Liste 0x7C12 nicht belegt.
- **Zeitpunkt der Züge:** das Original würfelt die Angebotshöhe beim Anklicken, das Remake beim Angebot.
- **Listenpreis eigener Spieler:** das Original kopiert den Kaderplatz (0x48:0x2BD4) samt altem +0x2C (Start- bzw. Kaufpreis);
  das Remake rechnet den Wert beim Anbieten frisch.
- **Spieler, der am Saisonende aufhört** (Kaderplatz +0x18 Bit 0x80, Meldung DGROUP 0x12BD): bekommt im Original keine
  Angebote und kann nicht gekauft werden – nicht abgebildet.
- **Leihe:** Gehalt des geliehenen Spielers (0x47918–0x47941 altes Gehalt; 0x42F1F · random(90, 115) / 100 bei +0x0C = 99)
  nicht gegen das Remake geprüft; das Remake nutzt die Forderung aus Stärke und Talent.
- **Versicherungsleistung** (`GameState.PayInsurance`, Teil 2) hat noch keinen Auslöser: Verletzungen im Spiel (0x38D6A) und im
  Tagesablauf (0x7542) sowie das Laufbahnende (+0x0D = 0xFF, 0x2772B) gibt es im Remake noch nicht (spielberechnung-komplett,
  Teil 3). Dort jeweils `PayInsurance(manager, spieler, careerEnd)` aufrufen.
- **Gehalt in ganzen DM** (R8W3I-08): Das Original rechnet in Drittel-DM; das Remake speichert ganze DM und rundet Forderung,
  Prämie und Vergleichsforderung ab (bis 2/3 DM je Monat). `[APPROX]` am Datenmodell, im Code an `Contracts.InsuranceOffer`
  und `ComputedDemand` vermerkt.
- **Angebot des Spielers verfällt** (random(0, 5), dann Sperre random(9, 17)) und Angebot der Ankündigung „von 1 auf n Jahre“
  stammen im Remake aus der Spur „kader“ (`[APPROX]`, R2-11); umgesetzt ist nur, dass „Mach' ich!“ ohne Eingabe das Angebot
  annimmt.
- **Wechsel der Liste 0x494EF** (Pool von 200 Spielern) – das Remake zieht aus den vollen Kadern der Computervereine (Teil 3, oben).
- **Knöpfe im Verhandlungsfenster 1 Pixel zu weit rechts** (nachgemessen in Teil 4, Tabelle unten): In allen drei Vorlagen
  (`oberflaeche_1030`, `karriere_01924`, `karriere_03555`) passen beide Knöpfe mit Versatz −1 | 0 am besten, die Texte des
  Fensters mit 0 | 0. Das Original übergibt aber x = 0x0B bzw. 0x5A (Datei 0x4A444, 0x4A41E) wie das Remake, und 0x4BFF2
  blittet kb\\2 ab (17 | 179) ohne Korrektur an diese Stelle. Ursache offen (Verdacht: Spaltenlage von kb\\2 im Decoder oder
  der Blit 0x50:0xA79 – beides fremde Dateien); nicht geändert, weil ein örtlicher Versatz andere Knöpfe aus kb\\2 nicht träfe.
- **Positionsbuchstabe in der Transferliste:** Im Video ist das „S“ von LAPORTE dunkel (≈ 65 | 38 | 50) – in allen Bildern
  `oberflaeche_1054` … `1104`, unabhängig von der Maus –, bei den übrigen Einträgen in der Rollenfarbe. Bedeutung nicht belegt
  (LAPORTE ist der einzige Spieler „aus FRANKREICH“ in der Liste; Verdacht Ausländerkennung). Remake: Rollenfarbe.

## Pixelvergleich (Abnahme 3)

Ablauf `transfer-vertrag` (`Tools/Scenarios/TransferVertrag.cs`): Verhandlung mit SCHMEISSER (19 Jahre, 2 Jahre, 1.500 DM)
gegen `oberflaeche_1030`, Transferliste mit den sechs Spielern des Videos (Maus auf LAPORTE) gegen `oberflaeche_1102`. Kennzahl
wie `pixel-verein`: mittlere Graudifferenz / Anteil der Pixel über 40, beide Bilder leicht weichgezeichnet, eigenes Bild um
höchstens 1 Pixel verschoben, Mauszeiger maskiert.

| Bereich | Vorher | Nachher |
|---|---|---|
| Verhandlungsfenster (2 … 167 \| 110 … 176) | 7,8 / 1,2 % | unverändert (Lage stimmt, Abweichung Knöpfe s. o.) |
| Transferliste (176 … 318 \| 15 … 222) | 7,4 / 3,6 % | 4,2 / 0,9 % |
| Einträge (178 … 316 \| 36 … 135) | 11,9 / 6,5 % | 6,1 / 2,0 % |
| Titel „Transferliste“ | 8,5 / 5,5 % | 3,2 / 0,0 % |

Behoben: rechtsbündige Spalten der Transferliste (Stärken, Alter, Leihgebühr) standen 3 Pixel zu weit links, der Titel 2 Pixel zu
hoch und 1 Pixel zu weit rechts (Messung je Feld mit Verschiebungssuche ±5 Pixel, danach Versatz 0 | 0).

### Neue Lagen aus Teil 1 (Spur `transfer-vertraege-komplett`)

Ablauf `transfer-vertrag-kauf-saisonende` (`Tools/Scenarios/TransferVertragKaufSaisonende.cs`), Kennzahl wie oben. Die Vorlagen
wurden in den Reihen `oberflaeche` und `karriere` per Vorlagensuche der Titelzeile „Vertragsverhandlung“ gefunden: Das Fenster
steht im Original nur an zwei Stellen, y = 109 (Zeile &lt; 12) und y = 29 (sonst und beim Kauf).

| Bereich | Vorlage | Kennzahl | Versatz |
|---|---|---|---|
| Kauf: Fenster (2 … 167 \| 30 … 96) | `karriere_01924` (RUSSLER, 25 Jahre, 4 Jahre, 5.125 DM) | 5,8 / 1,2 % | 0 \| 0 |
| Kauf: Titel (4 … 165 \| 31 … 43) | | 5,3 / 0,0 % | 0 \| 0 |
| Saisonende: Fenster (2 … 167 \| 110 … 176) | `karriere_03555` (A.ONNEN, 27 Jahre, 1 Jahr, 6.480 DM) | 5,9 / 0,8 % | 0 \| 0 |
| Saisonende: Titel (4 … 165 \| 111 … 123) | | 4,9 / 0,0 % | 0 \| 0 |
| Saisonende: Kopfzeile (0 … 319 \| 0 … 14) | | 7,7 / 5,0 % | – |

Lage und Inhalt beider Fenster stimmen. Abweichung nur hinter dem Fenster am Saisonende (`[APPROX]` oben): Die Liste des
Originals (0x108:0x43, `karriere_03555`, `karriere_03648`) trägt den Vereinsnamen als Titel („BK OLDENBURG“), zeigt
ausgelaufene Verträge rot als „kein V.“ und hat rechts unten ein Symbolfeld statt der Hinweiszeile; das Remake zeigt dort die
Liste „Verträge“. Gehört zu Teil 3 (Knopfdarstellung/Listen) bzw. eigener Befund.

### Teil 4 (Spur `transfer-vertraege-komplett`): Markierung in der Transferliste, Knöpfe

Kennzahl wie oben, Mauszeiger (rot) maskiert, beide Bilder mit σ = 0,6 weichgezeichnet, eigenes Bild um höchstens 1 Pixel
verschoben (Messung in der Arbeitsumgebung, nicht im Repo).

| Bereich | Vorlage | Vorher | Nachher | Versatz |
|---|---|---|---|---|
| Transferliste (176 … 318 \| 15 … 222) | `oberflaeche_1102` | 4,4 / 0,9 % | 3,3 / 0,0 % | 0 \| 0 |
| Einträge (178 … 316 \| 36 … 135) | | 6,0 / 2,0 % | 3,7 / 0,1 % | 0 \| 0 |
| Eintrag LAPORTE mit Linien (178 … 316 \| 114 … 135) | | 15,9 / 9,1 % | 5,4 / 0,4 % | 0 \| 0 |
| Titel „Transferliste“ (210 … 290 \| 15 … 30) | | 4,2 / 0,0 % | 4,2 / 0,0 % | 0 \| 0 |
| Verhandlungsfenster (2 … 167 \| 110 … 176) | `oberflaeche_1030` | 7,4 / 0,9 % | unverändert | 0 \| 0 |
| Knopf „Mach' ich!“ (10 … 66, 12 Zeilen) | `oberflaeche_1030` / `karriere_01924` / `karriere_03555` | 10,5 / 10,6 / 8,9 | unverändert | bei −1 \| 0: 7,3 / 7,1 / 5,4 |
| Knopf „Abbruch“ (97 … 152) | dieselben | 9,5 / 8,0 / 7,9 | unverändert | bei −1 \| 0: 6,7 / 4,9 / 5,6 |

Behoben: Der Eintrag unter der Maus (bzw. im offenen Dialog) wird im Original durch die **Trennlinien** über und unter dem
Eintrag markiert – obere Zeile der Linie (y − 4 und y + 12) dunkelrot, gemessen 68–75 | 38 | 40–47 (`oberflaeche_1100` bei LANGE
mit der Linie unter der Kopfzeile, `oberflaeche_1102` bei LAPORTE). Das Remake zeichnete hellrot (Index 17) eine Zeile über dem
Namen, die untere Linie überschrieb die Trennlinie. Farbe Index 201 (65 | 0 | 0) `[APPROX]`: aus der Unschärfe geschätzt
(Mischung mit dem Hintergrund passt besser als 16 oder 253). Die gemeinsame Zeilenmarkierung `TeamUi.HoverRow` (Kader- und
Vertragslisten) ist nicht geändert. Test: `TransferVertraegeKomplettTeil4ScreenTests`.

Testlücken aus Teil 4 (`TransferVertraegeKomplettTeil4Tests`): „unfair“ der Verhandlung bei negativen Eingaben ohne Sperre und
ohne Zufallszug (R8W3I-10), Umkehrung des Gegenbeispiels nach dem Saisonende auf Level 3 und 5 (R8W3I-16), Zugfolge der
Werbebewertung mit einem Spieler mit Angebot (SW-01). Jeder Test wurde gegen die passende Mutation rot geprüft.
