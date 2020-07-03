using System;

namespace FractalMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var light = new Light();
            light.Parse("'ciao come \\' stai'");
            
        }
    }
}
