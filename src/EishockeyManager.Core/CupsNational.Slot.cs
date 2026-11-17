namespace EishockeyManager.Core;

/// <summary>Spieler – Kaderplatz im Satz 5 (WM-Kader des Bundestrainers; Spur national Teil 3, A412).</summary>
public sealed partial class Player
{
    /// <summary>
    /// Kopie für einen neuen Kaderplatz im Satz 5 (ohne ID, siehe <see cref="GameState.NewNationalSlot"/>). [ORIGINAL] 0x42CB1 leert
    /// den Platz (0x34 Byte, Datei 0x42E3A) und schreibt Poolindex, Kondition, Technik und Form aus dem Pool +0x15–0x17 (Datei
    /// 0x42EC3–0x42F06; die Form des Pools, A066), Spiele +0x06 = Pool +0x1C (Ligaspiele der Saison, Datei 0x42EAE–0x42EB5);
    /// 0x3CABF setzt danach Tore +0x03–0x05 = 0 (Datei 0x3CB8C–0x3CBCE) und den Vertrag +0x0B = 1 (Datei 0x3CB4A). Verletzung,
    /// Sperre, Strafen, Spiele in Pokal/Europapokal und die Zähler beim jetzigen Verein beginnen leer, Leihe gibt es nicht (+0x0C =
    /// Verein + 0x80 merkt den Verein, <see cref="NationalSquadMember.Club"/>).
    /// [APPROX] Name, Alter, Talent, Rolle und versteckte Werte führt das Remake am Platz mit (im Original Pool bzw. +0x19 = 0x77,
    /// das <see cref="HiddenSkills.ForMatch"/> für das Nationalteam ohnehin nimmt).
    /// </summary>
    internal Player CopyForNationalSlot()
    {
        var slot = (Player)MemberwiseClone();
        slot.Id = 0;
        slot.Loan = null;
        slot.Form = PoolForm ?? Form;
        slot.PoolForm = null;
        slot.InjuredGames = 0;
        slot.SuspendedGames = 0;
        slot.InjuryKind = InjuryKinds.Unknown;
        slot.Games = SeasonGames(CompetitionSlot.League);
        slot.CupGames = slot.EuropeGames = 0;
        slot.Goals = slot.CupGoals = slot.EuropeGoals = 0;
        slot.Penalties2 = slot.Penalties5 = 0;
        slot.StartClubCounters();
        slot.ContractYears = 1;
        return slot;
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Neuer Kaderplatz des Satzes 5 für einen Kandidaten (siehe <see cref="NationalSquadMember"/>, A412). [ORIGINAL] Würfe von
    /// 0x42CB1 in dieser Reihenfolge: Belastung random(80, 120) (Datei 0x42E4C–0x42E5E), Tendenz 0x4907A (Datei 0x42E6B), mit
    /// +0x0C = 0x63 (Aufruf Datei 0x3CAFF) der Gehaltswurf random(90, 115) (Datei 0x42F1F–0x42F34). [APPROX] Das Gehalt des Platzes
    /// (0x49D36) führt das Remake nicht; der Wurf wird verworfen.
    /// </summary>
    internal Player NewNationalSlot(Player source)
    {
        var slot = source.CopyForNationalSlot();
        AssignPlayerId(slot);
        var random = Random;
        slot.Strain = TransferRules.Random16(random, PlayerFactory.StartStrainMin, PlayerFactory.StartStrainMax);
        slot.RollTrend(random);
        _ = TransferRules.Random16(random, 90, 115);
        return slot;
    }
}
