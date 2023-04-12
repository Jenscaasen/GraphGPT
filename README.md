# GraphGPT
This repository aims to translate unstructired commands, that can be fullfilled by Microsoft Graph, into a single- or multi-step process of execution. For example: you can get the command "Enable the user peter.parker@newspaper.io in azure AD" from a software, bot or workflow, and let GraphGPT handle the extraction of entities, choice of HTTP method, URL and Body.

# Human in the loop
The Solution is build with the "Human in the loop" in mind. To run a command there is a 2 step process:
first, the preparation (/prepare), which extracts what to do and how to do it. This endpoint returns a list of parameters which need to be checked by a human. In case of missing information ("Enable the account" << what account?) the user needs to supply the information. The user can also alter the actual Graph calls to his liking. One might want to skip this step entirely and use the automatically extracted information.

Step 2 is the execution, this is where the parameters are placed into the placeholders of the actual graph call, and the graph call is executed.

# Features
-Cloud Native Solution  
-Executes free written commands as Microsoft Graph API REST Requests  
-Multiple stages enable control for humans  
-Can use application or user permissions  
-Endpoints are OpenAPI enabled to make integration easier  

# Demo

https://user-images.githubusercontent.com/8245848/231466057-e975505c-bc6d-46e1-8870-2e983cc1241f.mp4


# Autorization of Graph Calls
There a 2 ways of autorizing the calls towards Graph API: using a self supplied token, or automatically via DefaultAzureCredentials
The /prepare endpoint prompts the user to supply a "Token" parameter which is used by the /execute endpoint to make the call. Use this parameter when you want to run the Graph Call with the users rights or if you want to choose the JWT Token yourself, for example by choosing a different Application Registration depending on the access rights needed.

When no Token is provided the /execute function uses DefaultAzureCredentials as a fallback, which means your deployed application needs to have the apropriate rights via Managed Identities or have an EnviromentCredential set up

# Requirements
To run GraphGPT, you need a Microsoft Azure Account and knowledge in deploying and configuring Azure Functions. You also need an OpenAI Account with an API Key you need to provide to the Azure Function.

# Known Issues
-GPT-4 sometimes does not handle JSON correctly or does not understand his own choice of parameters, and gives up. To circumvent this, try again  
-In some complex scenarios, especially with filters, the documentation on how Graph works is different from how it actually works, and GPT-4 only knows what was written in the official documentation (which is wrong sometimes regarding Microsoft Graph)
