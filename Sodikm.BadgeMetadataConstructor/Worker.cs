using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Sodikm.BadgeMetadataConstructor
{
    internal class WorkerFactory
    {
        private class AwardingUniverse
        {
            [JsonPropertyName("rootPlaceId")]
            public long PlaceId { get; set; }
        }

        private class BadgeInformation
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = default!;

            [JsonPropertyName("awardingUniverse")]
            public AwardingUniverse AwardingUniverse { get; set; } = default!;
        }

        private const int MaxRetries = 3;

        private HttpClient _HttpClient;
        private Progress _Progress;
        private long _PlaceId;
        private Queue<long> _BadgeIds;
        private StreamWriter _StreamWriter;

        public WorkerFactory(HttpClient httpClient, Progress progress, long placeId, Queue<long> badgeIds)
        {
            _HttpClient = httpClient;
            _Progress = progress;
            _PlaceId = placeId;
            _BadgeIds = badgeIds;

            _StreamWriter = new StreamWriter($"badge_metadata_constructor_{placeId}.log", false);
            _StreamWriter.AutoFlush = true;
        }

        private async Task Request(long badgeId)
        {
            for (int i = 1; i <= MaxRetries; i++)
            {
                try
                {
                    HttpResponseMessage response = await _HttpClient.GetAsync($"https://badges.roblox.com/v1/badges/{badgeId}");
                    response.EnsureSuccessStatusCode();

                    string responseContent = await response.Content.ReadAsStringAsync();
                    BadgeInformation information = JsonSerializer.Deserialize<BadgeInformation>(responseContent)
                                                        ?? throw new Exception("Failed to deserialise response content");

                    if (_PlaceId < 1 || information.AwardingUniverse.PlaceId == _PlaceId)
                    {
                        _StreamWriter.WriteLine($"{badgeId}: Adding to badge metadata");

                        BadgeData data = new BadgeData
                        {
                            Id = badgeId,
                            Name = information.Name
                        };

                        _Progress.Success(data);
                    }
                    else
                    {
                        _StreamWriter.WriteLine($"{badgeId}: Not for place id {_PlaceId}");
                        _Progress.Success();
                    }

                    return;
                }
                catch (Exception ex)
                {
                    _StreamWriter.WriteLine($"Failed to fetch {badgeId} (try {i}): {ex}");
                }
            }

            _StreamWriter.WriteLine($"Failed to fetch {badgeId}: Failed to fetch in {MaxRetries} tries");
            _Progress.Fail();
        }

        public async Task Create()
        {
            while (_BadgeIds.TryDequeue(out long badgeId))
            {
                await Request(badgeId);
            }
        }
    }
}
