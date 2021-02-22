using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;
using Microsoft.ProgramSynthesis.Utils.Interactive;
using Microsoft.ProgramSynthesis.Wrangling.Tree;
using static MergeConflictsResolution.Utils;

namespace MergeConflictsResolution
{
    /// <summary>
    ///     The implementations of the operators in the language.
    /// </summary>
    public static class Semantics
    {
        private static readonly string[] ProjectSpecificKeywords = { "edge", "microsoft", "EDGE" };

        /// <summary>
        ///     Return the action if the pattern is matched.
        /// </summary>
        /// <param name="pattern">Pattern is matched or not.</param>
        /// <param name="action">Resolved conflict.</param>
        /// <returns>Returns resolved conflict if pattern matches.</returns>
        public static IReadOnlyList<Node> Apply(bool pattern, IReadOnlyList<Node> action)
        {
            return pattern == true ? action : null;
        }

        /// <summary>
        ///     Removes list of selected Nodes from the input list.
        /// </summary>
        /// <param name="input">Input conflict/file content.</param>
        /// <param name="selected">Selected Node.</param>
        /// <returns>Return a list of Node after removing the selected nodes.</returns>
        public static IReadOnlyList<Node> Remove(IReadOnlyList<Node> input, IReadOnlyList<Node> selected)
        {
            List<Node> result = input.ToList();
            List<int> removedIndices = new List<int>();
            int returnIndex;
            foreach (Node n in selected)
            {
                returnIndex = IndexNode(input, n);
                if (returnIndex != -1)
                {
                    removedIndices.Add(returnIndex);
                }
            }

            foreach (int index in removedIndices.OrderByDescending(v => v))
            {
                result.RemoveAt(index);
            }

            return result;
        }

        /// <summary>
        ///     Returns index of the selected Node in the list.
        /// </summary>
        /// <param name="input">Input conflict/file content.</param>
        /// <param name="selected">Selected Node.</param>
        /// <returns>Returns the index of the select node in the list.</returns>
        public static int IndexNode(IReadOnlyList<Node> input, Node selected)
        {
            string attrValue = NodeValue(selected, Path);
            int k = 0;

            foreach (Node n in input)
            {
                if (NodeValue(n, Path) == attrValue)
                {
                    return k;
                }

                k++;
            }

            return -1;
        }

        /// <summary>
        ///     Joins two set of lines in the conflicts (List of nodes).
        /// </summary>
        /// <param name="input1">Input conflict/file content.</param>
        /// <param name="input2">Input conflict/file content.</param>
        /// <returns>Returns the joined list of nodes.</returns>
        public static IReadOnlyList<Node> Concat(IReadOnlyList<Node> input1, IReadOnlyList<Node> input2)
        {
            return input1.Concat(input2).ToList();
        }

        /// <summary>
        ///     Selects the upstream line by index.
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <param name="k">Index of the upstream node.</param>
        /// <returns>Returns the upstream node with the matched index.</returns>
        public static IReadOnlyList<Node> SelectUpstreamIdx(MergeConflict x, int k)
        {
            List<Node> result = new List<Node>();
            if (x.Upstream.Count > k)
            {
                result.Add(x.Upstream[k]);
            }

            return result;
        }

        /// <summary>
        ///     Visits all the nodes.
        /// </summary>
        /// <param name="node">Input node.</param>
        /// <returns>Returns the list of visited nodes.</returns>
        public static IReadOnlyList<Node> AllNodes(Node node)
        {
            return PostOrderVisitor.GetAllNodes(node);
        }

        /// <summary>
        ///     Selects the downstream line by index. 
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <param name="k">index</param>
        /// <returns>Returns the downstream node with the matched index.</returns>
        public static IReadOnlyList<Node> SelectDownstreamIdx(MergeConflict x, int k)
        {
            List<Node> result = new List<Node>();
            if (x.Downstream.Count > k)
            {
                result.Add(x.Downstream[k]);
            }

            return result;
        }

        /// <summary>
        ///     Selects downstream.
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <returns>Return list of downstream nodes.</returns>
        public static IReadOnlyList<Node> SelectDownstream(MergeConflict x)
        {
            return x.Downstream;
        }

        /// <summary>
        ///     Selects upstream.
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <returns>Return list of upstream nodes.</returns>
        public static IReadOnlyList<Node> SelectUpstream(MergeConflict x)
        {
            return x.Upstream;
        }

