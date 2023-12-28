// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.18.1

using GPT4Chatbot.Model;
using GPT4Chatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/*
 *The GPTBot class serves as a chatbot that communicates with users, sends their messages to an external
 *Chat OpenAI API
 * retrieves responses, and maintains a chat context for the ongoing conversation. 
*/
namespace GPT4Chatbot.Bots
{
    public class GPTBot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly IStorageHelper _storageHelper;

        public GPTBot(IConfiguration configuration, IStorageHelper storageHelper)
        {
            _configuration = configuration;
            _storageHelper = storageHelper;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;
            ChatContext chatContext = null;

            if (text.ToLower().Equals("new"))
            {
                chatContext = new ChatContext()
                {
                    Model = _configuration["OpenAI:Model"],
                    Messages = new List<Message>()
                {
                    new Message()
                    {
                        Role = "user",
                        Content = "hi"
                    }
                }
                };
            }
            else
            {
                var chatContextEntity = await _storageHelper.GetEntityAsync<GptResponseEntity>(_configuration["StorageAccount:GPTContextTable"], turnContext.Activity.From.Id, turnContext.Activity.Conversation.Id);

                if (chatContextEntity != null)
                {
                    chatContext = new ChatContext();
                    chatContext.Messages = JsonConvert.DeserializeObject<List<Message>>(chatContextEntity.UserContext);
                    chatContext.Model = _configuration["OpenAI:Model"];
                    chatContext.Messages.Add(new Message()
                    {
                        Role = "user",
                        Content = text
                    });
                }
                else
                {
                    chatContext = new ChatContext()
                    {
                        Model = _configuration["OpenAI:Model"],
                        Messages = new List<Message>()
                {
                    new Message()
                    {
                        Role = "user",
                        Content = text
                    }
                }
                    };
                }
            }

            Message gptResponse = await GetGPTResponse(text, chatContext);

            if (gptResponse != null)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(gptResponse.Content, gptResponse.Content), cancellationToken);
                chatContext.Messages.Add(gptResponse);
                await _storageHelper.InsertEntityAsync(_configuration["StorageAccount:GPTContextTable"], new GptResponseEntity()
                {
                    PartitionKey = turnContext.Activity.From.Id,
                    RowKey = turnContext.Activity.Conversation.Id,
                    UserContext = JsonConvert.SerializeObject(chatContext.Messages)
                });
            }
            else
                await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, I didn't understand that."), cancellationToken);
        }

        private async Task<Message> GetGPTResponse(string text, object jsonBody)
        {
            // call an api with a POST request and json body with headers
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(_configuration["OpenAI:APIEndpoint"]);
            client.DefaultRequestHeaders.Accept.Clear();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _configuration["OpenAI:APIKey"]);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);

            request.Content = new StringContent(JsonConvert.SerializeObject(jsonBody), Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request).ConfigureAwait(false);
            var responseString = string.Empty;
            try
            {
                response.EnsureSuccessStatusCode();
                responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseJson = JObject.Parse(responseString);
                return JsonConvert.DeserializeObject<Message>(responseJson["choices"][0]["message"].ToString());
            }
            catch (HttpRequestException ex)
            {
               // await Console.Out.WriteLineAsync(ex.Message);
                return null;
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello this is Andara, I'm using ChatGpt framework. I'm here to assist you in anyways I can";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
