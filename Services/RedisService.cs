using StackExchange.Redis;

public class RedisService
{
    private static IDatabase _db;

    public RedisService(IDatabase db)
    {
        _db = db;
    }

    public IDatabase Database => _db;
}