        /// <summary>
        ///     Select the node with the specified path (upstream).
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <param name="k">Path name</param>
        /// <returns>Returns the list of downstream nodes that matches with the path name.</returns>
        public static IReadOnlyList<Node> SelectDownstreamPath(MergeConflict x, string k)
        {
            return SelectPath(x.Downstream, k);
        }

        /// <summary>
        ///     Select the node with the specified path (downstream).
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <param name="k">Path name</param>
        /// <returns>Returns the list of downstream nodes that matches with the path name.</returns>
        public static IReadOnlyList<Node> SelectUpstreamPath(MergeConflict x, string k)
        {
            return SelectPath(x.Upstream, k);
        }

        /// <summary>
        ///     Selects the matched node from the input.
        /// </summary>
        /// <param name="list">File or conflict content.</param>
        /// <param name="k">Path name.</param>
        /// <returns>Returns all the nodes that matches with the path name.</returns>
        private static IReadOnlyList<Node> SelectPath(IReadOnlyList<Node> list, string k)
        {
            List<Node> result = new List<Node>();
            foreach (Node node in list)
            {
                if (node.Attributes.TryGetValue(Path, out string ret) && ret == k)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        /// <summary>
        ///     Returns a list of matched nodes.
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <param name="paths">List of include file names or macro-related content.</param>
        /// <returns>Returns the resolved conflict.</returns>
        public static List<IReadOnlyList<Node>> FindMatch(MergeConflict x, string[] paths)
        {
            List<IReadOnlyList<Node>> result = new List<IReadOnlyList<Node>>
            {
                FindDuplicateInUpstreamAndDownstream(x),
                FindFreqPattern(x, paths),
                FindDuplicateInDownstreamOutside(x),
                FindDuplicateInUpstreamOutside(x),
                FindUpstreamSpecific(x),
                FindDownstreamSpecific(x)
            };

            return result;
        }

        /// <summary>
        ///     Validates if enabled predicate is present or not.
        /// </summary>
        /// <param name="dup">A list of matched patterns.</param>
        /// <param name="enabledPredicate">A guarding condition to apply the action related to the match.</param>
        /// <returns>Returns a bool based on the match.</returns>
        public static bool Check(IReadOnlyList<IReadOnlyList<Node>> dup, int[] enabledPredicate)
        {
            return enabledPredicate.All(predicateCheck => dup[predicateCheck].Count > 0);
        }

        /// <summary>
        ///     Identify duplicate headers inside the conflicting region.
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <returns>Returns the duplicate node within the downstream and the upstream.</returns>
        public static IReadOnlyList<Node> FindDuplicateInUpstreamAndDownstream(MergeConflict x)
        {
            List<Node> nodes = new List<Node>();
            foreach (Node upstream in x.Upstream)
            {
                string upstreamValue = NodeValue(upstream, Path);
                foreach (Node downstream in x.Downstream)
                {
                    string downstreamValue = NodeValue(downstream, Path);
                    if (IsMatchPath(upstreamValue, downstreamValue))
                    {
                        nodes.Add(upstream);
                    }
                }
            }

            return nodes;
        }

        /// <summary>
        ///     Identifies the project specific pattern.
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <param name="paths"></param>
        /// <returns>Returns a list of node that has project specific pattern.</returns>
        public static IReadOnlyList<Node> FindFreqPattern(MergeConflict x, string[] paths)
        {
            IEnumerable<Node> nodes = from stream in Concat(x.Downstream, x.Upstream)
                                      from path in paths
                                      where NodeValue(stream, Path) == path
                                      select stream;
            return nodes.ToList();
        }

        /// <summary>
        ///     Identify duplicate headers outside the conflicting region (downstream specific)
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <returns>Returns the duplicate nodes outside the conflicted region.</returns>
        public static List<Node> FindDuplicateInDownstreamOutside(MergeConflict x)
        {
            List<Node> nodes = new List<Node>();
            IReadOnlyList<Node> downstreamFileIncludeAst = x.UpstreamContent;
            foreach (Node downstream in x.Downstream)
            {
                string downstreamValue = NodeValue(downstream, Path);
                foreach (Node outsideInclude in downstreamFileIncludeAst)
                {
                    string outsideIncludeValue = NodeValue(outsideInclude, Path);
                    if (IsMatchPath(outsideIncludeValue, downstreamValue))
                    {
                        nodes.Add(downstream);
                    }
                }
            }

            return nodes;
        }

        /// <summary>
        ///     Identify duplicate headers outside the conflicting region (upstream specific).
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <returns>Returns the duplicate nodes outside the conflicted region.</returns>
        public static List<Node> FindDuplicateInUpstreamOutside(MergeConflict x)
        {
            List<Node> nodes = new List<Node>();
            IReadOnlyList<Node> upstreamFileIncludeAst = x.UpstreamContent;
            foreach (Node upstream in x.Upstream)
            {
                string upstreamValue = NodeValue(upstream, Path);
                foreach (Node outsideInclude in upstreamFileIncludeAst)
                {
                    string outsideIncludeValue = NodeValue(outsideInclude, Path);
                    if (IsMatchPath(outsideIncludeValue, upstreamValue))
                    {
                        nodes.Add(upstream);
                    }
                }
            }

            return nodes;
        }

        /// <summary>
        ///     Identify the upstream specific content.
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <returns>Returns the upstream specific content.</returns>
        public static List<Node> FindUpstreamSpecific(MergeConflict x)
        {
            return FindSpecific(x.Upstream, x.Downstream);
        }

        /// <summary>
        ///     Identify the downstream specific header path.
        /// </summary>
        /// <param name="x">The input merge conflict.</param>
        /// <returns>Returns the downstream specific section of the conflict.</returns>
        public static List<Node> FindDownstreamSpecific(MergeConflict x)
        {
            return FindSpecific(x.Downstream, x.Upstream);
        }

        /// <summary>
        ///     Identifies whether any project specific keywords contains in the merge conflict.
        /// </summary>
        /// <param name="stream1">Upstream content of the conflict.</param>
        /// <param name="stream2">Downstream content of the conflict.</param>
        /// <returns>Returns a list of nodes that matches with the keywords.</returns>
        public static List<Node> FindSpecific(IReadOnlyList<Node> stream1, IReadOnlyList<Node> stream2)
        {
            List<Node> nodes = new List<Node>();
            foreach (Node node1 in stream1)
            {
                string value1 = NodeValue(node1, Path);
                if (!value1.Contains(".h"))
                {
                    bool flag = false;
                    string split = value1.Split('(')[0];
                    foreach (Node node2 in stream2)
                    {
                        string value2 = NodeValue(node2, Path);
                        if (split != value2.Split('(')[0])
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (flag == true)
                    {
                        if (ProjectSpecificKeywords.Any(s => value1.Contains(s)))
                        {
                            nodes.Add(node1);
                        }
                    }
                }
                else if (ProjectSpecificKeywords.Any(s => value1.Contains(s)))
                {
                    nodes.Add(node1);
                }
            }

            return nodes;
        }

        /// <summary>
        ///     Validates if the list of node empty or not.
        /// </summary>
        /// <param name="l">File or conflict content</param>
        /// <returns>Returns a bool based on the match.</returns>
        public static bool Match(IReadOnlyList<Node> l)
        {
            return l.Count != 0;
        }

        /// <summary>
        ///     Checks two include path and match based on the name of the file.
        /// </summary>
        /// <param name="path1">Name of the include file.</param>
        /// <param name="path2">Name of the include file.</param>
        /// <returns>Returns a bool based on the match.</returns>
        static bool IsMatchPath(string path1, string path2)
        {
            return path1.Split('/').Last() == path2.Split('/').Last();
        }


        /// <summary>
        ///     Extracts the value associated with the variable in the Node.
        /// </summary>
        /// <param name="node">Node content.</param>
        /// <param name="name">Attribute name in the node.</param>
        /// <returns>Returns the value associated with the variable.</returns>
        public static string NodeValue(Node node, string name)
        {
            node.Attributes.TryGetValue(name, out string attributeNameSource);
            return attributeNameSource;
        }
    }

    internal class PostOrderVisitor : NodeVisitor<Node>
    {
        private PostOrderVisitor() { }

        public static IReadOnlyList<Node> GetAllNodes(Node node) {
            var visitor = new PostOrderVisitor();
            node.AcceptVisitor(visitor);
            return visitor._nodes.ToArray();
        }

        private readonly List<Node> _nodes = new List<Node>();

        public override Node VisitStruct(StructNode node)
        {
            node.Children.ForEach(c => c.AcceptVisitor(this));
            _nodes.Add(node);
            return node;
        }

        public override Node VisitSequence(SequenceNode node)
        {
            node.Children.ForEach(c => c.AcceptVisitor(this));
            _nodes.Add(node);
            return node;
        }
    }
}