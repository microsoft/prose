using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis.Transformation.Formula;
using Microsoft.ProgramSynthesis.Transformation.Formula.Constraints;
using Microsoft.ProgramSynthesis.Transformation.Formula.Translation;
using Microsoft.ProgramSynthesis.Translation;
using Microsoft.ProgramSynthesis.Wrangling;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;

namespace Transformation.Formula
{
    /// <summary>
    ///     Sample of how to use the Transformation.Formula API. Transformation.Formula generates programs from input/output examples.
    /// </summary>
    internal static class SampleProgram {
        private static void Main(string[] args) {
            // Simplest usage: learn from one or more examples with ONE source string:
            LearnFormatName();
            // Learning top-k ranked programs instead of just the single best one:
            LearnTop10ExtractName();
            // Learning with additional inputs:
            LearnNormalizeDate();
            // Learning with unicode regexes enabled and disabled:
            LearnUnicodeRegexes();
            // Complete scenario: Learn without unicode regexes, but if it fails, then with it.
            LearnWithoutUnicodeThenWithUnicodeRegexes();
            Console.WriteLine("\n\nDone.");
        }

        /// <summary>
        ///     Learn to reformat a name written "First Last" as "Last, F." where 'F' is the first initial.
        ///     Demonstrates basic usage of Transformation.Formula API.
        /// </summary>
        private static void LearnFormatName() {
            var session = new Session();

            // Examples are given as an Example object which takes an input and output.
            // Here we have 3 inputs -- a string, an int, a datetime -- and one string output in each example. 
            session.Constraints.Add(
                // 1 or more input-output examples needed, here we provide 2
                new Example(new InputRow("Kettil Hansson", 1, new DateTime(2022,11,30,6,6,6)), "Hansson, K."),
                new Example(new InputRow("Foo Bar", 2, new DateTime(2022,12,12,10,45,30)), "Bar, F.")
            );
            // Given just the examples, the best program is returned
            Program topRankedProgram = session.Learn();

            if (topRankedProgram == null) {
                Console.Error.WriteLine("Error: failed to learn format name program.");
            } else {
                // Run the program on some new inputs.
                foreach (var name in new[] { "Etelka Bala", "Myron Lampros" }) {
                    string formatted = topRankedProgram.Run(new InputRow(name)) as string;
                    Console.WriteLine("\"{0}\" => \"{1}\"", name, formatted);
                }
                var translation = session.Translate(TargetLanguage.Pandas, topRankedProgram);
                if (translation == null) {
                    Console.Error.WriteLine("Error: failed to translate to target language.");
                } else {
                    Console.WriteLine($"The generated program in Pandas:\n{translation.ToString()}");
                }
            }
        }

        /// <summary>
        ///     Look at the top 10 programs learned from a single example for extracting the last name and show
        ///     the behavior of them on slightly differently formatted name. Demonstrates learning more than just
        ///     the single top program, and shows the variation in outputs among the top-ranked programs on unseen
        ///     input formats.
        /// </summary>
        private static void LearnTop10ExtractName() {
            var session = new Session();

            session.Constraints.Add(new Example(new InputRow("Greta Hermansson"), "Hermansson"));
            IReadOnlyList<Program> programs = session.LearnTopK(k: 10);

            // This attempts running the top 10 programs on an input not directly similar to the example
            // to see different behaviours.
            // Here, we will see the outputs:
            // a. "Smith", corresponding to programs that extract the last name.
            // b. "Hansson Smith", corresponding to programs that extract everything after the first name.
            int i = 0;
            foreach (var program in programs) {
                var input = new InputRow("Kettil Hansson Smith"); // Notice that we now include a middle name too.
                Console.WriteLine("Program {0}: \"{1}\" => \"{2}\"", ++i, input, program.Run(input));
            }
        }

        /// <summary>
        ///     Learns a program to convert dates from "DD/MM/YYYY" to "YYYY-MM-DD".
        ///     Demonstrates providing additional inputs (other inputs, without corresponding outputs).
        /// </summary>
        private static void LearnNormalizeDate() {
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

            if (topRankedProgram == null) {
                Console.Error.WriteLine("Error: failed to learn normalize date program.");
            } else {
                foreach (var date in new[] { "12/02/1972", "31/01/1983" }) {
                    string normalized = topRankedProgram.Run(new InputRow(date)) as string;
                    Console.WriteLine("\"{0}\" => \"{1}\"", date, normalized);
                }
            }
        }

