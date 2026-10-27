#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TabBlazor.Tests.Components.DataViews
{
    public sealed class Row
    {
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = "";

        public bool Active { get; set; }
        public decimal Total { get; set; }
        public SourceTestStatus Status { get; set; }
        public DateTime Date { get; set; } = new(2026, 1, 1);
        public int? Score { get; set; }

        public static List<Row> Many(int count) =>
            Enumerable.Range(1, count).Select(i => new Row
            {
                Id = i,
                Name = $"Row {i:00}",
                Total = i * 10.5m,
                Status = (SourceTestStatus)(i % 3),
                Active = i % 2 == 0,
                Date = new DateTime(2026, 1, 1).AddDays(i),
                Score = i % 3 == 0 ? null : i
            }).ToList();
    }

    public sealed class Address
    {
        public string City { get; set; } = "";
    }

    public sealed class Shipment
    {
        public int Id { get; set; }
        public Address? To { get; set; }
    }

    public sealed record Tag(string Name);

    /// <summary>A source whose responses the test completes by hand.</summary>
    public sealed class FakeSource<T> : IDataViewSource<T>
    {
        public sealed record Call(DataRequest Request, CancellationToken Token, TaskCompletionSource<DataPage<T>> Response);

        public PagingMode PagingMode { get; set; } = PagingMode.Offset;

        public List<Call> Calls { get; } = [];

        /// <summary>When set, requests are answered at once.</summary>
        public Func<DataRequest, DataPage<T>>? Respond { get; set; }

        public Task<DataPage<T>> GetPageAsync(DataRequest request, CancellationToken cancellationToken)
        {
            var call = new Call(request, cancellationToken, new TaskCompletionSource<DataPage<T>>(TaskCreationOptions.RunContinuationsAsynchronously));
            lock (Calls)
            {
                Calls.Add(call);
                foreach (var waiter in waiters.Where(w => Calls.Count >= w.Count).ToList())
                {
                    waiters.Remove(waiter);
                    waiter.Signal.TrySetResult();
                }
            }

            if (Respond != null)
            {
                call.Response.SetResult(Respond(request));
            }

            return call.Response.Task;
        }

        private readonly List<(int Count, TaskCompletionSource Signal)> waiters = [];

        /// <summary>Completes when the source has been called <paramref name="count"/> times (fails after 5 seconds).</summary>
        public Task WaitForCallsAsync(int count)
        {
            lock (Calls)
            {
                if (Calls.Count >= count)
                {
                    return Task.CompletedTask;
                }

                var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                waiters.Add((count, signal));
                return signal.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }

        public Call Last => Calls[^1];

        public static FakeSource<T> Answering(IReadOnlyList<T> items, bool includeTotal = true)
        {
            var memory = new InMemorySource<T>(items, new InMemorySourceOptions { IncludeTotalCount = includeTotal });
            return new FakeSource<T> { Respond = memory.GetPage };
        }
    }
}
