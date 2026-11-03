namespace EishockeyManager.Core;

/// <summary>Art einer Nachricht im Posteingang („Neuer“).</summary>
public enum MessageKind
{
    /// <summary>Reine Meldung.</summary>
    Info,

    /// <summary>
    /// „Möchten Sie den Vertrag mit … verlängern ?“ – Ja/Nein. Nur noch in Spielständen vor Schema 7: Seit #133 kommt die
    /// Frage im Werbebildschirm (<see cref="GameState.OpenRenewal"/>), der Posteingang meldet nur den Verhandlungstisch.
    /// </summary>
    SponsorRenewal,

    /// <summary>Kreditangebot eines anderen Managers – Annehmen/Ablehnen, verfällt nach einer Woche.</summary>
    LoanRequest,
}

/// <summary>Nachricht an einen Manager. Texte nach em.exe, je Zeile höchstens eine Zeile der Nachrichtenleiste.</summary>
public sealed class Message
{
    public required MessageKind Kind { get; init; }
    public required int Day { get; init; }
    /// <summary>Saison der Erstellung; zusammen mit Day ein Zeitpunkt auf der fortlaufenden Zeitleiste.</summary>
    public int Season { get; internal set; }
    public required string[] Lines { get; init; }

    /// <summary>
    /// [ORIGINAL] Nachrichten ohne Spieler (Art Bit 3, Datei 0x4B56A) tragen im oberen Halbbyte von <c>+0</c> eine Restlaufzeit,
    /// die der Tageslauf herunterzählt (Datei 0x54F07–0x54F87): bei 0 ist der Platz frei. Anfangswert 3 (<c>or 0x30</c>).
    /// Fragen (<see cref="IsQuestion"/>) und Nachrichten zu einem Spieler (<see cref="Player"/>) altern nicht.
    /// Das Feld „gelesen“ je Nachricht gibt es im Original nicht (Entscheidung Nutzer D8 = A, #537).
    /// </summary>
    public int DaysLeft { get; internal set; } = GameState.MessageLifetimeDays;

    /// <summary>
    /// [ORIGINAL] Poolspieler der Nachricht (Satz <c>+2</c>, Pool <c>+0x1E</c> = „offene Meldung“, Datei 0x4B2F6): Angebote
    /// („bietet Ihnen an“, „ist an … interessiert“). Solche Nachrichten bleiben, bis das Angebot erledigt ist
    /// (<see cref="GameState.RemoveMessagesOf"/>, Datei 0x4B270).
    /// </summary>
    public Player? Player { get; internal set; }

    /// <summary>Erledigt (beantwortet oder verfallen); erledigte Fragen bleiben als Meldung stehen.</summary>
    public bool Done { get; set; }

    /// <summary>Verlängerung (nur Spielstände vor Schema 7): der abgelaufene Vertrag.</summary>
    public SponsorContract? Contract { get; init; }

    /// <summary>Kreditwunsch: wer leihen möchte und wie viel.</summary>
    public int Borrower { get; init; }
    public long Amount { get; init; }

    public bool IsQuestion => Kind != MessageKind.Info && !Done;
}

public sealed partial class ManagerFinance
{
    /// <summary>Posteingang, neueste Nachricht zuletzt.</summary>
    public List<Message> Inbox { get; } = [];
}

public sealed partial class GameState
{
    /// <summary>Tage, nach denen eine unbeantwortete Kreditanfrage verfällt.</summary>
    public const int LoanRequestDays = 7;

    /// <summary>
    /// [ORIGINAL] Plätze des gemeinsamen Nachrichtenspeichers aller Manager (Datei 0x4B101: Sätze 0 … 23 zu 0x71 Byte,
    /// DGROUP 0xD294). Ist er voll, entfällt die neue Nachricht (Datei 0x4B544: Rückgabe 0, kein Kennzeichen).
    /// </summary>
    public const int MessageCapacity = 24;

    /// <summary>[ORIGINAL] Tage, die eine Nachricht ohne Spieler liegen bleibt (Anfangswert des oberen Halbbytes 3, Datei 0x4B56A).</summary>
    public const int MessageLifetimeDays = 3;

    public IReadOnlyList<Message> Inbox(Manager manager) => Finance(manager).Inbox;

    /// <summary>Belegte Plätze des gemeinsamen Nachrichtenspeichers.</summary>
    public int MessagesStored => Managers.Sum(m => Finance(m).Inbox.Count);

    /// <summary>Legt eine Meldung ab; null, wenn der Nachrichtenspeicher voll ist (<see cref="MessageCapacity"/>).</summary>
    public Message? Notify(Manager manager, params string[] lines) => Post(manager, new Message { Kind = MessageKind.Info, Day = Day, Lines = lines });

    /// <summary>Meldung zu einem Poolspieler (Angebot): bleibt, bis <see cref="RemoveMessagesOf"/> sie löscht.</summary>
    public Message? NotifyAbout(Manager manager, Player player, params string[] lines) =>
        Post(manager, new Message { Kind = MessageKind.Info, Day = Day, Lines = lines, Player = player });

