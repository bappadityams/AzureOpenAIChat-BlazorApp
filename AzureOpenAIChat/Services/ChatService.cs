
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Graph;
using AzureOpenAIChat.Data;

namespace AzureOpenAIChat.Services
{
 

using Message = Data.Message;

    public class ChatService
    {
        private readonly IConfiguration _configuration;

        private string SystemMessage = "You are an AI assistant that helps people find information about food.  For anything other than food, respond with 'I can only answer questions about food.'";

        public ChatService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<Message> GetResponse(List<Message> messagechain)
        {
            string response = "";

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

            Response<ChatCompletions> resp = await client.GetChatCompletionsAsync(options);

            ChatCompletions completions = resp.Value;

            response = completions.Choices[0].Message.Content;

            Message responseMessage = new Message(response, false);
            return responseMessage;
        }

    }
}

