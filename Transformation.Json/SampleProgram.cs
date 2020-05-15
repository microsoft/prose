using System;

namespace Transformation.Json
{
    /// <summary>
    ///     Sample of how to use PROSE's Json Transformation APIs. 
    ///     PROSE generates JsonToJson and TableToJson transformation programs from constraints.
    /// </summary>
    internal static partial class Sample
    {
        private static void Main(string[] args)
        {
            // Demonstrate Json To Json Transformation
            Console.WriteLine("JSON to JSON Transformation By Example\n");
            JsonToJsonSample();

            Console.WriteLine("\n\n");

            // Demonstrate Table To Json Transformation
            Console.WriteLine("Table to JSON Transformation By Example\n");
            TableToJsonSample();
        }
    }
}
