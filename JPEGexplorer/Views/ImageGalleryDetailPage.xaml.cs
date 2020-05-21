using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using JPEGexplorer.Models;
using JPEGexplorer.Services;
using JPEGexplorer.Helpers;
using JPEGexplorer.FFT;

using Microsoft.Toolkit.Uwp.UI.Animations;

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace JPEGexplorer.Views
{
    public sealed partial class ImageGalleryDetailPage : Page, INotifyPropertyChanged
    {
        private object _selectedImage;

        public object SelectedImage
        {
            get => _selectedImage;
            set
            {
                Set(ref _selectedImage, value);
            }
        }

        public ObservableCollection<ImageItem> Source { get; } = new ObservableCollection<ImageItem>();

        public ImageGalleryDetailPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await (e.Parameter as ImageItem).ToImageAsync();
            base.OnNavigatedTo(e);
            Source.Clear();

            Source.Add(e.Parameter as ImageItem);
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            await Source[0].ToThumbnailAsync();
        }

        private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                e.Handled = true;
            }
        }

        private void OnGoBack(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
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
