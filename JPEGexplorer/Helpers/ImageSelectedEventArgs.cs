using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JPEGexplorer.Core.Models;

namespace JPEGexplorer.Helpers
{
    public class ImageSelectedEventArgs : EventArgs
    {
        public ImageSelectedEventArgs(SampleImage s) { SelectedImage = s; }
        public SampleImage SelectedImage { get; }
    }
}
