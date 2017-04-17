using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace ImageStorageLibrary
{
    public class DocumentDBHelper
    {
        public static string EndpointUri { get; set; }
        public static string AccessKey { get; set; }
        public static string DatabaseName { get; set; }
        public static string CollectionName { get; set; }

        public static async Task<DocumentDBHelper> BuildAsync()
        {
            if (string.IsNullOrWhiteSpace(EndpointUri))
                throw new ArgumentNullException("EndpointUri");
            if (string.IsNullOrWhiteSpace(AccessKey))
                throw new ArgumentNullException("AccessKey");
            if (string.IsNullOrWhiteSpace(DatabaseName))
                throw new ArgumentNullException("DatabaseName");
            if (string.IsNullOrWhiteSpace(CollectionName))
                throw new ArgumentNullException("CollectionName");

            var client = new DocumentClient(new Uri(EndpointUri), AccessKey);
            var db = (await client.CreateDatabaseIfNotExistsAsync(new Database() {Id = DatabaseName})).Resource;
            var coll = (await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(db.Id),
                new DocumentCollection() {Id = CollectionName})).Resource;

            return new DocumentDBHelper() {Client = client, Database = db, Collection = coll};
        }

        private DocumentDBHelper()
        {
            
        }

        private DocumentClient Client { get; set; }
        private Database Database { get; set; }
        private DocumentCollection Collection { get; set; }

        public async Task<T> CreateDocumentIfNotExistsAsync<T>(T document, string id)
            where T : new()
        {
            try
            {
                return
                (await this.Client.ReadDocumentAsync<T>(DocumentUri(id))).Document;
            }
            catch (DocumentClientException)
            {
                await this.Client.CreateDocumentAsync(CollectionUri(), document);
                return document;
            }
        }

        public IQueryable<T> FindAllDocuments<T>()
            where T : new()
        {
            var queryOptions = new FeedOptions() {MaxItemCount = -1};
            return this.Client.CreateDocumentQuery<T>(
                CollectionUri(), queryOptions);
        }

        public IQueryable<T> FindMatchingDocuments<T>(string query)
            where T : new()
        {
            var queryOptions = new FeedOptions() { MaxItemCount = -1 };
            return this.Client.CreateDocumentQuery<T>(
                CollectionUri(), query, queryOptions);
        }

        public async Task<T> FindDocumentById<T>(string id)
            where T : new()
        {
            return (await this.Client.ReadDocumentAsync<T>(DocumentUri(id))).Document;
        }

        private Uri CollectionUri()
        {
            return UriFactory.CreateDocumentCollectionUri(this.Database.Id, this.Collection.Id);
        }

        private Uri DocumentUri(string documentId)
        {
            return UriFactory.CreateDocumentUri(this.Database.Id, this.Collection.Id, documentId);
        }
    }
}
