using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using JPEGexplorer.Helpers;
using JPEGexplorer.Services;

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace JPEGexplorer.Views
{
    public sealed partial class ShellPage : Page, INotifyPropertyChanged
    {
        private readonly KeyboardAccelerator _altLeftKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu);
        private readonly KeyboardAccelerator _backKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.GoBack);

        public ShellPage()
        {
            InitializeComponent();
            NavigationService.Frame = shellFrame;
            MenuNavigationHelper.Initialize(splitView, rightFrame);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            KeyboardAccelerators.Add(_altLeftKeyboardAccelerator);
            KeyboardAccelerators.Add(_backKeyboardAccelerator);
        }

        private void ShellMenuItemClick_Views_Main(object sender, RoutedEventArgs e)
        {
            MenuNavigationHelper.UpdateView(typeof(MainPage));
        }

        private void ShellMenuItemClick_File_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator() { Key = key };
            if (modifiers.HasValue)
            {
                keyboardAccelerator.Modifiers = modifiers.Value;
            }

            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
            return keyboardAccelerator;
        }

        private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var result = NavigationService.GoBack();
            args.Handled = result;
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
