namespace GraphGPT.Data
{
    public class GraphParameter
    {
        /// <summary>
        /// The Description of why the Parameter is needed
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// An example of what to enter for this parameter
        /// </summary>
        public string Example { get; set; }

        /// <summary>
        /// Who has to fullfill the parameter, e.g. "user" or "system". Default is "user"
        /// </summary>
        public string Fullfill { get; set; } = "user";

        /// <summary>
        /// The Name of the Parameter
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The Value extracted from the prompt, if any
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The variable name of the parameter to be used to replace body contents, e.g. "$userID"
        /// </summary>
        public string VariableName { get; set; }
    }
}