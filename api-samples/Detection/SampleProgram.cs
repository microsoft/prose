using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.Detection;
using Microsoft.ProgramSynthesis.Detection.RichDataTypes;

namespace DetectionSample
{
    /// <summary>
    ///     Sample of how to use the Detection API. It is used to infer datatype of a collection of strings.
    /// </summary>
    internal static class SampleProgram
    {
        private static void Main(string[] args)
        {
            // create detector to detect bool, datetype, and numerictype
            RichDataTypeDetector richDataTypeDetector = new RichDataTypeDetector(
                    new IRichDataType[] {
                    new RichBooleanType(),
                    new RichDateType(),
                    new RichNumericType(), // other available options are RichCategoricalType(), RichStringType()
                });

            // get the strings
            IEnumerable<string> samples = new[] { "22/4/15", "22/3/4", "22/9/23" };
            // Call the Detect method to get a result object
            IRichDataType result = richDataTypeDetector.Detect(samples);
            // The result object has lots of information.
            // If you are only interested in knowing the type, the Kind property is a DataKind enum.
            // The following will print: "Type of 22/4/15, 22/3/4, 22/9/23 is: Date"
            Console.WriteLine($"Type of {String.Join(", ", samples)} is: {Enum.GetName(typeof(DataKind), result.Kind)}");
            
            // An example of numbers
            richDataTypeDetector = new RichDataTypeDetector(
                    new IRichDataType[] {
                    new RichBooleanType(),
                    new RichDateType(),
                    new RichNumericType(), // other available options are RichCategoricalType(), RichStringType()
                });
            samples = new[] { "22.15", "22", "-9.0" };
            result = richDataTypeDetector.Detect(samples);
            // Type of 22.15, 22, -9.0 is: Numeric
            Console.WriteLine($"Type of {String.Join(", ", samples)} is: {Enum.GetName(typeof(DataKind), result.Kind)}");

            // An example of Boolean
            richDataTypeDetector = new RichDataTypeDetector(
                    new IRichDataType[] {
                    new RichBooleanType(),
                    new RichDateType(),
                    new RichNumericType(), // other available options are RichCategoricalType(), RichStringType()
                });
            samples = new[] { "true", "False", "True", "false" };
            result = richDataTypeDetector.Detect(samples);
            // Type of true, False, True, false is: Boolean
            Console.WriteLine($"Type of {String.Join(", ", samples)} is: {Enum.GetName(typeof(DataKind), result.Kind)}");

            // An example of Time
            richDataTypeDetector = new RichDataTypeDetector(
                    new IRichDataType[] {
                    new RichBooleanType(),
                    new RichDateType(),
                    new RichNumericType(), // other available options are RichCategoricalType(), RichStringType()
                });
            samples = new[] { "0830", "1145", "1215", "0100" };
            result = richDataTypeDetector.Detect(samples);
            // Type of 0830, 1145, 1215, 0100 is: Time
            Console.WriteLine($"Type of {String.Join(", ", samples)} is: {Enum.GetName(typeof(DataKind), result.Kind)}");

            // An example of DateTime
            richDataTypeDetector = new RichDataTypeDetector(
                    new IRichDataType[] {
                    new RichBooleanType(),
                    new RichDateType(),
                    new RichNumericType(), // other available options are RichCategoricalType(), RichStringType()
                });
            samples = new[] { "2022/11/09T12:30", "2022/11/09T14:45" };
            result = richDataTypeDetector.Detect(samples);
            // Type of 2022/11/09:1230, 2022/11/09:1445 is: DateTime
            Console.WriteLine($"Type of {String.Join(", ", samples)} is: {Enum.GetName(typeof(DataKind), result.Kind)}");

            // An example of categorical type, which is detected as string by default
            richDataTypeDetector = new RichDataTypeDetector(
                    new IRichDataType[] {
                    new RichBooleanType(),
                    new RichDateType(),
                    new RichNumericType(), 
                    new RichCategoricalType(),
                    new RichStringType()
                });
            samples = new[] { "Male", "Female", "Female", "Female", "NA", "Null", "Male", "Female" };
            result = richDataTypeDetector.Detect(samples);
            // Type of "Male", "Female", "Female", "Female", "NA", "Null", "Male", "Female" is: String
            Console.WriteLine($"Type of {String.Join(", ", samples)} is: {Enum.GetName(typeof(DataKind), result.Kind)}");

            // The categorical type detector requires some minimum number of samples, it can be configured.
            richDataTypeDetector = new RichDataTypeDetector(
                    new IRichDataType[] {
                    new RichBooleanType(),
                    new RichDateType(),
                    new RichNumericType(), 
                    new RichCategoricalType(minSamplesForCategorical: 5, sampleCountMultiplier: 0.5),
                    new RichStringType()
                });
            samples = new[] { "Male", "Female", "Female", "Female", "NA", "Null", "Male", "Female" };
            result = richDataTypeDetector.Detect(samples);
            // Type of "Male", "Female", "Female", "Female", "NA", "Null", "Male", "Female" is: Categorical
            Console.WriteLine($"Type of {String.Join(", ", samples)} is: {Enum.GetName(typeof(DataKind), result.Kind)}");

            // Finally, you can cast the result into one of the classes used above to initialize 
            // RichDataTypeDetector to get more information.
            if (result is RichCategoricalType categoricalType) {
                Console.WriteLine($"Inferred categories: {String.Join(", ", categoricalType.Categories)}");
            }
            // NA and Null were treated as values, but empty strings and nulls are not treated as values.
            richDataTypeDetector = new RichDataTypeDetector(
                    new IRichDataType[] {
                    new RichBooleanType(),
                    new RichDateType(),
                    new RichNumericType(), 
                    new RichCategoricalType(minSamplesForCategorical: 5, sampleCountMultiplier: 0.5),
                    new RichStringType()
                });
            samples = new[] { "Male", "Female", "Female", "Female", "", null, "Male", "Female" };
            result = richDataTypeDetector.Detect(samples);
            Console.WriteLine($"Type of {String.Join(", ", samples)} is: {Enum.GetName(typeof(DataKind), result.Kind)}");
            Console.WriteLine($"Number of Null values detected: {richDataTypeDetector.NullValueCount}");
            Console.WriteLine($"Number of Empty values detected: {richDataTypeDetector.EmptyStringCount}");
            if (result is RichCategoricalType detectedCatType) {
                Console.WriteLine($"Inferred categories: {String.Join(", ", detectedCatType.Categories)}");
            }
        }
    }
}
