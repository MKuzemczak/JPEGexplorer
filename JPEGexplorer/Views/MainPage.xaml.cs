using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


using Windows.UI.Xaml.Controls;

using JPEGexplorer.Helpers;
using JPEGexplorer.RSA;
using System.Security.Cryptography;

namespace JPEGexplorer.Views
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public MainPage()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task ImageGalleryPage_ImageSelected(object sender, ImageSelectedEventArgs e)
        {
            splitView.IsPaneOpen = true;
            await detailControl.HandleSelectedImage(e.SelectedImage);
        }

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

        private void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            byte[] bytes = new byte[] { 4, 25, 156, 243 };
            long p = /*10007*/7;
            long q = /*10009*/13;
            var keys = RSAService.ZnajdzWykladnikPublicznyiPrywatny(p, q);

            var result = RSAService.Encode(bytes, keys[0], p * q);


            
            var inverse = RSAService.Decode(result, keys[1], p * q);

            int a = 0;
        }
    }
}
