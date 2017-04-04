using ImageProcessingLibrary;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
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

        public IEnumerable<string> Tags { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void ProcessImagesClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                folderPicker.FileTypeFilter.Add(".jpeg");
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
            this.AllResults.Clear();
            this.FilteredResults.Clear();

            foreach (var item in await rootFolder.GetFilesAsync())
            {
                ImageInsights insights = await ImageProcessor.ProcessImageAsync(item.OpenStreamForReadAsync, item.Path);

                this.AllResults.Add(new ImageInsightsViewModel(insights, await item.OpenStreamForReadAsync()));
            }

            this.ApplyFilters();

            this.TagFilters.Clear();
            foreach (var tag in this.FilteredResults.SelectMany(r => r.Insights.VisionInsights.Tags).Distinct().OrderBy(t => t))
            {
                this.TagFilters.Add(new TagFilterViewModel(tag));
            }
        }

        private void TagFilterChanged(object sender, RoutedEventArgs e)
        {
            this.ApplyFilters();
        }

        private void ApplyFilters()
        {
            this.FilteredResults.Clear();

            var checkedTags = this.TagFilters.Where(t => t.IsChecked);
            if (checkedTags.Any())
            {
                this.FilteredResults.AddRange(this.AllResults.Where(r => HasTag(checkedTags, r.Insights.VisionInsights.Tags)));
            }
            else
            {
                this.FilteredResults.AddRange(this.AllResults);
            }
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
