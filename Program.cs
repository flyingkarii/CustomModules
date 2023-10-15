using BattleBitAPI.Features;
using System;
using System.Collections.Generic;

namespace BBRModules
{
    public class Program
    {
        public static void Main()
        {
            PaginatorLib paginator = new PaginatorLib(1, 2, 3, 4, 5, 6, 2, 1, 3, 4, 5, 3, 2, 1)
                .SetPageSize(4);
            List<string> page = paginator.GetPage(2);

            Console.WriteLine("Page Size: " + paginator.PageSize + 
                "\nPages: " + paginator.CountPages());

            for (int i = 0; i < page.Count; i++)
            {
                string value = page[i];
                Console.WriteLine($"{i}: {value}");
            }
        }
    }
}
