using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using JPEGexplorer.Models;
using JPEGexplorer.Services;
using JPEGexplorer.Helpers;

using Microsoft.Toolkit.Uwp.UI.Animations;

using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace JPEGexplorer.Views
{
    public sealed partial class ImageGalleryPage : Page, INotifyPropertyChanged
    {
        public const string ImageGallerySelectedIdKey = "ImageGallerySelectedIdKey";
        private static int previouslySelectedItemIndex = -1;

        public ObservableCollection<SampleImage> Source { get; } = new ObservableCollection<SampleImage>();

        public ImageGalleryPage()
        {
            InitializeComponent();
            Loaded += ImageGalleryPage_OnLoaded;
        }

        private async void ImageGalleryPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            //StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures).AsTask().GetAwaiter().GetResult().SaveFolder;
            //Source.Clear();

            //var data = await SampleDataService.GetImageGalleryDataAsync("ms-appx:///Assets");

            //foreach (var item in data)
            //{
            //    Source.Add(item);
            //}

            //if (previouslySelectedItemIndex >= 0 && previouslySelectedItemIndex < Source.Count)
            //{
            //    imageGridView.SelectRange(new Windows.UI.Xaml.Data.ItemIndexRange(previouslySelectedItemIndex, 1));
            //}
            var data = await ImageLoaderService.GetImageGalleryDataAsync((await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures)).SaveFolder);

            if (data != null)
            {
                imageGridView.ItemsSource = data;
            }

            if (previouslySelectedItemIndex >= 0 && previouslySelectedItemIndex < Source.Count)
            {
                imageGridView.SelectRange(new Windows.UI.Xaml.Data.ItemIndexRange(previouslySelectedItemIndex, 1));
            }
        }


        // public event EventHandler<ImageSelectedEventArgs> ImageSelected;
        public delegate Task ImageSelectedEventHandler(object sender, ImageSelectedEventArgs e);
        public event ImageSelectedEventHandler ImageSelected;

        private void ImageGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var senderObject = sender as GridView;

            var selected = senderObject.SelectedItem as ImageItem;

            NavigationService.Frame.SetListDataItemForNextConnectedAnimation(selected);
            NavigationService.Navigate<ImageGalleryDetailPage>(selected);
        }

        private void ImagesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var senderObject = sender as GridView;

            if (senderObject.SelectedRanges.Count == 1)
            {
                previouslySelectedItemIndex = senderObject.SelectedIndex;
                var selected = senderObject.SelectedItem as ImageItem;
                ImageSelected?.Invoke(this, new ImageSelectedEventArgs(selected));
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
