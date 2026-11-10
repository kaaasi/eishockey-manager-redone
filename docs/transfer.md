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
| Wechsel der Liste | 0x494EF (Stub 0x5C) | gerufen beim Saisonstart (0x43A9F) und täglich mit random(0, Managerzahl + 2) = 0 (0x54E71); Ablauf und Umsetzung in Teil 5 (unten) |

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

## Transferfolgen beim Computerverein (Spur `kader-transfer`, Teil 3; F005, F006)

Umsetzung: `Core/Transfer.ComputerClub.cs` (`ComputerClubTransferRules`, `GameState.StrengthenBuyer`,
`GameState.AcceptSponsorSubsidy`; `TransferMarket.Bid`/`Sign` mit `out SponsorSubsidyOffer?`); Frage in `UI/Screens/Team/TransferDialog.cs` (`DialogKind.Subsidy`);
Tests `KaderTransferTeil3Tests`, `KaderTransferTeil3ScreenTests`. Kein neues Spielstandsfeld.

| Regel | Fundstelle | Inhalt |
|---|---|---|
| Käufer stärker | 0x058DC (0x238:0x175C) | Argumente Verein, s = (+0x10 + +0x11 + +0x12) / 3; a = (Kondition + Technik eines Teils) / 2; d = (s − a + 10) / 3; Teil k = random(0, 1); d auf 1 … 10 (0x120:0x52); Kondition 0x18 + k und Technik 0x1A + k je + random(d − 1, d + 1), ohne Grenze |
| Aufruf Angebot | 0x46F89–0x46FB3 | „Verkaufen“ bei einem unaufgeforderten Angebot, nur Verein &lt; 0x40; danach Pool +0x1A = 6 |
| Aufruf Markt | 0x4743C–0x47483 | Gebot auf einen eigenen Spieler am Markt angenommen, ohne Prüfung des Vereins; danach Gutschrift 0x4748A |
| Zuschuss | 0x063BE (0x18:0x223E) | nur bei random(0, 6) = 0; Z = P · random(25, 65) / 30000 · 300 (Drittel-DM, volle 100 DM); Frage 0x4C246 „Nehmen Sie einen Sponsor- / zuschuß von / Z DM an ?“, „Aber immer!“ / „Lieber nicht.“; Ja → Konto + Z (0x064AD) |
| Aufruf Zuschuss | 0x47A00–0x47A34 | nach dem Abzug des gezahlten Betrags P (Kauf nach der Verhandlung und Leihe); nicht bei eigenem Spieler (+[bp−0x11]) und nicht beim Kauf von einem anderen Manager (Rückfrage 0x4774E setzt [bp−0x12]) |

Beleg Zuschuss: FILE05 → FILE07 (`docs/formate/spielstand-original.md`): Gebot 400.000 DM, Zuschuss 220.000 DM = Faktor 55.

`[APPROX]`: Das Mittel a liest das Original über das Register `si`, das 0x058DC nicht setzt (Mauszeile des Transferbildschirms,
0x46AA1) – also beliebige Bytes der Vereinstabelle; das Remake nimmt (Kondition + Technik beider Teile) / 4. Ohne gezogene
Teamwerte (neues Spiel vor dem ersten Spiel) keine Wirkung. Der Zuschuss wird als Transfereinnahme gebucht (Original ohne
Buchungsaufruf); er lebt nur im Fenster (Aufrufer ohne Frage verwerfen ihn wie „Lieber nicht.“); Vertragslose wie Marktspieler; Titel der Frage = Name des Managers; nach
der Antwort zeigt das Remake weiter „Ihr Angebot wurde angenommen !“. Das Remake nimmt den Spieler zusätzlich in den Kader des
Computervereins auf (das Original kennt dort nur Teamwerte).

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
(+0x2C) führt das Remake seit Spur `kader-transfer` Teil 2 als `Player.PurchasePrice`. Computervereine, Leihen, Talente und Verlängerungen der Computervereine behalten die
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

**Befund aus Teil 3, umgesetzt in Teil 5:** Dasselbe Fenster zeigt bei einem Angebot für einen Spieler **auf dem Markt** (Bit
0x80, Aufruf 0x473FF mit Argument 0) unter „ABLÖSESUMME :“ Angebot − Angebot / 7 · Restlaufzeit (+0x0B) und schreibt diesen
Rest gut (0x49332–0x49371, danach 0x4748A). Der Listenwechsel 0x494EF zieht aus dem Pool von 200 Spielern (Teil 5, unten).

## Belegt und umgesetzt (Spur `transfer-vertraege-komplett`, Teil 5: Spielerpool, Listenwechsel, Ablöse)

Entscheidungen Nutzer 01.11.2026 (PR #308). Umsetzung: `Core/Transfer.Pool.cs` (`TransferPoolRules`, `PoolSlot`,
`TransferMarket.BuildPool`, `StartPoolSeason`, `ChangeList`), `Core/Transfer.cs` (`TransferListing.FromPool`, `TransferFee`,
`AdvanceDay`, `Initialize`, `StartSeason`), Angebotsfenster für Gebote `UI/Screens/Team/TransferDialog.cs`; Schema 20
(vorläufig, `Core/SaveGame.Migration.TransferVertraegeKomplett5.cs`); Tests `TransferVertraegeKomplettTeil5Tests`,
`TransferVertraegeKomplettTeil5ScreenTests`.

