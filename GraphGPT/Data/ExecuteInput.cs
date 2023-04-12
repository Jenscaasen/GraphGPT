using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphGPT.Data
{
    public class ExecuteInput
    {
        public string prompt { get; set; }
        public List<GraphParameter> Parameters { get; set; }
        /// <summary>
        /// List of actual Graph Calls to be performed, copied from the PrepareOutput
        /// </summary>
        public List<GraphCallTemplate> GraphCalls { get; set; }
    }
}
