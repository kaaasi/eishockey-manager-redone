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
    public bool Read { get; set; }

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

    public IReadOnlyList<Message> Inbox(Manager manager) => Finance(manager).Inbox;

    public Message Notify(Manager manager, params string[] lines) => Post(manager, new Message { Kind = MessageKind.Info, Day = Day, Lines = lines });

    private Message Post(Manager manager, Message message)
    {
        message.Season = Season;
        Finance(manager).Inbox.Add(message);
        manager.MarkNewMessage(); // [ORIGINAL] Kennzeichen „neue Nachricht“ DGROUP 0x5A50 + Manager (0x4B5E6, Spur ui-meldungen Teil 4)
        return message;
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
        message.Read = true;
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
            foreach (var m in Finance(manager).Inbox)
                if (!m.Done && MessageExpired(m, FinanceDay))
                    m.Done = true;
    }

    private bool MessageExpired(Message message, int day) => message.Kind == MessageKind.LoanRequest
        && (long)(Season - message.Season) * SeasonCalendar.Length + day - message.Day >= LoanRequestDays;

    private void ConstructionCompleted(Manager manager, StadiumConstruction construction) =>
        Notify(manager, $"Der Stadion-Ausbau ({construction.Label})", "ist abgeschlossen.");
}
