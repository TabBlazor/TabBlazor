using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tabler.Docs.Components.DataViews;

public enum DemoOrderStatus
{
    Open,
    [Display(Name = "On hold")] OnHold,
    Shipped,
    Cancelled
}

public class DemoCustomer
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class DemoOrder
{
    public int Id { get; set; }

    [Required, Display(Name = "Order")]
    public string OrderNo { get; set; }

    [Required, Display(Name = "First name")]
    public string FirstName { get; set; }

    [Required, Display(Name = "Last name")]
    public string LastName { get; set; }

    public DemoCustomer Customer { get; set; }
    public DemoOrderStatus Status { get; set; }

    [Display(Name = "Date")]
    public DateTime OrderDate { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; }

    public decimal Price { get; set; }
    public decimal Total => Quantity * Price;
    public bool IsOverdue => Status == DemoOrderStatus.Open && OrderDate < new DateTime(2026, 6, 1);
    public string Comment { get; set; }
}

public class DemoOrderDetails
{
    public string Notes { get; set; }
    public List<string> Lines { get; set; } = [];
}

public static class DataViewDemoData
{
    /// <summary>Stands in for a slower call that loads heavy details for one order.</summary>
    public static async Task<DemoOrderDetails> LoadDetailsAsync(DemoOrder order)
    {
        await Task.Delay(400);
        return new DemoOrderDetails
        {
            Notes = order.Comment ?? "No notes.",
            Lines = Enumerable.Range(1, order.Quantity % 4 + 1).Select(i => $"Line {i}: article {order.Id * 10 + i}").ToList()
        };
    }

    private static readonly string[] FirstNames = ["Ada", "Alan", "Grace", "Linus", "Margaret", "Dennis", "Barbara", "Ken", "Frances", "Edsger"];
    private static readonly string[] LastNames = ["Lovelace", "Turing", "Hopper", "Torvalds", "Hamilton", "Ritchie", "Liskov", "Thompson", "Allen", "Dijkstra"];
    private static readonly string[] Customers = ["Odio Corporation", "Nascetur AB", "Justo Eu Institute", "Ani Vent", "Cali Inc", "Kalle Anka"];

    public static List<DemoOrder> Orders { get; } = Create(240);

    public static List<DemoOrder> Create(int count)
    {
        var random = new Random(42);
        var customers = Customers.Select((name, i) => new DemoCustomer { Id = i + 1, Name = name }).ToList();
        return Enumerable.Range(1, count).Select(i => new DemoOrder
        {
            Id = i,
            OrderNo = $"SO-2026-{i:0000}",
            FirstName = FirstNames[random.Next(FirstNames.Length)],
            LastName = LastNames[random.Next(LastNames.Length)],
            Customer = customers[random.Next(customers.Count)],
            Status = (DemoOrderStatus)random.Next(4),
            OrderDate = new DateTime(2026, 1, 1).AddDays(random.Next(300)),
            Quantity = random.Next(1, 50),
            Price = random.Next(500, 50000) / 100m,
            Comment = i % 7 == 0 ? "Customer asked for delivery before the end of the month; call before shipping." : null
        }).ToList();
    }
}
