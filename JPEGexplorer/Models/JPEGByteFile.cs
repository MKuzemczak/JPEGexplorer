using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Windows.Storage;

namespace JPEGexplorer.Models
{
    public class JPEGByteFile
    {
        public byte[] FileBytes { get; set; }

        public string Name { get; set; }

        private string _path;
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;
                Name = _path.Split('/').Last();
            }
        }

        public List<Segment> Segments = new List<Segment>();

        public async Task SaveFile()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await storageFolder.CreateFileAsync(Name, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(sampleFile, FileBytes);
        }

        public void RemoveSegment(int index)
        {
            if (index > Segments.Count)
                return;

            byte[] start = (byte[])FileBytes.Take(Segments[index].SegmentStartFileIndex).ToArray();
            byte[] end = (byte[])FileBytes.Skip(Segments[index].SegmentEndFileIndex).ToArray();

            byte[] tmpBytes = new byte[start.Length + end.Length];

            start.CopyTo(tmpBytes, 0);
            end.CopyTo(tmpBytes, start.Length);

            FileBytes = tmpBytes; 
        }
    }
}
