using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.ProgramSynthesis.Extraction.Web.Semantics;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;

namespace Microsoft.ProgramSynthesis.Extraction.Web.Sample
{
    internal class SampleProgram
    {
        private static string _sampleDocs;

        private static void Main(string[] args)
        {
            var assemblyDir = Path.GetDirectoryName(typeof(SampleProgram).Assembly.Location) ?? ".";
            _sampleDocs = Path.Combine(assemblyDir, "SampleDocuments");

            LearnFirstSurnameInDocumentUsingOneExample();
            LearnFirstSurnameInDocumentUsingMultipleExamples();
            LearnSurnameWithRespectToTableRow();
            LearnSurnameWithRespectToTableRowUsingNegativeExample();
            LearnAllSurnamesInDocument();
            SerializeProgram();
            Console.WriteLine("\nDone.");
        }

        /// <summary>
        /// Learns a program to extract the first surname in the document from one example.
        /// </summary>
        public static void LearnFirstSurnameInDocumentUsingOneExample()
        {
            string s = File.ReadAllText(Path.Combine(_sampleDocs, "sample-document-1.html"));
            HtmlDoc doc = HtmlDoc.Create(s);
            WebRegion referenceRegion = new WebRegion(doc);
            WebRegion exampleRegion = doc.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row
            CorrespondingMemberEquals<WebRegion, WebRegion> exampleSpec = new CorrespondingMemberEquals<WebRegion, WebRegion>(referenceRegion, exampleRegion);
            Web.RegionProgram prog = Web.RegionLearner.Instance.Learn(new[] { exampleSpec });
            if (prog == null) return;
            //run the program to extract first surname from the document
            WebRegion region = prog.Run(new [] { referenceRegion })?.SingleOrDefault();

            Console.WriteLine("Learn first surname in document from one example: ");
            Console.WriteLine(region.GetSpecificSelector());
            Console.WriteLine(region.Text());
            Console.WriteLine();
        }

        /// <summary>
        /// Learns a program to extract the first surname in the document from two examples 
        /// from two different documents.
        /// </summary>
        public static void LearnFirstSurnameInDocumentUsingMultipleExamples()
        {
            string s1 = File.ReadAllText(Path.Combine(_sampleDocs, "sample-document-1.html"));
            HtmlDoc doc1 = HtmlDoc.Create(s1);
            string s2 = File.ReadAllText(Path.Combine(_sampleDocs, "sample-document-2.html"));
            HtmlDoc doc2 = HtmlDoc.Create(s2);
            WebRegion referenceRegion1 = new WebRegion(doc1);
            WebRegion referenceRegion2 = new WebRegion(doc2);
            WebRegion exampleRegion1 = doc1.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row of doc1
            WebRegion exampleRegion2 = doc2.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row of doc2
            CorrespondingMemberEquals<WebRegion, WebRegion> exampleSpec1 = new CorrespondingMemberEquals<WebRegion, WebRegion>(referenceRegion1, exampleRegion1);
            CorrespondingMemberEquals<WebRegion, WebRegion> exampleSpec2 = new CorrespondingMemberEquals<WebRegion, WebRegion>(referenceRegion2, exampleRegion2);
            Web.RegionProgram prog = Web.RegionLearner.Instance.Learn(new[] { exampleSpec1, exampleSpec2 });
            if (prog == null) return;
            //run the program on the second document 
            WebRegion region = prog.Run(new [] { referenceRegion2 })?.SingleOrDefault();

            Console.WriteLine("Learn first surname in document from multiple examples: ");
            Console.WriteLine(region.GetSpecificSelector());
            Console.WriteLine(region.Text());
            Console.WriteLine();
        }

        /// <summary>
        /// Learns a program to extract the surname from a given table row (rather than a whole document).
        /// </summary>
        public static void LearnSurnameWithRespectToTableRow()
        {
            string s = File.ReadAllText(Path.Combine(_sampleDocs, "sample-document-1.html"));
            HtmlDoc doc = HtmlDoc.Create(s);
            WebRegion referenceRegion1 = doc.GetRegion("tr:nth-child(1)"); //1st table row
            WebRegion exampleRegion1 = doc.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row
            WebRegion referenceRegion2 = doc.GetRegion("tr:nth-child(2)"); //2nd table row
            WebRegion exampleRegion2 = doc.GetRegion("tr:nth-child(2) td:nth-child(2)"); //2nd cell in 2nd table row
            CorrespondingMemberEquals<WebRegion, WebRegion> exampleSpec1 = new CorrespondingMemberEquals<WebRegion, WebRegion>(referenceRegion1, exampleRegion1);
            CorrespondingMemberEquals<WebRegion, WebRegion> exampleSpec2 = new CorrespondingMemberEquals<WebRegion, WebRegion>(referenceRegion2, exampleRegion2);
            Web.RegionProgram prog = Web.RegionLearner.Instance.Learn(new[] { exampleSpec1, exampleSpec2 });
            if (prog == null) return;
            //run the program on 5th table row
            WebRegion fifthRowRegion = doc.GetRegion("tr:nth-child(5)"); //5th table row
            WebRegion region = prog.Run(new [] { fifthRowRegion })?.SingleOrDefault();

            Console.WriteLine("Learn surname with respect to table row: ");
            Console.WriteLine(region.GetSpecificSelector());
            Console.WriteLine(region.Text());
            Console.WriteLine();
        }

