namespace Bot.Models;

public record ItemMarket(MarketPayload payload);

public record MarketPayload(ItemOrder[] orders);

public record ItemOrder(
    DateTimeOffset creation_date,
    bool visible,
    uint quanitity,
    OrderOwner user,
    DateTimeOffset last_update,
    uint platinum,
    string platform,
    int quantity,
    string order_type,
    int modrank = 0
);

public record OrderOwner(
    int reputation,
    string locale,
    string? avatar,
    string ingame_name,
    DateTimeOffset last_seen,
    string id,
    string region,
    string status
);