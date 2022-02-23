using System;
using System.Collections.Generic;

namespace Tabler.Docs.Data
{
    public static class SampleData
    {

        public static List<Order> GeneratedOrders(int count)
        {
            var rnd = new Random();
            var orders = new List<Order>();
            var customer = new Customer("Ani Vent");

            for (int i = 1; i <= count; i++)
            {
                orders.Add(new Order { Customer = customer, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-12), GrossValue = rnd.Next(2000, 50000), DiscountPrecentage = rnd.Next(10, 50), OrderType = (OrderType)rnd.Next(0, 4) });
            }

            return orders;
        }

        //public static List<Order> GetRandomOrders()
        //{
        //    var rnd = new Random();
        //    var orders = new List<Order>();
        //    var customer = new Customer("Ani Vent");

        //    for (int i = 0; i < rnd.Next(5, 20); i++)
        //    {
        //        orders.Add(new Order { Customer = customer, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-12), GrossValue = rnd.Next(2000, 50000), DiscountPrecentage = rnd.Next(10, 50), OrderType = (OrderType)rnd.Next(0, 4) });
        //    }

        //    return orders;
        //}

        public static List<Customer> GetCustomers()
        {

            return new List<Customer> {
                new Customer { CustomerName = "Odio Corporation", Percentage=30, Visits=5402 },
                new Customer { CustomerName = "Nascetur AB", Percentage=15, Visits=1134 },
                new Customer { CustomerName = "Justo Eu Institute", Percentage=23, Visits=26431 },
                new Customer { CustomerName = "Ani Vent", Percentage=88, Visits=97654 },
                new Customer { CustomerName = "Cali Inc", Percentage=77, Visits=43543 }
                };

        }

        public static List<Order> GetOrders()
        {
            var odio =  new Customer("Odio Corporation");
            var nascetur = new Customer("Nascetur AB");
            var justo = new Customer("Justo Eu Institute");
            var ani = new Customer("Ani Vent");
            var cali = new Customer("Cali Inc");

            var orders = new List<Order>();
            orders.Add(new Order { OrderStatus = OrderStatus.Active, Customer = odio, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-12), GrossValue = 34531, DiscountPrecentage = 21, OrderType = OrderType.Contract });
            orders.Add(new Order { OrderStatus = OrderStatus.Shipped, Customer = odio, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-100), GrossValue = 2800, DiscountPrecentage = 12, OrderType = OrderType.Mail });
            orders.Add(new Order { OrderStatus = OrderStatus.Active, Customer = odio, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-128), GrossValue = 12532, DiscountPrecentage = 24, OrderType = OrderType.Contract });
            orders.Add(new Order { OrderStatus = OrderStatus.OnHold, Customer = odio, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-232), GrossValue = 1400, DiscountPrecentage = 12, OrderType = OrderType.Mail });
            orders.Add(new Order { OrderStatus = OrderStatus.Active, Customer = odio, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-321), GrossValue = 22000, DiscountPrecentage = 10, OrderType = OrderType.Contract });
            orders.Add(new Order { OrderStatus = OrderStatus.Cancelled, Customer = odio, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-400), GrossValue = 3000, DiscountPrecentage = 17, OrderType = OrderType.Web });

            orders.Add(new Order { OrderStatus = OrderStatus.Active, Customer = nascetur, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-17), GrossValue = 2134, DiscountPrecentage = 10, OrderType = OrderType.Phone });
            orders.Add(new Order { OrderStatus = OrderStatus.Shipped, Customer = nascetur, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-27), GrossValue = 11345, DiscountPrecentage = 12, OrderType = OrderType.Phone });
            orders.Add(new Order { OrderStatus = OrderStatus.Shipped, Customer = nascetur, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-124), GrossValue = 63400, DiscountPrecentage = 32, OrderType = OrderType.Mail });
            orders.Add(new Order { OrderStatus = OrderStatus.Cancelled, Customer = nascetur, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-299), GrossValue = 1235, DiscountPrecentage = 12, OrderType = OrderType.Mail });
            orders.Add(new Order { OrderStatus = OrderStatus.Cancelled, Customer = nascetur, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-372), GrossValue = 44000, DiscountPrecentage = 11, OrderType = OrderType.Phone });
            orders.Add(new Order { OrderStatus = OrderStatus.Active, Customer = nascetur, Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-410), GrossValue = 17000, DiscountPrecentage = 5, OrderType = OrderType.Phone });

            orders.Add(new Order { OrderStatus = OrderStatus.Active, Customer = justo, Country = "Spain", OrderDate = DateTimeOffset.Now.AddDays(-13), GrossValue = 2800, DiscountPrecentage = 12, OrderType = OrderType.Mail });
            orders.Add(new Order { OrderStatus = OrderStatus.Shipped, Customer = justo, Country = "Spain", OrderDate = DateTimeOffset.Now.AddDays(-45), GrossValue = 12532, DiscountPrecentage = 24, OrderType = OrderType.Web });
            orders.Add(new Order { OrderStatus = OrderStatus.Shipped, Customer = justo, Country = "Spain", OrderDate = DateTimeOffset.Now.AddDays(-60), GrossValue = 1400, DiscountPrecentage = 12, OrderType = OrderType.Mail });
            orders.Add(new Order { OrderStatus = OrderStatus.Shipped, Customer = justo, Country = "Spain", OrderDate = DateTimeOffset.Now.AddDays(-150), GrossValue = 22000, DiscountPrecentage = 10, OrderType = OrderType.Web });
            orders.Add(new Order { OrderStatus = OrderStatus.Active, Customer = justo, Country = "Spain", OrderDate = DateTimeOffset.Now.AddDays(-200), GrossValue = 3000, DiscountPrecentage = 17, OrderType = OrderType.Web });

            orders.Add(new Order { OrderStatus = OrderStatus.OnHold, Customer = ani, Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-17), GrossValue = 2134, DiscountPrecentage = 10, OrderType = OrderType.Phone });
            orders.Add(new Order { OrderStatus = OrderStatus.Active, Customer = ani, Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-27), GrossValue = 11345, DiscountPrecentage = 12, OrderType = OrderType.Phone });
            orders.Add(new Order { OrderStatus = OrderStatus.Shipped, Customer = ani, Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-124), GrossValue = 17002, DiscountPrecentage = 32, OrderType = OrderType.Mail });

            orders.Add(new Order { OrderStatus = OrderStatus.Active, Customer = cali, Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-10), GrossValue = 77000, DiscountPrecentage = 17, OrderType = OrderType.Web });
            orders.Add(new Order { OrderStatus = OrderStatus.Shipped, Customer = cali, Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-110), GrossValue = 120000, DiscountPrecentage = 23, OrderType = OrderType.Web });
            orders.Add(new Order { OrderStatus = OrderStatus.Active, Customer = cali, Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-243), GrossValue = 44000, DiscountPrecentage = 8, OrderType = OrderType.Web });

            return orders;
        }
    }
}

