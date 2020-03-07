using System;

using JPEGexplorer.Core.Models;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace JPEGexplorer.Views
{
    public sealed partial class MasterDetailDetailControl : UserControl
    {
        public SampleImage SourceImage
        {
            get { return GetValue(SourceImageProperty) as SampleImage; }
            set { SetValue(SourceImageProperty, value); }
        }

        public SampleOrder MasterMenuItem
        {
            get { return GetValue(MasterMenuItemProperty) as SampleOrder; }
            set { SetValue(MasterMenuItemProperty, value); }
        }

        public static readonly DependencyProperty SourceImageProperty = DependencyProperty.Register("SourceImage",
            typeof(SampleImage),
            typeof(MasterDetailDetailControl),
            new PropertyMetadata(null, OnSourceImagePropertyChanged));
        public static readonly DependencyProperty MasterMenuItemProperty = DependencyProperty.Register("MasterMenuItem",
            typeof(SampleOrder),
            typeof(MasterDetailDetailControl),
            new PropertyMetadata(null, OnMasterMenuItemPropertyChanged));

        public MasterDetailDetailControl()
        {
            InitializeComponent();
        }

        public void HandleSelectedImage(SampleImage i)
        {
            SourceImage = i;
            shipToTextBlock.Text = "hehe";
        }

        private static void OnSourceImagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MasterDetailDetailControl;
            control.ForegroundElement.ChangeView(0, 0, 1);
        }

        private static void OnMasterMenuItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MasterDetailDetailControl;
            control.ForegroundElement.ChangeView(0, 0, 1);
            
        }
    }
}
