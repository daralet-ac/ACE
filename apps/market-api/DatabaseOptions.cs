namespace ACE.MarketApi;

public class DatabaseOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3306;
    public string ShardDatabase { get; set; } = "ace_shard";
    public string WorldDatabase { get; set; } = "ace_world";
    public string AuthDatabase { get; set; } = "ace_auth";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public string ShardConnectionString =>
        $"Server={Host};Port={Port};Database={ShardDatabase};User Id={Username};Password={Password};";

    public string WorldConnectionString =>
        $"Server={Host};Port={Port};Database={WorldDatabase};User Id={Username};Password={Password};";
}

public class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
