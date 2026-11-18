namespace BusTravelAccountsSaFunction;

public sealed class DatabaseCredentials
{
    public DatabaseCredentials(string username, string password, string host, string dbname, int port)
    {
        Username = username;
        Password = password;
        Host = host;
        Dbname = dbname;
        Port = port;
    }

    public string Username { get; }

    public string Password { get; }

    public string Host { get; }

    public string Dbname { get; }

    public int Port { get; }
}