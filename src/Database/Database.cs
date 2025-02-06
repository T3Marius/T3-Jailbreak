using MySqlConnector;
using Dapper;
using Microsoft.Extensions.Logging;

namespace T3Jailbreak
{
    public static class JBDatabase
    {
        private static string DatabaseConnectionString { get; set; } = string.Empty;

        private static async Task<MySqlConnection> ConnectAsync()
        {
            MySqlConnection connection = new(DatabaseConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        public static async Task CreateJailbreakTableAsync(Database_Config config)
        {
            if (string.IsNullOrEmpty(config.DatabaseHost) ||
                string.IsNullOrEmpty(config.DatabaseName) ||
                string.IsNullOrEmpty(config.DatabaseUser) ||
                string.IsNullOrEmpty(config.DatabasePassword))
            {
                throw new Exception("[T3-Jailbreak] You need to setup Database credentials in config");
            }

            MySqlConnectionStringBuilder builder = new()
            {
                Server = config.DatabaseHost,
                Database = config.DatabaseName,
                UserID = config.DatabaseUser,
                Password = config.DatabasePassword,
                Port = config.DatabasePort,
                Pooling = true,
                MinimumPoolSize = 0,
                MaximumPoolSize = 600,
                ConnectionIdleTimeout = 30,
                AllowZeroDateTime = true
            };

            DatabaseConnectionString = builder.ConnectionString;

            await using MySqlConnection connection = await ConnectAsync();
            await using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            try
            {
                await connection.ExecuteAsync(CreateTableQuery, transaction: transaction);
                await connection.ExecuteAsync(CreateCTBanTableQuery, transaction: transaction);
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"[ERROR] Failed to create T3_Jailbreak table: {ex.Message}");
                throw;
            }
        }

        const string CreateTableQuery = @"
            CREATE TABLE IF NOT EXISTS JB_LastRequest (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                PlayerName VARCHAR(256) NOT NULL UNIQUE,
                Wins INT NOT NULL DEFAULT 0,
                Losses INT NOT NULL DEFAULT 0,
                DateAdded TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
        );";

        const string CreateCTBanTableQuery = @"
            CREATE TABLE IF NOT EXISTS JB_CTBans (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                SteamID VARCHAR(32) NOT NULL UNIQUE,
                PlayerName VARCHAR(256) NOT NULL,
                AdminName VARCHAR(256) NOT NULL,
                Reason TEXT NOT NULL,
                Duration INT NOT NULL, -- Duration in minutes
                BanTime TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ExpirationTime DATETIME NOT NULL
        );";


        public static async Task UpdatePlayerStatsAsync(string playerName, bool isWinner)
        {
            const string UpdateQuery = @"
            INSERT INTO JB_LastRequest (PlayerName, Wins, Losses)
             VALUES (@PlayerName, @Wins, @Losses)
             ON DUPLICATE KEY UPDATE
             Wins = Wins + @Wins,
             Losses = Losses + @Losses,
             DateAdded = CURRENT_TIMESTAMP;";

            await using var connection = await ConnectAsync();
            await connection.ExecuteAsync(UpdateQuery, new
            {
                PlayerName = playerName,
                Wins = isWinner ? 1 : 0,
                Losses = isWinner ? 0 : 1
            });
        }
        public static async Task<IEnumerable<(string PlayerName, int Wins)>> GetTopPlayersAsync(int top = 50)
        {
            const string SelectTopPlayersQuery = @"
            SELECT PlayerName, Wins
             FROM JB_LastRequest
             ORDER BY Wins DESC
             LIMIT @Top;";

            await using var connection = await ConnectAsync();
            return await connection.QueryAsync<(string PlayerName, int Wins)>(SelectTopPlayersQuery, new { Top = top });
        }

        public static async Task<int> GetPlayerWinsAsync(string playerName)
        {
            const string SelectQuery = @"
            SELECT Wins
            FROM JB_LastRequest
            WHERE PlayerName = @PlayerName;";

            await using var connection = await ConnectAsync();
            return await connection.ExecuteScalarAsync<int>(SelectQuery, new { PlayerName = playerName });
        }
        public static async Task AddCTBan(string steamId, string playerName, string adminName, string reason, int duration)
        {
            const string InsertQuery = @"
            INSERT INTO JB_CTBans (SteamID, PlayerName, AdminName, Reason, Duration, ExpirationTime)
            VALUES (@SteamID, @PlayerName, @AdminName, @Reason, @Duration, DATE_ADD(NOW(), INTERVAL @Duration MINUTE))
            ON DUPLICATE KEY UPDATE 
            AdminName = @AdminName, Reason = @Reason, Duration = @Duration, ExpirationTime = DATE_ADD(NOW(), INTERVAL @Duration MINUTE);";

            await using var connection = await ConnectAsync();
            await connection.ExecuteAsync(InsertQuery, new
            {
                SteamID = steamId,
                PlayerName = playerName,
                AdminName = adminName,
                Reason = reason,
                Duration = duration
            });
        }

        public static async Task RemoveCTBan(string steamId)
        {
            const string DeleteQuery = "DELETE FROM JB_CTBans WHERE SteamID = @SteamID";

            await using var connection = await ConnectAsync();
            await connection.ExecuteAsync(DeleteQuery, new { SteamID = steamId });
        }

        public static async Task<bool> CheckForCTBan(string steamId)
        {
            const string SelectQuery = @"
            SELECT COUNT(*) FROM JB_CTBans 
            WHERE SteamID = @SteamID AND ExpirationTime > NOW();";

            await using var connection = await ConnectAsync();
            int count = await connection.ExecuteScalarAsync<int>(SelectQuery, new { SteamID = steamId });
            return count > 0;
        }

        public static async Task<string?> GetPlayerCTBanInfo(string steamId)
        {
            const string SelectQuery = @"
            SELECT PlayerName, AdminName, Reason, Duration, ExpirationTime 
            FROM JB_CTBans 
            WHERE SteamID = @SteamID AND ExpirationTime > NOW();";

            await using var connection = await ConnectAsync();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(SelectQuery, new { SteamID = steamId });

            if (result == null)
                return null;

            return $"Player: {result.PlayerName}\nAdmin: {result.AdminName}\nReason: {result.Reason}\nDuration: {result.Duration} minutes\nExpires: {result.ExpirationTime}";
        }
    }
}
