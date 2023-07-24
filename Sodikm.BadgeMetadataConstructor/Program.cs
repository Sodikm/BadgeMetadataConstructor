using System.Reflection;
using System.Text.Json;

namespace Sodikm.BadgeMetadataConstructor
{
    internal class Program
    {
        static string ReadNonEmptyLine()
        {
            while (true)
            {
                string? line = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(line))
                    return line;

                Console.WriteLine("An input is required!");
            }
        }

        static void Main(string[] args)
        {
            string? version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            Console.WriteLine($"Sodikm Badge Metadata Constructor v{version}");

            Console.WriteLine("Creator's name:");
            string creator = ReadNonEmptyLine();

            Console.WriteLine("Place ID (optional):");
            string? placeIdStr = Console.ReadLine();
            long.TryParse(placeIdStr, out long placeId);

            Console.WriteLine("Number of download workers (optional):");
            string? workerCountStr = Console.ReadLine();
            if (!int.TryParse(workerCountStr, out int workerCount) || workerCount < 0)
                workerCount = 3;

            Console.WriteLine("List of badges, in the format of \"id,id,id\". Duplicate badge ids are automatically removed.");
            string badgeIdsStr = ReadNonEmptyLine();
            IEnumerable<string> badgeIdsStrs = badgeIdsStr.Split(',').Distinct();

            Queue<long> badgeIds = new Queue<long>();

            foreach (string badgeIdStr in badgeIdsStrs)
            {
                if (!long.TryParse(badgeIdStr, out long badgeId))
                {
                    Console.WriteLine($"Invalid badge ID: {badgeIdStr}");
                    continue;
                }

                if (!badgeIds.Contains(badgeId))
                    badgeIds.Enqueue(badgeId);
            }

            Console.WriteLine($"Creator: {creator}");
            if (placeId > 0)
                Console.WriteLine($"Place ID: {placeId}");
            Console.WriteLine($"Download workers: {workerCount}");
            Console.WriteLine($"Unique badges count: {badgeIds.Count}");

            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };

            HttpClient httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromMinutes(1)
            };

            Progress progress = new Progress(badgeIds.Count);

            List<Task> workers = new List<Task>();

            WorkerFactory workerFactory = new WorkerFactory(httpClient, progress, placeId, badgeIds);

            for (int i = 1; i <= workerCount; i++)
            {
                Task worker = workerFactory.Create();
                workers.Add(worker);
            }

            Task.WaitAll(workers.ToArray());

            Progress.ClearCurrentConsoleLine(); // remove progress bar

            Console.WriteLine("Badge scrape complete!");
            Console.WriteLine($"Successful: {progress.Succeeded}");
            Console.WriteLine($"Failed: {progress.Failed}");
            Console.WriteLine($"Badges: {progress.Badges.Count}");

            BadgeMetadata metadata = new BadgeMetadata
            {
                Creator = creator,
                Badges = progress.Badges
            };

            string metadataStr = JsonSerializer.Serialize(metadata);

            File.WriteAllText($"{placeId}.meta.json", metadataStr);
        }
    }
}