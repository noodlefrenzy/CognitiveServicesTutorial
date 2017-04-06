using ImageProcessingLibrary;
using Newtonsoft.Json;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public List<ImageInsightsViewModel> AllResults { get; set; } = new List<ImageInsightsViewModel>();
        public ObservableCollection<ImageInsightsViewModel> FilteredResults { get; set; } = new ObservableCollection<ImageInsightsViewModel>();
        public ObservableCollection<TagFilterViewModel> TagFilters { get; set; } = new ObservableCollection<TagFilterViewModel>();
        public ObservableCollection<FaceFilterViewModel> FaceFilters { get; set; } = new ObservableCollection<FaceFilterViewModel>();
        public ObservableCollection<EmotionFilterViewModel> EmotionFilters { get; set; } = new ObservableCollection<EmotionFilterViewModel>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // callbacks for core library
            FaceServiceHelper.Throttled = () => Util.ShowToastNotification("The Face API is throttling your requests. Consider upgrading to a Premium Key.");
            EmotionServiceHelper.Throttled = () => Util.ShowToastNotification("The Emotion API is throttling your requests. Consider upgrading to a Premium Key.");
            VisionServiceHelper.Throttled = () => Util.ShowToastNotification("The Vision API is throttling your requests. Consider upgrading to a Premium Key.");
            ErrorTrackingHelper.TrackException = (exception, message) => { Debug.WriteLine("ImageProcessingLibrary exception: {0}", message); }; 
            ErrorTrackingHelper.GenericApiCallExceptionHandler = Util.GenericApiCallExceptionHandler;

            // Enter API Keys here
            FaceServiceHelper.ApiKey = "";
            EmotionServiceHelper.ApiKey = "";
            VisionServiceHelper.ApiKey = "";

            base.OnNavigatedTo(e);
        }

        private async void ProcessImagesClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                folderPicker.FileTypeFilter.Add(".jpeg");
                folderPicker.FileTypeFilter.Add(".jpg");
                folderPicker.FileTypeFilter.Add(".png");
                folderPicker.FileTypeFilter.Add(".gif");
                folderPicker.FileTypeFilter.Add(".bmp");
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    await ProcessImagesAsync(folder);
                }
            }
            catch (Exception ex)
            {
                await ErrorTrackingHelper.GenericApiCallExceptionHandler(ex, "Error picking the target folder.");
            }
        }

        private async Task ProcessImagesAsync(StorageFolder rootFolder)
        {
            this.progressRing.IsActive = true;

            this.AllResults.Clear();
            this.FilteredResults.Clear();
            this.TagFilters.Clear();
            this.EmotionFilters.Clear();
            this.FaceFilters.Clear();

            List<ImageInsights> insightsList = new List<ImageInsights>();

            // see if we have pre-computed results and if so load it from the json file
            try
            {
                StorageFile insightsResultFile = (await rootFolder.TryGetItemAsync("ImageInsights.json")) as StorageFile;
                if (insightsResultFile != null)
                {
                    using (StreamReader reader = new StreamReader(await insightsResultFile.OpenStreamForReadAsync()))
                    {
                        insightsList = JsonConvert.DeserializeObject<List<ImageInsights>>(await reader.ReadToEndAsync());
                        foreach (var insights in insightsList)
                        {
                            await AddImageInsightsToViewModel(rootFolder, insights);
                        }
                    }
                }
            }
            catch
            {
                // We will just compute everything again in case of errors
            }

            bool foundErrors = false;

            if (!insightsList.Any())
            {
                // start with fresh face lists
                await FaceListManager.ResetFaceLists();

                // enumerate through the first 50 files and extract the insights 
                foreach (var item in (await rootFolder.GetFilesAsync()).Take(50))
                {
                    try
                    {
                        ImageInsights insights = await ImageProcessor.ProcessImageAsync(item.OpenStreamForReadAsync, item.Name);
                        insightsList.Add(insights);
                        await AddImageInsightsToViewModel(rootFolder, insights);
                    }
                    catch (Exception)
                    {
                        // Ignore the error with this image but continue so we try process as many as we can.
                        foundErrors = true;
                    }
                }

                // save to json
                StorageFile jsonFile = await rootFolder.CreateFileAsync("ImageInsights.json", CreationCollisionOption.ReplaceExisting);
                using (StreamWriter writer = new StreamWriter(await jsonFile.OpenStreamForWriteAsync()))
                {
                    string jsonStr = JsonConvert.SerializeObject(insightsList, Formatting.Indented);
                    await writer.WriteAsync(jsonStr);
                }
            }

            var sortedTags = this.TagFilters.OrderBy(t => t.Tag).ToArray();
            this.TagFilters.Clear();
            this.TagFilters.AddRange(sortedTags);

            var sortedEmotions = this.EmotionFilters.OrderBy(t => t.Emotion).ToArray();
            this.EmotionFilters.Clear();
            this.EmotionFilters.AddRange(sortedEmotions);

            this.progressRing.IsActive = false;

            if (foundErrors)
            {
                await new MessageDialog("Error processing some of the photos. We skipped the errors and did what we could.").ShowAsync();
            }
        }

        private async Task AddImageInsightsToViewModel(StorageFolder rootFolder, ImageInsights insights)
        {
            ImageInsightsViewModel insightsViewModel = new ImageInsightsViewModel(insights, await (await rootFolder.GetFileAsync(insights.ImageId)).OpenStreamForReadAsync());

            this.AllResults.Add(insightsViewModel);
            this.FilteredResults.Add(insightsViewModel);

            foreach (var tag in insights.VisionInsights.Tags)
            {
                if (!this.TagFilters.Any(t => t.Tag == tag))
                {
                    this.TagFilters.Add(new TagFilterViewModel(tag));
                }
            }

            foreach (var faceInsights in insights.FaceInsights)
            {
                if (!this.FaceFilters.Any(f => f.FaceId == faceInsights.UniqueFaceId))
                {
                    var imageStream = (await (await rootFolder.GetFileAsync(insights.ImageId)).OpenStreamForReadAsync()).AsRandomAccessStream();
                    ImageSource croppedFaced = await Util.GetCroppedBitmapAsync(imageStream, faceInsights.FaceRectangle);
                    this.FaceFilters.Add(new FaceFilterViewModel(faceInsights.UniqueFaceId, croppedFaced));
                }

                if (!this.EmotionFilters.Any(f => f.Emotion == faceInsights.TopEmotion))
                {
                    this.EmotionFilters.Add(new EmotionFilterViewModel(faceInsights.TopEmotion));
                }
            }
        }

        private void ApplyFilters()
        {
            this.FilteredResults.Clear();

            var checkedTags = this.TagFilters.Where(t => t.IsChecked);
            var checkedFaces = this.FaceFilters.Where(f => f.IsChecked);
            var checkedEmotions = this.EmotionFilters.Where(e => e.IsChecked);
            if (checkedTags.Any() || checkedFaces.Any() || checkedEmotions.Any())
            {
                var fromTags = this.AllResults.Where(r => HasTag(checkedTags, r.Insights.VisionInsights.Tags));
                var fromFaces = this.AllResults.Where(r => HasFace(checkedFaces, r.Insights.FaceInsights));
                var fromEmotion = this.AllResults.Where(r => HasEmotion(checkedEmotions, r.Insights.FaceInsights));

                this.FilteredResults.AddRange((fromTags.Concat(fromFaces).Concat(fromEmotion)).Distinct());
            }
            else
            {
                this.FilteredResults.AddRange(this.AllResults);
            }
        }

        private bool HasFace(IEnumerable<FaceFilterViewModel> checkedFaces, FaceInsights[] faceInsights)
        {
            foreach (var item in checkedFaces)
            {
                if (faceInsights.Any(f => f.UniqueFaceId == item.FaceId))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasEmotion(IEnumerable<EmotionFilterViewModel> checkedEmotions, FaceInsights[] faceInsights)
        {
            foreach (var item in checkedEmotions)
            {
                if (faceInsights.Any(f => f.TopEmotion == item.Emotion))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasTag(IEnumerable<TagFilterViewModel> checkedTags, string[] tags)
        {
            foreach (var item in checkedTags)
            {
                if (tags.Any(t => t == item.Tag))
                {
                    return true;
                }
            }

            return false;
        }

        private void TagFilterChanged(object sender, RoutedEventArgs e)
        {
            this.ApplyFilters();
        }

        private void FaceFilterChanged(object sender, RoutedEventArgs e)
        {
            this.ApplyFilters();
        }

        private void EmotionFilterChanged(object sender, RoutedEventArgs e)
        {
            this.ApplyFilters();
        }
    }

    public static class Extensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }

    }
}