        /// <summary>
        ///     Learn to extract numerical temperature from a string containing unicode characters.
        ///     Demonstrates the setting of using unicode regexes versus not using them.
        /// </summary>
        private static void LearnUnicodeRegexes() {
            var session = new Session();

            // Examples are given as an Example object which takes an input and output.
            session.Constraints.Add(
                new Example(new InputRow("Min 34° Celcius"), "34"),
                new Example(new InputRow("Max 50° C"), "50"),
                new Example(new InputRow("Avg 44°C"), "44"),
                new Example(new InputRow("54°"), "54"),
                new Example(new InputRow("24"), "24")
            );
            // Given just the examples, the best program is returned
            Program topRankedProgram = session.Learn();

            if (topRankedProgram == null) {
                Console.Error.WriteLine("Error: failed to learn program to extract number.");
                return;
            }
            var translation = session.Translate(TargetLanguage.Pandas, topRankedProgram);
            if (translation == null) {
                Console.Error.WriteLine("Error: failed to translate to target language.");
                return;
            }
            Console.WriteLine($"The default setting uses unicodes, and requires regex module:\n{translation.ToString()}");
            session.Constraints.AddOrReplace(new LearnConstraint() { EnableMatchUnicode = false });
            topRankedProgram = session.Learn();
            if (topRankedProgram == null) {
                Console.Error.WriteLine("Error: failed to learn program to extract number.");
                return;
            }
            translation = session.Translate(TargetLanguage.Pandas, topRankedProgram);
            if (translation == null) {
                Console.Error.WriteLine("Error: failed to translate to target language.");
                return;
            }
            Console.WriteLine($"The custom setting forbids use of unicodes, and uses the re module:\n{translation.ToString()}");
        }

        /// <summary>
        ///     Learn without unicode regexes, but if it doesn't work, learn with unicode, and if the latter
        ///     succeeds, then log such instances. Illustrates use of cancellation token.
        /// </summary>
        private static void LearnWithoutUnicodeThenWithUnicodeRegexes() {
            var session = new Session();

            // Examples are given as an Example object which takes an input and output.
            session.Constraints.Add(
                new Example(new InputRow("Renée"), "Renée"), // extracting UpperCase.LowerCase*
                new Example(new InputRow("Noël#"), "Noël"),
                new Example(new InputRow("Sørina44.."), "Sørina"),
                new Example(new InputRow("Österreich____"), "Österreich"),
                new Example(new InputRow("Ångström*****"), "Ångström"),
                new Example(new InputRow("Zoë2"), "Zoë"),
                new Example(new InputRow("ZoëAb"), "Zoë"),
                new Example(new InputRow("ZoëËab"), "Zoë"),
                new Example(new InputRow("ÅngströmB"), "Ångström"),
                new Example(new InputRow("ÅngströmÒa"), "Ångström")
            );
            // Learn without unicode categories in regexes
            session.Constraints.Add(new LearnConstraint() { EnableMatchUnicode = false });

            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken token = cancellationTokenSource.Token;

            // Call with a cancellation token and time limit of 0.2 seconds
            cancellationTokenSource.CancelAfter(200);
            Program topRankedProgram = null;
            try {
                topRankedProgram = session.Learn(cancel: token);
            } catch (TaskCanceledException) {
            } finally {
                cancellationTokenSource.Dispose();
            }

            if (topRankedProgram != null) {
                Console.WriteLine("Succeeded to learn the following program using non-unicode regexes:");
                Console.WriteLine($"{session.Translate(TargetLanguage.Pandas, topRankedProgram)?.ToString()}");
                return;
            }
            Console.WriteLine("Failed to learn program to extract name without unicode.");
            // Replace old learn constraint by a new one where unicode matching is enabled
            session.Constraints.Remove(session.Constraints.OfType<LearnConstraint>());
            session.Constraints.Add(new LearnConstraint() { EnableMatchUnicode = true });
            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(2000); // Cancel after 2 seconds
            try {
                topRankedProgram = session.Learn(cancel: cancellationTokenSource.Token);
            } catch (TaskCanceledException) {
            } finally {
                cancellationTokenSource.Dispose();
            }
            if (topRankedProgram == null) {
                Console.Error.WriteLine("Error: Failed to learn program to extract name with unicode.");
                return;
            }
            Console.WriteLine("Succeeded to learn with Unicode matches. We should log such examples!");
            FormulaTranslation translation = null;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(2000); // Cancel after 2 seconds
            try {
                translation = session.Translate(TargetLanguage.Pandas, topRankedProgram, cancellationTokenSource.Token);
            } catch (TaskCanceledException) {
            } finally {
                cancellationTokenSource.Dispose();
            }
            if (translation == null) {
                Console.Error.WriteLine("Error: failed to translate to target language.");
                return;
            }
            Console.WriteLine($"{translation.ToString()}");
        }
    }
}