| Regel | Fundstelle | Inhalt |
|---|---|---|
| Pool zu Spielbeginn | 0x5AF50–0x5B0B1 (66C3:0730) | je Index 1–199: Alter +0x14 = random(18, 33); b = random(30, 89), unter 45 mit random(0, 1) ≠ 0 neu random(40, 89); +0x16 = b + 10 − random(0, 20); Form +0x17 = random(45, 55); +0x15 = b + 10 − random(0, 20); +0x19 = 0; Besitzer 6 |
| Talent, Namen | 0x5B2CD–0x5B387, 0x5AAA0 (66C3:0280) | je Index Talent +0x12 = random(0, 9); namenlose Plätze: Kaderplatz s = random(DGROUP 0x164[k], 0x165[k] − 1) (0, 2, 12, 25 nach Art, Grenzen DGROUP 0x382: 21, 97), Verein c = random(0, 11), nach einem doppelten Namen einmal random(0, 35) (Merker DGROUP 0x55EE), Managerverein verworfen; +0x1D = c |
| Art (Rollenwert +0x18) | 0x5B391–0x5B4A9 | 1–20 Torhüter, 21–96 Verteidiger 25 + random(0, 16), 97–179 Stürmer 75 + random(0, 16), 180–183 Torhüter, 184–190 Verteidiger, 191–199 Stürmer |
| Nationalspieler | 0x2A0B5 → 0x5ACA0 (0x140:0x48, Art 0) | Satz DGROUP 0x55F4 + 6 · Art: Art 0 Indizes 179–199, Vereine 52–63 (DGROUP 0x6AD … + 11), Wechsel der Kaderart bei 184/191; Art 1 Indizes 1–179, Vereine 0–23, Wechsel bei 21/97. Nur Besitzer 6 mit Verein ohne Manager; Verein random, Kaderplatz random der Art, mit random(0, 15) = 0 random(0, 1); doppelter Name (vergeben oder kleinerer Index) → neuer Versuch |
| Saisonwechsel | 0x274A7 (Saisonende, im Zug von Manager 0), 0x56467/0x56470 | Alter aller 200 Poolplätze + 1; neue Namen der freien Plätze (0x5ACA0 Art 0, dann Art 1); die Werte bleiben |
| Listenwechsel, Räumen | 0x494EF–0x49549, 0x425B6 (0x108:0x48) | Markt-Satz 4, 10 Plätze: je k = 0 … 9 Platz k frei; ein Platz eines Managers (Besitzer ≠ 4) bleibt und zählt, ein Markt-Platz wird entfernt, die folgenden rücken auf, k zählt weiter – also bleibt etwa jeder zweite; Poolspieler wieder Besitzer 6 |
| Listenwechsel, Füllen | 0x49566–0x4980F | n = 10 − Managerplätze; n-mal: cx = random(0, 9); Managerplatz → nichts; Markt-Platz → entfernen; Poolindex random(1, 179), mit random(0, 5) = 0 random(180, 199), bis Besitzer 6 (höchstens 1500 Versuche, sonst Ende); Form random(45, 55); Ersatzverein bei Managerverein: Liga nach DGROUP 0x7C0/0x7B1, random(0x7C6[2l], 0x7C7[2l]) bis ohne Manager (+0x1D bleibt); 0x4981E; neuer Platz am Ende (0x108:0x5C = 0x42CB1, Energie random(80, 120)), Besitzer 4, +0x1D, +0x2C = Wert(Preis), +0x28 = Wert(Gehalt), +0x19 = random(3, 12) · 16 + random(3, 12) |
| Grundstärken | 0x4981E–0x4991C | t = 1 bei Nationalteam (0x128:0x20 = 0x4D2F0); +0x15 = Vereinsfeld 0x19 + random(10t, 14) − 10, +0x16 = Vereinsfeld 0x1B + random(10t, 14) − 10, mit random(0, 4) = 0 + random(12, 20); je auf 10 … 99 (0x120:0x52) |
| Zeitpunkt | 0x43A9F, 0x54E65–0x54E7C | beim Saisonstart und im Tageslauf mit random(0, Managerzahl + 2) = 0 |
| Ablöse bei Markt-Angeboten | 0x49332–0x49371 → 0x4748A | Gutschrift Angebot − Angebot / 7 · Restlaufzeit (Drittel-DM); das Angebotsfenster zeigt den Rest als „ABLÖSESUMME“ |

Folgen: Die Startliste ist ein Listenwechsel ab leerem Markt – jeder der 10 Würfe trifft mit (10 − n) / 10 einen freien Platz,
im Mittel 6,5 Spieler (Video 6, FILE05 7). Poolspieler altern ohne Laufbahnende (nur 0x274A7), die Liste wird über die Jahre
älter – so bleibt es (Entscheidung Nutzer 01.11.2026: Originaltreue); eine Verjüngung beim Umbenennen ist als Erweiterung nach
v1.0 vorgemerkt (`docs/anforderungen/zusatzfunktionen.md`, Abschnitt C). Gegenprobe FILE05: freie Plätze 1–178 auf Vereinen 0–23 ohne Köln (über 90 % 0–11), 179–199 auf Nationalteams.

`[APPROX]`:
- **Computerkader bleiben** (Statistik, Kader-KI des Remakes), aber **jeder Name nur einmal** (Entscheidung Nutzer
  01.11.2026): Der Pool wird vor den Kadern gebaut (`TransferMarket.ReservePool`, aus `SquadSetup.Initialize`), seine
  Datenbankspieler fehlen den Computervereinen; fehlende Plätze ergänzt der Kaderaufbau. Beim Saisonwechsel tauschen Pool und
  Computerkader: Steht der neu gezogene Datenbankspieler in einem Computerkader, wird er Poolspieler (mit seinen Werten), der
  bisherige Poolspieler übernimmt seinen Kaderplatz. Spieler in Managerkadern, bei den Vertragslosen, im Ruhestand, geliehene
  und nominierte gelten als leere Plätze. Kaderplatz s = s-ter Datenbankspieler der Art (Platznummern leerer Plätze sind nach
  dem Laden der Datenbank nicht mehr bekannt). Spieler werden über ihre ID unterschieden; gleiche Namen bleiben möglich, wenn
  eine geänderte Datenbank sie enthält, und entstehen weiter durch Talentnamen (`PlayerFactory.TalentName`, unverändert).
- **Poolgröße flexibel** (Wunsch Nutzer 01.11.2026): `TransferPoolLayout` mit dem Original (200) und Aufteilung anderer
  Größen im Verhältnis des Originals (z. B. 400); Ziehung, Arten und Namensbereiche folgen der Aufteilung.
- **Managerkader** baut das Remake wie bisher aus der Datenbank, nicht aus dem Pool (DGROUP 0x381); alle 199 Plätze werden
  benannt (daher öfter der weite Vereinsbereich 0–35 als im Original). Gibt es keinen freien Datenbanknamen (synthetische
  Datenbank, Spielstand vor Teil 5), heißt der Platz „POOLSPIELER n“.
