using System;
using System.Collections.Generic;

public class Example
{
    public static void Main()
    {
        var currentExamples = FetchExamples();

        foreach (var ex in currentExamples)
        {
            Console.WriteLine(GetText(ex, diff.BeforeFile));
            Console.WriteLine(GetText(ex, diff.AfterFile));
        }
        var expectedOutput = Run(currentExamples.First().Input);
        AssertEqual(currentExamples.First().Output, expectedOutput);
    }
}