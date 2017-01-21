using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Split.Text;
using Microsoft.ProgramSynthesis.Split.Text.Semantics;

namespace Split.Text {
    internal class Program {
        private static void Main(string[] args) {
            // create a new ProseSplit session
            var splitSession = new SplitSession();

            // add the input rows to the session
            // each input is a StringRegion object containing the text to be split
            var inputs = new List<StringRegion> {
                SplitSession.CreateStringRegion("a,b;c"),
                SplitSession.CreateStringRegion("1,2;3")
            };
            splitSession.AddInputs(inputs);

            // add the constraint to include all delimiters in the program output
            splitSession.AddConstraints(new IncludeDelimitersInOutput(true));

            // call the learn function to learn a splitting program from the given input examples
            SplitProgram program = splitSession.Learn();

            // check if the program is null (no program could be learnt from the given inputs)
            if (program == null) {
                Console.WriteLine("No program learned.");
                return;
            }

            // a valid program has been learnt and we execute it on the example inputs 
            SplitCell[][] splitResult = inputs.Select(input => program.Run(input)).ToArray();

            // display the result of the splitting
            Console.WriteLine("\n\nExecuting the program on the example inputs. Displaying all output regions:");
            foreach (SplitCell[] resultRow in splitResult) {
                //each result row is an array of SplitCells representing the regions (fields or delimiters) into which the input has been split
                Console.Write("\n|\t");
                foreach (SplitCell cell in resultRow) {
                    Console.Write(cell.CellValue.Value + "\t|\t");
                }
            }
            /* This displays the following output:
                |	a	|	,	|	b	|	;	|	c	|	
                |	1	|	,	|	2	|	;	|	3	|	
            */

            // learn a program to extract only the fields and no delimiters
            splitSession.RemoveAllConstraints();
            splitSession.AddConstraints(new IncludeDelimitersInOutput(false));
            SplitProgram programWithoutDelimiters = splitSession.Learn();
            if (programWithoutDelimiters == null) {
                Console.WriteLine("No program learned.");
                return;
            }
            splitResult = inputs.Select(input => programWithoutDelimiters.Run(input)).ToArray();
            Console.WriteLine("\n\nDisplaying only extracted fields without delimiters:");
            foreach (SplitCell[] resultRow in splitResult) {
                Console.Write("\n|\t");
                foreach (SplitCell cell in resultRow) {
                    Console.Write(cell.CellValue.Value + "\t|\t");
                }
            }
            /* This displays the following output:
               |       a       |       b       |       c       |
               |       1       |       2       |       3       |
            */

            // execute the learnt program on some new inputs
            var newInputs = new List<StringRegion>();
            newInputs.Add(SplitSession.CreateStringRegion("e,f;g"));
            newInputs.Add(SplitSession.CreateStringRegion("q,w;p"));
            newInputs.Add(SplitSession.CreateStringRegion("17,551;2"));
            newInputs.Add(SplitSession.CreateStringRegion("foo,bar;15"));
            Console.WriteLine("\n\nExecuting the program on new inputs. Displaying all output regions: ");
            foreach (StringRegion newInput in newInputs) {
                SplitCell[] outputForRow = program.Run(newInput);
                Console.Write("\n|\t");
                foreach (SplitCell field in outputForRow) {
                    Console.Write(field.CellValue.Value + "\t|\t");
                }
            }
            /* This displays the following output:
                |	e	|	,	|	f	|	;	|	g	|	
                |	q	|	,	|	w	|	;	|	p	|	
                |	17	|	,	|	551	|	;	|	2	|	
                |	foo	|	,	|	bar	|	;	|	15	|
            */
            Console.ReadLine();
        }
    }
}
