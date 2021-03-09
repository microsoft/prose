using System.Collections.Generic;
using Microsoft.ProgramSynthesis.Wrangling.Tree;
using static MergeConflictsResolution.Utils;

namespace MergeConflictsResolution
{
    /// <summary>
    ///     Class to represent a merge conflict.
    /// </summary>
    public class MergeConflict
    {
        /// <summary>
        ///     Constructs a merge conflict object.
        /// </summary>
        /// <param name="conflict">The merge conflict text.</param>
        /// <param name="fileContent">The optional file content.</param>
        /// <param name="filePath">The optional file path.</param>
        public MergeConflict(string conflict, string fileContent = null, string filePath = null)
        {
            string headLookup = "<<<<<<< HEAD";
            string middleLookup = "=======";
            string endLookup = ">>>>>>>";

            bool inHeadSection = false;
            string[] linesConflict = conflict.SplitLines();
            List<string> conflictsInForked = new List<string>();
            List<string> conflictsInMain = new List<string>();
            foreach (string line in linesConflict)
            {
                if (line.StartsWith(Include) && inHeadSection == false)
                {
                    conflictsInMain.Add(line.NormalizeInclude());
                }

                if (line.StartsWith(Include) && inHeadSection == true)
                {
                    conflictsInForked.Add(line.NormalizeInclude());
                }

                if (line.StartsWith(headLookup))
                {
                    inHeadSection = true;
                }

                if (line.StartsWith(middleLookup))
                {
                    inHeadSection = false;
                }
            }

            string[] linesContent = fileContent.SplitLines();
            bool isOutsideConflicts = true;
            List<string> outsideConflictContent = new List<string>();
            foreach (string line in linesContent)
            {
                if (line.StartsWith(Include) && isOutsideConflicts)
                {
                    outsideConflictContent.Add(line.NormalizeInclude());
                }

                if (line.StartsWith(headLookup))
                {
                    isOutsideConflicts = false;
                }

                if (line.StartsWith(endLookup))
                {
                    isOutsideConflicts = true;
                }
            }

            this.Upstream = PathToNode(conflictsInForked);
            this.Downstream = PathToNode(conflictsInMain);
            this.UpstreamContent = PathToNode(outsideConflictContent);
            this.BasePath = filePath;
        }

        internal string BasePath { get; }

        internal IReadOnlyList<Node> Upstream { get; }

        internal IReadOnlyList<Node> Downstream { get; }

        internal IReadOnlyList<Node> UpstreamContent { get; }

        private static IReadOnlyList<Node> PathToNode(List<string> path)
        {
            List<Node> list = new List<Node>();
            foreach (string pathValue in path)
            {
                Attributes.Attribute attr = new Attributes.Attribute(Path, pathValue);
                Attributes.SetKnownSoftAttributes(new[] { "", "" });
                Node node = StructNode.Create("node1", new Attributes(attr));
                list.Add(node);
            }
            return list.AsReadOnly();
        }
    }
}
