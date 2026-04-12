using Npgsql;

var connString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? "Host=db.pvmdtjxixrpckfdbrhpz.supabase.co;Username=postgres;Password=%/i_EjK/eq5EV2$;Database=postgres;Port=5432;SSL Mode=Require;Trust Server Certificate=true";

try
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    Console.WriteLine("Connected to Supabase/Postgres successfully.");

    using var cmd = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    Console.WriteLine("Tables in public schema:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine(" - " + reader.GetString(0));
    }
}
catch (Exception ex)
{
    Console.WriteLine("Failed to connect: " + ex.Message);
}
