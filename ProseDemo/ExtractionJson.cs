using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CsvHelper;
using JetBrains.Annotations;
using Microsoft.ProgramSynthesis.Extraction.Json.Constraints;
using Microsoft.ProgramSynthesis.Utils;
using Microsoft.ProgramSynthesis.Wrangling.Json;
using Microsoft.ProgramSynthesis.Wrangling.Schema.TableOutput;
using EJSession = Microsoft.ProgramSynthesis.Extraction.Json.Session;
using EJProgram = Microsoft.ProgramSynthesis.Extraction.Json.Program;

namespace ProseDemo {
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public static class ExtractionJson {
        [UsedImplicitly]
        public static void _Main(string[] args) {
            using (var session = new EJSession()) {
                ParsedJson json = Utils.Parse(File.ReadAllText("911.json"));
                session.AddConstraints(new FlattenRegion(json));
                EJProgram program = session.Learn();

                string[] headers = program.Schema.DescendantOutputFields.ToArray();
                Console.WriteLine(headers.DumpCollection());
                Console.WriteLine(program.Serialize());
                IEnumerable<TableRow<JsonRegion>> output = program.RunTable(json.Region);

                using (var stream = new FileStream("911.csv", FileMode.Create)) {
                    PrintTable(stream, headers, output);
                }
            }
        }

        private static void PrintTable(Stream stream, IEnumerable<string> headers,
                                       IEnumerable<TableRow<JsonRegion>> output) {
            using (var writer = new StreamWriter(stream))
            using (var csv = new CsvWriter(writer)) {
                foreach (string header in headers) csv.WriteField(header);
                csv.NextRecord();
                foreach (TableRow<JsonRegion> row in output) {
                    foreach (KeyValuePair<string, JsonRegion> cell in row.Value)
                        csv.WriteField(cell.Value?.Value ?? "");
                    csv.NextRecord();
                }
            }
        }
    }
}