- **Gekaufte Poolspieler** bleiben „vergeben“; steht ihr Spieler beim Saisonwechsel nicht mehr in einem Managerkader, bekommt
  der Platz einen neuen Spieler „POOLSPIELER n“ mit den Werten des alten (im Original wird der Platz bei Verkauf oder
  Vertragsende wieder frei), der danach wie jeder freie Platz umbenannt wird. Ein Name ist im Remake fest: ein neuer Name ohne
  Tausch erzeugt einen neuen Spieler mit neuer ID (nur für Poolspieler ohne Spiele).
- **Takt:** seit Spur `kader-transfer`, Teil 4 täglich wie im Original (unten); Zufall aus der Hauptquelle.
- **Vertragslose** stehen nicht auf der Liste (R204, Runde 15 Spur kader Teil 3, R15-D4 = A): Vertragsende in einem Managerverein
  (Datei 0x27DE3–0x27E9D) gibt nur den Kaderplatz frei (0x425B6, ohne Pool +0x1A), der Listenwechsel zieht nur Besitzer 6 – Beleg
  FILE44 → FILE46 KJELLBERG (Pool +0x1A bleibt 0). `[APPROX]` Im Remake bleiben sie vertragslos (Kader-KI der Computervereine).
  Bis Runde 14 standen sie zusätzlich auf der Liste (höchstens 6).
- Sonderprüfung DGROUP 0x6AC (Startwert 56 = Deutschland) in 0x5ACA0 nicht abgebildet (FILE05 zeigt einen Poolspieler
  Deutschlands); Vereinsfelder aus Datenbankstärke und Abweichung des Laders, Nationalteams seit Teil 4 aus der Ländertabelle 0x4C4E (A209);
  Energie auf 99 begrenzt; 0x4907A (0x118:0x4D) seit Teil 4 abgebildet (Tendenz, Steuerbyte).
- Poolspieler sind für den WM-Kader nicht nominierbar (`NationalCandidates`, ihr Platz wird neu vergeben).

## Belegt und umgesetzt (Spur `kader-transfer`, Teil 1: Kadergröße, Kaufgrenze)

| Regel | Fundstelle em.exe | Umsetzung |
|---|---|---|
| Kader der Managervereine zu Spielbeginn nach **Start-Liga** (`[0x11A] · 4 + Gruppe`), nicht nach Level: Bundesliga 2/10/13, 2. Liga 2/9/11, Oberliga 2/8/10 (Tabelle DGROUP 0x169, Datei 0x16F39) | 0x29A3F | `SquadSetup.ManagedSquadSize(League)`; Liga des Vereins wie bei der Startbasis (`[APPROX]` je Manager) |
| Kauf und Leihe: „Kaderplatz anlegen“ (0x108:0x5C = 0x42CB1) mit Grenze 0x1D; bei mehr als 28 Spielern des Managers (Zählung 0xF8:0x75) „Schon 29 Mann / im Team.“ (DGROUP 0x127D) | 0x477CD, 0x42D1E–0x42D44 | `TransferMarket.MaxSquad` = 29 (vorher 22 aus der Meldung der Aufstellung); `[APPROX]` gezählt wird nur der eigene Kader (F089 offen) |
| Beleg Spielstände | FILE01/09/10 (Bundesliga) 25 Plätze, FILE12 (Oberliga) 20; FILE05 → FILE07 Kauf als 26. Spieler | `KaderLigaGroesseTests` |

## Belegt und umgesetzt (Spur `kader-transfer`, Teil 2: Spielerdaten im Modell)

Felder des Kaderplatzes (0x34 Byte, DGROUP 0x5DC3 + 0x618 · Manager) für das Spielerdaten-Fenster 0x041AC und die Hinweiszeile
0x41683; Inventur A360, A368, A369 (P2 Nr. 8). Umsetzung `Core/Player.Slot.cs`, Schema 23 (`SaveGame.Migration.KaderTransfer.cs`).

| Regel | Fundstelle em.exe | Umsetzung |
|---|---|---|
| Kaufpreis +0x2C (ohne Bit 31) bei Spielbeginn = Wert(Preis) je Kaderplatz des Managers, ohne Zufall | 0x29EB4 | `Player.PurchasePrice`, `Contracts.ValuePrice`, `ApplyStartSalaries` |
| Kauf und Leihe: +0x2C = angenommenes Gebot (bei der Leihe die Gebühr), Bit 31 bleibt; nur ohne Merker [bp−0x11] | 0x4781B–0x47833 | `TransferMarket.Complete` |
| Spielerdaten-Fenster Feld 0 = +0x2C, Feld 2 = Prämie +0x30 (0 → „keine“) | 0x041AC (Sprungtabelle 0xABD) | `PlayerDataWindow.DataValues`, `PlayerDataWindow.PurchasePrice` |
| Verletzungsart +0x17, Namen der 28 Arten (Zeigertabelle DGROUP 0xA00) | 0x05B1A, 0x41A69–0x41A86 | `Player.InjuryKind`, `InjuryKinds.Names` |
| Hinweiszeile „Verletzt.(Art)“: DGROUP 0xB84 + 0x50BE „(“ + Name + 0x50C0 „)“, ohne Leerzeichen | 0x41A2F–0x41AA4 | `PlayerHintLine.Status` |
| Sperre: Strafe setzt +0x0A = 0, +0x09 \| 1, **+0x0D = Strafcode & 7** (2 bzw. 5 Minuten); je Spielminute −1 (0x5488C, 0x5490C); Ende der Strafe +0x09 &amp; ~1, +0x0D = 0 (0x35795); nach dem Spiel löscht 0x551D4–0x551E0 Bit 0 bei +0x0D ≠ 0 | 0x35C02–0x35C0C | nicht umgesetzt (siehe unten) |
| Beleg Spielstände | FILE01/09/10: +0x2C = Wert(Preis) aller Managerplätze; FILE07 DAFFNER 400.000 DM; alle 13 Stände ohne Sperre, keiner mit Verletzung | `KaderTransferTeil2Tests` |

`[APPROX]` und Anschlüsse:

