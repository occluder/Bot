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

        await PostgresQueryLock.WaitAsync();
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

            await InsertRecipient(
                _recipients.Where(x => TimeSpan.FromMilliseconds(UnixMs() - x.Key) >= TimeSpan.FromMinutes(5))
                .Select(x => new
                {
                    GiftId = _encoder.Encode(x.Value.CommunityGiftId),
                    RecipientName = x.Value.Recipient.Name,
                    RecipientId = x.Value.Recipient.Id,
                })
                .ToArray()
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

    readonly Dictionary<long, IGiftSubNotice> _recipients = [];

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

        if (notice.CommunityGiftId == 0)
        {
            ulong newId = (ulong)notice.TmiSentTs;
            await PostgresQueryLock.WaitAsync();
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
                PostgresQueryLock.Release();
            }

            return;
        }

        _recipients[notice.TmiSentTs] = notice;
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
        _logger.Information("Gift ({Count}): {Id}", giftAmount, giftIdEncoded);
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
}