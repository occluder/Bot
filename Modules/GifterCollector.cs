﻿using Bot.Models;
using MiniTwitch.Irc.Enums;
using MiniTwitch.Irc.Interfaces;
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
            await Postgres.ExecuteAsync(
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
                    GiftId = _encoder.Encode(notice.CommunityGiftId),
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
                        _ => 0
                    },
                    TimeSent = notice.SentTimestamp.ToUnixTimeSeconds()
                }, commandTimeout: 10
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error inserting gifter");
        }
        finally
        {
            _ = PostgresQueryLock.Release();
        }
    }

    private async ValueTask OnGiftedSubNotice(IGiftSubNotice notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
        {
            return;
        }

        if (notice.CommunityGiftId == 0)
        {
            _logger.Warning("Received sub gift notice with CommunityGiftId 0");
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
            await Postgres.ExecuteAsync(
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
                    GiftId = _encoder.Encode(notice.CommunityGiftId),
                    RecipientName = notice.Recipient.Name,
                    RecipientId = notice.Recipient.Id
                }, commandTimeout: 10
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error inserting recipient");
        }
        finally
        {
            PostgresQueryLock.Release();
        }
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
