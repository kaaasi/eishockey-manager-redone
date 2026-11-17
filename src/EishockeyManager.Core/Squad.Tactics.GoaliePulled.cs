namespace EishockeyManager.Core;

/// <summary>
/// A838 (Spur wettbewerbe-regeln): Vereinsfeld 0x2C Bit 3 „Torhüter draußen“ zwischen den Spielen.
/// </summary>
/// <remarks>
/// [ORIGINAL] Bit 3 bleibt nach dem Spiel stehen (gesetzt im Spiel von 0x80CB, Datei 0x82AF/0x8342, und der Neuzählung
/// 0x38A36, Datei 0x38B6F; gelöscht ebenda und in der Spielvorbereitung 0x8365, Datei 0x844E/0x84C7). Der Spieltag 0x36B5D ruft
/// vor den Vorbereitungen (Datei 0x36C4F vor 0x36D12) 0x84D7(0) für alle Vereine: mit gesetztem Bit 3 Abwehrreihe 0x2E = 2
/// (Datei 0x855D–0x857D), für jeden Verein, nicht nur Computervereine (Abfrage nicht an 0x16 gebunden). Ein Verein, dessen
/// letztes Spiel mit herausgenommenem Torhüter endete, beginnt sein nächstes Spiel also mit Abwehrreihe 2; danach löscht 0x8365
/// das Bit. [APPROX] Das Remake setzt die Reihe beim nächsten Spiel des Vereins (das Original an jedem Spieltag bis dahin,
/// gleichwertig für das Spiel); HOK-Import und -Export führen das Bit nicht (Dateien anderer Spuren).
/// </remarks>
public sealed partial class Tactics
{
    /// <summary>Vereinsfeld 0x2C Bit 3 am Ende des letzten Spiels.</summary>
    private bool _goaliePulledAtEnd;

    /// <summary>Torhüter am Ende des letzten Spiels draußen (Vereinsfeld 0x2C Bit 3).</summary>
    public bool GoaliePulledAtEnd => _goaliePulledAtEnd;

    /// <summary>Hält Bit 3 am Ende eines Spiels fest.</summary>
    internal void RecordGoaliePulled(bool pulled) => _goaliePulledAtEnd = pulled;

    /// <summary>
    /// Spieltagsbeginn 0x84D7(0) und Vorbereitung 0x8365 für das nächste Spiel des Vereins: mit gesetztem Bit 3 Abwehrreihe 2,
    /// danach Bit 3 gelöscht (siehe Klasse).
    /// </summary>
    internal void StartMatchAfterPulledGoalie()
    {
        if (!_goaliePulledAtEnd)
            return;
        RecordMatchLines(2, MatchAttackLine);
        _goaliePulledAtEnd = false;
    }
}
