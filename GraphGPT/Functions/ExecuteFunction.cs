using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Azure;
using Azure.Identity;
using GraphGPT.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using OpenAI_API;

namespace GraphGPT.Functions
{
    public class ExecuteFunction
    {
        private readonly ILogger<ExecuteFunction> _logger;

        public ExecuteFunction(ILogger<ExecuteFunction> logger)
        {
            _logger = logger ?? NullLogger<ExecuteFunction>.Instance;
        }

        [FunctionName("execute")]
        [OpenApiOperation(operationId: "execute", tags: new[] { "execute" })]
        [OpenApiRequestBody("application/json", typeof(ExecuteInput), Description = "The Parameters needed to run the graph calls")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(List<ExecuteOutput>), Description = "The output of the graph calls")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function running 'execute'.");

            try
            {
                // Read Request Body and Deserialize into ExecuteInput
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogDebug("Request body: {RequestBody}", requestBody);
                var executeInput = JsonConvert.DeserializeObject<ExecuteInput>(requestBody);
                _logger.LogInformation("Request's executeInput: {ExecuteInput}", executeInput);

                // Get GraphCallTemplates and execute Graph Calls
                var templates = await FinishGraphCallTemplatesAsync(executeInput);
                var graphCallOutputs = await ExecuteGraphCallsAsync(templates, executeInput);

                // Return the response
                return new OkObjectResult(graphCallOutputs ?? throw new ArgumentNullException(nameof(graphCallOutputs)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing the function");
                return new BadRequestObjectResult("An error occurred while processing the request.");
            }
        }

        private static HttpMethod GetMethodByName(string method)
        {
            return method.ToLower() switch
            {
                "get" => HttpMethod.Get,
                "post" => HttpMethod.Post,
                "patch" => HttpMethod.Patch,
                "put" => HttpMethod.Put,
                "delete" => HttpMethod.Delete,
                _ => throw new Exception("Unknown Method: " + method),
            };
        }

        private async Task<List<ExecuteOutput>> ExecuteGraphCallsAsync(List<GraphCallTemplate> templates, ExecuteInput executeInput)
        {
            using var httpClient = new HttpClient();
            var graphCallOutputs = new List<ExecuteOutput>();
            var accessToken = string.Empty;

            // Get Access Token from Bearer Token Parameter or from Azure SDK Default Credential
            var bearerTokenParameter = executeInput.Parameters.FirstOrDefault(p => p.Name == "Token");
            if (bearerTokenParameter != null)
            {
                accessToken = bearerTokenParameter.Value;
            }
            else
            {
                var credential = new DefaultAzureCredential();
                var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));
                accessToken = token.Token;
            }

            // Execute each Graph Call
            foreach (var template in templates)
            {
                var request = new HttpRequestMessage
                {
                    Method = new HttpMethod(template.Method),
                    Headers =
                    {
                        { "Authorization", $"Bearer {accessToken}" },
                        { "ContentType", "application/json" }
                    }
                };

                var requestUrl = template.Url;
                request.RequestUri = new Uri(requestUrl);

                if (!string.IsNullOrEmpty(template.Body))
                {
                    request.Content = new StringContent(template.Body, Encoding.UTF8, "application/json");
                }

                // Send the request and store the response
                var response = await httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();
                graphCallOutputs.Add(new ExecuteOutput
                {
                    Success = response.IsSuccessStatusCode,
                    Url = requestUrl,
                    Result = responseText
                });
            }

            // Return the responses
            return graphCallOutputs;
        }

        private async Task<List<GraphCallTemplate>> FinishGraphCallTemplatesAsync(ExecuteInput executeInput)
        {
            // Get OpenAI API Key
            var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(openAIApiKey))
            {
                throw new Exception("Please add 'OPENAI_API_KEY' to your environment variables");
            }

            var api = new OpenAIAPI(openAIApiKey);

            // Create a Conversation and Prompt the ai for Graph Calls
            var chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage("You are a JSON AI. You only answer with JSON, no explanations, acknowledgements or any other text.");
            chat.AppendUserInput(executeInput.prompt);
            chat.AppendUserInput($"Graph Calls: {JsonConvert.SerializeObject(executeInput.GraphCalls)}");
            chat.AppendUserInput($"Parameters: {JsonConvert.SerializeObject(executeInput.Parameters)}");
            chat.AppendUserInput("Complete the list of Graph Calls with the parameters provided by replacing placeholders and return it");

            var graphCallsResponseJson = await chat.GetResponseFromChatbotAsync();
            var templates = JsonConvert.DeserializeObject<List<GraphCallTemplate>>(graphCallsResponseJson);

            return templates;
        }
    }
}