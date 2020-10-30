using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tabler.Docs.Components.Charts
{
  public static class SampleData
    {
   

            public static List<Order> GetRandomOrders()
            {
                var rnd = new Random();
                var orders = new List<Order>();

                for (int i = 0; i < rnd.Next(5, 20); i++)
                {
                    orders.Add(new Order { CustomerName = "Odio Corporation", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-12), GrossValue = rnd.Next(2000, 50000), DiscountPrecentage = rnd.Next(10, 50), OrderType = (OrderType)rnd.Next(0, 4) });
                }

                return orders;
            }

            public static List<Order> GetOrders()
            {
                var orders = new List<Order>();
                orders.Add(new Order { CustomerName = "Odio Corporation", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-12), GrossValue = 34531, DiscountPrecentage = 21, OrderType = OrderType.Contract });
                orders.Add(new Order { CustomerName = "Odio Corporation", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-100), GrossValue = 2800, DiscountPrecentage = 12, OrderType = OrderType.Mail });
                orders.Add(new Order { CustomerName = "Odio Corporation", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-128), GrossValue = 12532, DiscountPrecentage = 24, OrderType = OrderType.Contract });
                orders.Add(new Order { CustomerName = "Odio Corporation", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-232), GrossValue = 1400, DiscountPrecentage = 12, OrderType = OrderType.Mail });
                orders.Add(new Order { CustomerName = "Odio Corporation", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-321), GrossValue = 22000, DiscountPrecentage = 10, OrderType = OrderType.Contract });
                orders.Add(new Order { CustomerName = "Odio Corporation", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-400), GrossValue = 3000, DiscountPrecentage = 17, OrderType = OrderType.Web });

                orders.Add(new Order { CustomerName = "Nascetur AB", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-17), GrossValue = 2134, DiscountPrecentage = 10, OrderType = OrderType.Phone });
                orders.Add(new Order { CustomerName = "Nascetur AB", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-27), GrossValue = 11345, DiscountPrecentage = 12, OrderType = OrderType.Phone });
                orders.Add(new Order { CustomerName = "Nascetur AB", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-124), GrossValue = 63400, DiscountPrecentage = 32, OrderType = OrderType.Mail });
                orders.Add(new Order { CustomerName = "Nascetur AB", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-299), GrossValue = 1235, DiscountPrecentage = 12, OrderType = OrderType.Mail });
                orders.Add(new Order { CustomerName = "Nascetur AB", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-372), GrossValue = 44000, DiscountPrecentage = 11, OrderType = OrderType.Phone });
                orders.Add(new Order { CustomerName = "Nascetur AB", Country = "Sweden", OrderDate = DateTimeOffset.Now.AddDays(-410), GrossValue = 17000, DiscountPrecentage = 5, OrderType = OrderType.Phone });

                orders.Add(new Order { CustomerName = "Justo Eu Institute", Country = "Spain", OrderDate = DateTimeOffset.Now.AddDays(-13), GrossValue = 2800, DiscountPrecentage = 12, OrderType = OrderType.Mail });
                orders.Add(new Order { CustomerName = "Justo Eu Institute", Country = "Spain", OrderDate = DateTimeOffset.Now.AddDays(-45), GrossValue = 12532, DiscountPrecentage = 24, OrderType = OrderType.Web });
                orders.Add(new Order { CustomerName = "Justo Eu Institute", Country = "Spain", OrderDate = DateTimeOffset.Now.AddDays(-60), GrossValue = 1400, DiscountPrecentage = 12, OrderType = OrderType.Mail });
                orders.Add(new Order { CustomerName = "Justo Eu Institute", Country = "Spain", OrderDate = DateTimeOffset.Now.AddDays(-150), GrossValue = 22000, DiscountPrecentage = 10, OrderType = OrderType.Web });
                orders.Add(new Order { CustomerName = "Justo Eu Institute", Country = "Spain", OrderDate = DateTimeOffset.Now.AddDays(-200), GrossValue = 3000, DiscountPrecentage = 17, OrderType = OrderType.Web });

                orders.Add(new Order { CustomerName = "Ani Vent", Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-17), GrossValue = 2134, DiscountPrecentage = 10, OrderType = OrderType.Phone });
                orders.Add(new Order { CustomerName = "Ani Vent", Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-27), GrossValue = 11345, DiscountPrecentage = 12, OrderType = OrderType.Phone });
                orders.Add(new Order { CustomerName = "Ani Vent", Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-124), GrossValue = 17002, DiscountPrecentage = 32, OrderType = OrderType.Mail });

                orders.Add(new Order { CustomerName = "Cali Inc", Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-10), GrossValue = 77000, DiscountPrecentage = 17, OrderType = OrderType.Web });
                orders.Add(new Order { CustomerName = "Cali Inc", Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-110), GrossValue = 120000, DiscountPrecentage = 23, OrderType = OrderType.Web });
                orders.Add(new Order { CustomerName = "Cali Inc", Country = "France", OrderDate = DateTimeOffset.Now.AddDays(-243), GrossValue = 44000, DiscountPrecentage = 8, OrderType = OrderType.Web });

                return orders;
            }
        }
    }

