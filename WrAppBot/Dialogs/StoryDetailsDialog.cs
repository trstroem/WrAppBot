// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.3.0

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication;

namespace WrAppBot.Dialogs
{
    public class StoryDetailsDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<StoryDetails> _storyDetailsAccessor;

        public StoryDetailsDialog()
            : base(nameof(StoryDetailsDialog))
        {


            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                TopicStepAsync,
                MoodStepAsync,
                StoryLineAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> TopicStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What should your story be about?") }, cancellationToken);
   
         }

        private async Task<DialogTurnResult> MoodStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Result of previous step
            //stepContext.Values["Topic"] = (string)stepContext.Result;

            var storyDetails = (StoryDetails)stepContext.Options;
            storyDetails.Topic = (string)stepContext.Result;

            //var storyDetails = (StoryDetails)stepContext.Options;

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What is the mood of your story?") }, cancellationToken);

        }

        private async Task<DialogTurnResult> StoryLineAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //Result of previous step
            var storyDetails = (StoryDetails)stepContext.Options;
            storyDetails.Mood = (string)stepContext.Result;

            //var storyDetails = (StoryDetails)stepContext.Options;

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text($"Write the first line of your {storyDetails.Mood} story about {storyDetails.Topic}!") }, cancellationToken);

        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //Result of previous step
            var storyDetails = (StoryDetails)stepContext.Options;
            storyDetails.StoryLine = (string)stepContext.Result;

            //Code for making call to cognitive service

            var resultText = CallCognitiveWebService(storyDetails.StoryLine);

            //End code for call to cognitive service

            var msg = $"Result from AI call: {resultText} ";

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                //var storyDetails = (StoryDetails)stepContext.Options;

                // Get the current profile object from user state.
                var storyDetails = await _storyDetailsAccessor.GetAsync(stepContext.Context, () => new StoryDetails(), cancellationToken);

                // This is already done in the previous steps
                //storyDetails.Topic = (string)stepContext.Values["topic"];
                //storyDetails.Mood = (string)stepContext.Values["mood"];


                return await stepContext.EndDialogAsync(storyDetails, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }


        private string CallCognitiveWebService(string documentText)
        {

            string url = "https://westeurope.api.cognitive.microsoft.com/text/analytics/v2.1/keyPhrases";
            string subscriptionKeyValue = "ba133c233f2849ea9f904b15903fda74";
            const string SUBSCRIPTION_KEY_NAME = "Ocp-Apim-Subscription-Key";
            string result;

            //create the request for the URL
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            //append subscirption as header
            httpWebRequest.Headers.Add(SUBSCRIPTION_KEY_NAME, subscriptionKeyValue);

            //create the payload
            var payload = new
            {
                documents = new[] { new Document() { language = "en", id = "1", text = documentText } ,
                                    new Document() { language = "en", id = "2", text = "Everyone in my family liked the trail but thought it was too challenging for the less athletic among us. Not necessarily recommended for small children." }
                }
            };

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(JsonConvert.SerializeObject(payload, Formatting.Indented));
            }

            //call the web service and get the response
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            return result;
        }

        class Document
        {
            public string id { get; set; }
            public string language { get; set; }

            public string text { get; set; }

        }


    }
}