- **„Sperre in Minuten“ (A369) ist die Strafbank im laufenden Spiel**, keine Spielsperre: +0x0D teilen sich Verletzungswochen und
  Strafminuten. `Player.SuspendedGames` des Remakes (Spiele) setzt kein Ablauf; die Hinweiszeile zeigt es weiter als Minuten.
  Die Strafbank führt die Live-Spielberechnung (`Core/MatchEngine*.cs`, Spur `wettbewerbe-spiel`).
- **Anschluss `wettbewerbe-spiel`** (`Core/MatchEngine.Injuries.cs`, `PlayerInjuries.Inflict`): gezogene Art in
  `player.InjuryKind` schreiben (eine Zeile, 0x05B1A); bis dahin zeigt die Hinweiszeile „Verletzt.“ ohne Klammer.
- **Anschluss Leitstelle** (`UI/Screens/Statistics/PlayerStatisticsScreen.cs`, A360, R11P-03): Spalte Kaufpreis aus
  `Player.PurchasePrice` statt des Spielerwerts.
- **Anschluss Doku** (`docs/reverse-engineering.md`, Hinweiszeile „[APPROX]“): Verletzungsart umgesetzt; Sperre = Strafbank.
- Spielstände vor Schema 23: Kaufpreis 0 („-“), Art unbekannt. Computervereine tragen keinen Kaufpreis.
- Eigener Spieler auf dem Markt: das Original zeigt als Listenpreis das alte +0x2C (Kaderplatz kopiert); das Remake rechnet den Wert
  frisch (Teil 4).

## Belegt und umgesetzt (Spur `kader-transfer`, Teil 4: Transfermarkt-Takt)

Umsetzung: `TransferMarket.CatchUp`/`AdvanceDay` (`Core/Transfer.cs`), `GameState.TransferAccount` (`Core/Transfer.AccountBar.cs`),
Kontoleiste `TransferScreen.DrawAccountBar`; Tests `KaderTransferTeil4Tests`. Kein neues Spielstandsfeld.

