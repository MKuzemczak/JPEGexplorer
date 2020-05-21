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
            long p = 6792763;
            long q = 6792781;
            var keys = RSAService.ZnajdzWykladnikPublicznyiPrywatny(p, q);

            var result = RSAService.code(bytes, keys[0], p * q);


            // zamiana dwuwymiarowej (result) tablicy na jednowymiarowa (resultOneD), zeby ja przekazac do odszyfrowania
            byte[] resultOneD = new byte[result.Length * 8];

            int cntr = 0;
            foreach (var item in result)
            {
                item.CopyTo(resultOneD, cntr);
                cntr += item.Length;
            }

            var inverse = RSAService.code(resultOneD, keys[1], p * q);

            int a = 0;
        }
    }
}