        /// <summary>
        /// Learns a program to extract the surname from a given table row (rather than a whole document) 
        /// using a negative example.
        /// </summary>
        public static void LearnSurnameWithRespectToTableRowUsingNegativeExample()
        {
            string s = File.ReadAllText(Path.Combine(_sampleDocs, "sample-document-1.html"));
            HtmlDoc doc = HtmlDoc.Create(s);
            WebRegion referenceRegion1 = doc.GetRegion("tr:nth-child(1)"); //1st table row
            WebRegion referenceRegion2 = doc.GetRegion("tr:nth-child(2)"); //2nd table row
            var posExampleSpec = new CorrespondingMemberEquals<WebRegion, WebRegion>(referenceRegion1, doc.GetRegion("tr:nth-child(1) td:nth-child(2)"));
            var negExampleSpec = new CorrespondingMemberDoesNotEqual<WebRegion, WebRegion>(referenceRegion2, doc.GetRegion("tr:nth-child(2) td:nth-child(1)"));
            Web.RegionProgram prog = Web.RegionLearner.Instance.Learn(new Constraint<IEnumerable<WebRegion>, IEnumerable<WebRegion>>[] { posExampleSpec, negExampleSpec });
            if (prog == null) return;
            WebRegion region = prog.Run(new [] { referenceRegion1 })?.SingleOrDefault();

            Console.WriteLine("Learn surname with respect to table row using negative example: ");
            Console.WriteLine(region.GetSpecificSelector());
            Console.WriteLine(region.Text());
            Console.WriteLine();
        }

        /// <summary>
        /// Learns a program to extract the sequence of all surnames in a document.
        /// </summary>
        public static void LearnAllSurnamesInDocument()
        {
            string s = File.ReadAllText(Path.Combine(_sampleDocs, "sample-document-1.html"));
            HtmlDoc doc = HtmlDoc.Create(s);
            WebRegion referenceRegion = new WebRegion(doc);
            WebRegion exampleRegion1 = doc.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row of doc
            WebRegion exampleRegion2 = doc.GetRegion("tr:nth-child(2) td:nth-child(2)"); //2nd cell in 2nd table row of doc
            var exampleSpec = new MemberSubset<WebRegion, WebRegion>(referenceRegion, new[] { exampleRegion1, exampleRegion2 });
            Web.SequenceProgram prog = Web.SequenceLearner.Instance.Learn(new[] { exampleSpec });
            if (prog == null) return;
            IEnumerable<WebRegion> executionResult = prog.Run(new [] { referenceRegion })?.SingleOrDefault();
            Console.WriteLine("Learn all surnames in document: ");
            foreach (WebRegion region in executionResult)
            {
                Console.WriteLine(region.GetSpecificSelector());
                Console.WriteLine(region.Text());
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Learns a program and then serializes and deserializes it.
        /// </summary>
        public static void SerializeProgram()
        {
            string s = File.ReadAllText(Path.Combine(_sampleDocs, "sample-document-1.html"));
            HtmlDoc doc = HtmlDoc.Create(s);
            WebRegion referenceRegion = new WebRegion(doc);
            WebRegion exampleRegion = doc.GetRegion("tr:nth-child(1) td:nth-child(2)"); //2nd cell in 1st table row
            CorrespondingMemberEquals<WebRegion, WebRegion> exampleSpec = new CorrespondingMemberEquals<WebRegion, WebRegion>(referenceRegion, exampleRegion);
            Web.RegionProgram prog = Web.RegionLearner.Instance.Learn(new[] { exampleSpec });
            if (prog == null) return;
            string progText = prog.Serialize();
            Web.RegionProgram loadProg = Web.Loader.Instance.Region.Load(progText);
            IEnumerable<WebRegion> executionResult = loadProg.Run(new[] { referenceRegion });
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
