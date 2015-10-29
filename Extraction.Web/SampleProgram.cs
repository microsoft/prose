using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.ProgramSynthesis.Extraction.Web.Semantics;

namespace Microsoft.ProgramSynthesis.Extraction.Web.Sample
{
    internal class SampleProgram
    {
        private static void Main(string[] args)
        {
            LearnFirstSurnameInDocumentUsingOneExample();
            LearnFirstSurnameInDocumentUsingMultipleExamples();
            LearnSurnameWithRespectToTableRow();
            LearnSurnameWithRespectToTableRowUsingNegativeExample();
            LearnAllSurnamesInDocument();
            SerializeProgram();
            Console.ReadLine();
        }

        /// <summary>
        /// Learns a program to extract the first surname in the document from one example.
        /// </summary>
        public static void LearnFirstSurnameInDocumentUsingOneExample()
        {
            string s = File.ReadAllText(@"..\..\SampleDocuments\sample-document-1.html");
            HtmlDoc doc = HtmlDoc.Create(s);
            WebRegion referenceRegion = new WebRegion(doc);
            WebRegion exampleRegion = doc.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row
            ExtractionExample<WebRegion> exampleSpec = new ExtractionExample<WebRegion>(referenceRegion, exampleRegion);
            Web.Program prog = Web.Learner.Instance.LearnRegion(new[] { exampleSpec }, Enumerable.Empty<ExtractionExample<WebRegion>>());
            if (prog != null)
            {
                //run the program to extract first surname from the document
                IEnumerable<WebRegion> executionResult = prog.Run(referenceRegion);
                foreach (WebRegion region in executionResult)
                {
                    Console.WriteLine("Learn first surname in document from one example: ");
                    Console.WriteLine(region.GetSpecificSelector());
                    Console.WriteLine(region.Text());
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Learns a program to extract the first surname in the document from two examples 
        /// from two different documents.
        /// </summary>
        public static void LearnFirstSurnameInDocumentUsingMultipleExamples()
        {
            string s1 = File.ReadAllText(@"..\..\SampleDocuments\sample-document-1.html");
            HtmlDoc doc1 = HtmlDoc.Create(s1);
            string s2 = File.ReadAllText(@"..\..\SampleDocuments\sample-document-2.html");
            HtmlDoc doc2 = HtmlDoc.Create(s2);
            WebRegion referenceRegion1 = new WebRegion(doc1);
            WebRegion referenceRegion2 = new WebRegion(doc2);
            WebRegion exampleRegion1 = doc1.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row of doc1
            WebRegion exampleRegion2 = doc2.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row of doc2
            ExtractionExample<WebRegion> exampleSpec1 = new ExtractionExample<WebRegion>(referenceRegion1, exampleRegion1);
            ExtractionExample<WebRegion> exampleSpec2 = new ExtractionExample<WebRegion>(referenceRegion2, exampleRegion2);
            Web.Program prog = Web.Learner.Instance.LearnRegion(new[] { exampleSpec1, exampleSpec2 },
                                                                          Enumerable.Empty<ExtractionExample<WebRegion>>());
            if (prog != null)
            {
                //run the program on the second document 
                IEnumerable<WebRegion> executionResult = prog.Run(referenceRegion2);
                foreach (WebRegion region in executionResult)
                {
                    Console.WriteLine("Learn first surname in document from multiple examples: ");
                    Console.WriteLine(region.GetSpecificSelector());
                    Console.WriteLine(region.Text());
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Learns a program to extract the surname from a given table row (rather than a whole document).
        /// </summary>
        public static void LearnSurnameWithRespectToTableRow()
        {
            string s = File.ReadAllText(@"..\..\SampleDocuments\sample-document-1.html");
            HtmlDoc doc = HtmlDoc.Create(s);
            WebRegion referenceRegion1 = doc.GetRegion("tr:nth-child(1)"); //1st table row
            WebRegion exampleRegion1 = doc.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row
            WebRegion referenceRegion2 = doc.GetRegion("tr:nth-child(2)"); //2nd table row
            WebRegion exampleRegion2 = doc.GetRegion("tr:nth-child(2) td:nth-child(2)"); //2nd cell in 2nd table row
            ExtractionExample<WebRegion> exampleSpec1 = new ExtractionExample<WebRegion>(referenceRegion1, exampleRegion1);
            ExtractionExample<WebRegion> exampleSpec2 = new ExtractionExample<WebRegion>(referenceRegion2, exampleRegion2);
            Web.Program prog = Web.Learner.Instance.LearnRegion(new[] { exampleSpec1, exampleSpec2 },
                                                                          Enumerable.Empty<ExtractionExample<WebRegion>>());
            if (prog != null)
            {
                //run the program on 5th table row
                WebRegion fifthRowRegion = doc.GetRegion("tr:nth-child(5)"); //5th table row
                IEnumerable<WebRegion> executionResult = prog.Run(fifthRowRegion);
                foreach (WebRegion region in executionResult)
                {
                    Console.WriteLine("Learn surname with respect to table row: ");
                    Console.WriteLine(region.GetSpecificSelector());
                    Console.WriteLine(region.Text());
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Learns a program to extract the surname from a given table row (rather than a whole document) 
        /// using a negative example.
        /// </summary>
        public static void LearnSurnameWithRespectToTableRowUsingNegativeExample()
        {
            string s = File.ReadAllText(@"..\..\SampleDocuments\sample-document-1.html");
            HtmlDoc doc = HtmlDoc.Create(s);
            WebRegion referenceRegion1 = doc.GetRegion("tr:nth-child(1)"); //1st table row
            WebRegion referenceRegion2 = doc.GetRegion("tr:nth-child(2)"); //2nd table row
            ExtractionExample<WebRegion> posExampleSpec = new ExtractionExample<WebRegion>(referenceRegion1, doc.GetRegion("tr:nth-child(1) td:nth-child(2)"));
            ExtractionExample<WebRegion> negExampleSpec = new ExtractionExample<WebRegion>(referenceRegion2, doc.GetRegion("tr:nth-child(2) td:nth-child(1)"));
            Web.Program prog = Web.Learner.Instance.LearnRegion(new[] { posExampleSpec },
                                                                          new[] { negExampleSpec });
            if (prog != null)
            {
                IEnumerable<WebRegion> executionResult = prog.Run(referenceRegion1);
                foreach (WebRegion region in executionResult)
                {
                    Console.WriteLine("Learn surname with respect to table row using negative example: ");
                    Console.WriteLine(region.GetSpecificSelector());
                    Console.WriteLine(region.Text());
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Learns a program to extract the sequence of all surnames in a document.
        /// </summary>
        public static void LearnAllSurnamesInDocument()
        {
            string s = File.ReadAllText(@"..\..\SampleDocuments\sample-document-1.html");
            HtmlDoc doc = HtmlDoc.Create(s);
            WebRegion referenceRegion = new WebRegion(doc);
            WebRegion exampleRegion1 = doc.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row of doc
            WebRegion exampleRegion2 = doc.GetRegion("tr:nth-child(2) td:nth-child(2)"); //2nd cell in 2nd table row of doc
            ExtractionExample<WebRegion> exampleSpec1 = new ExtractionExample<WebRegion>(referenceRegion, exampleRegion1);
            ExtractionExample<WebRegion> exampleSpec2 = new ExtractionExample<WebRegion>(referenceRegion, exampleRegion2);
            Web.Program prog = Web.Learner.Instance.LearnSequence(new[] { exampleSpec1, exampleSpec2 }, Enumerable.Empty<ExtractionExample<WebRegion>>());
            if (prog != null)
            {
                IEnumerable<WebRegion> executionResult = prog.Run(referenceRegion);
                Console.WriteLine("Learn all surnames in document: ");
                foreach (WebRegion region in executionResult)
                {
                    Console.WriteLine(region.GetSpecificSelector());
                    Console.WriteLine(region.Text());
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Learns a program and then serializes and deserializes it.
        /// </summary>
        public static void SerializeProgram()
        {
            string s = File.ReadAllText(@"..\..\SampleDocuments\sample-document-1.html");
            HtmlDoc doc = HtmlDoc.Create(s);
            WebRegion referenceRegion = new WebRegion(doc);
            WebRegion exampleRegion = doc.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row
            ExtractionExample<WebRegion> exampleSpec = new ExtractionExample<WebRegion>(referenceRegion, exampleRegion);
            Web.Program prog = Web.Learner.Instance.LearnRegion(new[] { exampleSpec }, Enumerable.Empty<ExtractionExample<WebRegion>>());
            if (prog != null)
            { 
                string progText = prog.Serialize();
                Web.Program loadProg = Web.Program.Load(progText);
                IEnumerable<WebRegion> executionResult = loadProg.Run(referenceRegion);
                Console.WriteLine("Run first surname extraction program after serialization and deserialization: ");
                foreach (WebRegion region in executionResult)
                {
                    Console.WriteLine(region.GetSpecificSelector());
                    Console.WriteLine(region.Text());
                }
                Console.WriteLine();
            }
        }
    }
}
