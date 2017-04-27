using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProseDemo.Web.Models {
    public class TTextLearnResponse : Response {
        public object[] Output { get; set; }
    }

    public class STextLearnResponse : Response {
        public List<string[]> Output { get; set; }
    }

    public class Response {
        public string Description { get; set; }
        public string ProgramXML { get; set; }
        public string ProgramHumanReadable { get; set; }
        public string ProgramPython { get; set; }
    }
}
