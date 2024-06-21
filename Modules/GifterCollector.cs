using Bot.Models;
using Bot.Utils;
using MiniTwitch.Irc.Enums;
using MiniTwitch.Irc.Interfaces;
using Sqids;

namespace Bot.Modules;

internal class GifterCollector: BotModule
{
    static readonly ILogger _logger = ForContext<GifterCollector>();
    static readonly SqidsEncoder<ulong> _encoder = new();
    readonly BackgroundTimer _timer;

    public GifterCollector()
    {
        _timer = new(TimeSpan.FromMinutes(10), CommitOld, PostgresQueryLock);
    }

    private ValueTask OnGiftedSubNoticeIntro(IGiftSubNoticeIntro notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
        {
            return default;
        }

        var giftId = _encoder.Encode(notice.CommunityGiftId);
        _giftIds[notice.TmiSentTs] = giftId;
        _gifters[giftId] = new
        {
            GiftId = giftId,
            Username = notice.Author.Name,
            UserId = notice.Author.Id,
            Channel = notice.Channel.Name,
            ChannelId = notice.Channel.Id,
            GiftAmount = notice.GiftCount,
            Tier = notice.SubPlan switch
            {
                SubPlan.Tier1 => 1,
                SubPlan.Tier2 => 2,
                SubPlan.Tier3 => 3,
                _ => 0,
            },
            TimeSent = notice.SentTimestamp.ToUnixTimeSeconds(),
        };

        return default;
    }

    readonly List<Recipient> _recipients = [];
    readonly Dictionary<string, object> _gifters = [];
    readonly Dictionary<long, string> _giftIds = [];

    private ValueTask OnGiftedSubNotice(IGiftSubNotice notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
        {
            return default;
        }

        _logger.Verbose(
            "@{User} received a {Tier} sub to #{Channel} from @{Gifter}!",
            notice.Recipient.Name,
            notice.SubPlan,
            notice.Channel.Name,
            notice.Author.Name
        );

        if (notice.CommunityGiftId == 0)
        {
            var giftId = _encoder.Encode((ulong)notice.TmiSentTs);
            _giftIds[notice.TmiSentTs] = giftId;
            _gifters[giftId] = new
            {
                GiftId = giftId,
                Username = notice.Author.Name,
                UserId = notice.Author.Id,
                Channel = notice.Channel.Name,
                ChannelId = notice.Channel.Id,
                GiftAmount = 1,
                Tier = notice.SubPlan switch
                {
                    SubPlan.Tier1 => 1,
                    SubPlan.Tier2 => 2,
                    SubPlan.Tier3 => 3,
                    _ => 0,
                },
                TimeSent = notice.SentTimestamp.ToUnixTimeSeconds(),
            };
            _recipients.Add(new(giftId, notice.Recipient.Name, notice.Recipient.Id));

            return default;
        }

        var giftId2 = _encoder.Encode(notice.CommunityGiftId);
        _recipients.Add(new(giftId2, notice.Recipient.Name, notice.Recipient.Id));

        return default;
    }

    async Task CommitOld()
    {
        int inserted = 0;
        int removed = 0;
        foreach (var oldGift in _giftIds.Where(x => UnixMs() - x.Key > 60_000).ToArray())
        {
            _giftIds.Remove(oldGift.Key);
            inserted += await InsertGifter(_gifters[oldGift.Value]);
            _gifters.Remove(oldGift.Value);
            var recipients = _recipients.Where(x => x.GiftId == oldGift.Value).ToArray();
            inserted += await InsertRecipient(recipients);
            foreach (var recipient in recipients)
            {
                removed += _recipients.Remove(recipient) ? 1 : 0;
            }
        }

        _logger.Information("GiftCollector ran {Inserted} inserts, removed {Removed} from list", inserted, removed);
    }

    private static Task<int> InsertGifter(object obj)
    {
        return Postgres.ExecuteAsync(
            """
            insert into 
                sub_gifter 
            values (
                @GiftId,
                @Username, 
                @UserId, 
                @Channel, 
                @ChannelId, 
                @GiftAmount, 
                @Tier, 
                @TimeSent
            )
            """, obj, commandTimeout: 10
        );
    }

    private static Task<int> InsertRecipient(IReadOnlyCollection<object> objects)
    {
        return Postgres.ExecuteAsync(
            """
            insert into
                sub_recipient
            values (
                @GiftId,
                @RecipientName,
                @RecipientId
            )
            """, objects, commandTimeout: 10
        );
    }

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnGiftedSubNoticeIntro += OnGiftedSubNoticeIntro;
        AnonClient.OnGiftedSubNoticeIntro += OnGiftedSubNoticeIntro;
        MainClient.OnGiftedSubNotice += OnGiftedSubNotice;
        AnonClient.OnGiftedSubNotice += OnGiftedSubNotice;
        return default;
    }

    protected override ValueTask OnModuleDisabled()
    {
        MainClient.OnGiftedSubNoticeIntro -= OnGiftedSubNoticeIntro;
        AnonClient.OnGiftedSubNoticeIntro -= OnGiftedSubNoticeIntro;
        MainClient.OnGiftedSubNotice -= OnGiftedSubNotice;
        AnonClient.OnGiftedSubNotice -= OnGiftedSubNotice;
        return default;
    }

    record Recipient(string GiftId, string RecipientName, long RecipientId);
}
