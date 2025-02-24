namespace Bot.Models;

public record ItemMarket(string apiVersion, ItemOrder[] data);

public record ItemOrder(
    DateTimeOffset createdAt,
    bool visible,
    uint quanitity,
    OrderOwner user,
    DateTimeOffset updatedAt,
    uint platinum,
    string platform,
    int quantity,
    string type,
    int modrank = 0
);

public record OrderOwner(
    int reputation,
    string locale,
    string ingameMame,
    DateTimeOffset lastSeen,
    string id,
    string status
);