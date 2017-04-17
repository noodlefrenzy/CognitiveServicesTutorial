using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ImageProcessingLibrary;
using ImageStorageLibrary;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using ServiceHelpers;

namespace TestCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);
            var dirs = app.Option("-process", "The directory to process", CommandOptionType.MultipleValue);
            var q = app.Option("-query", "The query to run", CommandOptionType.SingleValue);
            app.HelpOption("-? | -h | --help");
            app.OnExecute(() =>
            {
                if (dirs != null && dirs.Values.Any())
                {
                    InitializeAsync().Wait();
                    foreach (var dir in dirs.Values)
                    {
                        ProcessDirectoryAsync(dir).Wait();
                    }
                    return 0;
                }
                else if (q != null)
                {
                    InitializeAsync().Wait();
                    RunQuery(q.Value());
                    return 0;
                }
                else 
                {
                    app.ShowHelp("Must provide a directory to process");
                    return -1;
                }
            });

            app.Execute(args);
        }

        private static DocumentDBHelper documentDb;
        private static async Task InitializeAsync()
        {
            using (var settingsReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("TestCLI.settings.json")))
            using (var textReader = new JsonTextReader(settingsReader))
            {
                dynamic settings = new JsonSerializer().Deserialize(textReader);

                FaceServiceHelper.ApiKey = settings.CognitiveServicesKeys.Face;
                EmotionServiceHelper.ApiKey = settings.CognitiveServicesKeys.Emotion;
                VisionServiceHelper.ApiKey = settings.CognitiveServicesKeys.Vision;

                BlobStorageHelper.ConnectionString = settings.AzureStorage.ConnectionString;
                DocumentDBHelper.AccessKey = settings.DocumentDB.Key;
                DocumentDBHelper.EndpointUri = settings.DocumentDB.EndpointURI;
                DocumentDBHelper.DatabaseName = "images";
                DocumentDBHelper.CollectionName = "metadata";
                documentDb = await DocumentDBHelper.BuildAsync();
            }
        }

        private static async Task ProcessDirectoryAsync(string dir)
        {
            Console.WriteLine($"Processing Directory {dir}");
            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png",
                ".jpg",
                ".bmp",
                ".jpeg",
                ".gif"
            };
            foreach (var file in 
                from file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                where imageExtensions.Contains(Path.GetExtension(file))
                select file)
            {
                try
                {
                    Console.WriteLine($"Processing {file}");
                    var fileName = Path.GetFileName(file);
                    // Resize (if needed) in order to reduce network latency and errors due to large files. Then store the result in a temporary file.
                    var resized = Util.ResizeIfRequired(file, 750);
                    Func<Task<Stream>> imageCB = async () => File.OpenRead(resized.Item2);
                    ImageInsights insights = await ImageProcessor.ProcessImageAsync(imageCB, fileName);
                    Util.AdjustFaceInsightsBasedOnResizing(insights, resized.Item1);
                    Console.WriteLine($"Insights: {insights}");
                    var imageBlob = await BlobStorageHelper.UploadImageAsync(imageCB, fileName);
                    var metadata = new ImageMetadata(file);
                    metadata.AddInsights(insights);
                    metadata.BlobUri = imageBlob.Uri;
                    metadata = await documentDb.CreateDocumentIfNotExistsAsync(metadata, metadata.Id);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e}");
                }
            }
        }

        private static void RunQuery(string query)
        {
            foreach (var doc in documentDb.FindMatchingDocuments<ImageMetadata>(query))
            {
                Console.WriteLine(doc);
                Console.WriteLine();
            }
        }
    }
}
