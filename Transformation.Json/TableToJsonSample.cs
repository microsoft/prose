using System;
using System.Linq;
using Microsoft.ProgramSynthesis.Transformation.Json.TableToJson;
using Microsoft.ProgramSynthesis.Transformation.Json.TableToJson.Constraint;
using Newtonsoft.Json.Linq;

namespace Transformation.Json {
    /// <summary>
    ///    Illustrates PROSE Table To JSON capability.
    /// </summary>
    internal static partial class Sample {
        private static void TableToJsonSample() {
            var input = "column1,column2\na,b\nc,d\ne,f";
            var inputTable = ParseCsv(input);

            // Option 1: Automatically transform a table to a JSON file
            var autoConstraint = new[] { new AutoTransform(inputTable) };

            var autoProgram = TableToJsonLearner.Instance.Learn(autoConstraint);

            // Run the program
            JToken jsonOutput = autoProgram.Run(inputTable);
            Console.WriteLine($"Auto transformation output:\n{jsonOutput}");

            // Serialize and deserialize the program
            var programText = autoProgram.Serialize();
            var loadedAutoProgram = TableToJsonLoader.Instance.Load(programText);

            jsonOutput = loadedAutoProgram.Run(inputTable);
            Console.WriteLine($"Deserialized program output:\n{jsonOutput}");


            // Option 2: Transform a table to a JSON file using examples
            // We can remove columns, rename columns, change the structures.
            var trainInput = "column1,column2\na,b\nc,d";
            var trainInputTable = ParseCsv(trainInput);

            var trainOuput =
            @"{ ""extra field"" : ""new data"",
                ""data"": [
                    {
                        ""key1"": ""a"",
                        ""oldcolumn2"": {""name"": ""b""}
                    },
                    {
                        ""key1"": ""c"",
                        ""oldcolumn2"": {""name"": ""d""}
                    }
                ]
            }";
            var trainOutputToken = JToken.Parse(trainOuput);

            var examples = new[] { new TableToJsonExample(trainInputTable, trainOutputToken) };

            // Learn a Table to Json transformation program from the example
            var byExampleProgram = TableToJsonLearner.Instance.Learn(examples);

            // Run the program
            jsonOutput = byExampleProgram.Run(inputTable);

            Console.WriteLine($"By-example transformation output:\n{jsonOutput}");
            Console.ReadKey();
        }

        private static Table ParseCsv(string content) {
            var lines = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var splitLines =
                lines.Select(line => line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
            var header = splitLines[0];
            var rows = splitLines.Skip(1);
            return new Table(header, rows);
        }
    }
}