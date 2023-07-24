using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Sodikm.BadgeMetadataConstructor
{
    internal class BadgeMetadata
    {
        [JsonPropertyName("creator")]
        public string Creator { get; set; } = string.Empty;

        [JsonPropertyName("badges")]
        public IEnumerable<BadgeData> Badges { get; set; } = Enumerable.Empty<BadgeData>();
    }

    internal class BadgeData
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    internal class Progress
    {
        private const int NumOfBlocks = 10;

        private object _Lock = new object();

        private int _Total;
        public int Succeeded { get; private set; } = 0;
        public int Failed { get; private set; } = 0;
        public List<BadgeData> Badges { get; private set; } = new List<BadgeData>();

        public Progress(int total)
        {
            _Total = total;
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private void Draw()
        {
            lock (_Lock)
            {
                ClearCurrentConsoleLine();

                int downloaded = Succeeded + Failed;
                float progress = (float)downloaded / (float)_Total;

                int progressBlockAmount = (int)(progress * NumOfBlocks);
                string progressBlocks = new string('#', progressBlockAmount);
                string progressEmptyBlocks = new string('-', NumOfBlocks - progressBlockAmount);
                int progressPercentage = (int)(progress * 100);

                Console.Write($"[{progressBlocks}{progressEmptyBlocks}] {progressPercentage}%");
            }
        }

        public void Success(BadgeData? data = null)
        {
            if (data != null)
                Badges.Add(data);

            Succeeded++;
            Draw();
        }

        public void Fail()
        {
            Failed++;
            Draw();
        }
    }
}
