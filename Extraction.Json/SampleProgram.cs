using System;
using System.Collections.Generic;
using Microsoft.ProgramSynthesis.Extraction.Json;
using Microsoft.ProgramSynthesis.Extraction.Json.Constraints;
using Microsoft.ProgramSynthesis.Wrangling.Schema.TableOutput;

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
            session.Constraints.Add(new FlattenDocument(jsonText));
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

            // Run the program and obtain the table
            ITable<string> table = program.Run(jsonText);
            Console.WriteLine("Joining Inner Array Table!");
            PrintTable(table);

            // Option 2: No Joining Inner Arrays
            var noJoinSession = new Session();
            noJoinSession.Constraints.Add(new FlattenDocument(jsonText), new NoJoinInnerArrays());
            Program noJoinProgram = noJoinSession.Learn();

            if (noJoinProgram == null)
            {
                Console.WriteLine("Fail to learn a program!");
                return;
            }

            // Run the program and obtain the table output
            table = noJoinProgram.Run(jsonText);
            Console.WriteLine("No Joining Inner Array Table!");
            PrintTable(table);

            Console.WriteLine("Done.");
        }

        private static void PrintTable(IEnumerable<IEnumerable<string>> table)
        {
            foreach (IEnumerable<string> row in table)
            {
                foreach (string cell in row)
                {
                    Console.Write($"{cell ?? "null"}, ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
