using System.IO;
using System.Text;

namespace BenchmarkDriver
{
    internal class Utils
    {
        public static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public static void Write(string path, string contents) =>
            File.WriteAllText(path, contents, Utf8WithoutBom);
    }
}