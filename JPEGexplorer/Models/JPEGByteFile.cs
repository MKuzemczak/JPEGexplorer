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

        private List<Segment> RemovedSegments = new List<Segment>();

        private List<Change> ChangesHistory = new List<Change>();

        public bool Modified = false;

        class Change
        {
            public Action UndoAction;
            public ChangeType TypeOfChange;
        }

        private enum ChangeType
        {
            SegmentRemoval,
            ExcessBytesRemoval
        };


        public async Task SaveFile()
        {
            if (Modified)
                await FileIO.WriteBytesAsync(File, FileBytes);
        }

        public void RemoveSegment(int index)
        {
            if (index > Segments.Count)
                return;

            byte[] BytesBeforeSegment = FileBytes.Take(Segments[index].SegmentStartByteIndexInFile).ToArray();
            byte[] BytesAfterSegment = FileBytes.Skip(Segments[index].SegmentEndByteIndexInFile).ToArray();

            byte[] tmpBytes = new byte[BytesBeforeSegment.Length + BytesAfterSegment.Length];

            BytesBeforeSegment.CopyTo(tmpBytes, 0);
            BytesAfterSegment.CopyTo(tmpBytes, BytesBeforeSegment.Length);

            FileBytes = tmpBytes;

            int removedSegmentTotalLength = Segments[index].SegmentEndByteIndexInFile - Segments[index].SegmentStartByteIndexInFile;

            RemovedSegments.Add(Segments[index]);
            Segments.RemoveAt(index);

            for (int i = index; i < Segments.Count; i++)
            {
                Segments[i].SegmentStartByteIndexInFile -= removedSegmentTotalLength;
                Segments[i].SegmentEndByteIndexInFile -= removedSegmentTotalLength;
                Segments[i].SegmentIndexInFile--;
            }

            Modified = true;
            ChangesHistory.Add(new Change() { TypeOfChange = ChangeType.SegmentRemoval, UndoAction = UndoLastSegmentRemoval });
        }

        private void UndoLastSegmentRemoval()
        {
            if (!RemovedSegments.Any())
                return;

            int addedSegmentTotalLength = RemovedSegments.Last().SegmentEndByteIndexInFile - RemovedSegments.Last().SegmentStartByteIndexInFile;
            Segments.Insert(RemovedSegments.Last().SegmentIndexInFile, RemovedSegments.Last());

            byte[] newBytes = new byte[FileBytes.Length + addedSegmentTotalLength];

            FileBytes.Take(RemovedSegments.Last().SegmentStartByteIndexInFile).ToArray().CopyTo(newBytes, 0);
            RemovedSegments.Last().Content.CopyTo(newBytes, RemovedSegments.Last().SegmentStartByteIndexInFile);
            FileBytes.Skip(RemovedSegments.Last().SegmentStartByteIndexInFile).ToArray().CopyTo(newBytes, RemovedSegments.Last().SegmentEndByteIndexInFile);
            FileBytes = newBytes;

            for (int i = RemovedSegments.Last().SegmentIndexInFile; i < Segments.Count; i++)
            {
                Segments[i].SegmentStartByteIndexInFile += addedSegmentTotalLength;
                Segments[i].SegmentEndByteIndexInFile += addedSegmentTotalLength;
                Segments[i].SegmentIndexInFile++;
            }

            RemovedSegments.RemoveAt(RemovedSegments.Count - 1);
        }

        public void RemoveExcessBytesAfterSegment(int index)
        {
            if (index > Segments.Count)
                return;

            int numberOfExcessBytes = Segments[index].ExcessBytesAfterSegment;
            int segmentEndByteIndexInFile = Segments[index].SegmentEndByteIndexInFile;

            byte[] removedBytes = FileBytes.Skip(segmentEndByteIndexInFile).Take(numberOfExcessBytes).ToArray();
            byte[] tmpBytes = new byte[FileBytes.Length - numberOfExcessBytes];

            FileBytes.Take(segmentEndByteIndexInFile).ToArray().CopyTo(tmpBytes, 0);
            FileBytes.Skip(segmentEndByteIndexInFile + numberOfExcessBytes).ToArray().CopyTo(tmpBytes, segmentEndByteIndexInFile);

            FileBytes = tmpBytes;
            Segments[index].ExcessBytesAfterSegment = 0;
            RemovedSegments.Add(new Segment()
            {
                Length = numberOfExcessBytes,
                Content = removedBytes,
                SegmentStartByteIndexInFile = segmentEndByteIndexInFile,
                SegmentEndByteIndexInFile = segmentEndByteIndexInFile + numberOfExcessBytes,
                SegmentIndexInFile = index
            });

            for (int i = index + 1; i < Segments.Count; i++)
            {
                Segments[i].SegmentStartByteIndexInFile -= numberOfExcessBytes;
                Segments[i].SegmentEndByteIndexInFile -= numberOfExcessBytes;
            }

            Modified = true;
            ChangesHistory.Add(new Change() { TypeOfChange = ChangeType.ExcessBytesRemoval, UndoAction = UndoLastExcessBytesAfterSegmentRemoval });
        }

        private void UndoLastExcessBytesAfterSegmentRemoval()
        {
            Segment restoredSegment = RemovedSegments.Last();

            byte[] tmpBytes = new byte[FileBytes.Length + restoredSegment.Length];
            FileBytes.Take(restoredSegment.SegmentStartByteIndexInFile).ToArray().CopyTo(tmpBytes, 0);
            restoredSegment.Content.CopyTo(tmpBytes, restoredSegment.SegmentStartByteIndexInFile);
            FileBytes.Skip(restoredSegment.SegmentStartByteIndexInFile).ToArray().CopyTo(tmpBytes, restoredSegment.SegmentEndByteIndexInFile);
            FileBytes = tmpBytes;

            Segments[restoredSegment.SegmentIndexInFile].ExcessBytesAfterSegment = restoredSegment.Length;

            for (int i = restoredSegment.SegmentIndexInFile + 1; i < Segments.Count; i++)
            {
                Segments[i].SegmentStartByteIndexInFile += restoredSegment.Length;
                Segments[i].SegmentEndByteIndexInFile += restoredSegment.Length;
            }

            RemovedSegments.RemoveAt(RemovedSegments.Count - 1);
        }

        public void UndoLastChange()
        {
            if (ChangesHistory.Count == 0)
                return;

            ChangesHistory.Last().UndoAction();
            ChangesHistory.RemoveAt(ChangesHistory.Count - 1);

            if (ChangesHistory.Count == 0)
            {
                Modified = false;
            }
        }
    }
}
