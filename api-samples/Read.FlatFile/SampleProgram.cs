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

        private static readonly string ETextSampleInput = @"
SEVERE [MBT Director for SESS_0.8396720957269632] TCP should be ready, begin recreate IO Socket
INFO [MBT Director for SESS_0.8396720957269632] SendTCPMessage: Sending - performGeneralAction`StartApplication`calc``
INFO [MBT Director for SESS_0.8396720957269632] SendTCPMessage: Received - -1
SEVERE [MBT Director for SESS_0.8396720957269632] performGeneralAction(): return code is ERROR - unknown. An exception may have occurred.
INFO [vUser-3-thread-1] SendTCPMessage: Sending - findElement`Clear`true`name`button`1`false`0`true
INFO [vUser-3-thread-1] SendTCPMessage: Received - 1
INFO [vUser-3-thread-1] findElement(): return code is FAILED";

        private static void SimpleApi()
        {
            // The pattern of the simple API is:
            // => Learn(input, options, override_params)
            // => Learn[Csv|Fw](input, options, override_params)
            // ---
            // In the first case the type of program (CSV/FW) is automatically learned, while in the second case
            // it is specificed by the method called. The parameters to the functions are:
            // - input, which can be a string or a buffer (TextReader), specifies the text file to learn a program from
            // - (optional) options (LearningOptions) specify various options for learning
            // - (optional) override_params allow to override some of the learned program parameters,
            //  and in that case the overridden parameter is not learned, but the provided value is used
            //  (e.g., delimiter can be overriden; see the example below).

            // I. Learn CSV or FW program
            Program prog1 = ReadFlatFileLearner.Learn(CsvSampleInput);
            PrintProgramProperties(prog1);
            PrintProgramOutput(prog1, CsvSampleInput);

            // II. Learn FW program (learn on a buffer instead of a string)
            // Note: We override the number of lines to use for learning (the default was 200)
            //  and specify the learning timeout.
            var options = new LearningOptions {
                TimeLimit = TimeSpan.FromSeconds(10),
                LinesToLearn = 30
            };
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
            // We mimic the learn operations performed above with the simple API.

            // I. Learn CSV or FW program
            var session = new Session();
            session.AddInput(CsvSampleInput);
            Program prog1 = session.Learn();
            PrintProgramProperties(prog1);
            PrintProgramOutput(prog1, CsvSampleInput);

            // II. Learn FW program (learn on a buffer instead of a string)
            // Note: We are re-using the session, but we could have also created a new one.
            session.Inputs.Clear();
            using (var reader = new StringReader(FwSampleInput))
            {
                // Override the number of lines to use for learning (the default was 200).
                session.AddInput(reader, linesToLearn: 30);
            }
            // Add constraint to learn *only* FW programs
            session.Constraints.Add(new FixedWidth());
            // Specify a timeout for learning.
            CancellationToken timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
            Program prog2 = session.Learn(cancel: timeout);
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

            // IV. Learn EText program
            session.Inputs.Clear();
            session.Constraints.Clear();
            session.AddInput(ETextSampleInput);
            // EText learning is disabled by default while we work to improve its quality.
            session.Constraints.Add(EnableExtractionTextLearning.Instance);
            Program prog4 = session.Learn();
            PrintProgramProperties(prog4);
            PrintProgramOutput(prog4, ETextSampleInput);
        }

        private static void PrintProgramProperties(Program program)
        {
            if (program is SimpleProgram simple)
            {
                // Print common properties
                Console.WriteLine($"Skip: {simple.Skip}");
                Console.WriteLine($"Column names: {string.Join(", ", simple.ColumnNames)}");
                Console.WriteLine($"New-line strings: {string.Join(", ", simple.NewLineStrings.ToLiteral())}");
            }

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
                },
                etextProgram =>
                {
                    Console.WriteLine(
                        $"Program: {etextProgram.WrappedProgram}");
                });
        }

        private static void PrintProgramOutput(Program program, string input, int numRows = 3)
            => PrintTableOutput(program.Run(input).Rows.Take(numRows));

        private static void PrintProgramOutput(Program program, TextReader input, int numRows = 3)
            => PrintTableOutput(program.Run(input).Rows.Take(numRows));

        private static void PrintTableOutput(IEnumerable<IEnumerable<string>> rows)
        {
            foreach (IEnumerable<string> row in rows)
            {
                Console.WriteLine(string.Join(" | ", row));
            }
            Console.WriteLine();
        }
    }
}
