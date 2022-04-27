using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.Transformation.Text;
using Microsoft.ProgramSynthesis.Wrangling;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;

namespace Transformation.Text
{
    /// <summary>
    ///     Sample of how to use the Transformation.Text API. Transformation.Text generates string programs from input/output examples.
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
            LearnTop10ExtractName();
            // Learning with additional inputs:
            LearnNormalizeDate();
            // Convert program to string and back:
            SerializeProgram();

            Console.WriteLine("\n\nDone.");
        }

        /// <summary>
        ///     Learn to reformat a name written "First Last" as "Last, F." where 'F' is the first initial.
        ///     Demonstrates basic usage of Transformation.Text API.
        /// </summary>
        private static void LearnFormatName()
        {
            var session = new Session();

            // Examples are given as an Example object which takes an input and output.
            session.Constraints.Add(
                new Example(new InputRow("Kettil Hansson"), "Hansson, K.")
            );
            // Given just the examples, the best program is returned
            Program topRankedProgram = session.Learn();

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn format name program.");
            }
            else
            {
                // Run the program on some new inputs.
                foreach (var name in new[] { "Etelka Bala", "Myron Lampros" })
                {
                    string formatted = topRankedProgram.Run(new InputRow(name)) as string;
                    Console.WriteLine("\"{0}\" => \"{1}\"", name, formatted);
                }
            }
        }

        /// <summary>
        ///     Learn to normalize phone numbers in a few input formats to the same output format.
        ///     Demonstrates giving Transformation.Text multiple examples.
        /// </summary>
        private static void LearnNormalizePhoneNumber()
        {
            var session = new Session();

            // Some programs may require multiple examples.
            // More examples ensures the proper program is learned and may speed up learning.
            session.Constraints.Add(
                new Example(new InputRow("425-829-5512"), "425-829-5512"),
                new Example(new InputRow("(425) 829 5512"), "425-829-5512")
            );
            Program topRankedProgram = session.Learn();

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn normalize phone number program.");
            }
            else
            {
                foreach (var phoneNumber in new[] { "425 233 1234", "(425) 777 3333" })
                {
                    string normalized = topRankedProgram.Run(new InputRow(phoneNumber)) as string;
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
            var session = new Session();

            // Inputs may be made up of multiple strings. If so, all inputs must contain the same number of strings.
            session.Constraints.Add(
                new Example(new InputRow("Kettil", "Hansson"), "Hansson, Kettil")
            );
            // Inputs for which the corresponding output is not known. May be used for improving ranking.
            session.Inputs.Add(
                new InputRow("Greta", "Hermansson")
            );
            Program topRankedProgram = session.Learn();

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn merge names program.");
            }
            else
            {
                var testInputs = new[] { new InputRow("Etelka", "Bala"), new InputRow("Myron", "Lampros") };
                foreach (var name in testInputs)
                {
                    string merged = topRankedProgram.Run(name) as string;
                    Console.WriteLine("{0} => \"{1}\"", name, merged);
                }
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
            var session = new Session();

            session.Constraints.Add(
                new Example(new InputRow("(425) 829 5512"), "425-829-5512")
            );
            // Request is for number of distinct rankings, not number of programs,
            //  so more programs will be generated if there are ties.
            int numRankingsToGenerate = 10;
            IReadOnlyList<Program> programs = session.LearnTopK(k: numRankingsToGenerate);

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
                    foreach (var phoneNumber in new[] { "425 233 1234", "(425) 777 3333" })
                    {
                        string normalized = programs[i].Run(new InputRow(phoneNumber)) as string;
                        Console.WriteLine("Program {2}: \"{0}\" => \"{1}\"", phoneNumber, normalized, (i + 1));
                    }
                }
            }
        }

        /// <summary>
        ///     Look at the top 10 programs learned from a single example for extracting the last name and show
        ///     the behavior of them on slightly differently formatted name. Demonstrates learning more than just
        ///     the single top program, and shows the variation in outputs among the top-ranked programs on unseen
        ///     input formats.
        /// </summary>
        /// <seealso cref="LearnTop10NormalizePhoneNumber" />
        private static void LearnTop10ExtractName()
        {
            var session = new Session();

            session.Constraints.Add(new Example(new InputRow("Greta Hermansson"), "Hermansson"));
            IReadOnlyList<Program> programs = session.LearnTopK(k: 10);

            // This attempts running the top 10 programs on an input not directly similar to the example
            // to see different behaviours.
            // Here, we will see the outputs:
            // a. "Smith", corresponding to programs that extract the last name.
            // b. "Hansson Smith", corresponding to programs that extract everything after the first name.
            int i = 0;
            foreach (var program in programs)
            {
                var input = new InputRow("Kettil Hansson Smith"); // Notice that we now include a middle name too.
                Console.WriteLine("Program {0}: \"{1}\" => \"{2}\"", ++i, input, program.Run(input));
            }
        }

        /// <summary>
        ///     Learns a program to convert dates from "DD/MM/YYYY" to "YYYY-MM-DD".
        ///     Demonstrates providing examples using <see cref="string" /> instead of <see cref="Example" />
        ///     and providing additional inputs.
        /// </summary>
        private static void LearnNormalizeDate()
        {
            var session = new Session();

            session.Constraints.Add(
                new Example(new InputRow("02/04/1953"), "1953-04-02")
            );
            // Inputs for which the corresponding output is not known. May be used for improving ranking.
            session.Inputs.Add(
                new InputRow("04/02/1962"),
                new InputRow("27/08/1998")
            );
            Program topRankedProgram = session.Learn();

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn normalize date program.");
            }
            else
            {
                foreach (var date in new[] { "12/02/1972", "31/01/1983" })
                {
                    string normalized = topRankedProgram.Run(new InputRow(date)) as string;
                    Console.WriteLine("\"{0}\" => \"{1}\"", date, normalized);
                }
            }
        }

        /// <summary>
        ///     Learns a programs for formatting a name but serializes and deserializes it before running it.
        ///     Demonstrates serializing a Program to a string.
        /// </summary>
        private static void SerializeProgram()
        {
            var session = new Session();

            session.Constraints.Add(
                new Example(new InputRow("Kettil Hansson"), "Hansson, K.")
            );
            Program topRankedProgram = session.Learn();

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn format name program.");
            }
            else
            {
                // Programs can be serialized using .Serialize().
                string serializedProgram = topRankedProgram.Serialize();
                // Serialized programs can be loaded in another program using the Transformation.Text API using .Load():
                var parsedProgram = Loader.Instance.Load(serializedProgram);
                foreach (var name in new[] { "Etelka Bala", "Myron Lampros" })
                {
                    string formatted = parsedProgram.Run(new InputRow(name)) as string;
                    Console.WriteLine("\"{0}\" => \"{1}\"", name, formatted);
                }
            }
        }
    }
}
