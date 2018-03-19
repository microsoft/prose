using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.DslLibrary;
using Microsoft.ProgramSynthesis.Extraction.Text;
using Microsoft.ProgramSynthesis.Extraction.Text.Constraints;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Utils;
using Microsoft.ProgramSynthesis.VersionSpace;
using Microsoft.ProgramSynthesis.Wrangling;


namespace Extraction.Text
{
    /// <summary>
    ///     Extraction.Text learns programs to extract a single string region or a sequence of string regions from text files.
    ///     This class demonstrates some common usage of Extraction.Text APIs.
    /// </summary>
    internal static class LearningSamples
    {
        private static void Main(string[] args)
        {
            LearnRegion();

            LearnRegionUsingMultipleFiles();

            LearnRegionWithNegativeExamples();

            LearnRegionWithAdditionalReferences();

            LearnRegionReferencingParent();

            LearnRegionReferencingPrecedingSibling();

            LearnRegionReferencingSucceedingSibling();

            LearnTop3RegionPrograms();

            LearnAllRegionPrograms();

            LearnRegionWithRegexes();

            SerializeProgram();

            // Learning sequence is similar to learning region. 
            // We only illustrate some API usages. Other sequence learning APIs are similar to their region APIs counterpart.
            // Note: we need to give positive examples continuously. 
            // For instance, suppose we learn a list of {A, B, C, D, E}.
            // {A, B} is a valid set of examples, while {A, C} is not.
            // In case of { A, C}, Extraction.Text assumes that B is a negative example. 
            // This helps our learning converge more quickly.
            LearnSequence();

            LearnSequenceReferencingSibling();

            Console.WriteLine("\n\nDone.");
        }

        /// <summary>
        ///     Learns a program to extract a single region from a file.
        /// </summary>
        private static void LearnRegion()
        {
            var session = new RegionSession();
            StringRegion input = RegionSession.CreateStringRegion("Carrie Dodson 100");

            // Only one example because we extract one region from one file.
            // Position specifies the location between two characters in the file. It starts at 0 (the beginning of the file).
            // An example is identified by a pair of start and end positions.
            session.Constraints.Add(new RegionExample(input, input.Slice(7, 13))); // "Carrie Dodson 100" => "Dodson"

            RegionProgram topRankedProg = session.Learn();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            StringRegion testInput = RegionSession.CreateStringRegion("Leonard Robledo 75"); // expect "Robledo"
            StringRegion output = topRankedProg.Run(testInput);
            if (output == null)
            {
                Console.Error.WriteLine("Error: Extracting fails!");
                return;
            }
            Console.WriteLine("\"{0}\" => \"{1}\"", testInput, output);
        }

        /// <summary>
        ///     Learns a program to extract a single region using two examples in two different files.
        ///     Learning from different files is similar to learning with multiple examples from a single file.
        ///     Demonstrates how to learn with examples from different files.
        /// </summary>
        private static void LearnRegionUsingMultipleFiles()
        {
            var session = new RegionSession();
            StringRegion input1 = RegionSession.CreateStringRegion("Carrie Dodson 100");
            StringRegion input2 = RegionSession.CreateStringRegion("Leonard Robledo 75");

            session.Constraints.Add(
                new RegionExample(input1, input1.Slice(7, 13)), // "Carrie Dodson 100" => "Dodson"
                new RegionExample(input2, input2.Slice(8, 15)) // "Leonard Robledo 75" => "Robledo"
            );

            RegionProgram topRankedProg = session.Learn();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            StringRegion testInput = RegionSession.CreateStringRegion("Margaret Cook 320"); // expect "Cook"
            StringRegion output = topRankedProg.Run(testInput);
            if (output == null)
            {
                Console.Error.WriteLine("Error: Extracting fails!");
                return;
            }
            Console.WriteLine("\"{0}\" => \"{1}\"", testInput, output);
        }


