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
using Microsoft.Bot.Builder.Adapters;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;
using System.Collections;
using System.Diagnostics.Tracing;

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

            var resultCS = new CognitiveResult();

            var resultCSString = await CallCognitiveWebServiceAsync(storyDetails.StoryLine);

            var deserializedDocument = JsonConvert.DeserializeObject<CognitiveResult>(resultCSString);

            //End code for call to cognitive service

            //Now, we need to disentangle the result, which looks like this:
            // {�documents�:[{�id�:�1�,�keyPhrases�:[�gnome�,�pink clothes�,�bitcoin wallet�]}],�errors�:[]}

 
            // Getting the first keyword
            var firstKeyword = deserializedDocument.KeyPhrases[0].keyword.ToString();
            var sentiment = deserializedDocument.Sentiment;

            var resultFromStoryAI = callStoryAI(firstKeyword, sentiment);

            var msg = $"{resultFromStoryAI}";



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


        private async Task<string> CallCognitiveWebServiceAsync(string documentText)
        {

            //New
            string SubscriptionKey = "ba133c233f2849ea9f904b15903fda74";
            string Endpoint = "https://westeurope.api.cognitive.microsoft.com";


            var credentials = new ApiKeyServiceClientCredentials(SubscriptionKey);
            var client = new TextAnalyticsClient(credentials)
            {
                Endpoint = Endpoint
            };

            double sentimentResult = await SentimentAnalysisExample(client, documentText);
            IList<string> keyPhraseResult = await KeyPhraseExtractionExample(client, documentText);

            var returnCSValues = new CognitiveResult();

            returnCSValues.Sentiment = sentimentResult;

            if (returnCSValues.KeyPhrases == null)
            {
                //It's null - create it
                returnCSValues.KeyPhrases = new List<KeyPhrase>();
            }

            foreach (var phrase in keyPhraseResult)
            {

                returnCSValues.KeyPhrases.Add(new KeyPhrase { keyword = phrase });
                
            }

            // Not used:
            // DetectLanguageExample(client).Wait();
            // RecognizeEntitiesExample(client).Wait();


            //TODO: build a return variable that contains both the sentiment and key phrases!!

            return JsonConvert.SerializeObject(returnCSValues, Formatting.Indented);
        }

        private string callStoryAI(string keyword, double sentiment)
        {
            if (sentiment > 0.5)
                {
                return $"That sounds like a really cool {keyword}";
                }
            else
            {
                return $"I am sorry to hear about the {keyword}...";
            }
        }

        class Document
        {
            public string id { get; set; }
            public string language { get; set; }

            public string text { get; set; }

        }

        class CognitiveResult
        {
            [JsonProperty("keyphrases")]
            public List<KeyPhrase> KeyPhrases { get; set; }

            [JsonProperty("sentiment")]
            public double Sentiment { get; set; }

        }

        class KeyPhrase
        {
            public string keyword;
        }


        /// <summary>
        /// Allows authentication to the API by using a basic apiKey mechanism
        /// </summary>
        class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            private readonly string subscriptionKey;

            /// <summary>
            /// Creates a new instance of the ApiKeyServiceClientCredentails class
            /// </summary>
            /// <param name="subscriptionKey">The subscription key to authenticate and authorize as</param>
            public ApiKeyServiceClientCredentials(string subscriptionKey)
            {
                this.subscriptionKey = subscriptionKey;
            }

            /// <summary>
            /// Add the Basic Authentication Header to each outgoing request
            /// </summary>
            /// <param name="request">The outgoing request</param>
            /// <param name="cancellationToken">A token to cancel the operation</param>
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request == null)
                {
                    throw new ArgumentNullException("request");
                }

                request.Headers.Add("Ocp-Apim-Subscription-Key", this.subscriptionKey);
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }

        public static async Task<double> SentimentAnalysisExample(TextAnalyticsClient client, string inputText)
        {
            // The documents to be analyzed. Add the language of the document. The ID can be any value.
            var inputDocuments = new MultiLanguageBatchInput(
                new List<MultiLanguageInput>
                {
                    new MultiLanguageInput("en", "1", inputText)
                });
            //...

            var result = await client.SentimentAsync(false, inputDocuments);

            return Convert.ToDouble(result.Documents[0].Score);
        }

        public static async Task<IList<string>> KeyPhraseExtractionExample(TextAnalyticsClient client, string inputText)
        {
            var inputDocuments = new MultiLanguageBatchInput(
                        new List<MultiLanguageInput>
                        {
                            new MultiLanguageInput("en", "1", inputText),
                        });
            var kpResults = await client.KeyPhrasesAsync(false, inputDocuments);

            return kpResults.Documents[0].KeyPhrases;

        }

    }
}
