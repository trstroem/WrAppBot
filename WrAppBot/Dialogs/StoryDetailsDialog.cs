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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WrAppBot.Dialogs
{
    public class StoryDetailsDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<StoryDetails> _storyDetailsAccessor;
        static HttpClient client = new HttpClient();

        public StoryDetailsDialog()
            : base(nameof(StoryDetailsDialog))
        {

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            //AddDialog(new DateResolverDialog());

            // This is the "main" dialog for WrApp. It currently contains separate steps for
            // harvesting the story topic, mood, and then presents the user with the first storyline,
            // followed by a confirmation question and a final step. Quite useless, really.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                StoryLineAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> StoryLineAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the Mood from the value harvested in the previous step
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

            string resultFromStoryAI = "";

            /* Cognitive services call has been commented out for now due to subscription de-activated!!!
             
            // Variant 1: Call Azure Cognitive Services
            // var resultCS = new CognitiveResult();    never used
            var resultCSString = await CallCognitiveWebServiceAsync(storyDetails.StoryLine);

            var deserializedDocument = JsonSerializer.Deserialize<CognitiveResult>(resultCSString);

            //End code for call to cognitive service

            //Now, we need to disentangle the result, which looks like this:
            // {�documents�:[{�id�:�1�,�keyPhrases�:[�gnome�,�pink clothes�,�bitcoin wallet�]}],�errors�:[]}


            var sentiment = deserializedDocument.Sentiment;
            
            int numberOfCalls = 2;

            foreach (var keyphrase in deserializedDocument.KeyPhrases) 
                {
                    resultFromStoryAI += callStoryAI(keyphrase.Keyword.ToString(), sentiment);
                    numberOfCalls -= 1;
                    if (numberOfCalls == 0)
                    {
                        break;
                    }
            }

            resultFromStoryAI += "!";

            //            var firstKeyword = deserializedDocument.KeyPhrases[0].keyword.ToString();
            //            var resultFromStoryAI = callStoryAI(firstKeyword, sentiment);

            // End Variant 1: Call Azure CS

            */

            // Variant 2: Call to SAP HANA Text Analysis service:
            // Example of call:
            // https://trondsdbs0020313454trial.hanatrial.ondemand.com/public/wrapp/services/WrappTAService.xsjs?sampleText=storyDetails.StoryLine
            var resultTAString = await CallHANATAWebServiceAsync(storyDetails.StoryLine);
            var deserializedTA = JsonSerializer.Deserialize<HANATAResult>(resultTAString);

            // Blanking out the result from the call to Azure CS above, as we want to replace it with HANA intelligence!
            resultFromStoryAI = "";

            // Now do something with the result from the HANA service.
            resultFromStoryAI = callStoryAITA(deserializedTA.TAKeyPhrases);

            // End Variant 2

            var msg = $"{resultFromStoryAI}";

            return await stepContext.ReplaceDialogAsync(nameof(StoryDetailsDialog), storyDetails, cancellationToken);
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
                returnCSValues.KeyPhrases.Add(new KeyPhrase { Keyword = phrase });
            }

            // Not used:
            // DetectLanguageExample(client).Wait();
            // RecognizeEntitiesExample(client).Wait();

            return JsonSerializer.Serialize(returnCSValues);
        }

        private async Task<string> CallHANATAWebServiceAsync(string documentText)
        {
           string Endpoint = "https://trondsdbs0020313454trial.hanatrial.ondemand.com/public/wrapp/services/WrappTAService.xsjs";

            string path = Endpoint + "?sampleText='" + documentText + "'";
            string result = "";

            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadAsStringAsync();
            }

            //return JsonSerializer.Serialize(result);
            return result;
        }

        private string callStoryAI(string keyword, double sentiment)
        {

            Random rand = new Random();

            int phraseNumber = rand.Next(11);
            string phraseText;

            if (sentiment > 0.5)
            {
                switch (phraseNumber)
            	{
                    case 1:
                        phraseText = $" That sounds like a really cool {keyword}! What color is it?";
                        break;
                    case 2:
                        phraseText = $" I once had a {keyword}, too! But then, someone stole it";
                        break;
                    case 3:
                        phraseText = $" When I was younger, I wanted to be a {keyword}. They're so cool!";
                        break;
                    case 4:
                        phraseText = $" My uncle once trapped a {keyword} in his swimming pool. Can you believe it?";
                        break;
                    case 5:
                        phraseText = $" I like {keyword}'s. Especially the red ones.";
                        break;
                    case 6:
                        phraseText = $" Can you tell me more about the {keyword}?";
                        break;
                    case 7:
                        phraseText = $" Wow! I never met anyone who talked about {keyword}!";
                        break;
                    case 8:
                        phraseText = $" What did the {keyword} look like?";
                        break;
                    case 9:
                        phraseText = $" Was it a smelly {keyword}? I heard most of them are.";
                        break;
                    case 10:
                        phraseText = $" Can I play with your {keyword}? It's so boring to live inside a computer..";
                        break;

                    default:
                        phraseText = $" I really like what you tell me about the {keyword}.";
                        break;

	            }
            }
            else
            {
                switch (phraseNumber)
            	{
                    case 1:
                        phraseText = $" That sounds like a really horrible {keyword}...";
                        break;
                    case 2:
                        phraseText = $" That sounds like a sad story. Tell me more about the {keyword}!";
                        break;
                    case 3:
                        phraseText = $" What did the {keyword} look like? Was it scary?";
                        break;
                    case 4:
                        phraseText = $" I know what happened next. The {keyword} freaked out and scared your cat, right?";
                        break;
                    case 5:
                        phraseText = $" Hearing about {keyword} makes me sad. Can we talk about something else?";
                        break;
                    case 6:
                        phraseText = $" Are you really sure there was a {keyword}?";
                        break;
                    case 7:
                        phraseText = $" I can't believe it. A real {keyword}?";
                        break;
                    case 8:
                        phraseText = $" I don't really like the thing about the {keyword}...";
                        break;
                    case 9:
                        phraseText = $" I'm sure the {keyword} was big and ugly, right?";
                        break;
                    case 10:
                        phraseText = $" Once, an evil {keyword} stole my fake beard. You never knew chatbots had beards, did you?";
                        break;

                    default:
                        phraseText = $" I'm really sad to hear about the horrible {keyword}. Can you tell me more?";
                        break;
	            }
            }
            return phraseText;
        }

        private string callStoryAITA(List<TAKeyPhrase>keyPhrases)
        {
            string phraseText = "";
            Boolean usedNouns = false;
            Boolean usedAdjectives = false;
            Boolean usedProperNames = false;

            foreach (var takeyphrase in keyPhrases)
            {
                if (takeyphrase.Type.ToString() == "noun" && usedNouns == false)
                {
                    phraseText += "I would love to hear more about your " + takeyphrase.Value.ToString() + "! ";
                    usedNouns = true;
                }

                /*
                if (takeyphrase.Type.ToString() == "verb")
                {
                    phraseText += "Did you really " + takeyphrase.Value.ToString() + "? What happened next? ";
                }
                */

                if (takeyphrase.Type.ToString() == "proper name" && usedProperNames == false)
                {
                    phraseText += takeyphrase.Value.ToString() + "! That's a cool name! ";
                    usedProperNames = true;
                }

                if (takeyphrase.Type.ToString() == "adjective" && usedAdjectives == false)
                {
                    phraseText += "Wow! You really sure it was " + takeyphrase.Value.ToString() + "? ";
                    usedAdjectives = true;
                }

            }
            return phraseText;
        }

        class Document
        {
            public string Id { get; set; }
            public string Language { get; set; }

            public string Text { get; set; }
        }

        class CognitiveResult
        {
            public List<KeyPhrase> KeyPhrases { get; set; }
 
            public double Sentiment { get; set; }
        }

        class HANATAResult
        {
            public List<TAKeyPhrase> TAKeyPhrases { get; set; }
        }

        class KeyPhrase
        {
            [JsonPropertyName("keyword")]
            public string Keyword { get; set; }
        }

        class TAKeyPhrase
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("value")]
            public string Value { get; set; }
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
