// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.3.0

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace WrAppBot.Dialogs
{
    public class StoryDialog : CancelAndHelpDialog
    {
        public StoryDialog()
            : base(nameof(StoryDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                TopicStepAsync,
                MoodStepAsync,
                ConfirmStepAsync,
               FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> TopicStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var storyDetails = (StoryDetails)stepContext.Options;

            if (storyDetails.Topic == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What should your story be about?") }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(storyDetails.Topic, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> MoodStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var storyDetails = (StoryDetails)stepContext.Options;

            if (storyDetails.Mood == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What is the mood of your story?") }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(storyDetails.Mood, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var storyDetails = (StoryDetails)stepContext.Options;

            storyDetails.Mood = (string)stepContext.Result;

            var msg = $"Let us write a {storyDetails.Mood} story about {storyDetails.Topic}!";

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var storyDetails = (StoryDetails)stepContext.Options;

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
    }
}