| Regel | Fundstelle em.exe | Umsetzung |
|---|---|---|
| Listenwechsel mit random(0, Managerzahl + 2) = 0 **im Zug jedes Managers** an Tagen mit Ereignis (Tagesbyte ∉ {0, 9, 255}, 9 gilt als 0, Datei 0x54D4C–0x54D65), nicht in der WM-Schleife, nicht beim Saisonwechsel (Liste bleibt, R688-05) | 0x54E65–0x54E7C | `TransferMarket.ListChangeTurn` je Manager aus `MainPathListChanges` (#678; vorher einmal je Kalendertag); A256 |
| Angebot für eigene Spieler auf dem Markt im Tagesablauf, Gebotsverfall nur im Lauf von Manager 0, Markt-Verfall +0x13 > 0x38 → −10 | 0x072DD, 0x07215, 0x071BB (Tagesablauf 0x07169, Hauptweg nur am Zähler ≡ 0/4) | `TransferMarket.CatchUpManagerDay` aus `ManagerDailyRun` bzw. Schritt (c) der WM-Schleife (#678) |
| Bietender Verein: 0x059B8 mit Europapokal-Schalter 0 (Vereine 0 … 42 ohne Manager, Stärke nahe der Spielerstärke) | 0x07364 | `UnsolicitedOfferRules.Bidder` (vorher beliebiger Ligaverein); A257 |
| Kontoleiste: Kontostand, Schulden (0x57768), Anlagen (Aktien zum Kurs + Forderungen samt Sparbuch), Saldo = Konto + Anlagen − Schulden | 0x463CC | `GameState.TransferAccount`; A381, F109 |

`[APPROX]`: Zufall weiter aus der Hauptquelle (Abdrücke der Mehrsaisonproben neu); Höhe des Markt-Angebots weiter beim
Angebot statt beim Anklicken (0x47347); Bieter mit der Stärke des Spielers wie bei den unaufgeforderten Angeboten; Saldo je
Partner wie bei `TotalDebts` (Kredite zwischen zwei Managern verrechnet, Bank Sparbuch minus Kredit); Lage der Leiste unverändert.

**Rest von Teil 4** (A258, F113, A209; Tests `KaderTransferTeil4Tests`, `KaderTransferTeil4ScreenTests`, Schema 24
`SaveGame.Migration.KaderTransfer4.cs`):

| Regel | Fundstelle em.exe | Umsetzung |
|---|---|---|
| Gebot auf den Spieler eines Managers: erst Geld (0x474F0), dann Unfair-Grenze 60 … 140 % des Werts (0x47546–0x475DB), dann **Frage an den Verkäufer** über Meldung/Frage 0x4C246 (Stub 0x120:0x7A, Argument Besitzer Pool +0x1A): DGROUP 0xD98 „Nehmen Sie das Angebot (“ + Betrag + 0x14D5 „ DM)“, 0xD9C „für “ + Name + 0xDA0 „ an ?“, Knöpfe 0x12D5 „Na gut.“ / 0x12D9 „Nein !“ | 0x47655–0x47743 | `TransferMarket.Bid(…, out SellerQuestion?)` → `TransferResult.SellerAsked`, `AnswerSeller`; Fragefenster `TransferDialog.ForSeller`; A258 |
| „Na gut.“: angenommen, weiter wie beim Computermarkt (Kaderplatz 0x477E7, Verhandlung, Geld); „Nein !“: Meldung DGROUP 0x1209 und Sperre des Markt-Platzes für den Bieter (+0x1A \|= 0x80 \| Bit) | 0x4774A, 0x47AC8–0x47ADD | `AnswerSeller`; Sperre jetzt auch bei abgelehnten Geboten an Manager (vorher nur Computermarkt) |
| Nachbearbeitung eines neuen Kaderplatzes: Tendenz +0x0E = random(35, 65); Steuerbyte +0x14 = random(1, 13) \| random(1, 3) << 4 \| random(0, 1) << 7 | 0x4907A (aus 0x42CB1 an 0x42E6B, nach der Energie 0x42E4C) | `Player.RollTrend`, `TrendScore`, `TrendControl` beim Listenwechsel (`RollSlotFields`); F113 |
| Anzeige der Tendenz (Wert − 30) / 13 → „Fallend./Konstant./Steigend.“; Balken (Wert − 30) · 100 / 38; Eingang des Spielerwerts | 0x41AE0, 0x041AC, 0x4A0A7 | `Player.TrendOf`, `PlayerInfoPopup.BarValues`, `PlayerValue.Inputs` |
| Grundstärken des Listenwechsels aus Vereinsfeld 0x19/0x1B; bei Nationalteams (t = 1) setzt der Lader 0x2EF1B diese Felder aus der Ländertabelle DGROUP 0x4C4E (je −2), Deutschland ohne Bundestrainer neu gewürfelt (0x55506) | 0x4981E–0x4991C | `RollMarketStrengths` mit `GameState.NationalRatings` (vorher Datenbankstärke); A209 |

**Änderung der Tendenz je Takt** (umgesetzt mit #688, R688-07, Inventur A134; `GameState.AdvanceTrend` im Kaderteil von 0x07169): Im Tagesablauf 0x07169 je Kaderplatz des Managers
am Zug (Datei 0x7B29–0x7C04, nach der Heilung 0x7C12): t = +0x0E; mit random(0, 3) ≠ 0 wird t um (+0x14 >> 4) & 7 erhöht
(Bit 7 gesetzt) bzw. erniedrigt; dann zählt +0x14 & 0xF um eins herunter. Erreicht der Zähler 0, ruft 0x7BD4 erneut 0x4907A
(neuer Wert und neues Steuerbyte, t verfällt); sonst wird t auf 30 … 68 begrenzt (0x7BEA) und nach +0x0E geschrieben.

`[APPROX]`: Tendenz aus eigener Zufallsquelle `GameState.TrendRandom` (das Original nutzt seinen einzigen Generator; so behalten
Markt, Spiele und Mehrsaisonproben ihre Folgen); ein Kauf übernimmt Tendenz und Steuerbyte vom Markt-Platz (das Original legt
beim Kauf 0x477E7 einen neuen Kaderplatz an, würfelt also neu, und kopiert den Markt-Platz darüber nur bei [bp−0x11] ≠ 0 oder
einer Leihe, Datei 0x478CF–0x47907; Bedeutung von [bp−0x11] nicht geprüft), Startkader und ältere Spielstände ohne Wert (0 → Mitte der Stufe);
die Änderung je Takt (A134, seit #688) überspringt Plätze ohne Wert oder Steuerbyte (0). Die Frage an den Verkäufer zeigt das Fragefenster des Transferbildschirms mit dem Namen des
Verkäufers als Kopf, bis der Baustein 0x4C246 (Spur `ui-meldungen`) angeschlossen ist; ohne Oberfläche (`TransferMarket.Buy`)
bleibt die simulierte Antwort. Länderwerte aus der Aufstellung eines Bundestrainers (0x952C) gehen in die Grundstärken nicht ein.

## Belegt und umgesetzt (Spur `kader-transfer`, Teil 5: Saisonende, Poolumverteilung)

**Zähler am Saisonende (A226).** Das Saisonende 0x26F20 arbeitet in drei Schleifen über alle Manager: erst Prämien, Altern und
Herunterzählen (Datei 0x26FB0–0x279FA), dann die Saisonzähler aller 29 Kaderplätze auf 0 (Datei 0x27A0C–0x27AA8: +0x00, +0x01
Strafen, +0x03–+0x05 Tore, +0x06–+0x08 Saisonspiele), erst dann die Verhandlungen (ab Datei 0x27B64). Die Spiele beim jetzigen
Verein (+0x1C/+0x1E/+0x20, Gewicht 50/20/30 im Spielerwert) werden nur erhöht (Datei 0x41622, 0x5499E, zusammen mit +0x06) und
bei einem neuen Kaderplatz 0 (FILE07); am Saisonende bleiben sie stehen. Der Auf- und Abstieg liegt schon davor (Datei
0x56137–0x563AD, Aufruf 0x5645D). Remake: `Player.GamesForClub` (+0x1C) = Saisonspiele + Übertrag `ClubGamesCarry`; der Übertrag
wird vor dem Zurücksetzen der Zähler fortgeschrieben (`GameState.CarryGamesForClub`, `GameState.Integration.cs`) und beim Eintritt
in einen Kader (`PlayerFactory.Join`) auf −Saisonspiele gesetzt. Schema 25 (vorläufig). Die bisherige Annahme, das Original
verhandle vor dem Zurücksetzen und je Manager nach dessen Prämien, war falsch: Tore und Strafen sind auch im Original 0.

**Poolumverteilung (M-K7, M-K8; F002–F004, F013–F015).** `Core/Transfer.PoolDistribution.cs`.

| Schritt | Fundstelle | Regel |
|---|---|---|
| Soll je Liga | Datei 0x05521 (aus 0x057E3 und 0x07C7E) | k[l] = belegte Kaderplätze (0x4BE17) der Manager in Liga l = 0 … 2; je Platz 1 … 199 Drift von +0x15 und +0x16 (0x054D9); Plätze &lt; 180 mit Verein ≥ 64 sind „im Ausland“; random(5, 15)-mal Platz random(1, 179): bei mehr als 4 im Ausland bekommt ein gezogener Auslandsspieler einen Verein über 0x059B8 (Schalter 0) und der Zug zählt nicht; Spieler eines Managervereins und, solange mehr als 4 im Ausland sind, alle anderen werden verworfen; sonst neuer Verein über 0x059B8. Dann T = Σ k + Ausland, R = 179 − T, T = 0 → 1, s[l] = (100 · k[l] / T) · R / 100 (Langzahlen) |
| Drift | Datei 0x054D9 | Wert + random(1, 7) − 4, begrenzt 30 … 99 (0x4BD35) |
| Zielverein | Datei 0x07EF1 | Liga &gt; 2: random(64, 150); sonst Ligaanfang DGROUP 0x7C0[l] + DGROUP 0x48FE[z], z = DGROUP 0x48FB[l] (dann z + 1, nach 17 → 0; Startwert 0); Managerverein → nächstes z. 0x48FE = 0 0 1 1 2 2 3 3 4 4 5 6 7 8 9 10 11 |
| Spielstart | Datei 0x057E3 (Aufruf 0x43A83, nach 0x28165 und vor dem ersten Listenwechsel 0x43A9F) | für Platz 1 … 178 ohne Managerverein: für jede Liga q = 0 … 2 mit s[q] ≠ 0 Verein = Zielverein(q), s[q] − 1 – **ohne Abbruch**: ein Spieler zählt bei allen Ligen mit Rest und landet in der letzten |
| Saisonwechsel | Datei 0x07C7E (Aufruf 0x56631, nach 0x56467/0x56470) | s[3] = 0; Gruppen q = 0 … 3 (Indexbereiche DGROUP 0x7C6/0x7C7: 0–11, 12–23, 24–35, 36–42): je Platz 1 … 179 im Bereich s[q] − 1 (nicht unter 0), Besitzer &gt; 3 → Liste frei[q]; für l = 0 … 3 solange s[l] &gt; 0 (höchstens 1000 Durchgänge) aus jeder anderen Gruppe q zufällige Listenplätze (random(0, Länge − 1), bis 1000 Wiederholungen) mit Zielverein(l) |
| Vereinslisten | Datei 0x07F6B | Vereine einer Liga nach Stärke sortiert; Ergebnis ungenutzt (0x07EF1 liest das Argument nicht) – nicht abgebildet (F015) |

Beleg FILE01–FILE07 (Spielstart, Köln = Verein 2): 146 der 154 freien Ligaplätze stehen auf 1.-Liga-Vereinen ohne Köln,
0/1/3/4 mit 20/20/20/22 Spielern, 5 … 11 mit 9–10 (Test `Start_OriginalDatabase_MatchesFile05Distribution`); nur 8 Namen stehen
noch bei ihrem Datenbankverein.

`[APPROX]`: Gruppe einer Liga = Vereine mit dieser Liga, Gruppe 3 = Vereine ohne Liga unter 48, Platz in der Liga nach Vereinsindex
(das Original ordnet die Sätze beim Ligawechsel um, 0x35EDD); Verein 150 gibt es im Remake nicht (149); die Drift wirkt nur auf
freie Plätze (Markt- und Kaderspieler führen ihre Werte am Spieler, im Original im Kaderplatz), gezogen wird für jeden Platz; die
Managerkader stammen im Remake nicht aus dem Pool, k[l] zählt sie trotzdem (das Soll ist dadurch dasselbe, der Pool hat aber mehr
freie Spieler); Spielstart mit der Quelle des Kaderaufbaus, Saisonwechsel mit der Hauptquelle, im Modus „nur Weltkugel“ ohne
Saisonumverteilung (Tageslauf 0x54C9F fehlt dort, 0x54CEE); Schleifen ohne Grenze im Original enden nach 100.000 Versuchen.

## Belegt und umgesetzt (Spur `kader-transfer`, Teil 7: Ankündigungen der Kaderspieler)

Regeln und Fundstellen: `docs/analyse/re-nachtrag.md`, Abschnitt 1 (Tagesablauf 0x07169), Umsetzung U1–U3, U6; im Code
`SquadRules.RetirementRoll`, `SquadRules.OfferLimit`, `SquadRules.OfferedYears`, `GameState.AnnounceContractEnds`.

- **Ruhestand (A231, A232, A238):** täglich je Platz random(350, 450) &lt; Alter · c / 10 (c = 100, Torhüter 85, − (Technik −
  Kondition) / 2) → „… kündigt an, daß er seinen Vertrag nicht mehr verlängern wird.“ (Art 4), unabhängig von der Laufzeit, auch
  bei Leihspielern; das Bit bleibt über den Saisonwechsel, der Spieler hört bei Vertragsende auf.
- **Verlängerungsangebot (A229, A239):** nur ohne Leihe und bei Restlaufzeit 1, random(0, p) = 0 und random(0, 3) = 0 mit
  p = (60 · s + 40 · a) / 100 − p · e / 10; n = 2/3/4 mit 71/20/10 von 101; **keine Frist** – wer sich nicht meldet, kommt in die
  Verhandlung am Saisonende.
- **Absage (A230):** gibt es nicht; die 10 % `RefuseChance` entfallen (`ContractNotice.Refused` nur noch aus alten Spielständen).
- **Angebote der Computervereine (A136):** Bit 0x80 nur am Marktplatz – das Remake entsprach schon dem Original (Kommentar).

`[APPROX]`: „offene Meldung“ = schon gemeldet; Züge aus der Vertragsquelle `ContractRandom`; +0x1E/+0x20 fehlen in e.

## Belegt und umgesetzt (Spur `kader-transfer`, Runde 12, Teil 1: U4, U5)

- **Verfall offener Angebote (U4):** Verlängerungsangebot verfällt mit random(0, 5) = 0 je Tag (Datei 0x0767A): Meldung weg,
  Verhandlungssperre random(9, 17) Tage; ein neues Angebot ersetzt eine laufende Sperre (0x07951). Unaufgefordertes Angebot eines
  Vereins (Bit 0x40, `Transfer.Unsolicited.cs`) und Gebot am Marktplatz (Bit 0x80, `Transfer.cs`) verfallen mit random(0, 3) = 0 je
  Tag (Datei 0x07626 bzw. 0x07215). Der Wurf fällt nur bei einem offenen Angebot.
- **Laufbahnende ohne Managerkader (U5):** Alter > random(37, 42) (`SquadRules.CareerEnds`, Datei 0x2787A) für Computerspieler,
  Vertragslose und Poolplätze statt der Tabelle 33–37. Nachfolger (`PlayerFactory.CreateRenewal`): Alter random(18, 25),
  s = random(40, 92), Kondition und Technik je random(s − 5, s + 5); beim Computerverein im selben Kader (Art bleibt), beim
  Poolplatz im selben Platz (`GameState.RenewAgedPoolPlayer`).
- `[APPROX]`: Der Nachfolger hat einen neuen Namen und eine neue ID (das Original behält den Pooleintrag samt Namen); Form
  random(45, 60), Talent random(0, 9), Energie 80 sind ergänzt. Zufall der Verträge statt der Hauptquelle. Kader-KI der
  Computervereine (Verlängern, Auffüllen) bleibt eine Ergänzung des Remakes.
- Testhilfen `TestSeason.KeepSquad` und `KeepSquadHelper` verlängern nun auch Spieler, deren Angebot verfallen ist.

## Belegt und umgesetzt (Spur `kader-start`, Runde 14, Teil 2c: A380 – eigener Spieler auf der Transferliste)

| Was | Fundstelle | Regel |
|---|---|---|
| Aufsetzen | 0x46FDD–0x470DE | höchstens 3 Markt-Plätze mit Pool +0x1A = Manager (0x11F9), Pool +0x1A muss der Manager sein (0x11F1); Markt-Platz in Satz 4 (0x108:0x5C, Grenze 0x0A), alter Kaderplatz +0x0A = 0, Platz (0x34 Byte) unverändert kopiert (0x470C4), Kaderplatz frei (0x108:0x48): **der Spieler verlässt den Kader**, die Plätze rücken auf; Pool-Satz behält Besitzer und Verein (FILE40 → FILE41) |
| Zurückziehen | 0x47331 → 0x474E4 → 0x477A9 | Klick auf den eigenen Markt-Platz ohne Gebot (+0x09 ohne Bit 0x80): Kaderplatz anlegen (0x108:0x5C, Grenze 0x1D, nach Art einsortiert), ohne Verhandlung und Buchung zurückkopiert (0x47907), Markt-Platz frei (0x47954); mit Gebot das Angebotsfenster (0x473FF) |
| Gehalt | 0x3E8DC–0x3E99F | Monatssumme +0x28/+0x30 über die Kaderplätze **und** die Markt-Plätze mit Pool +0x1A = Manager: der Spieler auf der Liste kostet weiter (`FinanceRules.MonthlyWages(game, club)`) |
| Saisonende | 0x27547–0x27665 | nicht verkaufte eigene Spieler kehren in den Kader zurück, nach Art einsortiert, nicht eingesetzt (FILE44 → FILE46); das Remake gibt sie vor Altern und Herunterzählen zurück (`TransferMarket.ReturnListedPlayers`) |

`[APPROX]`: Mindestbesetzung je Art beim Aufsetzen (wie bisher, im Original nicht gefunden), Listenpreis aus dem Spielerwert nur
noch ohne Kaufpreis (Altstände; seit Teil 4 `[ORIGINAL]`, siehe unten), Klickfläche des eigenen Markt-Platzes wie Kauf/Leihe,
voller Kader am Saisonende (Spieler wird vertragslos). Altstände (Spieler vor A380 noch im Kader) verlieren beim Zurückziehen nur
den Markt-Platz. Anzeige (blauer Art-Buchstabe, Statuszeile „von <Manager>“) → `ui-rest`.

## Belegt und umgesetzt (Spur `kader-start`, Runde 14, Teil 4: Saisonende-Abgang)

Grundlage `docs/analyse/re-runde-14.md` Sitzung 4 (FILE44 → FILE46) und Review `docs/reviews/2026-11-08-re-dosbox-r14-2.md`
(R14RD2-03, -05).

| Was | Fundstelle | Regel |
|---|---|---|
| Listenpreis eigener Markt-Plätze | 0x470C4 | Markt-Platz = Kopie des Kaderplatzes, also Preis = +0x2C (`Player.PurchasePrice`): HOHENADL FILE40 316.000 DM → FILE41 Markt 316.000 DM, `file40_04` „316.000 DM (K)“, „105.333 DM (L)“ = Preis / 3; ohne Kaufpreis (Altstand) `[APPROX]` der Spielerwert |
| A115 Leihende | 0x27547–0x27665 | Pool +0x1A ≥ 4 (Leihe vom Markt) → +0x1A = 6, der Spieler ist frei (VALLIERE FILE44 → FILE46); Besitzer ein Manager → neuer Kaderplatz bei ihm (0x42CB1, nach Art) mit einer **Kopie des Leih-Kaderplatzes** (0x48:0x2BD4 alt → neu, R14RD2-03): Laufzeit heruntergezählt, Gehalt und Versicherung aus der Leihe; +0x0C = 0, +0x18 &= 0x7F. Laufzeit 0 → Verhandlung beim Verleiher (Schleife ab 0x27B64). `[APPROX]` Leihe zwischen Managern nur nach dem Code; vom Markt Geliehene werden Vertragslose (`TransferMarket.Release`) statt freier Poolplätze. Import (#688, R688S-02): Kaderplatz `+0x0C` = Vereinssatz + 0x80 → `OnLoan`/`Loan` (FILE44 VALLIERE `0xB9`) |
| A362 Liste der Verhandlungen | 0x27C9F, 0x27D15–0x27D70, 0x27F31, 0x423A2 | Liste einmal vor der Schleife gezeichnet, nach einem Abgang neu: offene Verträge „kein V.“ (DGROUP 0x510E) mit altem Gehalt (KJELLBERG 18.800 DM, Fenster 19.152 DM, `file46_3`); Abbild im Bildschirm (`ContractsScreen.SeasonEndRows`), altes Gehalt `GameState.SalaryBeforeRenewal` (kein Spielstandsfeld). Fenster (2 \| 29)–(166 \| 95), Kopfzeile y 19, Linien y 25/209 ab x 7 (`file46_3`, `file40_11`) |
| A088 Saisonzähler | 0x27A0C–0x27AA8 | Kaderplatz +0x00, +0x01, +0x03, +0x04, +0x06, +0x07 → 0, +0x1C–+0x26 und +0x02 bleiben (FILE44 → FILE46); +0x05/+0x08 nur nach dem Code |
| Zahlen | 0x27D1F–0x27D70, 0x27DE3–0x27E1C | Gehalt = Wert(Gehalt) · (147 − Alter) / 100 (16.800 · 114 / 100 = 19.152), Ablöse = Wert(Preis) / 2 (311.000 / 2 = 155.500), Prämie 100.000 DM = eine Einzelprämie |

Restlücken: Hintergrund der Verhandlungsliste (Vereinsname oben, Kachel rechts unten in `file46_3`) fehlt; Rahmenfarben des Fensters
im Bild (146 \| 178 \| 195) hell und (32 \| 48 \| 81) dunkel, Ecken in Füllfarbe, Schatten treppenförmig, Knopfschrift 1 Pixel links –
gemeinsamer Baustein `TeamUi.DialogFrame`/`DialogButton` (`ui-rest`); Spalte WERT 1 Pixel (Abstand Zahl – „DM“).

## Noch offen (`[APPROX]` im Code, weitere Teile)

- **Antwort des verkaufenden Managers:** seit Spur `kader-transfer`, Teil 4 gefragt (0x47655–0x47743); nur ohne Oberfläche
  (`TransferMarket.Buy`) nimmt das Remake ab dem geforderten Betrag an, darunter bis 85 % mit sinkender Wahrscheinlichkeit.
- **Bietender Verein** für Spieler auf dem Markt (Bit 0x80): seit Spur `kader-transfer`, Teil 4 nach 0x059B8 angeschlossen;
  Pflege der Liste 0x7C12 nicht belegt.
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

## Belegt und umgesetzt (Spur `kader-transfer`, Runde 12, Teil 2: U9, U15)

- **Liga-Spielplan (U9):** `Schedule.Rounds` nutzt bei 12 Vereinen die feste Tabelle aus em.exe (Datei 0x4E7C7, DGROUP 0x2DC,
  `docs/analyse/re-nachtrag.md` Abschnitt 4): Spieltag 1 = `ab cd ef gh ij kl`, Spieltag 12 = `ba dc fe hg ji lk` (Heimrecht
  getauscht, Runde 2 und 4), Runde 3 wie Runde 1. Buchstabe = Rang des Vereinsindex innerhalb der Liga `[APPROX]` (das Original nimmt
  den Listenplatz, der beim Auf-/Abstieg mit dem Vereinstausch wandert). Ligen mit anderer Vereinszahl und die Play-off-Runden
  behalten das Kreisverfahren. Die Mischung der Vereine in `NewGame.Create` bleibt nur, damit der Zufallsverbrauch gleich bleibt.
- **Vereinsplatz beim Start (U15), umgesetzt:** Der Zufallsverein der Start-Liga (Datei 0x29C78) ist nie ein Europapokal-Teilnehmer
  (Zeile 1, Datei 0x45CD0). `[APPROX]`: zu Spielbeginn nur der Deutsche Meister (Verein 2); weitere Einträge der Tabelle
  DGROUP 0x3B1 sind nicht ausgewertet. Fest gewählte Vereine: siehe U15-a unten.
- **U15-Rest, entschieden, Umsetzung Runde 13:** Das Original setzt den Manager *immer* in die Start-Liga (auch ein fest gewählter
  Verein wird dorthin getauscht, bei Bundesliga immer mit einem Zufallsverein) und kennt den Sonderfall „eine Saison“ (Datei
  0x29CFA). Das Remake wählt den Verein bisher fest oder zufällig; die Start-Liga-Regler gehört nur zur Zufallswahl. Entscheidung
  Nutzer **D7 = D** (Runde 12): wie im Original; dazu **D2 = A** (04.11.2026): „eine Saison“ nur mit Start-Liga Bundesliga (Sperre
  im `SetupScreen`). Umsetzung in Runde 13, Spur `kader-start` Teil 2 (nach `re-dosbox`, `docs/arbeitspakete.md`).
- **U15-a und U15-b umgesetzt (Runde 13, Spur `kader-start` Teil 2, L1, L13 = A):** `NewGame.PlaceInStartLeague`
  (Datei 0x29C3B–0x29CF2): Ein fest gewählter Verein bleibt nur, wenn die Start-Liga nicht die Bundesliga ist, er schon in ihr
  spielt und sein Platz ≤ 35 ist; sonst tauscht er mit einem Zufallsplatz der Start-Liga (nie der Verein eines früheren
  Managers, nie Platz 0: Der Europapokal-Filter 0x45CD0(r, 1) sieht beim Spielstart eine leere Tabelle, Runde 14). Die Teamwerte (`Club.Strength`) bleiben am Platz, der Verdrängte übernimmt Liga
  bzw. „ohne Liga“ des Managervereins; ein Verein ohne Liga landet direkt in der Start-Liga (A112 „rückt in die Oberliga“
  entfällt). Belege FILE27/FILE28/FILE15 (`docs/analyse/re-runde-13.md`), Tests `KaderStartStartLigaTests`. Die Sperre „eine
  Saison“ nur mit Bundesliga prüft `GameSetup.Step` (Datei 0x2890E–0x2891C). `[APPROX]`: Zufallsquelle `GameState.Random`;
  gezogen wird in Indexfolge, solange die Satzfolge erst nach der Vergabe entsteht (R-K5, Teil 3). U15-c umgesetzt (Runde 14, `kader-start` Teil 2, L2 aufgehoben): Ein Manager und eine Saison → der Managerverein
  tauscht auf Platz 0 der Bundesliga (Datei 0x29CFA–0x29D9C, FILE29; `NewGame.OneSeasonSingleManager`); das Managerfeld
  DGROUP 0x9744 = 1 ist nicht abgebildet (`docs/analyse/re-runde-14.md`).

## Probe-Rückfallweg (WS-05, Runde 12)

`TestSquadKeeper.KeepPlayable` meldet jetzt per `KeepReport`, wie viele Vertragslose gekauft (`Bought`), wie viele Spieler
ohne Kauf umgesetzt (`Moved`, Rückfallweg) wurden und ob die Transferliste leer war (`MarketEmpty`). `KaderTransferWs05Tests`
sichern ab, dass der Rückfallweg nur bei leerem Markt greift, und geben je Saison die Zahlen aus. **Befund:** In vier
Saisonwechseln (Seeds 3/21 synthetisch, 3/5 mit Originaldaten, passiv und aktiv) wurde der Rückfallweg nie benutzt
(gekauft 0, umgesetzt 0, Markt-leer 0): Die Kader des Managers bleiben dank Vertragsverlängerungen ≥ 18; ob zu Saisonbeginn
nach Abstieg Vertragslose verpflichtbar sind, ist damit in diesen Seeds nicht gefordert. Der frühere Seed-3-Fall der Probe
(Kader 12 nach Abstieg, Review Runde 11) wurde hier nicht nachgestellt; offen bleibt, ob er mit anderen Seeds oder einem
Abstieg des Managers wieder auftritt.
