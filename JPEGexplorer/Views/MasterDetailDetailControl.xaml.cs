using System;
using System.Collections.Generic;
using System.Linq;
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

        public JPEGByteFile SourceByteFile { get; set; }

        public MasterDetailDetailControl()
        {
            InitializeComponent();
        }

        public async Task HandleSelectedImage(SampleImage i)
        {
            SourceImage = i;

            block.Children.Clear();

            SourceByteFile = new JPEGByteFile();

            try
            {
                SourceByteFile = await JPEGAnalyzerService.GetFileSegmentsAsync(i.Source);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Excepion thrown while reading file segments: {e}");
            }


            int segmentCntr = 0;
            foreach (Segment s in SourceByteFile.Segments)
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
                    Text = "Length: " + s.Length.ToString() + " B, Excess bytes after segment: " + s.ExcessBytesAfterSegment.ToString() + " B",
                    Style = (Style)Application.Current.Resources["DetailBodyBaseMediumStyle"]
                };
                block.Children.Add(content);

                Button removeSegmentButton = new Button()
                {
                    Content = "Remove segment",
                    Tag = "BTN-" + segmentCntr
                };

                removeSegmentButton.Click += RemoveSegmentButton_Click;
                block.Children.Add(removeSegmentButton);
                segmentCntr++;
            }

            //shipToTextBlock.Text = "hehe";
        }

        private async void RemoveSegmentButton_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((sender as Button).Tag as string).Split('-').Last());

            SourceByteFile.RemoveSegment(index);
            string path = SourceByteFile.Path;
            SourceByteFile.Path = path.Substring(0, path.LastIndexOf("/") + 1) + "tmp.jpg";
            await SourceByteFile.SaveFile();
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
