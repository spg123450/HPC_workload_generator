using System;
using System.Text.Json.Serialization;

namespace WorkloadGenerator.Models
{
    public class TestScenario
    {
        [JsonPropertyName("scenarioId")]
        public string ScenarioId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("workstream")]
        public string Workstream { get; set; } // e.g., "HCS", "ClusterToolkit", "Orbax"

        [JsonPropertyName("action")]
        public string Action { get; set; } // e.g., "CreateCluster", "SubmitSlurmJob", "SimulateStorageLatency"

        [JsonPropertyName("payload")]
        public object Payload { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}