        /// <summary>
        ///     Learns a program to extract a region with both positive and negative examples.
        ///     Demonstrates the use of negative examples.
        /// </summary>
        private static void LearnRegionWithNegativeExamples()
        {
            var session = new RegionSession();
            StringRegion input =
                RegionSession.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo NA\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract "100", "320".
            session.Constraints.Add(
                new RegionExample(records[0], records[0].Slice(14, 17)), // "Carrie Dodson 100" => "100"
                new RegionNegativeExample(records[1], records[1]) // no extraction in "Leonard Robledo NA"
            );

            // Extraction.Text will find a program whose output does not OVERLAP with any of the negative examples.
            RegionProgram topRankedProg = session.Learn();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion record in records)
            {
                string output = topRankedProg.Run(record)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", record, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a region and provides other references to help find the intended program.
        ///     Demonstrates the use of additional references.
        /// </summary>
        private static void LearnRegionWithAdditionalReferences()
        {
            var session = new RegionSession();
            StringRegion input =
                RegionSession.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook ***");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract "100", "75", and "***".
            session.Constraints.Add(new RegionExample(records[0], records[0].Slice(14, 17)));
                // "Carrie Dodson 100" => "100"

            // Additional references help Extraction.Text observe the behavior of the learnt programs on unseen data.
            // In this example, if we do not use additional references, Extraction.Text may learn a program that extracts the first number.
            // On the contrary, if other references are present, it knows that this program is not applicable on the third record "Margaret Cook ***",
            // and promotes a more applicable program.
            session.Inputs.Add(records.Skip(1));

            RegionProgram topRankedProg = session.Learn();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion record in records)
            {
                string output = topRankedProg.Run(record)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", record, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a single region from a containing region (i.e., parent region).
        ///     Demonstrates how parent referencing works.
        /// </summary>
        private static void LearnRegionReferencingParent()
        {
            var session = new RegionSession();
            StringRegion input =
                RegionSession.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract the number out of a record
            session.Constraints.Add(
                new RegionExample(records[0], records[0].Slice(14, 17)), // "Carrie Dodson 100" => "100"
                new RegionExample(records[1], records[1].Slice(34, 36)) // "Leonard Robledo 75" => "75"
            );

            RegionProgram topRankedProg = session.Learn();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion record in records)
            {
                string output = topRankedProg.Run(record)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", record, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a single region using another region that appears before it as reference (i.e.,
        ///     preceding sibling region).
        ///     Demonstrates how sibling referencing works.
        /// </summary>
        private static void LearnRegionReferencingPrecedingSibling()
        {
            var session = new RegionSession();
            StringRegion input =
                RegionSession.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };
            StringRegion[] firstNames = { input.Slice(0, 6), input.Slice(18, 25), input.Slice(37, 45) };

            // Suppose we want to extract the number w.r.t the first name
            session.Constraints.Add(
                new RegionExample(firstNames[0], records[0].Slice(14, 17)), // "Carrie" => "100"
                new RegionExample(firstNames[1], records[1].Slice(34, 36)) // "Leonard" => "75"
            );

            RegionProgram topRankedProg = session.Learn();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion firstName in firstNames)
            {
                string output = topRankedProg.Run(firstName)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", firstName, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a single region using another region that appears after it as reference (i.e.,
        ///     succeeding sibling region).
        ///     Demonstrates how sibling referencing works.
        /// </summary>
        private static void LearnRegionReferencingSucceedingSibling()
        {
            var session = new RegionSession();
            StringRegion input =
                RegionSession.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };
            StringRegion[] numbers = { input.Slice(14, 17), input.Slice(34, 36), input.Slice(51, 54) };

            // Suppose we want to extract the first name w.r.t the number
            session.Constraints.Add(
                new RegionExample(numbers[0], records[0].Slice(0, 6)), // "Carrie" => "100"
                new RegionExample(numbers[1], records[1].Slice(18, 25)) // "Leonard" => "75"
            );

            RegionProgram topRankedProg = session.Learn();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion number in numbers)
            {
                string output = topRankedProg.Run(number)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", number, output);
            }
        }

        /// <summary>
        ///     Learns top-ranked 3 region programs.
        ///     Demonstrates access to lower-ranked programs.
        /// </summary>
        private static void LearnTop3RegionPrograms()
        {
            var session = new RegionSession();
            StringRegion input = RegionSession.CreateStringRegion("Carrie Dodson 100");

            session.Constraints.Add(new RegionExample(input, input.Slice(14, 17))); // "Carrie Dodson 100" => "Dodson"

            IEnumerable<RegionProgram> topKPrograms = session.LearnTopK(3);

            var i = 0;
            StringRegion[] otherInputs =
            {
                input, RegionSession.CreateStringRegion("Leonard Robledo NA"),
                RegionSession.CreateStringRegion("Margaret Cook 320")
            };
            foreach (RegionProgram prog in topKPrograms)
            {
                Console.WriteLine("Program {0}:", ++i);
                foreach (StringRegion str in otherInputs)
                {
                    var r = prog.Run(str);
                    Console.WriteLine(r != null ? r.Value : "null");
                }
            }
        }


        /// <summary>
        ///     Learns all region programs that satisfy the examples (advanced feature).
        ///     Demonstrates access to the entire program set.
        /// </summary>
        private static void LearnAllRegionPrograms()
        {
            var session = new RegionSession();
            StringRegion input = RegionSession.CreateStringRegion("Carrie Dodson 100");

            session.Constraints.Add(new RegionExample(input, input.Slice(14, 17))); // "Carrie Dodson 100" => "Dodson"

            ProgramSet allPrograms = session.LearnAll().ProgramSet;
            IEnumerable<ProgramNode> topKPrograms = allPrograms.TopK(RegionLearner.Instance.ScoreFeature, 3);

            var i = 0;
            StringRegion[] otherInputs =
            {
                input, RegionSession.CreateStringRegion("Leonard Robledo NA"),
                RegionSession.CreateStringRegion("Margaret Cook 320")
            };
            foreach (ProgramNode programNode in topKPrograms)
            {
                Console.WriteLine("Program {0}:", ++i);
                var program = new RegionProgram(programNode, ReferenceKind.Parent);
                foreach (StringRegion str in otherInputs)
                {
                    StringRegion r = program.Run(str);
                    Console.WriteLine(r == null ? "null" : r.Value);
                }
            }
        }


        /// <summary>
        ///     Learns a program to extract a region using positive examples and the matching regular expression.
        ///     Demonstrates the possibility to give other constraint (regex) to Extraction.Text.
        ///     This is an advanced feature.
        /// </summary>
        private static void LearnRegionWithRegexes()
        {
            StringRegion input =
                RegionSession.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo NA\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract the number out of a record
            var examples = new[]
            {
                new RegionExample(records[0], records[0].Slice(14, 17)), // "Carrie Dodson 100" => "100"
            };

            Regex lookBehindRegex = new Regex("\\s");
            Regex lookAheadRegex = null;
            Regex matchingRegex = new Regex("\\d+");

            IEnumerable<RegionProgram> topRankedPrograms = RegionLearner.Instance.LearnTopK(examples, 
                                                                                            RegionLearner.Instance.ScoreFeature,
                                                                                            1,
                                                                                            null,
                                                                                            default(ProgramSamplingStrategy),
                                                                                            null,
                                                                                            lookBehindRegex,
                                                                                            matchingRegex,
                                                                                            lookAheadRegex).TopPrograms;

            RegionProgram topRankedProg = topRankedPrograms.FirstOrDefault();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion record in records)
            {
                string output = topRankedProg.Run(record)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", record, output);
            }
        }


