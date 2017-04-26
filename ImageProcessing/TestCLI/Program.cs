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
            var forceUpdate = app.Option("-force", "Use to force update even if file has already been added.",
                CommandOptionType.NoValue);
            var settingsFile = app.Option("-settings",
                "The settings file (optional, will use embedded resource settings.json if not set)",
                CommandOptionType.SingleValue);
            var dirs = app.Option("-process", "The directory to process", CommandOptionType.MultipleValue);
            var q = app.Option("-query", "The query to run", CommandOptionType.SingleValue);
            app.HelpOption("-? | -h | --help");
            app.OnExecute(() =>
            {
                string settings = settingsFile.HasValue() ? settingsFile.Value() : null;
                if (dirs != null && dirs.Values.Any())
                {
                    InitializeAsync(settings).Wait();
                    foreach (var dir in dirs.Values)
                    {
                        ProcessDirectoryAsync(dir, forceUpdate.HasValue()).Wait();
                    }
                    return 0;
                }
                else if (q != null && q.HasValue())
                {
                    InitializeAsync(settings).Wait();
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

        private static BlobStorageHelper blobStorage;
        private static DocumentDBHelper documentDb;

        private static async Task InitializeAsync(string settingsFile = null)
        {
            using (Stream settingsStream = settingsFile == null
                ? Assembly.GetExecutingAssembly().GetManifestResourceStream("TestCLI.settings.json")
                : new FileStream(settingsFile, FileMode.Open, FileAccess.Read))
            using (var settingsReader = new StreamReader(settingsStream))
            using (var textReader = new JsonTextReader(settingsReader))
            {
                dynamic settings = new JsonSerializer().Deserialize(textReader);

                FaceServiceHelper.ApiKey = settings.CognitiveServicesKeys.Face;
                EmotionServiceHelper.ApiKey = settings.CognitiveServicesKeys.Emotion;
                VisionServiceHelper.ApiKey = settings.CognitiveServicesKeys.Vision;

                BlobStorageHelper.ConnectionString = settings.AzureStorage.ConnectionString;
                BlobStorageHelper.ContainerName = settings.AzureStorage.BlobContainer;
                blobStorage = await BlobStorageHelper.BuildAsync();

                DocumentDBHelper.AccessKey = settings.DocumentDB.Key;
                DocumentDBHelper.EndpointUri = settings.DocumentDB.EndpointURI;
                DocumentDBHelper.DatabaseName = settings.DocumentDB.DatabaseName;
                DocumentDBHelper.CollectionName = settings.DocumentDB.CollectionName;
                documentDb = await DocumentDBHelper.BuildAsync();
            }
        }

        private static async Task ProcessDirectoryAsync(string dir, bool forceUpdate = false)
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
                    var fileName = Path.GetFileName(file);
                    var existing = await documentDb.FindDocumentByIdAsync<ImageMetadata>(fileName);
                    if (existing == null || forceUpdate)
                    {
                        Console.WriteLine($"Processing {file}");
                        // Resize (if needed) in order to reduce network latency and errors due to large files. Then store the result in a temporary file.
                        var resized = Util.ResizeIfRequired(file, 750);
                        Func<Task<Stream>> imageCB = async () => File.OpenRead(resized.Item2);
                        ImageInsights insights = await ImageProcessor.ProcessImageAsync(imageCB, fileName);
                        Util.AdjustFaceInsightsBasedOnResizing(insights, resized.Item1);
                        Console.WriteLine($"Insights: {JsonConvert.SerializeObject(insights, Formatting.None)}");
                        var imageBlob = await blobStorage.UploadImageAsync(imageCB, fileName);
                        var metadata = new ImageMetadata(file);
                        metadata.AddInsights(insights);
                        metadata.BlobUri = imageBlob.Uri;
                        if (existing == null)
                            metadata = (await documentDb.CreateDocumentIfNotExistsAsync(metadata, metadata.Id)).Item2;
                        else
                            metadata = await documentDb.UpdateDocumentAsync(metadata, metadata.Id);
                    }
                    else
                    {
                        Console.WriteLine($"Skipping {file}, exists and 'forceUpdate' not set.");
                    }
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
