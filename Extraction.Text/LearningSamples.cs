using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.VersionSpace;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;

namespace Microsoft.ProgramSynthesis.Extraction.Text.Sample {
    /// <summary>
    ///     Extraction.Text learns programs to extract a single string region or a sequence of string regions from text files.
    ///     This class demonstrates some common usage of Extraction.Text APIs.
    /// </summary>
    internal static class LearningSamples {
        private static void Main(string[] args) {

            LearnRegion();

            LearnRegionUsingMultipleFiles();

            LearnRegionWithNegativeExamples();

            LearnRegionWithAdditionalReferences();

            LearnRegionReferencingParent();

            LearnRegionReferencingPrecedingSibling();

            LearnRegionReferencingSucceedingSibling();

            LearnTop3RegionPrograms();

            LearnAllRegionPrograms();

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
        private static void LearnRegion() {
            var input = RegionLearner.CreateStringRegion("Carrie Dodson 100");

            // Only one example because we extract one region from one file.
            // Position specifies the location between two characters in the file. It starts at 0 (the beginning of the file).
            // An example is identified by a pair of start and end positions.
            var examples = new[] {
                new CorrespondingMemberEquals<StringRegion, StringRegion>(input, input.Slice(7, 13)) // "Carrie Dodson 100" => "Dodson"
            };

            RegionProgram topRankedProg = RegionLearner.Instance.Learn(examples);
            if (topRankedProg == null) {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            var testInput = RegionLearner.CreateStringRegion("Leonard Robledo 75"); // expect "Robledo"
            StringRegion output = topRankedProg.Run(testInput);
            if (output == null) {
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
        private static void LearnRegionUsingMultipleFiles() {
            var input1 = RegionLearner.CreateStringRegion("Carrie Dodson 100");
            var input2 = RegionLearner.CreateStringRegion("Leonard Robledo 75");

            var examples = new[] {
                new CorrespondingMemberEquals<StringRegion, StringRegion>(input1, input1.Slice(7, 13)), // "Carrie Dodson 100" => "Dodson"
                new CorrespondingMemberEquals<StringRegion, StringRegion>(input2, input2.Slice(8, 15)) // "Leonard Robledo 75" => "Robledo"
            };

            RegionProgram topRankedProg = RegionLearner.Instance.Learn(examples);
            if (topRankedProg == null) {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            var testInput = RegionLearner.CreateStringRegion("Margaret Cook 320"); // expect "Cook"
            StringRegion output = topRankedProg.Run(testInput);
            if (output == null) {
                Console.Error.WriteLine("Error: Extracting fails!");
                return;
            }
            Console.WriteLine("\"{0}\" => \"{1}\"", testInput, output);
        }


        /// <summary>
        ///     Learns a program to extract a region with both positive and negative examples.
        ///     Demonstrates the use of negative examples.
        /// </summary>
        private static void LearnRegionWithNegativeExamples() {
            var input = RegionLearner.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo NA\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract "100", "320".
            var constraints = new Constraint<IEnumerable<StringRegion>, IEnumerable<StringRegion>>[] {
                new CorrespondingMemberEquals<StringRegion, StringRegion>(records[0], records[0].Slice(14, 17)), // "Carrie Dodson 100" => "100"
                new CorrespondingMemberDoesNotIntersect<StringRegion>(records[1], records[1]) // no extraction in "Leonard Robledo NA"
            };

            // Extraction.Text will find a program whose output does not OVERLAP with any of the negative examples.
            RegionProgram topRankedProg = RegionLearner.Instance.Learn(constraints);
            if (topRankedProg == null) {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion record in records) {
                string output = topRankedProg.Run(record)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", record, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a region and provides other references to help find the intended program.
        ///     Demonstrates the use of additional references.
        /// </summary>
        private static void LearnRegionWithAdditionalReferences() {
            var input = RegionLearner.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook ***");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract "100", "75", and "***".
            var examples = new[] {
                new CorrespondingMemberEquals<StringRegion, StringRegion>(records[0], records[0].Slice(14, 17)) // "Carrie Dodson 100" => "100"
            };

            // Additional references help Extraction.Text observe the behavior of the learnt programs on unseen data.
            // In this example, if we do not use additional references, Extraction.Text may learn a program that extracts the first number.
            // On the contrary, if other references are present, it knows that this program is not applicable on the third record "Margaret Cook ***",
            // and promotes a more applicable program.
            RegionProgram topRankedProg = RegionLearner.Instance.Learn(examples, new[] { records.Skip(1) });
            if (topRankedProg == null) {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion record in records) {
                string output = topRankedProg.Run(record)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", record, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a single region from a containing region (i.e., parent region).
        ///     Demonstrates how parent referencing works.
        /// </summary>
        private static void LearnRegionReferencingParent() {
            var input = RegionLearner.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

            // Suppose we want to extract the number out of a record
            var examples = new[] {
                new CorrespondingMemberEquals<StringRegion, StringRegion>(records[0], records[0].Slice(14, 17)), // "Carrie Dodson 100" => "100"
                new CorrespondingMemberEquals<StringRegion, StringRegion>(records[1], records[1].Slice(34, 36)) // "Leonard Robledo 75" => "75"
            };

            RegionProgram topRankedProg = RegionLearner.Instance.Learn(examples);
            if (topRankedProg == null) {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion record in records) {
                string output = topRankedProg.Run(record)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", record, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a single region using another region that appears before it as reference (i.e.,
        ///     preceding sibling region).
        ///     Demonstrates how sibling referencing works.
        /// </summary>
        private static void LearnRegionReferencingPrecedingSibling() {
            var input = RegionLearner.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };
            StringRegion[] firstNames = { input.Slice(0, 6), input.Slice(18, 25), input.Slice(37, 45) };

            // Suppose we want to extract the number w.r.t the first name
            var examples = new[] {
                new CorrespondingMemberEquals<StringRegion, StringRegion>(firstNames[0], records[0].Slice(14, 17)), // "Carrie" => "100"
                new CorrespondingMemberEquals<StringRegion, StringRegion>(firstNames[1], records[1].Slice(34, 36)) // "Leonard" => "75"
            };

            RegionProgram topRankedProg = RegionLearner.Instance.Learn(examples);
            if (topRankedProg == null) {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion firstName in firstNames) {
                string output = topRankedProg.Run(firstName)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", firstName, output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a single region using another region that appears after it as reference (i.e.,
        ///     succeeding sibling region).
        ///     Demonstrates how sibling referencing works.
        /// </summary>
        private static void LearnRegionReferencingSucceedingSibling() {
            var input = RegionLearner.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320");
            StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };
            StringRegion[] numbers = { input.Slice(14, 17), input.Slice(34, 36), input.Slice(51, 54) };

            // Suppose we want to extract the first name w.r.t the number
            var examples = new[] {
                new CorrespondingMemberEquals<StringRegion, StringRegion>(numbers[0], records[0].Slice(0, 6)), // "Carrie" => "100"
                new CorrespondingMemberEquals<StringRegion, StringRegion>(numbers[1], records[1].Slice(18, 25)) // "Leonard" => "75"
            };

            RegionProgram topRankedProg = RegionLearner.Instance.Learn(examples);
            if (topRankedProg == null) {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (StringRegion number in numbers) {
                string output = topRankedProg.Run(number)?.Value ?? "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", number, output);
            }
        }

        /// <summary>
        ///     Learns top-ranked 3 region programs.
        ///     Demonstrates access to lower-ranked programs.
        /// </summary>
        private static void LearnTop3RegionPrograms() {
            var input = RegionLearner.CreateStringRegion("Carrie Dodson 100");

            var examples = new[] {
                new CorrespondingMemberEquals<StringRegion, StringRegion>(input, input.Slice(14, 17)) // "Carrie Dodson 100" => "Dodson"
            };

            IEnumerable<RegionProgram> topKPrograms = RegionLearner.Instance.LearnTopK(examples, 3);

            var i = 0;
            StringRegion[] otherInputs = {
                input, RegionLearner.CreateStringRegion("Leonard Robledo NA"),
                RegionLearner.CreateStringRegion("Margaret Cook 320")
            };
            foreach (var prog in topKPrograms) {
                Console.WriteLine("Program {0}:", ++i);
                foreach (var str in otherInputs) {
                    var r = prog.Run(str);
                    Console.WriteLine(r != null ? r.Value : "null");
                }
            }
        }


        /// <summary>
        ///     Learns all region programs that satisfy the examples (advanced feature).
        ///     Demonstrates access to the entire program set.
        /// </summary>
        private static void LearnAllRegionPrograms() {
            var input = RegionLearner.CreateStringRegion("Carrie Dodson 100");

            var examples = new[] {
                new CorrespondingMemberEquals<StringRegion, StringRegion>(input, input.Slice(14, 17)) // "Carrie Dodson 100" => "Dodson"
            };

            ProgramSet allPrograms = RegionLearner.Instance.LearnAll(examples);
            IEnumerable<ProgramNode> topKPrograms =
                allPrograms.TopK(RegionLearner.Instance.ScoreFeature, 3); // "Score" is the ranking feature

            var i = 0;
            StringRegion[] otherInputs = {
                input, RegionLearner.CreateStringRegion("Leonard Robledo NA"),
                RegionLearner.CreateStringRegion("Margaret Cook 320")
            };
            foreach (var prog in topKPrograms) {
                Console.WriteLine("Program {0}:", ++i);
                foreach (var str in otherInputs) {
                    State inputState = State.Create(Language.Grammar.InputSymbol, str); // Create Microsoft.ProgramSynthesis input state
                    object r = prog.Invoke(inputState); // Invoke Microsoft.ProgramSynthesis program node on the input state
                    Console.WriteLine(r != null ? (r as StringRegion).Value : "null");
                }
            }
        }


        /// <summary>
        ///     Learns to serialize and deserialize Extraction.Text program.
        /// </summary>
        private static void SerializeProgram() {
            var input = RegionLearner.CreateStringRegion("Carrie Dodson 100");

            var examples = new[] {
                new CorrespondingMemberEquals<StringRegion, StringRegion>(input, input.Slice(7, 13)) // "Carrie Dodson 100" => "Dodson"
            };

            RegionProgram topRankedProg = RegionLearner.Instance.Learn(examples);
            if (topRankedProg == null) {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            string serializedProgram = topRankedProg.Serialize();
            RegionProgram deserializedProgram = Loader.Instance.Region.Load(serializedProgram);
            var testInput = RegionLearner.CreateStringRegion("Leonard Robledo 75"); // expect "Robledo"
            StringRegion output = deserializedProgram.Run(testInput);
            if (output == null) {
                Console.Error.WriteLine("Error: Extracting fails!");
                return;
            }
            Console.WriteLine("\"{0}\" => \"{1}\"", testInput, output);
        }

        /// <summary>
        ///     Learns a program to extract a sequence of regions using its preceding sibling as reference.
        /// </summary>
        private static void LearnSequence() {
            // It is advised to learn a sequence with at least 2 examples because generalizing a sequence from a single element is hard.
            // Also, we need to give positive examples continuously (i.e., we cannot skip any example).
            var input =
                SequenceLearner.CreateStringRegion(
                    "United States\nCarrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320\n" +
                    "Canada\nConcetta Beck 350\nNicholas Sayers 90\nFrancis Terrill 2430\n" +
                    "Great Britain\nNettie Pope 50\nMack Beeson 1070");
            // Suppose we want to extract all last names from the input string.
            var examples = new[] {
                new MemberPrefix<StringRegion, StringRegion>(input, new[] {
                    input.Slice(14, 20), // input => "Carrie"
                    input.Slice(32, 39), // input => "Leonard"
                })
            };

            SequenceProgram topRankedProg = SequenceLearner.Instance.Learn(examples);
            if (topRankedProg == null) {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (var r in topRankedProg.Run(input)) {
                var output = r != null ? r.Value : "null";
                Console.WriteLine(output);
            }
        }

        /// <summary>
        ///     Learns a program to extract a sequence of regions from a file.
        /// </summary>
        private static void LearnSequenceReferencingSibling() {
            var input =
                SequenceLearner.CreateStringRegion(
                    "United States\nCarrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320\n" +
                    "Canada\nConcetta Beck 350\nNicholas Sayers 90\nFrancis Terrill 2430\n" +
                    "Great Britain\nNettie Pope 50\nMack Beeson 1070");
            StringRegion[] areas = { input.Slice(0, 13), input.Slice(69, 75), input.Slice(134, 147) };

            // Suppose we want to extract all last names from the input string.
            var examples = new[] {
                new MemberPrefix<StringRegion, StringRegion>(input, new[] {
                    input.Slice(14, 20), // input => "Carrie"
                    input.Slice(32, 39), // input => "Leonard"
                })
            };

            SequenceProgram topRankedProg = SequenceLearner.Instance.Learn(examples);
            if (topRankedProg == null) {
                Console.Error.WriteLine("Error: Learning fails!");
                return;
            }

            foreach (var a in areas
                .SelectMany(area => topRankedProg.Run(area)
                                                    .Select(output => new { Input = area, Output = output }))) {
                var output = a.Output != null ? a.Output.Value : "null";
                Console.WriteLine("\"{0}\" => \"{1}\"", a.Input, output);
            }
        }
    }
}
