
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft;

class Program
{
    private static readonly string organization = "hello-radio-meter-world";
    private static readonly string project = "HelloWolrd";
    private static readonly string apiVersion = "7.2-preview";
    private static readonly int maxBatchSize = 200;

    static async Task Main()
    {
        string? personalAccessToken = ConfigurationManager.AppSettings["AZURE_PAT"] ?? null;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        if (personalAccessToken == null) {
            Console.WriteLine("Personal access token missing from env file");
            return;
        }

        // Auth token
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{personalAccessToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // Get all ids
        string wiqlQuery = $@"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{project}' ORDER BY [System.Id] ASC";

        var wiqlUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/wiql?api-version={apiVersion}";
        var wiqlPayload = new { query = wiqlQuery };

        Console.WriteLine("Running WIQL query...");
        var wiqlResponse = await client.PostAsync(wiqlUrl,
            new StringContent(JsonSerializer.Serialize(wiqlPayload), Encoding.UTF8, "application/json"));

        wiqlResponse.EnsureSuccessStatusCode();
        var wiqlResult = JsonDocument.Parse(await wiqlResponse.Content.ReadAsStringAsync());
        var ids = new List<int>();

        foreach (var wi in wiqlResult.RootElement.GetProperty("workItems").EnumerateArray())
        {
            ids.Add(wi.GetProperty("id").GetInt32());
        }

        Console.WriteLine($"Found {ids.Count} work items.");

        if (ids.Count == 0)
        {
            Console.WriteLine("No work items found.");
            return;
        }

        var connectionString = "Host=localhost;Username=postgres;Password=hwdb123;Database=hwdb;Port=2345";
        await using var dataSource = NpgsqlDataSource.Create(connectionString);

        for (int i = 0; i < ids.Count; i += maxBatchSize)
        {
            var batch = ids.GetRange(i, Math.Min(maxBatchSize, ids.Count - i));
            string idsParam = string.Join(",", batch);
            string workItemsUrl =
                $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems" +
                $"?ids={idsParam}&$expand=All&api-version={apiVersion}";

            Console.WriteLine($"Fetching {batch.Count} items (starting at ID {batch[0]})...");
            var resp = await client.GetAsync(workItemsUrl);
            resp.EnsureSuccessStatusCode();
            string json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var workItems = doc.RootElement.GetProperty("value");

            foreach (var item in workItems.EnumerateArray())
            {
                int id = item.GetProperty("id").GetInt32();

                if (!item.GetProperty("fields").TryGetProperty("System.Title", out JsonElement titleProp))
                {
                    continue;
                }

                string title = titleProp.GetString()!;

                if (!item.GetProperty("fields").TryGetProperty("System.State", out JsonElement stateProp))
                {
                    continue;
                }

                string state = stateProp.GetString()!;

                if (!item.GetProperty("fields").TryGetProperty("System.Tags", out JsonElement tagsProp))
                {
                    continue;
                }

                string tags = tagsProp.GetString()!;

                await using var cmd = dataSource.CreateCommand(@"
                    INSERT INTO work_items (id, tag, title, state)
                    VALUES ($1, $2, $3, $4)
                    ON CONFLICT (id) DO UPDATE
                    SET tag = EXCLUDED.tag,
                        title = EXCLUDED.title,
                        state = EXCLUDED.state;
                ");


                cmd.Parameters.AddWithValue(id);
                cmd.Parameters.AddWithValue(tags ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue(title ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue(state ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();

                Console.WriteLine($"Inserted work item {id} → {title}");
            }
        }
    }
}
