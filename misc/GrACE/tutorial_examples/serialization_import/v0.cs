using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example
{
    /// <summary>
    /// Helper API to the OpenAI API.
    /// </summary>
    public static class OpenAI
    {
        /// <summary>
        /// Complete the prompt using the specified parameters. Any non-specified parameters will fall back to default values specified in <see cref="DefaultCompletionRequestArgs"/>.
        /// </summary>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
            private static T ReadFromBinaryFile<T>(string filePath) {
                using (Stream stream = File.Open(filePath, FileMode.Open)) {
                    try {
                        var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        return (T) binaryFormatter.Deserialize(stream);
                    }
                    catch(Exception){
                        throw();
                    }
                }
            }
    }
}