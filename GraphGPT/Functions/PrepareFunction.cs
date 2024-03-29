using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using GraphGPT.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using OpenAI_API;

namespace GraphGPT.Functions
{
    public class Function
    {
        private readonly ILogger<Function> _logger;

        public Function(ILogger<Function> log)
        {
            _logger = log;
        }

        [FunctionName("prepare")]
        [OpenApiOperation(operationId: "prepare", tags: new[] { "Prepare" })]
        [OpenApiParameter(name: "prompt", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The prompt made by the user")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(PrepareOutput), Description = "The Output for processing by the user")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string prompt = req.Query["prompt"];

            PrepareOutput output = await GetOutputFromPrompt(prompt);
            output.Parameters.Add(new GraphParameter
            {
                Name = "Token",
                Description = "The Graph Call access token. should be filled by the system, not the user",
                Fullfill = "system"
            });
            _logger.LogInformation("Output generated from prompt.");

            return new OkObjectResult(output);
        }

        private async Task<PrepareOutput> GetOutputFromPrompt(string prompt)
        {
            string OpenAIAPIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(OpenAIAPIKey))
            {
                throw new Exception("Please add 'OPENAI_API_KEY' to your enviroment variables");
            }
            OpenAIAPI api = new OpenAIAPI(OpenAIAPIKey);

            var chat = api.Chat.CreateConversation();

            PrepareOutput exampleOutput = new PrepareOutput
            {
                Description = "Describe which parameters are needed to successfully perform the Graph Call, and what the final graph Call will look like as an example",
                EffectText = "Describe what will be the consequence of sending this Graph Call. What will happen?",
                StepByStep = "Think about it Step By Step: what is required, and what needs to happen?",
                Parameters = new System.Collections.Generic.List<GraphParameter> {
                new GraphParameter {
                    Name="The name of a parameter, e.g. Username",
                    Description = "The description, e.g. 'The username is needed as part of the HTTP GET to identify the account'",
                    Example = "jens.caasen@example.org",
                    Value = "jens.caasen@example.org",
                    VariableName = "$userID"
                },
                 new GraphParameter {
                    Name="AccountEnabled",
                    Description = "Sets if the Account will be enabled or not",
                    Example = "true or false",
                    Value = "true",
                    VariableName  = "$accountEnabled"
                }
            },
                GraphCall =
                    new GraphCallTemplate
                    {
                        Method = "PATCH",
                        Url = "https://graph.microsoft.com/v1.0/users/$userID",
                        Body = @"{""accountEnabled"": '$accountEnabled'}",
                        PermissionText = "User.ReadWrite.All"
                    }
            };
            _logger.LogInformation("Example output generated.");

            string exampleOutputText = System.Text.Json.JsonSerializer.Serialize(exampleOutput);
            chat.AppendSystemMessage("You are a JSON AI. You only answer with JSON, no explanations, acknowledgements or any other text.");
            string jsonCompletePromt = $"From this prompt, I want to know what parameters are needed to successfully perform the delegated Graph Call." +
                $"Use the Beta endpoint if possible." +
                $"Use all variables available in a graph call. When it makes sense that a user chooses a specific text or information, offer that " +
                $"as a variable as parameter. Do not fill out content that the user also could fill. Find a way to only do one graph call, not multiple." +
                $"Here is an example of what I expect to get back from you: {exampleOutputText}." +
                $"the current date and time is " + DateTime.Now.ToString();
            jsonCompletePromt += "Remember to use delegated permissions, not application permissions. This means to send an email for example, use 'https://graph.microsoft.com/v1.0/me/sendMail', not 'https://graph.microsoft.com/v1.0/users/{user-id}/sendMail'";

            jsonCompletePromt += "prompt: " + prompt;
            chat.AppendUserInput(jsonCompletePromt);
            chat.Model = "gpt-4";

            _logger.LogInformation("Prompt sent to AI model.");

            string modelResponse = await chat.GetResponseFromChatbotAsync();

            PrepareOutput modelOutput = System.Text.Json.JsonSerializer.Deserialize<PrepareOutput>(modelResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true });

            _logger.LogInformation("AI model response received.");

            return modelOutput;
        }
    }
}