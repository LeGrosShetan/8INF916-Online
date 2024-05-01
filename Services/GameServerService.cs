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
    
    /**
     * <summary>Tries to register a server to redis database using it's IP as a key and a struct containing info on server as value</summary>
     * <param name="key">The server's Ip</param>
     * <param name="server">The struct containing server info that we want to save</param>
     * <returns>A bool representing wether or not server has been saved. Additionally if true, server's Ip is saved in a list containing all registered server's ips</returns>
     */
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
    
    /**
     * <summary>Try to retrieve server info using it's Ip</summary>
     * <param name="key">The server's Ip</param>
     * <returns>The server's info if it is found. Returns null otherwise</returns>
     */
    public async Task<RedisGameServer> GetGameServerAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
            
        string serializedServer = await _db.StringGetAsync(key);
        if (!string.IsNullOrEmpty(serializedServer))
        {
            return JsonConvert.DeserializeObject<RedisGameServer>(serializedServer);
        }
        return null;
    }

    /**
     * <summary>Retrieves a list of redis' registered servers</summary>
     * <returns>A list containing all server infos of redis' registered servers</returns>
     */
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