    private Message? Post(Manager manager, Message message)
    {
        if (MessagesStored >= MessageCapacity)
            return null;
        message.Season = Season;
        Finance(manager).Inbox.Add(message);
        manager.MarkNewMessage(); // [ORIGINAL] Kennzeichen „neue Nachricht“ DGROUP 0x5A50 + Manager (0x4B5E6, Spur ui-meldungen Teil 4)
        return message;
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x4B270 (Aufrufer 0x07169, 0x26F20, 0x425B6, 0x465F1, 0x4A1DB): löscht die offene Meldung eines
    /// Spielers (Verkauf, Vertrag, Saisonende, Verfall des Angebots).
    /// </summary>
    public void RemoveMessagesOf(Player player)
    {
        foreach (var manager in Managers)
            Finance(manager).Inbox.RemoveAll(m => ReferenceEquals(m.Player, player));
    }

    /// <summary>
    /// Kreditanfrage an einen Mitmanager: er bekommt sie in den Posteingang (verfällt nach einer Woche).
    /// Nur für den späteren Netzwerkmodus (Entscheidung Nutzer E2, docs/analyse/re-geld.md); im lokalen Spiel holt der
    /// Bildschirm Finanzen das Geld wie im Original sofort (<see cref="BorrowFromManager"/>, em.exe Datei 0x59269 ff.).
    /// </summary>
    public Message? RequestLoan(int borrower, int lender, long amount)
    {
        if (amount <= 0 || borrower == lender)
            return null;
        // [APPROX] Erweiterung Netzwerkmodus (E2), im Original gibt es keine Anfrage; Wortlaut aus „Geld an … verleihen (2.5%).“ gebildet.
        return Post(Managers[lender], new Message
        {
            Kind = MessageKind.LoanRequest,
            Day = Day,
            Lines = [$"{Managers[borrower].Name} möchte sich {FormatMoney(amount)} leihen.", "Geld verleihen (2.5%) ?"],
            Borrower = borrower,
            Amount = amount,
        });
    }

    /// <summary>Beantwortet eine Frage im Posteingang. Liefert false, wenn es nichts (mehr) zu beantworten gibt.</summary>
    public bool Answer(Manager manager, Message message, bool yes)
    {
        if (!Managers.Contains(manager) || !Finance(manager).Inbox.Contains(message) || !message.IsQuestion)
            return false;
        if (MessageExpired(message, Math.Max(Day, FinanceDay)))
        {
            message.Done = true;
            return false;
        }
        message.Done = true;
        if (!yes)
            return true;
        switch (message.Kind)
        {
            case MessageKind.SponsorRenewal when message.Contract is { } old:
                var ads = Finance(manager).Sponsoring;
                var free = old.Kind != AdSlotKind.Bande || old.Boards.All(b => ads.ContractAt(new AdSlot(AdSlotKind.Bande, b)) is null);
                if (free && !ads.Contracts.Any(c => c.Kind == old.Kind && old.Kind != AdSlotKind.Bande))
                    ads.Contracts.Add(new SponsorContract
                    {
                        Kind = old.Kind, Sponsor = old.Sponsor, Boards = old.Boards,
                        MonthlyAmount = old.MonthlyAmount, MonthsLeft = old.OptionMonths > 0 ? old.OptionMonths : 12, // Verlängerungsoption des Vertrags (Spur finanzen-2)
                    });
                break;
            case MessageKind.LoanRequest:
                LendBetweenManagers(IndexOf(manager), message.Borrower, message.Amount);
                break;
        }
        return true;
    }

    /// <summary>Täglich: unbeantwortete Kreditanfragen verfallen nach einer Woche.</summary>
    private void ExpireMessages()
    {
        foreach (var manager in Managers)
        {
            var inbox = Finance(manager).Inbox;
            foreach (var m in inbox)
                if (!m.Done && MessageExpired(m, FinanceDay))
                    m.Done = true;
            // [APPROX] Das Original löscht die offene Meldung an den Stellen, die das Angebot beenden (Verkauf, Verfall, Saisonende,
            // Datei 0x4B270); das Remake setzt Angebote an mehreren Stellen zurück und räumt hier täglich nach.
            inbox.RemoveAll(m => m.Player is { } p && p.UnsolicitedOffer is null && p.Notice != ContractNotice.Offered);
            // [ORIGINAL] Tageslauf Datei 0x54F07–0x54F87: Meldungen ohne Spieler zählen herunter, bei 0 wird der Platz frei.
            foreach (var m in inbox.Where(m => m.Player is null && !m.IsQuestion))
                m.DaysLeft--;
            inbox.RemoveAll(m => m.Player is null && !m.IsQuestion && m.DaysLeft <= 0);
            SortInbox(inbox);
        }
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x4B127 (Aufruf Datei 0x54EA9, Tageslauf): Sortierung nach dem Schlüssel <c>+1</c> = (Vorrang &lt;&lt; 8) |
    /// min(Tageszähler DGROUP 0x3B7, 255), absteigend. Das Remake führt den Posteingang mit der neuesten Nachricht zuletzt (Anzeige
    /// kehrt um); der Schlüssel ist daher aufsteigend nach (Saison, Tag). [APPROX] Vorrang 0 (Aufrufer 0x7169/0x26F20 nicht
    /// ausgewertet); die Saison gehört dazu, weil das Remake den Tageszähler nicht zurücksetzt und Nachrichten saisonübergreifend liegen.
    /// Die Sortierung ist stabil: gleiche Schlüssel behalten ihre Reihenfolge.
    /// </summary>
    internal static void SortInbox(List<Message> inbox)
    {
        var sorted = inbox.OrderBy(m => m.Season).ThenBy(m => Math.Min(m.Day, 255)).ToList();
        for (var i = 0; i < sorted.Count; i++)
            inbox[i] = sorted[i];
    }

    private bool MessageExpired(Message message, int day) => message.Kind == MessageKind.LoanRequest
        && (long)(Season - message.Season) * SeasonCalendar.Length + day - message.Day >= LoanRequestDays;

    private void ConstructionCompleted(Manager manager, StadiumConstruction construction) =>
        Notify(manager, $"Der Stadion-Ausbau ({construction.Label})", "ist abgeschlossen.");
}
