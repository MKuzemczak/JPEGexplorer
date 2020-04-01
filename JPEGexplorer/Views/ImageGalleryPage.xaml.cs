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

        public ImageGalleryPage()
        {
            InitializeComponent();
            Loaded += ImageGalleryPage_OnLoaded;
        }

        private async void ImageGalleryPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            var data = await ImageLoaderService.GetImageGalleryDataAsync((await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures)).SaveFolder);

            if (data != null)
            {
                imageGridView.ItemsSource = data;
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
