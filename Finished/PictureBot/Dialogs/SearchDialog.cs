using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using PictureBot.Models;

namespace PictureBot.Dialogs
{
    [Serializable]
    public class SearchDialog : IDialog<object>
    {
        private string searchText = "";

        public SearchDialog(string facet)
        {
            // TODO: update to handle all the things
            searchText = facet;
        }

        public async Task StartAsync(IDialogContext context)
        {
            ISearchIndexClient indexClientForQueries = CreateSearchIndexClient();

            // For more examples of calling search with SearchParameters, see
            // https://github.com/Azure-Samples/search-dotnet-getting-started/blob/master/DotNetHowTo/DotNetHowTo/Program.cs.  

            DocumentSearchResult results = await indexClientForQueries.Documents.SearchAsync(searchText);
            SendResults(context, results); 
        }

        private async void SendResults(IDialogContext context, DocumentSearchResult results)
        {
            var message = context.MakeMessage();

            if (results.Results.Count == 0)
            {
                await context.PostAsync("There were no results found.");
                // TODO: wait here?  or end dialog?  
            }

            SearchHitStyler searchHitStyler = new SearchHitStyler();
            searchHitStyler.Apply(
                ref message,
                "Here are a few good options I found:",
                results.Results.Select(r => ImageMapper.ToSearchHit(r)).ToList().AsReadOnly());

            await context.PostAsync(message);
            /*await context.PostAsync(
                this.MultipleSelection ?
                "You can select one or more to add to your list, *list* what you've selected so far, *refine* these results, see *more* or search *again*." :
                "You can select one, *refine* these results, see *more* or search *again*.");
                */
            context.Wait(this.ActOnSearchResults);
            // TODO: "Stack is empty" error here
        }

        private async Task ActOnSearchResults(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            // TODO
 
            var activity = await result;
            var choice = activity.Text;

            //switch (choice.ToLowerInvariant())
            //{
            //    case "again":
            //    case "reset":
            //        this.QueryBuilder.Reset();
            //        await this.InitialPrompt(context);
            //        break;

            //    case "more":
            //        this.QueryBuilder.PageNumber++;
            //        await this.Search(context, null);
            //        break;

            //    case "refine":
            //        this.SelectRefiner(context);
            //        break;

            //    case "list":
            //        await this.ListAddedSoFar(context);
            //        context.Wait(this.ActOnSearchResults);
            //        break;

            //    case "done":
            //        context.Done(this.selected);
            //        break;

            //    default:
            //        await this.AddSelectedItem(context, choice);
            //        break;
            //}
        }

        private ISearchIndexClient CreateSearchIndexClient()
        {
            string searchServiceName = ConfigurationManager.AppSettings["SearchDialogsServiceName"];
            string queryApiKey = ConfigurationManager.AppSettings["SearchDialogsServiceKey"];
            string indexName = ConfigurationManager.AppSettings["SearchDialogsIndexName"];
            
            SearchIndexClient indexClient = new SearchIndexClient(searchServiceName, indexName, new SearchCredentials(queryApiKey));
            return indexClient;
        }

        [Serializable]
        public class SearchHitStyler : PromptStyler
        {
            public override void Apply<T>(ref IMessageActivity message, string prompt, IReadOnlyList<T> options, IReadOnlyList<string> descriptions = null)
            {
                var hits = options as IList<SearchHit>;
                if (hits != null)
                {
                    var cards = hits.Select(h => new HeroCard
                    {
                        Title = h.Title,
                        Images = new[] { new CardImage(h.PictureUrl) },
                        Buttons = new[] { new CardAction(ActionTypes.ImBack, "Pick this one", value: h.Key) },
                        Text = h.Description
                    });

                    message.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    message.Attachments = cards.Select(c => c.ToAttachment()).ToList();
                    message.Text = prompt;
                }
                else
                {
                    base.Apply<T>(ref message, prompt, options, descriptions);
                }
            }
        }

    }
}