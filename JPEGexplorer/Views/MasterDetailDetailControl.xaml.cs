using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using JPEGexplorer.Core.Models;
using JPEGexplorer.Models;
using JPEGexplorer.Services;

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

        public async Task HandleSelectedImage(SampleImage i)
        {
            SourceImage = i;

            block.Children.Clear();

            List<Segment> segments = new List<Segment>();

            try
            {
                segments = await JPEGAnalyzerService.GetFileSegmentsAsync(i.Source);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Excepion thrown while reading file segments: {e}");
            }

            foreach (Segment s in segments)
            {
                TextBlock title = new TextBlock
                {
                    Text = s.Name,
                    Style = (Style)Application.Current.Resources["DetailSubTitleStyle"],
                    Margin = (Thickness)Application.Current.Resources["SmallTopMargin"]
                };
                block.Children.Add(title);

                TextBlock content = new TextBlock
                {
                    Text = "Length: " + s.Length.ToString(),
                    Style = (Style)Application.Current.Resources["DetailBodyBaseMediumStyle"]
                };
                block.Children.Add(content);
            }

            //shipToTextBlock.Text = "hehe";
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
