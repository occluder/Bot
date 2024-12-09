﻿using System.Text.Json;
using Bot.Enums;
using Bot.Models;
using Bot.Services;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Query: ChatCommand
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public Query()
    {
        AddArgument(new("sql", 1, typeof(string)));
    }

    public override CommandInfo Info { get; } = new(
        "query",
        "Query the database",
        TimeSpan.Zero,
        CommandPermission.Whitelisted
    );

    public override async ValueTask Run(Privmsg message)
    {
        int firstSpace = message.Content.IndexOf(' ') + 1;
        string sql = message.Content[firstSpace..];
        await LiveConnectionLock.WaitAsync();
        try
        {
            IEnumerable<dynamic>? results = await LiveDbConnection.QueryAsync(sql, commandTimeout: 5);
            string serialized = JsonSerializer.Serialize(results, _options);
            if (serialized.Length > 450)
            {
                OneOf<string, Exception> uploadResult = await TextUploadService.UploadToHaste(serialized);
                if (!uploadResult.IsT0)
                    await message.ReplyWith($"{uploadResult.AsT1.GetType().Name}: {uploadResult.AsT1.Message}");
                else
                    await message.ReplyWith($"\ud83d\udcce {uploadResult.AsT0}");

                return;
            }

            await message.ReplyWith(serialized);
        }
        catch (Exception ex)
        {
            ForContext<Query>().Error(ex, "Query command failed");
            await message.ReplyWith($"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            LiveConnectionLock.Release();
        }
    }
}