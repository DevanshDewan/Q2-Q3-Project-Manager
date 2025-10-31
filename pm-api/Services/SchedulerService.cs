// Services/SchedulerService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pm_api.DTOs.Scheduler;

namespace pm_api.Services
{
    /// <summary>
    /// Simple scheduler that performs a topological sort over task titles.
    /// Produces a recommended order and diagnostics for missing dependencies or cycles.
    /// </summary>
    public class SchedulerService : ISchedulerService
    {
        public Task<SchedulerResponseDto> ComputeScheduleAsync(SchedulerRequestDto request)
        {
            var resp = new SchedulerResponseDto
            {
                RecommendedOrder = new List<string>(),
                Diagnostics = new List<string>()
            };

            if (request?.Tasks == null || request.Tasks.Count == 0)
            {
                resp.Diagnostics.Add("No tasks provided.");
                return Task.FromResult(resp);
            }

            // Map titles -> task DTO
            var titleToTask = new Dictionary<string, SchedulerTaskDto>(StringComparer.Ordinal);
            foreach (var t in request.Tasks)
            {
                if (t == null || string.IsNullOrWhiteSpace(t.Title))
                {
                    resp.Diagnostics.Add("Found task with empty or null title; skipping.");
                    continue;
                }

                if (titleToTask.ContainsKey(t.Title))
                {
                    resp.Diagnostics.Add($"Duplicate task title detected: '{t.Title}'. Subsequent duplicates ignored.");
                    continue;
                }

                titleToTask[t.Title] = t;
            }

            if (titleToTask.Count == 0)
            {
                resp.Diagnostics.Add("No valid tasks with titles were provided.");
                return Task.FromResult(resp);
            }

            // adjacency: node -> set of nodes that depend on it (edges: dep -> task)
            var adj = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            // indegree counts for each node (how many dependencies it has)
            var indeg = new Dictionary<string, int>(StringComparer.Ordinal);

            // initialize nodes
            foreach (var title in titleToTask.Keys)
            {
                adj[title] = new HashSet<string>(StringComparer.Ordinal);
                indeg[title] = 0;
            }

            // process dependencies
            foreach (var t in titleToTask.Values)
            {
                // Use same collection type as DTO (List<string>), fallback to empty list
                var deps = t.Dependencies ?? new List<string>();

                foreach (var dep in deps)
                {
                    if (string.IsNullOrWhiteSpace(dep))
                    {
                        resp.Diagnostics.Add($"Task '{t.Title}' has an empty dependency entry; ignored.");
                        continue;
                    }

                    if (!titleToTask.ContainsKey(dep))
                    {
                        resp.Diagnostics.Add($"Missing dependency: task '{t.Title}' depends on unknown task '{dep}'.");
                        // skip linking missing dependency
                        continue;
                    }

                    // add edge dep -> t.Title
                    var set = adj[dep];
                    if (set.Add(t.Title))
                    {
                        indeg[t.Title] = indeg.GetValueOrDefault(t.Title) + 1;
                    }
                }
            }

            // Kahn's algorithm: queue nodes with indeg == 0
            var q = new Queue<string>(indeg.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var ordered = new List<string>();

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                ordered.Add(cur);

                if (!adj.TryGetValue(cur, out var neighbors)) continue;
                foreach (var nb in neighbors.ToList())
                {
                    indeg[nb] = indeg.GetValueOrDefault(nb) - 1;
                    if (indeg[nb] == 0)
                    {
                        q.Enqueue(nb);
                    }
                }
            }

            var knownCount = titleToTask.Count;
            if (ordered.Count == knownCount)
            {
                resp.RecommendedOrder = ordered;
            }
            else
            {
                // some nodes remain -> cycle or unresolved dependencies
                var remaining = titleToTask.Keys.Except(ordered).ToList();
                resp.Diagnostics.Add($"Cycle or unresolved dependencies detected between tasks: {string.Join(", ", remaining)}");

                // deterministic fallback: append remaining sorted
                var fallback = ordered.Concat(remaining.OrderBy(x => x, StringComparer.Ordinal)).ToList();
                resp.RecommendedOrder = fallback;
            }

            return Task.FromResult(resp);
        }
    }

    // extension for Dictionary GetValueOrDefault on older runtimes (keeps code robust)
    static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue @default = default!)
        {
            if (dict == null) return @default!;
            if (key == null) return @default!;
            return dict.TryGetValue(key, out var v) ? v : @default!;
        }
    }
}
