using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.DslLibrary;
using Microsoft.ProgramSynthesis.Split.Text;
using Microsoft.ProgramSynthesis.Split.Text.Semantics;

namespace Split.Text
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // create a new ProseSplit session
            var splitSession = new SplitSession();

            // add the input rows to the session
            // each input is a StringRegion object containing the text to be split
            var inputs = new List<StringRegion>
            {
                SplitSession.CreateStringRegion("PE5 Leonard Robledo (Australia)"),
                SplitSession.CreateStringRegion("U109 Adam Jay Lucas (New Zealand)"),
                SplitSession.CreateStringRegion("R342 Carrie Dodson (United States)")
            };
            splitSession.Inputs.Add(inputs);

            // add the constraint to include all delimiters in the program output
            splitSession.Constraints.Add(new IncludeDelimitersInOutput(true));

            // call the learn function to learn a splitting program from the given input examples
            SplitProgram program = splitSession.Learn();

            // check if the program is null (no program could be learnt from the given inputs)
            if (program == null)
            {
                Console.WriteLine("No program learned.");
                return;
            }

            // serialize the learnt program and then deserialize
            string progText = program.Serialize();
            program = SplitProgramLoader.Instance.Load(progText);

            // a valid program has been learnt and we execute it on the given inputs 
            SplitCell[][] splitResult = inputs.Select(input => program.Run(input)).ToArray();

            // display the result of the splitting
            Console.WriteLine("\n\nLearning a splitting program and executing it on the given inputs:");
            foreach (SplitCell[] resultRow in splitResult)
            {
                //each result row is an array of SplitCells representing the regions (fields or delimiters) into which the input has been split
                Console.Write("\n|\t");
                foreach (SplitCell cell in resultRow)
                {
                    Console.Write(cell?.CellValue?.Value);
                    Console.Write("\t|\t");
                }
            }

            // learn a program to extract only the fields and no delimiters
            splitSession.Constraints.Clear();
            splitSession.Constraints.Add(new IncludeDelimitersInOutput(false));
            SplitProgram programWithoutDelimiters = splitSession.Learn();
            if (programWithoutDelimiters == null)
            {
                Console.WriteLine("No program learned.");
                return;
            }
            splitResult = inputs.Select(input => programWithoutDelimiters.Run(input)).ToArray();
            Console.WriteLine("\n\nDisplaying only extracted fields without delimiters:");
            foreach (SplitCell[] resultRow in splitResult)
            {
                Console.Write("\n|\t");
                foreach (SplitCell cell in resultRow)
                {
                    Console.Write(cell?.CellValue?.Value);
                    Console.Write("\t|\t");
                }
            }

            // execute the learnt program on some new inputs
            var newInputs = new List<StringRegion>();
            newInputs.Add(SplitSession.CreateStringRegion("TS51 Naomi Cole (Canada)"));
            newInputs.Add(SplitSession.CreateStringRegion("Y722 Owen Murphy (United States)"));
            newInputs.Add(SplitSession.CreateStringRegion("UP335 Zoe Erin Rees (UK)"));
            Console.WriteLine("\n\nExecuting the program on new inputs: ");
            foreach (StringRegion newInput in newInputs)
            {
                SplitCell[] outputForRow = programWithoutDelimiters.Run(newInput);
                Console.Write("\n|\t");
                foreach (SplitCell cell in outputForRow)
                {
                    Console.Write(cell?.CellValue?.Value);
                    Console.Write("\t|\t");
                }
            }

            // provide output examples to learn a program that produces a different splitting (separates the first name)
            splitSession.Constraints.Clear();
            splitSession.Inputs.Add(newInputs);
            splitSession.Constraints.Add(new IncludeDelimitersInOutput(false));
            // provide examples of the desired splitting: the example input string to split, the index of the split cell in the output, and the value desired in that split cell 
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 0, "PE5"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 1, "Leonard"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 2, "Robledo"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 3, "Australia"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 0, "U109"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 1, "Adam"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 2, "Jay Lucas"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 3, "New Zealand"));
            SplitProgram programFromExamples = splitSession.Learn();
            if (programFromExamples == null)
            {
                Console.WriteLine("No program learned.");
                return;
            }
            splitResult = splitSession.Inputs.Select(input => programFromExamples.Run(input)).ToArray();
            Console.WriteLine("\n\nLearning a different splitting program from examples (separate first name):");
            foreach (SplitCell[] resultRow in splitResult)
            {
                Console.Write("\n|\t");
                foreach (SplitCell cell in resultRow)
                {
                    Console.Write(cell?.CellValue?.Value);
                    Console.Write("\t|\t");
                }
            }

            // provide different examples to learn a program that separates the last name rather than the first
            splitSession.Constraints.Clear();
            splitSession.Constraints.Add(new IncludeDelimitersInOutput(false));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 0, "PE5"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 1, "Leonard"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 2, "Robledo"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 3, "Australia"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 0, "U109"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 1, "Adam Jay"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 2, "Lucas"));
            splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 3, "New Zealand"));
            SplitProgram programFromDifferentExamples = splitSession.Learn();
            if (programFromDifferentExamples == null)
            {
                Console.WriteLine("No program learned.");
                return;
            }
            splitResult = splitSession.Inputs.Select(input => programFromDifferentExamples.Run(input)).ToArray();
            Console.WriteLine("\n\nLearning a different splitting program from different examples (separate last name):");
            foreach (SplitCell[] resultRow in splitResult)
            {
                Console.Write("\n|\t");
                foreach (SplitCell cell in resultRow)
                {
                    Console.Write(cell?.CellValue?.Value);
                    Console.Write("\t|\t");
                }
            }

            Console.WriteLine("\n\nDone.");
        }
    }
}
