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
        public static void Main(string[] args) {
            /* TODO */
        }

        private static void PrintTable(Stream stream, IEnumerable<string> headers,
                                       IEnumerable<TableRow<JsonRegion>> output) {
            using (var writer = new StreamWriter(stream))
            using (var csv = new CsvWriter(writer)) {
                foreach (string header in headers) {
                    csv.WriteField(header);
                }
                csv.NextRecord();
                foreach (var row in output) {
                    foreach (var cell in row.Value) {
                        csv.WriteField(cell.Value?.Value ?? "");
                    }
                    csv.NextRecord();
                }
            }
        }
    }
}