        /// <summary>
        ///     Learns to serialize and deserialize Extraction.Text program.
        /// </summary>
        private static void SerializeProgram()
        {
            var session = new RegionSession();
            StringRegion input = RegionSession.CreateStringRegion("Carrie Dodson 100");

            session.Constraints.Add(new RegionExample(input, input.Slice(7, 13))); // "Carrie Dodson 100" => "Dodson"

            RegionProgram topRankedProg = session.Learn();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            string serializedProgram = topRankedProg.Serialize();
            RegionProgram deserializedProgram = Loader.Instance.Region.Load(serializedProgram);
            StringRegion testInput = RegionSession.CreateStringRegion("Leonard Robledo 75"); // expect "Robledo"
            StringRegion output = deserializedProgram.Run(testInput);
            if (output == null)
            {
                Console.Error.WriteLine("Error: Extracting fails!");
                return;
            }
            Console.WriteLine("\"{0}\" => \"{1}\"", testInput, output);
        }

        /// <summary>
        ///     Learns a program to extract a sequence of regions from a file.
        /// </summary>
        private static void LearnSequence()
        {
            var session = new SequenceSession();
            // It is advised to learn a sequence with at least 2 examples because generalizing a sequence from a single element is hard.
            // Also, we need to give positive examples continuously (i.e., we cannot skip any example).
            var input =
                SequenceSession.CreateStringRegion(
                    "United States\n Carrie Dodson 100\n Leonard Robledo 75\n Margaret Cook 320\n" +
                    "Canada\n Concetta Beck 350\n Nicholas Sayers 90\n Francis Terrill 2430\n" +
                    "New Zealand\n Nettie Pope 50\n Mack Beeson 1070");
            // Suppose we want to extract all last names from the input string.
            session.Constraints.Add(
                new SequenceExample(input, new[]
                {
                    input.Slice(15, 21), // input => "Carrie"
                    input.Slice(34, 41), // input => "Leonard"
                })
            );

            SequenceProgram topRankedProg = session.Learn();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion r in topRankedProg.Run(input))
            {
                string output = r != null ? r.Value : "null";
                Console.WriteLine(output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a sequence of regions using its preceding sibling as reference.
        /// </summary>
        private static void LearnSequenceReferencingSibling()
        {
            var session = new SequenceSession();
            var input =
                SequenceSession.CreateStringRegion(
                    "United States\n Carrie Dodson 100\n Leonard Robledo 75\n Margaret Cook 320\n" +
                    "Canada\n Concetta Beck 350\n Nicholas Sayers 90\n Francis Terrill 2430\n" +
                    "New Zealand\n Nettie Pope 50\n Mack Beeson 1070");
            // areas = { "United States", "Canada", "New Zealand" }
            StringRegion[] areas = { input.Slice(0, 13), input.Slice(72, 78), input.Slice(140, 151) };

            // Suppose we want to extract all last names from the input string.
            session.Constraints.Add(
                new SequenceExample(areas[0], new[]
                {
                    input.Slice(15, 21), // "United States" => "Carrie"
                    input.Slice(34, 41), // "United States" => "Leonard"
                })
            );

            SequenceProgram topRankedProg = session.Learn();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            // Note: we can't use SequenceProgram.Run(StringRegion) because of sibling referencing.
            // Read the documentation for more information.
            IEnumerable<IEnumerable<StringRegion>> outputSeq = topRankedProg.Run(areas);
            foreach (Record<IEnumerable<StringRegion>, StringRegion> tup in outputSeq.ZipWith(areas))
            {
                foreach (StringRegion output in tup.Item1)
                {
                    Console.WriteLine("\"{0}\" => \"{1}\"", tup.Item2, output == null ? "null" : output.Value);
                }
            }
        }
    }
}
