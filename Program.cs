using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using WorkloadGenerator.Models;

namespace WorkloadGenerator
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private static readonly HttpClient _httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            XmlConfigurator.Configure(new System.IO.FileInfo("log4net.config"));
            log.Info("Starting GCP Supercomputer Workload Generator...");

            _httpClient.BaseAddress = new Uri("http://localhost:8000/");
            log.Info("Agent HTTP interface initialized.");
            log.Info("Workload schedulers started. Press Ctrl+C to exit.");

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;
                log.Info("Shutting down generator...");
                cts.Cancel();
            };

            var hcsTask = RunHcsWorkloadLoop(cts.Token);
            var orbaxTask = RunOrbaxWorkloadLoop(cts.Token);

            try
            {
                await Task.WhenAll(hcsTask, orbaxTask);
            }
            catch (OperationCanceledException)
            {
                log.Info("Workload generation stopped.");
            }
        }

        private static async Task RunHcsWorkloadLoop(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

            while (await timer.WaitForNextTickAsync(token) && !token.IsCancellationRequested)
            {
                var scenario = new TestScenario
                {
                    Workstream = "HCS",
                    Action = "CreateCluster",
                    Payload = new
                    {
                        ClusterName = $"hcs-test-cluster-{Guid.NewGuid().ToString().Substring(0, 4)}",
                        ReservationAffinity = "consume",
                        StorageType = "Parallelstore"
                    }
                };

                log.Info($"[HCS] Generating scenario: {scenario.Action}");
                await DispatchToAgentAsync("agent-gateway/hcs", scenario);
            }
        }

        private static async Task RunOrbaxWorkloadLoop(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

            while (await timer.WaitForNextTickAsync(token) && !token.IsCancellationRequested)
            {
                var scenario = new TestScenario
                {
                    Workstream = "Orbax",
                    Action = "SimulateStorageLatency",
                    Payload = new
                    {
                        TargetStorage = "GCS",
                        LatencyMs = 5000,
                        ExpectedError = "Checkpoint Restore Timeout"
                    }
                };

                log.Info($"[Orbax] Injecting failure scenario: {scenario.Action}");
                await DispatchToAgentAsync("agent-gateway/orbax", scenario);
            }
        }

        private static async Task DispatchToAgentAsync(string endpoint, TestScenario scenario)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, scenario);
                log.Info($"Dispatched {scenario.ScenarioId}. Response Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                log.Error($"Failed to dispatch scenario {scenario.ScenarioId}. Is the Agent Engine running? Error: {ex.Message}");
            }
        }
    }
}