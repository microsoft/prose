using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis.Extraction;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.VersionSpace;

namespace Microsoft.ProgramSynthesis.Extraction.Text.Sample
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
        }

        /// <summary>
        ///     Learns a program to extract a single region from a file.
        /// </summary>
        private static void LearnRegion()
        {
            var input = StringRegion.Create("Carrie Dodson 100");

            // Only one example because we extract one region from one file.
            // Position specifies the location between two characters in the file. It starts at 0 (the beginning of the file).
            // An example is identified by a pair of start and end positions.
            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(input, input.Slice(7, 13)) // "Carrie Dodson 100" => "Dodson"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            Program topRankedProg = Learner.Instance.LearnRegion(positiveExamples, negativeExamples);
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            var testInput = StringRegion.Create("Leonard Robledo 75"); // expect "Robledo"
            IEnumerable<StringRegion> run = topRankedProg.Run(testInput);
            // Retrieve the first element because this is a region textProgram
            var output = run.FirstOrDefault();
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
            var input1 = StringRegion.Create("Carrie Dodson 100");
            var input2 = StringRegion.Create("Leonard Robledo 75");

            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(input1, input1.Slice(7, 13)), // "Carrie Dodson 100" => "Dodson"
                new ExtractionExample<StringRegion>(input2, input2.Slice(8, 15)) // "Leonard Robledo 75" => "Robledo"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            Program topRankedProg = Learner.Instance.LearnRegion(positiveExamples, negativeExamples);
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            var testInput = StringRegion.Create("Margaret Cook 320"); // expect "Cook"
            IEnumerable<StringRegion> run = topRankedProg.Run(testInput);
            // Retrieve the first element because this is a region textProgram
            var output = run.FirstOrDefault();
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
            var input = StringRegion.Create("Carrie Dodson 100\nLeonard Robledo NA\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract "100", "320".
            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(records[0], records[0].Slice(14, 17)) // "Carrie Dodson 100" => "100"
            };
            var negativeExamples = new[] {
                new ExtractionExample<StringRegion>(records[1], records[1]) // no extraction in "Leonard Robledo NA"
            };

            // Extraction.Text will find a program whose output does not OVERLAP with any of the negative examples.
            Program topRankedProg = Learner.Instance.LearnRegion(positiveExamples, negativeExamples);
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (var r in topRankedProg.Run(records))
            {
                var output = r.Output != null ? r.Output.Value : "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a region and provides other references to help find the intended program.
        ///     Demonstrates the use of additional references.
        /// </summary>
        private static void LearnRegionWithAdditionalReferences()
        {
            var input = StringRegion.Create("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook ***");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract "100", "75", and "***".
            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(records[0], records[0].Slice(14, 17)) // "Carrie Dodson 100" => "100"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            // Additional references help Extraction.Text observe the behavior of the learnt programs on unseen data.
            // In this example, if we do not use additional references, Extraction.Text may learn a program that extracts the first number.
            // On the contrary, if other references are present, it knows that this program is not applicable on the third record "Margaret Cook ***",
            // and promotes a more applicable program.
            Program topRankedProg = Learner.Instance.LearnRegion(positiveExamples, negativeExamples, records.Skip(1));
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (var r in topRankedProg.Run(records))
            {
                var output = r.Output != null ? r.Output.Value : "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a single region from a containing region (i.e., parent region).
        ///     Demonstrates how parent referencing works.
        /// </summary>
        private static void LearnRegionReferencingParent()
        {
            var input = StringRegion.Create("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract the number out of a record
            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(records[0], records[0].Slice(14, 17)), // "Carrie Dodson 100" => "100"
                new ExtractionExample<StringRegion>(records[1], records[1].Slice(34, 36)) // "Leonard Robledo 75" => "75"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            Program topRankedProg = Learner.Instance.LearnRegion(positiveExamples, negativeExamples);
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (var r in topRankedProg.Run(records))
            {
                var output = r.Output != null ? r.Output.Value : "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a single region using another region that appears before it as reference (i.e.,
        ///     preceding sibling region).
        ///     Demonstrates how sibling referencing works.
        /// </summary>
        private static void LearnRegionReferencingPrecedingSibling()
        {
            var input = StringRegion.Create("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };
            StringRegion[] firstNames = { input.Slice(0, 6), input.Slice(18, 25), input.Slice(37, 45) };

            // Suppose we want to extract the number w.r.t the first name
            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(firstNames[0], records[0].Slice(14, 17)), // "Carrie" => "100"
                new ExtractionExample<StringRegion>(firstNames[1], records[1].Slice(34, 36)) // "Leonard" => "75"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            Program topRankedProg = Learner.Instance.LearnRegion(positiveExamples, negativeExamples);
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (var r in topRankedProg.Run(firstNames))
            {
                var output = r.Output != null ? r.Output.Value : "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a single region using another region that appears after it as reference (i.e.,
        ///     succeeding sibling region).
        ///     Demonstrates how sibling referencing works.
        /// </summary>
        private static void LearnRegionReferencingSucceedingSibling()
        {
            var input = StringRegion.Create("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };
            StringRegion[] numbers = { input.Slice(14, 17), input.Slice(34, 36), input.Slice(51, 54) };

            // Suppose we want to extract the first name w.r.t the number
            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(numbers[0], records[0].Slice(0, 6)), // "Carrie" => "100"
                new ExtractionExample<StringRegion>(numbers[1], records[1].Slice(18, 25)) // "Leonard" => "75"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            Program topRankedProg = Learner.Instance.LearnRegion(positiveExamples, negativeExamples);
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (var r in topRankedProg.Run(numbers))
            {
                var output = r.Output != null ? r.Output.Value : "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
            }
        }

        /// <summary>
        ///     Learns top-ranked 3 region programs.
        ///     Demonstrates access to lower-ranked programs.
        /// </summary>
        private static void LearnTop3RegionPrograms()
        {
            var input = StringRegion.Create("Carrie Dodson 100");

            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(input, input.Slice(14, 17)) // "Carrie Dodson 100" => "Dodson"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            IEnumerable<Program> topKPrograms = Learner.Instance.LearnTopKRegion(positiveExamples, negativeExamples, 3);

            var i = 0;
            StringRegion[] otherInputs = { input, StringRegion.Create("Leonard Robledo NA"), StringRegion.Create("Margaret Cook 320") };
            foreach (var prog in topKPrograms)
            {
                Console.WriteLine("Program {0}:", ++i);
                foreach (var str in otherInputs)
                {
                    var r = prog.Run(str).FirstOrDefault();
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
            var input = StringRegion.Create("Carrie Dodson 100");

            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(input, input.Slice(14, 17)) // "Carrie Dodson 100" => "Dodson"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            ProgramSet allPrograms = Learner.Instance.LearnAllRegion(positiveExamples, negativeExamples);
            IEnumerable<ProgramNode> topKPrograms = allPrograms.TopK("Score", 3); // "Score" is the ranking feature

            var i = 0;
            StringRegion[] otherInputs = { input, StringRegion.Create("Leonard Robledo NA"), StringRegion.Create("Margaret Cook 320") };
            foreach (var prog in topKPrograms)
            {
                Console.WriteLine("Program {0}:", ++i);
                foreach (var str in otherInputs)
                {
                    State inputState = State.Create(Language.Grammar.InputSymbol, str); // Create Microsoft.ProgramSynthesis input state
                    object r = prog.Invoke(inputState); // Invoke Microsoft.ProgramSynthesis program node on the input state
                    Console.WriteLine(r != null ? (r as StringRegion).Value : "null");
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
            var input = StringRegion.Create("Carrie Dodson 100\nLeonard Robledo NA\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract the number out of a record
            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(records[0], records[0].Slice(14, 17)), // "Carrie Dodson 100" => "100"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            Regex lookBehindRegex = new Regex("\\s");
            Regex lookAheadRegex = null;
            Regex matchingRegex = new Regex("\\d+");

            IEnumerable<Program> topRankedPrograms = 
                Learner.Instance.LearnTopKRegion(positiveExamples, negativeExamples, null, 1, lookBehindRegex, matchingRegex, lookAheadRegex);

            Program topRankedProg = topRankedPrograms.FirstOrDefault();
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (var r in topRankedProg.Run(records))
            {
                var output = r.Output != null ? r.Output.Value : "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
            }
        }


        /// <summary>
        ///     Learns to serialize and deserialize Extraction.Text program.
        /// </summary>
        private static void SerializeProgram()
        {
            var input = StringRegion.Create("Carrie Dodson 100");

            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(input, input.Slice(7, 13)) // "Carrie Dodson 100" => "Dodson"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            Program topRankedProg = Learner.Instance.LearnRegion(positiveExamples, negativeExamples);
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            string serializedProgram = topRankedProg.Serialize();
            Program deserializedProgram = Program.Load(serializedProgram);
            var testInput = StringRegion.Create("Leonard Robledo 75"); // expect "Robledo"
            IEnumerable<StringRegion> run = deserializedProgram.Run(testInput);
            // Retrieve the first element because this is a region textProgram
            var output = run.FirstOrDefault();
            if (output == null)
            {
                Console.Error.WriteLine("Error: Extracting fails!");
                return;
            }
            Console.WriteLine("\"{0}\" => \"{1}\"", testInput, output);
        }

        /// <summary>
        ///     Learns a program to extract a sequence of regions using its preceding sibling as reference.
        /// </summary>
        private static void LearnSequence()
        {
            // It is advised to learn a sequence with at least 2 examples because generalizing a sequence from a single element is hard.
            // Also, we need to give positive examples continuously (i.e., we cannot skip any example).
            var input = StringRegion.Create("United States\nCarrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320\n" +
                                            "Canada\nConcetta Beck 350\nNicholas Sayers 90\nFrancis Terrill 2430\n" +
                                            "Great Britain\nNettie Pope 50\nMack Beeson 1070");
            // Suppose we want to extract all last names from the input string.
            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(input, input.Slice(14, 20)), // input => "Carrie"
                new ExtractionExample<StringRegion>(input, input.Slice(32, 39)) // input => "Leonard"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            Program topRankedProg = Learner.Instance.LearnSequence(positiveExamples, negativeExamples);
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (var r in topRankedProg.Run(input))
            {
                var output = r != null ? r.Value : "null";
                Console.WriteLine(output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a sequence of regions from a file.
        /// </summary>
        private static void LearnSequenceReferencingSibling()
        {
            var input = StringRegion.Create("United States\nCarrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320\n" +
                                            "Canada\nConcetta Beck 350\nNicholas Sayers 90\nFrancis Terrill 2430\n" +
                                            "Great Britain\nNettie Pope 50\nMack Beeson 1070");
            StringRegion[] countries = { input.Slice(0, 13), input.Slice(69, 75), input.Slice(134, 147) };

            // Suppose we want to extract all last names from the input string.
            var positiveExamples = new[] {
                new ExtractionExample<StringRegion>(countries[0], input.Slice(14, 20)), // "United States" => "Carrie"
                new ExtractionExample<StringRegion>(countries[0], input.Slice(32, 39)), // "United States" => "Leonard"
            };
            var negativeExamples = Enumerable.Empty<ExtractionExample<StringRegion>>();

            Program topRankedProg = Learner.Instance.LearnSequence(positiveExamples, negativeExamples);
            if (topRankedProg == null)
            {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (var r in topRankedProg.Run(countries))
            {
                var output = r.Output != null ? r.Output.Value : "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
            }
        }
    }
}
