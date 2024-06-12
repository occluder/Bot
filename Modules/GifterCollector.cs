using Bot.Models;
using MiniTwitch.Irc.Enums;
using MiniTwitch.Irc.Interfaces;
using MiniTwitch.Irc.Models;
using Sqids;

namespace Bot.Modules;

internal class GifterCollector: BotModule
{
    private static readonly ILogger _logger = ForContext<GifterCollector>();
    private static readonly SqidsEncoder<ulong> _encoder = new();

    private async ValueTask OnGiftedSubNoticeIntro(IGiftSubNoticeIntro notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
        {
            return;
        }

        if (notice.CommunityGiftId == 0)
        {
            _logger.Warning("Received sub gift intro notice with CommunityGiftId 0");
            return;
        }

        _logger.Debug("@{User} gifted {Amount} {Tier} subs to #{Channel}!",
            notice.Author.Name, notice.GiftCount, notice.SubPlan, notice.Channel.Name);

        await PostgresQueryLock.WaitAsync();
        try
        {
            await InsertGifter(
                notice.CommunityGiftId,
                notice.Author,
                notice.Channel,
                notice.TotalGiftCount,
                notice.SubPlan,
                notice.SentTimestamp.ToUnixTimeSeconds()
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to insert gifter");
        }
        finally
        {
            PostgresQueryLock.Release();
        }
    }

    private async ValueTask OnGiftedSubNotice(IGiftSubNotice notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
        {
            return;
        }

        _logger.Verbose(
            "@{User} received a {Tier} sub to #{Channel} from @{Gifter}!",
            notice.Recipient.Name,
            notice.SubPlan,
            notice.Channel.Name,
            notice.Author.Name
        );

        await PostgresQueryLock.WaitAsync();
        try
        {
            if (notice.CommunityGiftId == 0)
            {
                ulong newId = (ulong)Unix();
                await InsertGifter(
                    newId,
                    notice.Author,
                    notice.Channel,
                    1,
                    notice.SubPlan,
                    notice.SentTimestamp.ToUnixTimeSeconds()
                );

                await InsertRecipient(newId, notice.Recipient);
                return;
            }

            await InsertRecipient(notice.CommunityGiftId, notice.Recipient);
            // Throttle to 20 inserts/s
            await Task.Delay(50);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to insert recipient/gifter");
        }
        finally
        {
            PostgresQueryLock.Release();
        }
    }

    private static Task<int> InsertGifter(
        ulong giftId,
        MessageAuthor author,
        IBasicChannel channel,
        int giftAmount,
        SubPlan tier,
        long timeSent
    )
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
            """,
            new
            {
                GiftId = _encoder.Encode(giftId),
                Username = author.Name,
                UserId = author.Id,
                Channel = channel.Name,
                ChannelId = channel.Id,
                GiftAmount = giftAmount,
                Tier = tier switch
                {
                    SubPlan.Tier1 => 1,
                    SubPlan.Tier2 => 2,
                    SubPlan.Tier3 => 3,
                    _ => 0,
                },
                TimeSent = timeSent
            }, commandTimeout: 10
        );
    }

    private static Task<int> InsertRecipient(ulong giftId, IGiftSubRecipient recipient)
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
            """,
            new
            {
                GiftId = _encoder.Encode(giftId),
                RecipientName = recipient.Name,
                RecipientId = recipient.Id
            }, commandTimeout: 10
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
}
