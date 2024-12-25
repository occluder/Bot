using System.Collections.Concurrent;
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

        await LiveConnectionLock.WaitAsync();
        try
        {
            await InsertGifter(
                notice.CommunityGiftId,
                notice.Author,
                notice.Channel,
                notice.GiftCount,
                notice.SubPlan,
                notice.SentTimestamp.ToUnixTimeSeconds()
            );

            var recipients = _recipients
                .Where(x => TimeSpan.FromMilliseconds(UnixMs() - x.Key) >= TimeSpan.FromMinutes(5))
                .Select(x => new
                {
                    GiftId = _encoder.Encode(x.Value.CommunityGiftId),
                    RecipientName = x.Value.Recipient.Name,
                    RecipientId = x.Value.Recipient.Id,
                    x.Key,
                })
                .ToArray();

            if (recipients.Length == 0)
            {
                return;
            }

            foreach (var recipient in recipients)
            {
                _recipients.TryRemove(recipient.Key, out _);
            }
            await InsertRecipient(recipients);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to insert gifter: {@GifterNotice}", notice);
        }
        finally
        {
            LiveConnectionLock.Release();
        }
    }

    readonly ConcurrentDictionary<long, IGiftSubNotice> _recipients = [];

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

        if (notice.CommunityGiftId != 0)
        {
            _recipients[notice.TmiSentTs] = notice;
            return;
        }

        ulong newId = (ulong)notice.TmiSentTs;
        await LiveConnectionLock.WaitAsync();
        try
        {
            await InsertGifter(
                newId,
                notice.Author,
                notice.Channel,
                1,
                notice.SubPlan,
                notice.SentTimestamp.ToUnixTimeSeconds()
            );

            await InsertRecipient([
                new
                    {
                        GiftId = _encoder.Encode(newId),
                        RecipientName = notice.Recipient.Name,
                        RecipientId = notice.Recipient.Id
                    }
            ]);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error inserting something");
        }
        finally
        {
            LiveConnectionLock.Release();
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
        var giftIdEncoded = _encoder.Encode(giftId);
        _logger.Debug("Gift ({Count}): {Id}", giftAmount, giftIdEncoded);
        return LiveDbConnection.ExecuteAsync(
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
            ) on conflict(gift_id) do update set
                gift_amount = sub_gifter.gift_amount + excluded.gift_amount
            """,
            new
            {
                GiftId = giftIdEncoded,
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

    private static Task<int> InsertRecipient(object[] objects)
    {
        return LiveDbConnection.ExecuteAsync(
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
}