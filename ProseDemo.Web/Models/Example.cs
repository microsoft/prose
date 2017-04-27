using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProseDemo.Web.Models {
    public class Example {
        public int Row { get; set; }
        public string Output { get; set; }
    }

    public class TextTransformationRequest {
        public int? SourceColumn { get; set; }
        public Example[] Examples { get; set; }
    }
}
