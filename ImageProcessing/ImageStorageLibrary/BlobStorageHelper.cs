using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageStorageLibrary
{
    public class BlobStorageHelper
    {
        public const string DefaultImageContainer = "images";

        static BlobStorageHelper()
        {
            InitializeBlobStorage();
        }

        private static CloudStorageAccount storageAccount { get; set; }
        private static CloudBlobClient blobClient { get; set; }
        private static CloudBlobContainer container { get; set; }

        private static string connectionString;
        public static string ConnectionString
        {
            get
            {
                return connectionString;
            }

            set
            {
                var changed = connectionString != value;
                connectionString = value;
                if (changed)
                {
                    InitializeBlobStorage();
                }
            }
        }

        private static string containerName = DefaultImageContainer;
        private static bool ensureCreated = false;
        public static string ContainerName
        {
            get
            {
                return containerName;
            }
            set
            {
                var changed = containerName != value;
                containerName = value;
                if (changed)
                {
                    InitializeBlobStorage();
                }
            }
        }

        private static void InitializeBlobStorage()
        {
            if (connectionString != null && containerName != null)
            {
                Debug.WriteLine($"Initializing storage account for container {containerName}");
                try
                {
                    storageAccount = CloudStorageAccount.Parse(connectionString);
                    blobClient = storageAccount.CreateCloudBlobClient();
                    ensureCreated = false;
                    container = blobClient.GetContainerReference(containerName);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to initialize storage: {e}");
                }
            }
            else
            {
                Debug.WriteLine("No connection string or container name set, cannot initialize.");
            }
        }

        public static async Task<CloudBlockBlob> UploadImageAsync(Func<Task<Stream>> imageStreamCallback, string imageId)
        {
            if (!ensureCreated)
            {
                await container.CreateIfNotExistsAsync();
                ensureCreated = true;
            }

            CloudBlockBlob blob = container.GetBlockBlobReference(imageId);
            await blob.UploadFromStreamAsync(await imageStreamCallback());
            return blob;
        }

        public static async Task<IList<Uri>> GetImageURIsAsync()
        {
            if (!ensureCreated)
            {
                await container.CreateIfNotExistsAsync();
                ensureCreated = true;
            }

            return await GetImageURIsAsync("");
        }

        private static async Task<IList<Uri>> GetImageURIsAsync(string prefix)
        { 
            BlobContinuationToken tok = null;
            List<Uri> blobUris = new List<Uri>();
            do
            {
                BlobResultSegment curResult = await container.ListBlobsSegmentedAsync(tok);
                foreach (var result in curResult.Results)
                {
                    if (result is CloudBlobDirectory)
                    {
                        blobUris.AddRange(await GetImageURIsAsync(((CloudBlobDirectory)result).Prefix));
                    } else
                    {
                        blobUris.Add(((CloudBlob)result).Uri);
                    }
                }
                tok = curResult.ContinuationToken;
            } while (tok != null);
            return blobUris;
        }
    }
}
