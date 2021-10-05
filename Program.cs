using System;

namespace Prototype
{
    class Program
    {
        static void Main(string[] args)
        {
            var l = new List<int>();
            l.Add(1);
            l.Add(2);

            Console.WriteLine($"Count: {l.Count}, l[0]={l[0]}, l[1]={l[1]}");
        }
    }
}
