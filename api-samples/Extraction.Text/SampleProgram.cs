using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.Extraction.Text;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Wrangling.Schema.TableOutput;

namespace Extraction.Text {
    class SampleProgram {

        static void Main(string[] args) {
            LearnWithSpaces();
            LearnNumberText();
            LearnNumberTextTwoColumnsTwo();
        }

        /// <summary>
        /// Extracting a simple table and handling artefacts related to formatting.
        /// </summary>
        public static void LearnWithSpaces() {
            Console.WriteLine(">> Simple table with spaces.");

            // raw data
            string data = @"1; 4.4; 99
                            2; 4.5; 200
                            3; 4.7; 65
                            4; 3.2; 140";
            // make an example
            ITable<ExampleCell> oneExample = new Table<ExampleCell>(
                null,
                new[] {
                    new[] { new ExampleCell("1"), new ExampleCell("4.4"), new ExampleCell("99") }
                }
            );

            // learn the program
            Session session = new Session();
            session.AddExample(data, oneExample);
            Program program = session.Learn();

            // run it on the whole table and check the result
            ITable<string> result = program.Run(data);
            Console.WriteLine($"One example:");
            PrintTable(result);

            // the previous table will contain a cell with
            // "                            2" because the
            // formatting introduced additional spaces and by
            // giving an example of the expected content of 
            // this cell, the correct table is extracted
            ITable<ExampleCell> twoExample = new Table<ExampleCell>(
                columnNames: new[] { "Column 1", "Column 2", "Column 3" },
                // not all cells of the second row need to
                // be given and we can indicate that the
                // user did not provide these cells as examples
                rows: new[] {
                    new[] { new ExampleCell("1"), new ExampleCell("4.4"), new ExampleCell("99") },
                    new[] { new ExampleCell("2"), new ExampleCell(null, isUserSpecified: false), new ExampleCell(null, isUserSpecified: false) }
                }
            );
            session = new Session();
            session.AddExample(data, twoExample);
            program = session.Learn();
            result = program.Run(data);
            Console.WriteLine($"Partial examples:");
            PrintTable(table: result);

        }

        /// <summary>
        /// Extract a table from text copied from Stackoverflow sidebar.
        /// </summary>
        public static void LearnNumberText() {
            Console.WriteLine(">> Learning from table in one column.");

            // raw data
            string data = @"7169
What is the difference between String and string in C#?
3150
How to check if a string contains a substring in Bash
3243
How do I iterate over the words of a string?
4496
How do I read / convert an InputStream into a String in Java ?
4573
How do I make the first letter of a string uppercase in JavaScript ?
5127
How to replace all occurrences of a string in JavaScript
7417
How to check whether a string contains a substring in JavaScript ?
1837
How to split a string in Java
2140
How do I break a string in YAML over multiple lines?
3347
How do I convert a String to an int in Java ?";
            // make an example
            ITable<ExampleCell> example = new Table<ExampleCell>(
                new[] { "ID", "Question" },
                new[] {
                    new[] { new ExampleCell("7169"), new ExampleCell("What is the difference between String and string in C#?"), }
                }
            );

            Session session = new Session();
            session.AddExample(data, example);
            Program program = session.Learn();

            ITable<string> result = program.Run(data);
            PrintTable(result);

        }

        /// <summary>
        /// Extract a table with one column containing cells that are spread out over two rows.
        /// </summary>
        public static void LearnNumberTextTwoColumnsTwo() {
            Console.WriteLine(">> Learning from table in two columns with multi-line cell.");

            string data = @"7169 What is the difference
between String and string in C#?
315 How to check if a string
contains a substring in Bash
3243 How do I iterate over
the words of a string?
4496 How do I read / convert an
InputStream into a String in Java ?
4573 How do I make the first letter
of a string uppercase in JavaScript ?
0 How to replace all occurrences
of a string in JavaScript
7417 How to check whether a string
contains a substring in JavaScript ?
1837 How to split
a string in Java
1 How do I break a string in
YAML over multiple lines?
3347 How do I convert a String to an
int in Java ?";

            // make example as if someone selected the whole cell, which
            // includes the newline (all newlines in input and examples
            // are normalized into \n).
            ITable<ExampleCell> example = new Table<ExampleCell>(
                new[] { "ID", "Question" },
                new[] {
                    new[] { new ExampleCell("7169"), new ExampleCell("What is the difference\nbetween String and string in C#?"), }
                }
            );

            Session session = new Session();
            session.AddExample(data, example);
            Program program = session.Learn();

            ITable<string> result = program.Run(data);
            PrintTable(result, delimiter: " | ");

        }

        private static void PrintTable(IEnumerable<IEnumerable<string>> table, string delimiter = ",") {
            foreach (IEnumerable<string> row in table) {
                foreach (string cell in row) {
                    Console.Write($"{cell ?? "null"}{delimiter} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
