using StackExchange.Redis;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

public class GameServerService
{
    private readonly IDatabase _db;
    private ApplicationDbContext _context;
    private const string ServerKeySet = "gameServers"; // Redis set to store all game server keys

    public GameServerService(IDatabase database,
        ApplicationDbContext context)
    {
        _db = database;
        _context = context;
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

    /**
     * <summary>Retrieves all redis' stored game servers and finds the best suitable server for a user to join based on the distance
     * between his rank and the mean of the server's connected players' ranks</summary>
     * <remarks>A list of players currently matchmaking (awaiting a response from their matchmaking call to connect to an Ip)
     * could also do the trick, but seems only relevant when you want to join only servers that have not yet started their game</remarks>
     * <param name="user">The user we matchmake for</param>
     * <returns>The best suited server based on ranks distance, null if no servers are found in redis</returns>
     */
    public async Task<RedisGameServer> MatchmakeForUser(User user)
    {
        var servers = await GetAllGameServersAsync();
        if (servers.Count == 0)
        {
            return null;
        }

        var bestServer = servers.First();
        var bestServerRankDistance = ComputeRankDistance(user, bestServer);
        foreach (var server in servers)
        {
            if(server.PlayerUuids.Count != 0)
            {
                var serverRankDistance = ComputeRankDistance(user, server);
                if (serverRankDistance < bestServerRankDistance)
                {
                    bestServer = server;
                    bestServerRankDistance = serverRankDistance;
                }
            }
        }

        return bestServer;
    }

    /**
     * <summary>Retrieves a list of connected players to a redis game server and returns a mean of their ranks</summary>
     * <remarks>A User that is not yet ranked will be considered as a Silver</remarks>
     * <param name="gameServer">The redis game server we are studying</param>
     * <returns>A players Rank mean computed direclty from their Rank's Id.
     * If a server is empty, will return float.MaxValue to discourage matchmaking from choosing an empty server</returns>
     */
    private float ComputeRankMean(RedisGameServer gameServer)
    {
        if (gameServer.PlayerUuids.Count == 0)
        {
            return float.MaxValue;
        }
        
        var playersRankMean = 0.0f;
        
        foreach (var playerId in gameServer.PlayerUuids)
        {
            var playerRankId = _context.UsersRanks.Find(playerId)?.RankId;
            if(playerRankId.HasValue)
            {
                playersRankMean += playerRankId.Value;
            }
            else
            {
                playersRankMean += 2.0f;
            }
        }
        playersRankMean /= (float)gameServer.PlayerUuids.Count;
        
        return playersRankMean;
    }

    /**
     * <summary>Returns the rank distance between the rank mean of a server's connected players and a user in matchmaking</summary>
     * <remarks>A User that is not yet ranked will be considered as a Silver</remarks>
     * <param name="user">The user currently in matchmaking</param>
     * <param name="gameServer">A server that has 0..n connected players</param>
     * <returns>A float of the absolute value of ServerRankMean - UserRankId</returns>
     */
    private float ComputeRankDistance(User user, RedisGameServer gameServer)
    {
        var UserRankId = _context.UsersRanks.Find(user.Id)?.RankId;
        if(UserRankId.HasValue)
        {
            return float.Abs(ComputeRankMean(gameServer) - UserRankId.Value);
        }
        
        return float.Abs(ComputeRankMean(gameServer) - 2.0f);
    }
}