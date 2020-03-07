using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using JPEGexplorer.Core.Models;
using JPEGexplorer.Core.Services;
using JPEGexplorer.Helpers;
using JPEGexplorer.Services;

using Microsoft.Toolkit.Uwp.UI.Animations;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace JPEGexplorer.Views
{
    public sealed partial class ImageGalleryPage : Page, INotifyPropertyChanged
    {
        public const string ImageGallerySelectedIdKey = "ImageGallerySelectedIdKey";
        string currentlySelectedID;

        public ObservableCollection<SampleImage> Source { get; } = new ObservableCollection<SampleImage>();

        public ImageGalleryPage()
        {
            InitializeComponent();
            Loaded += ImageGalleryPage_OnLoaded;
        }

        private async void ImageGalleryPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            Source.Clear();

            // TODO WTS: Replace this with your actual data
            var data = await SampleDataService.GetImageGalleryDataAsync("ms-appx:///Assets");

            foreach (var item in data)
            {
                Source.Add(item);
            }
        }


        public event EventHandler<ImageSelectedEventArgs> ImageSelected;

        private void ImagesGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selected = e.ClickedItem as SampleImage;

            // Double click open the image
            if (selected.ID == currentlySelectedID)
            {
                ImagesNavigationHelper.AddImageId(ImageGallerySelectedIdKey, selected.ID);
                NavigationService.Frame.SetListDataItemForNextConnectedAnimation(selected);
                NavigationService.Navigate<ImageGalleryDetailPage>(selected.ID);
            }
            else // single click shows the metadata of the image
            {
                
                currentlySelectedID = selected.ID;
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
