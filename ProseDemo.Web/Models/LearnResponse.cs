using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProseDemo.Web.Models {
    public class LearnResponse {
        public object[] Output { get; set; }
        public string Description { get; set; }
        public string ProgramXML { get; set; }
        public string ProgramHumanReadable { get; set; }
        public string ProgramPython { get; set; }
    }
}
