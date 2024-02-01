using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tabler.Docs.Data;

public enum OrderType
{
    Web,
    Contract,
    Mail,
    Phone
}

public enum OrderStatus
{
    Active,
    Shipped,
    OnHold,
    Cancelled
}

public class Customer
{
    public Customer() { }

    public Customer(string customerName)
    {
        CustomerName = customerName;
    }

    public Guid CustomerId { get; set; } = Guid.NewGuid();
    public string CustomerName { get; set; }
    public int Visits { get; set; }
    public int Percentage { get; set; }

    public DateTimeOffset? LastOrderDate { get; set; }


    public override string ToString()
    {
        return $"{CustomerId} [{CustomerName}]";
    }
}

public class Order
{
    public Guid OrderId { get; set; } = Guid.NewGuid();
    [Required(ErrorMessage = "Customer is required")]
    public Customer Customer { get; set; }
    public string Country { get; set; }
    public DateTimeOffset OrderDate { get; set; }
    public OrderType OrderType { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public decimal GrossValue { get; set; }

    public decimal NetValue
    {
        get => GrossValue * (1 - (DiscountPercentage / 100));
    }

    public decimal DiscountPercentage { get; set; }

    public decimal Discount => GrossValue - NetValue;

    public int TestInt { get; set; } = 10;

    public List<OrderLine> OrderLines { get; set; }
}

public class OrderLine
{
    public string ItemNumber { get; set; }
    public decimal Quantity { get; set; }
    public OrderStatus Status { get; set; }
    public Dictionary<string, string> Configuration { get; set; }
}