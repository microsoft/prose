using System;
using Microsoft.ProgramSynthesis.Transformation.Json;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;
using Newtonsoft.Json.Linq;

namespace Transformation.Json
{
    internal static partial class Sample
    {
        /// <summary>
        ///    Illustrates PROSE JSON To JSON capability.
        /// </summary>
        internal static void JsonToJsonSample()
        {
            // The original input file (which can be very large).
            JToken input = JToken.Parse(@"
{
  ""datatype"": ""local"",
  ""data"": [
    {
      ""Name"": ""John"",
      ""status"": ""To Be Processed"",
      ""LastUpdatedDate"": ""2013-05-31 08:40:55.0""
    },
    {
      ""Name"": ""Paul"",
      ""status"": ""To Be Processed"",
      ""LastUpdatedDate"": ""2013-06-02 16:03:00.0""
    },
    {
      ""Name"": ""Alice"",
      ""status"": ""Finished"",
      ""LastUpdatedDate"": ""2013-07-02 12:04:00.0""
    }
  ]
}
            ");

            // The training input file, which is a small prefix of the input file.
            JToken trainInput = JToken.Parse(@"
{
  ""datatype"": ""local"",
  ""data"": [
    {
      ""Name"": ""John"",
      ""status"": ""To Be Processed"",
      ""LastUpdatedDate"": ""2013-05-31 08:40:55.0""
    },
    {
      ""Name"": ""Paul"",
      ""status"": ""To Be Processed"",
      ""LastUpdatedDate"": ""2013-06-02 16:03:00.0""
    }
  ]
}
            ");

            // The training output file, which is the desired output for the training input.
            JToken trainOutput = JToken.Parse(@"
[
    {
      ""John"" : ""To Be Processed""
    },
    {
      ""Paul"" : ""To Be Processed""
    }
  ]
            ");


            // Given just the examples, the best program is returned
            var session = new Session();
            session.Constraints.Add(new Example<JToken, JToken>(trainInput, trainOutput));
            Program topRankedProgram = session.Learn();

            if (topRankedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to learn format name program.");
                return;
            }
            // Run the program on the original input
            JToken output = topRankedProgram.Run(input);

            if (output == null)
            {
                Console.Error.WriteLine("Error: failed to execute the program on the input.");
                return;
            }
            Console.WriteLine(output.ToString());

            // Serialize the program, and deserialize it.
            string serializedProgram = topRankedProgram.Serialize();

            Program deserializedProgram = Loader.Instance.Load(serializedProgram);

            if (deserializedProgram == null)
            {
                Console.Error.WriteLine("Error: failed to load deserialized program.");
                return;
            }

            // Run the deserialized program on the original input
            output = deserializedProgram.Run(input);

            if (output == null)
            {
                Console.Error.WriteLine("Error: failed to execute the deserialized program on the input.");
                return;
            }
            Console.WriteLine(output.ToString());
        }
    }
}
