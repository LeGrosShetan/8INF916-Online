using StackExchange.Redis;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

public class GameServerService
{
    private readonly IDatabase _db;
    private const string ServerKeySet = "gameServers"; // Redis set to store all game server keys

    public GameServerService(IDatabase database)
    {
        _db = database;
    }
    
    public async Task<bool> SaveGameServerAsync(string key, RedisGameServer server)
    {
        string serializedServer = JsonConvert.SerializeObject(server);
        bool isSaved = await _db.StringSetAsync(key, serializedServer);
        if (isSaved)
        {
            await _db.SetAddAsync(ServerKeySet, key); // Add the key to the set of server keys
        }
        return isSaved;
    }
    
    // Retrieve GameServer data from Redis
    public async Task<RedisGameServer> GetGameServerAsync(string key)
    {
        string serializedServer = await _db.StringGetAsync(key);
        if (!string.IsNullOrEmpty(serializedServer))
        {
            return JsonConvert.DeserializeObject<RedisGameServer>(serializedServer);
        }
        return null;
    }

    public async Task<List<RedisGameServer>> GetAllGameServersAsync()
    {
        var serverKeys = await _db.SetMembersAsync(ServerKeySet); // Get all keys from the set
        var servers = new List<RedisGameServer>();

        foreach (var key in serverKeys)
        {
            string serializedServer = await _db.StringGetAsync(key.ToString());
            if (!string.IsNullOrEmpty(serializedServer))
            {
                servers.Add(JsonConvert.DeserializeObject<RedisGameServer>(serializedServer));
            }
        }

        return servers;
    }
}