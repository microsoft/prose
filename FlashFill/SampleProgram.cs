using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ProgramSynthesis.FlashFill.Sample
{
    /// <summary>
    ///     Sample of how to use the FlashFill API. FlashFill generates string programs from input/output examples.
    /// </summary>
    internal static class SampleProgram
    {
        private static void Main(string[] args)
        {
            // Simplest usage: single example of a single string input:
            LearnFormatName();
            // Learning a program using multiple examples:
            LearnNormalizePhoneNumber();
            // Learning a program that takes multiple strings (columns) as input:
            LearnMergeNames();
            // Learning top-k ranked programs instead of just the single best one:
            LearnTop10NormalizePhoneNumber();
            LearnTop10FormatName();
            // Learning with additional inputs:
            LearnNormalizeDate();
            // Convert program to string and back:
            SerializeProgram();
        }

        /// <summary>
        ///     Learn to reformat a name written "First Last" as "Last, F." where 'F' is the first initial.
        ///     Demonstrates basic usage of FlashFill API.
        /// </summary>
        private static void LearnFormatName()
        {
            // Examples are given as a FlashFillExample object which takes an input and output.
            IEnumerable<FlashFillExample> examples = new[]
            {
                new FlashFillExample("Kettil Hansson", "Hansson, K.")
            };
            // Given just the examples, the best program is returned
            FlashFillProgram topRankedProgram = FlashFillProgram.Learn(examples);

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn format name program.");
            }
            else
            {
                // Run the program on some new inputs.
                foreach (var name in new[] {"Etelka Bala", "Myron Lampros"})
                {
                    string formatted = topRankedProgram.Run(name);
                    Console.WriteLine("\"{0}\" => \"{1}\"", name, formatted);
                }
            }
        }

        /// <summary>
        ///     Learn to normalize phone numbers in a few input formats to the same output format.
        ///     Demonstrates giving FlashFill multiple examples.
        /// </summary>
        private static void LearnNormalizePhoneNumber()
        {
            // Some programs may require multiple examples.
            // More examples ensures the proper program is learned and may speed up learning.
            IEnumerable<FlashFillExample> examples = new[]
            {
                new FlashFillExample("425-829-5512", "425-829-5512"),
                new FlashFillExample("(425) 829 5512", "425-829-5512")
            };
            FlashFillProgram topRankedProgram = FlashFillProgram.Learn(examples);

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn normalize phone number program.");
            }
            else
            {
                foreach (var phoneNumber in new[] {"425 233 1234", "(425) 777 3333"})
                {
                    string normalized = topRankedProgram.Run(phoneNumber);
                    Console.WriteLine("\"{0}\" => \"{1}\"", phoneNumber, normalized);
                }
            }
        }

        /// <summary>
        ///     Learn to take two strings of a first name and last name and combine them into "Last, First" format.
        ///     Demonstrates inputs with multiple strings (columns) and also providing inputs without a known output.
        /// </summary>
        private static void LearnMergeNames()
        {
            // Inputs may be made up of multiple strings. If so, all inputs must contain the same number of strings.
            IEnumerable<FlashFillExample> examples = new[]
            {
                new FlashFillExample(new FlashFillInput("Kettil", "Hansson"), "Hansson, Kettil")
            };
            // Inputs for which the corresponding output is not known. May be used for improving ranking.
            FlashFillInput[] additionalInputs =
            {
                new FlashFillInput("Greta", "Hermansson")
            };
            FlashFillProgram topRankedProgram = FlashFillProgram.Learn(examples, additionalInputs);

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn merge names program.");
            }
            else
            {
                var testInputs = new[] {new FlashFillInput("Etelka", "Bala"), new FlashFillInput("Myron", "Lampros")};
                foreach (var name in testInputs)
                {
                    string merged = topRankedProgram.Run(name);
                    Console.WriteLine("{0} => \"{1}\"", name, merged);
                }
                // Instead of a FlashFillInput, .Run() can also take the inputs as an IEnumerable<string>
                //  or as a params string[]:
                Console.WriteLine("\"Nelly\", \"Akesson\" => \"{0}\"",
                    topRankedProgram.Run(new List<string> {"Nelly", "Akesson"}));
                Console.WriteLine("\"Nelly\", \"Akesson\" => \"{0}\"",
                    topRankedProgram.Run("Nelly", "Akesson"));
            }
        }

        /// <summary>
        ///     Look at the top 10 programs learned from a single example for normalizing a phone number like in
        ///     <see cref="LearnNormalizePhoneNumber" /> and show the behavior of them on a couple other phone nubmers.
        ///     Demonstrates learning more than just the single top program, and shows the variation in outputs
        ///     among the top-ranked programs on unseen input formats.
        /// </summary>
        /// <seealso cref="LearnTop10FormatName" />
        private static void LearnTop10NormalizePhoneNumber()
        {
            IEnumerable<FlashFillExample> examples = new[]
            {
                new FlashFillExample("(425) 829 5512", "425-829-5512")
            };
            // Request is for number of distinct rankings, not number of programs,
            //  so more programs will be generated if there are ties.
            int numRankingsToGenerate = 10;
            IList<FlashFillProgram> programs = FlashFillProgram.LearnTopK(examples, k: numRankingsToGenerate).ToList();

            if (!programs.Any())
            {
                Console.Error.WriteLine("Error: failed to learn normalize phone number program.");
            }
            else
            {
                // More than numRankingsToGenerate programs may be generated if there are ties in the ranking.
                Console.WriteLine("Learned {0} programs.", programs.Count);
                // Run all of the programs to see how their output differs.
                for (int i = 0; i < programs.Count; i++)
                {
                    foreach (var phoneNumber in new[] {"425 233 1234", "(425) 777 3333"})
                    {
                        string normalized = programs[i].Run(phoneNumber);
                        Console.WriteLine("Program {2}: \"{0}\" => \"{1}\"", phoneNumber, normalized, (i + 1));
                    }
                }
            }
        }

        /// <summary>
        ///     Look at the top 10 programs learned from a single example for formatting a name like in
        ///     <see cref="LearnFormatName" /> and show the behavior of them on slightly differently formatted name.
        ///     Demonstrates learning more than just the single top program, and shows the variation in outputs
        ///     among the top-ranked programs on unseen input formats.
        /// </summary>
        /// <seealso cref="LearnTop10NormalizePhoneNumber" />
        private static void LearnTop10FormatName()
        {
            var examples = new[] {new FlashFillExample("Greta Hermansson", "Hermansson, G.")};
            IEnumerable<FlashFillProgram> programs = FlashFillProgram.LearnTopK(examples, k: 10);

            // This attempts running the top 10 programs on an input not directly similar to the example
            //  to see if any of them work anyway.
            int i = 0;
            foreach (var program in programs)
            {
                var input = "Kettil hansson"; // Notice it's "hansson", not "Hansson".
                Console.WriteLine("Program {0}: \"{1}\" => \"{2}\"", ++i, input, program.Run(input));
            }
        }

        /// <summary>
        ///     Learns a program to convert dates from "DD/MM/YYYY" to "YYYY-MM-DD".
        ///     Demonstrates providing examples using <see cref="string" /> instead of <see cref="FlashFillExample" />
        ///     and providing additional inputs.
        /// </summary>
        private static void LearnNormalizeDate()
        {
            // Can give FlashFillProgram's .Learn() function an IDictionary<string, string>
            //  instead of an IEnumerable of FlashFillExample.
            IDictionary<string, string> examples = new Dictionary<string, string>
            {
                {"02/04/1953", "1953-04-02"}
            };
            // Inputs for which the corresponding output is not known. May be used for improving ranking.
            // Given as strings instead of FlashFillInputs when the examples are given as Tuple<string, string>.
            IEnumerable<string> additionalInputs = new[]
            {
                "04/02/1962",
                "27/08/1998"
            };
            FlashFillProgram topRankedProgram = FlashFillProgram.Learn(examples, additionalInputs);

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn normalize date program.");
            }
            else
            {
                foreach (var date in new[] {"12/02/1972", "31/01/1983"})
                {
                    string normalized = topRankedProgram.Run(date);
                    Console.WriteLine("\"{0}\" => \"{1}\"", date, normalized);
                }
            }
        }

        /// <summary>
        ///     Learns a programs for formatting a name but serializes and deserializes it before running it.
        ///     Demonstrates serializing a FlashFillProgram to a string.
        /// </summary>
        private static void SerializeProgram()
        {
            IEnumerable<FlashFillExample> examples = new[]
            {
                new FlashFillExample("Kettil Hansson", "Hansson, K.")
            };
            FlashFillProgram topRankedProgram = FlashFillProgram.Learn(examples);

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn format name program.");
            }
            else
            {
                // FlashFillPrograms can be serialized using .ToString().
                string serializedProgram = topRankedProgram.ToString();
                // Serialized programs can be loaded in another program using the FlashFill API using .Load():
                var parsedProgram = FlashFillProgram.Load(serializedProgram);
                foreach (var name in new[] {"Etelka Bala", "Myron Lampros"})
                {
                    string formatted = parsedProgram.Run(name);
                    Console.WriteLine("\"{0}\" => \"{1}\"", name, formatted);
                }
            }
        }
    }
}
