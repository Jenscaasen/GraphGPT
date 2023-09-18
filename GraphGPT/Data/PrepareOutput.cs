using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphGPT.Data
{
    public class PrepareOutput
    {
        /// <summary>
        /// Top Level description made by the model
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Description of the effect of sending the Graph Call
        /// </summary>
        public string EffectText { get; set; }

        /// <summary>
        /// List of actual Graph Calls to be performed
        /// </summary>
        public GraphCallTemplate GraphCall { get; set; }

        /// <summary>
        /// Parameters to prompt the "human in the loop" for
        /// </summary>
        public List<GraphParameter> Parameters { get; set; }

        public string StepByStep { get; set; }
    }
}