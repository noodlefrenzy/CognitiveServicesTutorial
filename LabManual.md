# Lab Manual #

## Setting up your Azure account ##

You may activate an Azure free trial at https://azure.microsoft.com/en-us/free/.  

If you have been given an Azure Pass to complete this lab, you may go to http://www.microsoftazurepass.com/ to activate it.  Please follow the instructions at https://www.microsoftazurepass.com/howto, which document the activation process.  A Microsoft account may have one free trial on Azure and one Azure Pass associated with it, so if you have already activated an Azure Pass on your Microsoft account, you will need to use the free trial or use another Microsoft account.  

## Navigating the Azure portal ##

After creating an Azure account, you may access the Azure portal at https://portal.azure.com.  

## Navigating the Solution ##

We have created a Solution (.sln) which contains several different projects, let's take a high-level look at them:

- **ImageProcessingLibrary**: This is a Portable Class Library (PCL) containing helper classes for accessing the various Cognitive Services related to Vision, and some "Insights" classes for encapsulating the results.
- **ImageStorageLibrary**: Since DocumentDB does not (yet) support UWP, this is a non-portable library for accessing Blob Storage and DocumentDB.
- **TestApp**: A UWP application that allows you to load your images and call the various cognitive services on them, then explore the results. Useful for experimentation and exploration of your images.
- **TestCLI**: A Console application allowing you to call the various cognitive services and then upload the images and data to Azure. Images are uploaded to Blob Storage, and the various metadata (tags, captions, faces) are uploaded to DocumentDB.

Both _TestApp_ and _TestCLI_ contain a `settings.json` file containing the various keys and endpoints needed for accessing the Cognitive Services and Azure. They start blank, so once you get your Azure Pass up and running we can provision your service keys and set up your storage account and DocumentDB instance.

## Getting Cognitive Services API Keys ##

TBD

## Settings up Storage ##

We'll be using two different stores in Azure for this project - one for storing the raw images, and the other for storing the results of our Cognitive Service calls. Azure Blob Storage is made for storing large amounts of data in a format that looks similar to a file-system, and is a great choice for storing data like images. Azure DocumentDB is our resilient NoSQL PaaS solution, and is incredibly useful for storing loosely structured data like we have with our image metadata results. There are other possible choices (Azure Table Storage, SQL Server), but DocumentDB gives us the flexibility to evolve our schema freely (like adding data for new services), query it easily, and can be quickly integrated into Azure Search.

### Azure Blob Storage ###

Detailed "Getting Started" instructions can be [found online](https://docs.microsoft.com/en-us/azure/storage/storage-dotnet-how-to-use-blobs), but let's just go over what you need for this lab.

Within the Azure Portal, click **New->Storage->Storage Account**

![New Azure Storage](./images/create-blob-storage.png)

Once you click it, you'll be presenting with the fields above to fill out. Choose your storage account name (lowercase letters and numbers), set _Account kind_ to _Blob storage_, _Replication_ to _Locally-Redundant storage (LRS)_ (this is just to save money), and create a new _Resource Group_. We'll be using this same resource group below for our DocumentDB, so pick something you like. _Pin to dashboard_ so that you can easily find it.

Now that you have an Azure Storage account, let's grab the _Connection String_ and add it to your _TestCLI_ `settings.json`.

![Azure Blob Keys](./images/blob-storage-keys.png)

### DocumentDB ###

Detailed "Getting Started" instructions can be [found online](https://docs.microsoft.com/en-us/azure/documentdb/documentdb-get-started), but we'll walk through what you need for this project here.

Within the Azure Portal, click **New->Databases->NoSQL (DocumentDB)**.

![New DocumentDB](./images/create-docdb-portal.png)

Once you click this, you'll have to fill out a few fields as you see fit. 

![DocumentDB Creation Form](./images/create-docdb-formfill.png)

In our case, select the ID you'd like, subject to the constraints that it needs to be lowercase letters, numbers, or dashes. We will be using the DocumentDB SDK and not Mongo, so select DocumentDB as the NoSQL API. Let's use the same Resource Group as we used for our previous steps, and the same location, select _Pin to dashboard_ to make sure we keep track of it and it's easy to get back to, and hit Create.

Once creation is complete, open the panel for your new database and select the _Keys_ sub-panel.

![Keys sub-panel for DocumentDB](./images/docdb-keys.png)

You'll need the **URI** and the **PRIMARY KEY** for your _TestCLI's_ `settings.json` file, so copy those into there and you're now ready to store images and data into the cloud.

## Loading Images Using TestCLI ##

Once you've set your Cognitive Services API keys, your Azure Blob Storage Connection String, and your DocumentDB Endpoint URI and Key in your _TestCLI's_ `settings.json`, you're ready to go. Build _TestCLI_, and run it:

    > .\bin\Debug\TestCLI.exe

    Usage:  [options]

    Options:
    -force            Use to force update even if file has already been added.
    -settings         The settings file (optional, will use embedded resource settings.json if not set)
    -process          The directory to process
    -query            The query to run
    -? | -h | --help  Show help information

By default, it will load your settings from `settings.json` (it builds it into the `.exe`), but you can provide your own using the `-settings` flag. To load images (and their metadata from Cognitive Services) into your cloud storage, you can just tell _TestCLI_ to `-process` your image directory as follows:

    > .\bin\Debug\TestCLI.exe -process c:\my\image\directory

Once it's done processing, you can query against your DocumentDB directly using _TestCLI_ as follows:

    > .\bin\Debug\TestCLI.exe -query "select * from images"

## Exploring DocumentDB ##

## Building an Azure Search Index ## 

