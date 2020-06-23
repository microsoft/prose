using Microsoft.ProgramSynthesis.Read.FlatFile;
using Microsoft.ProgramSynthesis.Read.FlatFile.Constraints;
using Microsoft.ProgramSynthesis.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Read.FlatFile
{
    public static class SampleProgram
    {
        /// <summary>
        ///     A sample program to read delimited (CSV) and fiexed-width (FW) files.
        /// </summary>
        public static void Main()
        {
            // There are two alternative APIs to read CSV/FW programs:

            // 1. Read file using the simple API
            Console.WriteLine("Using simple API...");
            SimpleApi();

            // 2. Read file using the Session API
            Console.WriteLine("Using Session API...");
            SessionApi();
        }

        private static readonly string CsvSampleInput = @"
# this is a header comment
col1, col2, col3
1, 2, A
3, 4, B
# mid file comment
5, 6, C";

        private static readonly string FwSampleInput = @"
col1 col2 col3
1    2    A
3    4    B

5    6    C";

        private static readonly string CsvOverrideSampleInput = @"
1,2;3
4,5;6
7,8;9";

        private static void SimpleApi()
        {
            // Set common learn options
            LearningOptions options = new LearningOptions
            {
                TimeLimit = TimeSpan.FromSeconds(10),
            };

            // I. Learn CSV or FW program
            Program prog1 = ReadFlatFileLearner.Learn(CsvSampleInput, options);
            PrintProgramProperties(prog1);
            PrintProgramOutput(prog1, CsvSampleInput);

            // II. Learn FW program (learn on a buffer instead of a string)
            // Note: We also override the number of lines to use for learning (the default was 200).
            options.LinesToLearn = 30;
            FwProgram prog2;
            using (var reader = new StringReader(FwSampleInput))
            {
                prog2 = ReadFlatFileLearner.LearnFw(reader, options);
                PrintProgramProperties(prog2);
            }
            using (var reader = new StringReader(FwSampleInput))
            {
                PrintProgramOutput(prog2, reader);
            }

            // III. Learn CSV program with manually set delimiter
            CsvProgram prog3 = ReadFlatFileLearner.LearnCsv(CsvOverrideSampleInput, delimiter: ";");
            PrintProgramProperties(prog3);
            PrintProgramOutput(prog3, CsvOverrideSampleInput);
        }

        private static void SessionApi()
        {
            // I. Learn CSV of FW program
            var session = new Session();
            session.AddInput(CsvSampleInput);
            CancellationToken timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
            Program prog1 = session.Learn(cancel: timeout);
            PrintProgramProperties(prog1);
            PrintProgramOutput(prog1, CsvSampleInput);

            // II. Learn FW program (learn on a buffer instead of a string)
            // Note: We are re-using the session, but we could have also created a new one.
            session.Inputs.Clear();
            using (var reader = new StringReader(FwSampleInput))
            {
                // Note: We also override the number of lines to use for learning (the default was 200).
                session.AddInput(reader, linesToLearn: 30);
            }
            // Add constraint to learn *only* FW programs
            session.Constraints.Add(new FixedWidth());
            Program prog2 = session.Learn();
            PrintProgramProperties(prog2);
            using (var reader = new StringReader(FwSampleInput))
            {
                PrintProgramOutput(prog2, reader);
            }

            // III. Learn CSV program with manually set delimiter
            session.Inputs.Clear();
            session.Constraints.Clear();
            session.AddInput(CsvOverrideSampleInput);
            // Add constraint to manually specify the column delimiter
            //  (implies that the learned program will be a CSV program).
            session.Constraints.Add(new Delimiter(";"));
            Program prog3 = session.Learn();
            PrintProgramProperties(prog3);
            PrintProgramOutput(prog3, CsvOverrideSampleInput);
        }

        private static void PrintProgramProperties(Program program)
        {
            // Print common properties
            Console.WriteLine($"Skip: {program.Skip}");
            Console.WriteLine($"Column names: {string.Join(", ", program.ColumnNames)}");
            Console.WriteLine($"New-line strings: {string.Join(", ", program.NewLineStrings.ToLiteral())}");

            // Print program-secific properties
            program.Switch(
                csvProgram =>
                {
                    // CSV properties
                    Console.WriteLine($"Delimiter: {csvProgram.Delimiter.ToLiteral()}");
                    Console.WriteLine($"Quote char: {csvProgram.QuoteChar.ToLiteral()}");
                },
                fwProgram =>
                {
                    // FW properties
                    string FwPosToString(Record<int, int?> pos) => $"({pos.Item1}, {pos.Item2})";
                    Console.WriteLine(
                        $"Field positions: {string.Join(", ", fwProgram.FieldPositions.Select(FwPosToString))}");
                });
        }

        private static void PrintProgramOutput(Program program, string input, int numRows = 3)
            => PrintTableOutput(program.Run(input).Rows.Take(numRows));

        private static void PrintProgramOutput(Program program, TextReader input, int numRows = 3)
            => PrintTableOutput(program.Run(input).Rows.Take(numRows));

        private static void PrintTableOutput(IEnumerable<IEnumerable<string>> rows)
        {
            foreach (var row in rows)
            {
                Console.WriteLine(string.Join(" | ", row));
            }
            Console.WriteLine();
        }
    }
}
