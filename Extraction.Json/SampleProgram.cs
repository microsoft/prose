using System;
using System.Collections.Generic;
using Microsoft.ProgramSynthesis.Extraction.Json;
using Microsoft.ProgramSynthesis.Extraction.Json.Constraints;
using Microsoft.ProgramSynthesis.Wrangling.Json;
using Microsoft.ProgramSynthesis.Wrangling.Schema;
using Microsoft.ProgramSynthesis.Wrangling.Schema.TableOutput;
using Microsoft.ProgramSynthesis.Wrangling.Schema.TreeOutput;

namespace Extraction.Json
{
    /// <summary>
    ///     Sample of how to use the Extraction.Json API. 
    ///     Extraction.Json generates extraction programs from constraints.
    /// </summary>
    internal static class SampleProgram
    {
        private static void Main(string[] args)
        {
            var jsonText = @"[
                                {""person"": {
                                        ""name"": {
                                                ""first"": ""Carrie"", 
                                                ""last"": ""Dodson""
                                        }, 
                                        ""address"": ""1 Microsoft Way"", 
                                        ""phone number"": []
                                        }
                                }, 
                                {""person"": {
                                        ""name"": {
                                                ""first"": ""Leonard"", 
                                                ""last"": ""Robledo""
                                        }, 
                                        ""phone number"": [
                                                ""123-4567-890"", 
                                                ""456-7890-123"", 
                                                ""789-0123-456""
                                        ]}
                                } 
                             ]";

            // Option 1: Joining Inner Arrays
            // Learn a program with the constraint to flatten the entire document.
            var session = new Session();
            session.AddConstraints(new FlattenDocument(jsonText));
            Program program = session.Learn();

            if (program == null)
            {
                Console.WriteLine("Fail to learn a program!");
                return;
            }

            // Serialize and deserialize the program
            var programText = program.Serialize();
            program = Loader.Instance.Load(programText);

            if (program == null)
            {
                Console.WriteLine("Fail to load deserialized program!");
                return;
            }

            // Run the program and obtain the tree output
            ITreeOutput<JsonRegion> tree = program.Run(jsonText);

            // Run the program and obtain the table output using outer join semantics
            IEnumerable<TableRow<JsonRegion>> outerJoinTable = program.RunTable(jsonText, TreeToTableSemantics.OuterJoin);
            Console.WriteLine("OuterJoin Semantic Table!");
            PrintTable(outerJoinTable);

            // Run the program and obtain the table output using inner join semantics
            IEnumerable<TableRow<JsonRegion>> innerJoinTable = program.RunTable(jsonText, TreeToTableSemantics.InnerJoin);
            Console.WriteLine("InnerJoin Semantic Table!");
            PrintTable(innerJoinTable);

            // Option 2: No Joining Inner Arrays
            var noJoinSession = new Session();
            noJoinSession.AddConstraints(new FlattenDocument(jsonText), new NoJoinInnerArrays());
            Program noJoinProgram = noJoinSession.Learn();

            if (noJoinProgram == null)
            {
                Console.WriteLine("Fail to learn a program!");
                return;
            }

            // Run the program and obtain the table output
            IEnumerable<TableRow<JsonRegion>> table = noJoinProgram.RunTable(jsonText);
            Console.WriteLine("No Joining Inner Array Table!");
            PrintTable(table);

            Console.WriteLine("Done.");
        }

        private static void PrintTable(IEnumerable<TableRow<JsonRegion>> table)
        {
            foreach (TableRow<JsonRegion> row in table)
            {
                foreach (KeyValuePair<string, JsonRegion> cell in row.Value)
                {
                    string value = cell.Value == null ? "null" : cell.Value.Value;
                    Console.Write($"<{cell.Key}:{value}>, ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
