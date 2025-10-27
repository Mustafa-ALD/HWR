using Npgsql;

internal class Program
{
    public record WorkItem(int id, string title, string tag, string state);

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = "Host=localhost;Username=postgres;Password=hwdb123;Database=hwdb;Port=2345";
        builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader());
        });


        var app = builder.Build();

        app.UseCors("AllowAll");


        //public record WorkItem(int id, string title, string tag, string state);
        app.MapGet("/work-items", async (NpgsqlDataSource dataSource) =>
        {
            var items = new List<WorkItem>();

            await using var conn = await dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, title, tag, state FROM work_items ORDER BY id";

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new WorkItem(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)
                ));
            }

            return Results.Ok(items);
        });

        app.Run();
    }
}