using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

namespace PictureBot.Dialogs
{
    [LuisModel("TODO-modelID", "TODO-luisKey")]
    [Serializable]
    public class RootDialog : DispatchDialog
    {
        //private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        //{
        //    var activity = await result as Activity;

        //    // calculate something for us to return
        //    int length = (activity.Text ?? string.Empty).Length;

        //    // return our reply to the user
        //    await context.PostAsync($"You sent {activity.Text} which was {length} characters");

        //    context.Wait(MessageReceivedAsync);
        //}

        [RegexPattern("^hello")]
        [RegexPattern("^hi")]
        [ScorableGroup(0)]
        public async Task Hello(IDialogContext context, IActivity activity)
        {
            await context.PostAsync("Hello from RegEx!  I am a Photo Organization Bot.  You can ask me things like 'find pictures of food'.  I can also share your photos on Twitter.");
        }

        [RegexPattern("^help")]
        [ScorableGroup(0)]
        public async Task Help(IDialogContext context, IActivity activity)
        {
            // TODO: launch help dialog with button menu.  
            await context.PostAsync("TODO: help info");
        }

        [LuisIntent("None")]
        [ScorableGroup(1)]
        public async Task None(IDialogContext context, LuisResult result)
        {
            // Luis returned with "None" as the winning intent,
            // so drop down to next level of ScorableGroups.  
            ContinueWithNextGroup();
        }

        [LuisIntent("SearchPics")]
        [ScorableGroup(1)]
        public async Task SearchPics(IDialogContext context, LuisResult result)
        {
            // TODO: change LUIS to only have facet entity?  
            // TODO: LUIS deserialization errors with entities - fixed?  
            string gender = null;
            string age = null;
            string emotion = null;
            string facet = null;

            EntityRecommendation rec;
            if (result.TryFindEntity("gender", out rec)) gender = rec.Entity;
            if (result.TryFindEntity("age", out rec)) age = rec.Entity;
            if (result.TryFindEntity("emotion", out rec)) emotion = rec.Entity;
            if (result.TryFindEntity("facet", out rec)) facet = rec.Entity;

            await context.PostAsync($"Searching pictures...");
            context.Call(new SearchDialog(facet), ResumeAfterSearchDialog);
        }

        private async Task ResumeAfterSearchDialog(IDialogContext context, IAwaitable<object> result)
        {
            // TODO: remove if this isn't used
            await context.PostAsync("Done searching pictures");
        }

        [LuisIntent("SharePic")]
        [ScorableGroup(1)]
        public async Task SharePic(IDialogContext context, LuisResult result)
        {
            PromptDialog.Confirm(context, AfterShareAsync,
                "Are you sure you want to tweet this picture?");            
        }

        private async Task AfterShareAsync(IDialogContext context, IAwaitable<bool> result)
        {
            if (result.GetAwaiter().GetResult() == true)
            {
                // Yes, share the picture.
                // TODO: change this code to post a tweet
                await context.PostAsync("Posting tweet...");
            }
            else
            {
                // No, don't share the picture.  
                await context.PostAsync("OK, I won't share it.");
            }
        }

        [LuisIntent("Greeting")]
        [ScorableGroup(1)]
        public async Task Greeting(IDialogContext context, LuisResult result)
        {
            // TODO: remove this?
            // Duplicate logic, for a teachable moment.  Try commenting out the "hello" RegEx above!  
            await context.PostAsync("Hello from LUIS!  I am a Photo Organization Bot.  You can ask me things like 'find pictures of food'.  I can also share your photos on Twitter.");
        }

        // Since none of the scorables in previous group won, the dialog sends a help message.
        [MethodBind]
        [ScorableGroup(2)]
        public async Task Default(IDialogContext context, IActivity activity)
        {
            await context.PostAsync("I'm sorry. I didn't understand you.");
            await context.PostAsync("You can tell me to find photos and tweet them.  Here is an example: \"find pictures of food\"");
        }

    }
}