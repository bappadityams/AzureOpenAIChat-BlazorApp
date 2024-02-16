
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Graph;
using AzureOpenAIChat.Data;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System.Buffers.Text;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.Graph.ExternalConnectors;
using Microsoft.Graph.SecurityNamespace;
using System.Threading;
using System.Security.Claims;

namespace AzureOpenAIChat.Services
{
 

using Message = Data.Message;

    public class ChatService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private string SystemMessage = "You are a knowledge base driven chat bot. When you respond, do not mention the context of your answers.  If you do not have context provided to answer your question, do not answer it.";

        public ChatService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<Message> GetResponse(List<Message> messagechain)
        {
            string response = "";

            // Cast to ClaimsIdentity.
            var identity = _httpContextAccessor.HttpContext.User;                       
           
            var groupClaim = identity.Claims.FirstOrDefault(c => c.Type == "groups");
            string user_group_id = groupClaim != null ? groupClaim.Value : string.Empty;

            string currentmessage = messagechain.Last().Body;

            var serviceName = _configuration.GetSection("AzureOpenAIServiceOptions")["CognitiveSearchServiceName"];
            var indexName = _configuration.GetSection("AzureOpenAIServiceOptions")["CognitiveSearchIndexName"];
            var apiKey = _configuration.GetSection("AzureOpenAIServiceOptions")["CognitiveSearchAPIKey"];
            var serviceEndpointURL = _configuration.GetSection("AzureOpenAIServiceOptions")["CognitiveSearchServiceEndpointURL"];
            var filter = user_group_id == null ? string.Empty : $"group_id eq '{user_group_id}'";
            Uri serviceEndpoint = new Uri(serviceEndpointURL);
            AzureKeyCredential credential = new AzureKeyCredential(apiKey);
            SearchClient searchclient = new SearchClient(serviceEndpoint, indexName, credential);
            SearchOptions searchoptions = new SearchOptions() { Filter = filter, Size = 5 };

            // Search for relevant articles based on all the questions in the thread
            StringBuilder allquestions = new StringBuilder();
            foreach (Message message in messagechain)
            {
                if (message.IsRequest)
                {
                    allquestions.Append(message.Body);
                }
            }

            StringBuilder augmentedmessage = new StringBuilder("");
            SearchResults<KnowledgeBaseEntry> results = await searchclient.SearchAsync<KnowledgeBaseEntry>(allquestions.ToString(), searchoptions);

            augmentedmessage.Append("Answer the question in the form of 'Based on my knowledge': \r\n");
            augmentedmessage.Append(currentmessage + "\r\n");
            augmentedmessage.Append("based on following knowledge:'\r\n");


            if (results.GetResults().Count() == 0)
            {
                augmentedmessage.Append("You have no knowledge.\r\n");
            }
            else
            {
                foreach (SearchResult<KnowledgeBaseEntry> result in results.GetResults())
                {
                    augmentedmessage.Append(result.Document.content);
                }
                augmentedmessage.Append("\r\n");
            }

            OpenAIClient client = new OpenAIClient(
                new Uri(_configuration.GetSection("AzureOpenAIServiceOptions")["Endpoint"]!),
                new AzureKeyCredential(_configuration.GetSection("AzureOpenAIServiceOptions")["Key"]!));


            ChatCompletionsOptions options = new ChatCompletionsOptions();
            options.DeploymentName = "chat";
            options.Messages.Add(new ChatRequestSystemMessage(SystemMessage));
            foreach (var msg in messagechain)
            {
                if (msg.IsRequest)
                {
                    options.Messages.Add(new ChatRequestUserMessage(msg.Body));
                }
                else
                {
                    options.Messages.Add(new ChatRequestAssistantMessage(msg.Body));
                }
            }
            options.Messages.Add(new ChatRequestUserMessage(augmentedmessage.ToString()));

            Response<ChatCompletions> resp = await client.GetChatCompletionsAsync(options);

            ChatCompletions completions = resp.Value;

            response = completions.Choices[0].Message.Content;

            Message responseMessage = new Message(response, false);
            return responseMessage;
        }

    }
}

