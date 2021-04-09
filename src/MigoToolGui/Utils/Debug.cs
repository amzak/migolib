using System;
using System.Linq;
using System.Reflection;
using Splat;

namespace MigoToolGui.Utils
{
    public class Debug
    {
        public static void DebugIoc()
        {
            var types = Assembly.GetExecutingAssembly()
                .GetTypes();

            var allTypes = Assembly.GetExecutingAssembly()
                .GetReferencedAssemblies()
                .Select(t => Assembly.Load(t))
                .SelectMany(t => t.GetTypes())
                .ToList();

            Console.WriteLine("registered types");
            foreach (var type in types)
            {
                if (Locator.CurrentMutable.HasRegistration(type))
                {
                    Console.WriteLine(type.Name);
                }
            }

            Console.WriteLine("searching in references...");

            foreach (var type in allTypes)
            {
                if (Locator.CurrentMutable.HasRegistration(type))
                {
                    Console.WriteLine(type.Name);
                }
            }

            Console.WriteLine("---");
        }
    }
}