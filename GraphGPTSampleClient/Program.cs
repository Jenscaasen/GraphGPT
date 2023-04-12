namespace GraphGPTSampleClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the GraphGPT Sample Client!");
            Console.WriteLine("Press any key once the function runtime has loaded");
            Console.ReadKey();

            HttpClient http = new HttpClient();
            Console.WriteLine("Write the prompt that you want to execute (e.g. 'Disable the useraccount 'ab@cd.ef')");
            string prompt = Console.ReadLine();

            swaggerClient client = new swaggerClient(http);

            Console.WriteLine($"Preparing for prompt '{prompt}'...");
            PrepareOutput prepareOutput = await client.PrepareAsync(prompt);
            
            ExecuteInput executeInput = new ExecuteInput();
            executeInput.GraphCalls = prepareOutput.GraphCalls;
            executeInput.Prompt = prompt;
            executeInput.Parameters = new List<GraphParameter>();
            Console.WriteLine("________________");
            Console.WriteLine(prepareOutput.Description);
            Console.WriteLine("________________");
            Console.WriteLine("Theese URLs will be called:");
            foreach (var graphCall in prepareOutput.GraphCalls)
            {
                Console.WriteLine($"{graphCall.Method} {graphCall.Url} ({graphCall.PermissionText})");
            }
            Console.WriteLine("________________");
            Console.WriteLine("Input Parameters");

            var callsForUser = prepareOutput.Parameters.Where(p => p.Fullfill == "user");
            foreach (var parameter in callsForUser)
            {
                Console.WriteLine("________________");
                Console.WriteLine($"Parameter '{parameter.Name}': {parameter.Description}");
                Console.WriteLine($"Example: {parameter.Example}");
                string value = parameter.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    Console.WriteLine($"Enter value (Autoselected is '{value}'): ");

                    string input = Console.ReadLine();
                    if (!string.IsNullOrEmpty(input))
                    {
                        value = input;
                    }
                    else
                    {
                        Console.WriteLine($"Using autoselect value '{value}'");
                    }
                }
                else
                {
                    Console.WriteLine($"Enter value: ");
                    value = Console.ReadLine();
                }
              
                executeInput.Parameters.Add(new GraphParameter() { Name = parameter.Name, Value = value });
            }

            Console.WriteLine($"using the following parameters to execute the Graph calls:");
            foreach (var parameter in executeInput.Parameters)
            {
                Console.WriteLine($"'{parameter.Name}': {parameter.Value}");
            }

          var executeResult = await client.ExecuteAsync(executeInput);

            Console.WriteLine("________________");
            Console.WriteLine("Outputs of execution:");
            foreach (var output in executeResult)
            {
                Console.WriteLine($"Success: '{output.Success}' on '{output.Url}'. ({output.Result})");
            }
            
            Console.WriteLine("done");
            Console.ReadLine();
        }     
    }
}