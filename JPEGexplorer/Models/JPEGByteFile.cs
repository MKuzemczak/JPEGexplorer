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

        public StorageFile File { get; set; }

        public List<Segment> Segments = new List<Segment>();

        public List<Segment> DeletedSegments = new List<Segment>();

        public bool Modified = false;

        public async Task SaveFile()
        {
            await FileIO.WriteBytesAsync(File, FileBytes);
        }

        public void RemoveSegment(int index)
        {
            if (index > Segments.Count)
                return;

            byte[] start = FileBytes.Take(Segments[index].SegmentStartByteIndexInFile).ToArray();
            byte[] end = FileBytes.Skip(Segments[index].SegmentEndByteIndexInFile).ToArray();

            byte[] tmpBytes = new byte[start.Length + end.Length];

            start.CopyTo(tmpBytes, 0);
            end.CopyTo(tmpBytes, start.Length);

            FileBytes = tmpBytes;

            int removedSegmentTotalLength = Segments[index].SegmentEndByteIndexInFile - Segments[index].SegmentStartByteIndexInFile;

            DeletedSegments.Add(Segments[index]);
            Segments.RemoveAt(index);

            for (int i = index; i < Segments.Count; i++)
            {
                Segments[i].SegmentStartByteIndexInFile -= removedSegmentTotalLength;
                Segments[i].SegmentEndByteIndexInFile -= removedSegmentTotalLength;
                Segments[i].SegmentIndexInFile--;
            }

            Modified = true;
        }

        public void UndoLastSegmentRemoval()
        {
            if (!DeletedSegments.Any())
                return;

            int addedSegmentTotalLength = DeletedSegments.Last().SegmentEndByteIndexInFile - DeletedSegments.Last().SegmentStartByteIndexInFile;
            Segments.Insert(DeletedSegments.Last().SegmentIndexInFile, DeletedSegments.Last());

            byte[] newBytes = new byte[FileBytes.Length + addedSegmentTotalLength];

            FileBytes.Take(DeletedSegments.Last().SegmentStartByteIndexInFile).ToArray().CopyTo(newBytes, 0);
            DeletedSegments.Last().Content.CopyTo(newBytes, DeletedSegments.Last().SegmentStartByteIndexInFile);
            FileBytes.Skip(DeletedSegments.Last().SegmentStartByteIndexInFile).ToArray().CopyTo(newBytes, DeletedSegments.Last().SegmentEndByteIndexInFile);
            FileBytes = newBytes;

            for (int i = DeletedSegments.Last().SegmentIndexInFile; i < Segments.Count; i++)
            {
                Segments[i].SegmentStartByteIndexInFile += addedSegmentTotalLength;
                Segments[i].SegmentEndByteIndexInFile += addedSegmentTotalLength;
                Segments[i].SegmentIndexInFile++;
            }

            DeletedSegments.RemoveAt(DeletedSegments.Count - 1);
        }
    }
}
