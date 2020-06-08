using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Windows.Storage;

using JPEGexplorer.Helpers;
using JPEGexplorer.RSA;

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

        public async Task EncodeRSA()
        {
            var segment = Segments.Find(i => i.Name == JPEGResources.SegmentNameDictionary[0xDA]);

            byte[] bytes = FileBytes.Take(segment.SegmentEndByteIndexInFile - 2).Skip(segment.SegmentStartByteIndexInFile + 20).ToArray();
            long p = 10007;
            long q = 10009;
            var keys = RSAService.ZnajdzWykladnikPublicznyiPrywatny(p, q);

            var result = RSAService.Encode(bytes, keys[0], p * q);

            int takeSkip = segment.SegmentEndByteIndexInFile - 2 - segment.SegmentStartByteIndexInFile - 20;
            result.Take(takeSkip)
                .ToArray()
                .CopyTo(FileBytes, segment.SegmentStartByteIndexInFile + 20);
            var newFileBytes = new byte[FileBytes.Length + result.Length - takeSkip];
            FileBytes.CopyTo(newFileBytes, 0);
            result.Skip(takeSkip).ToArray().CopyTo(newFileBytes, FileBytes.Length);
            FileBytes = newFileBytes;
            Modified = true;
            await SaveFile();
        }

        public async Task DecodeRSA()
        {
            var segment = Segments.Find(i => i.Name == JPEGResources.SegmentNameDictionary[0xDA]);

            byte[] bytes = FileBytes.Take(segment.SegmentEndByteIndexInFile - 2).Skip(segment.SegmentStartByteIndexInFile + 20).ToArray();
            int excessBytes = bytes.Length * 8 - bytes.Length;
            byte[] newBytes = new byte[bytes.Length * 8];
            bytes.CopyTo(newBytes, 0);
            FileBytes.Skip(FileBytes.Length - excessBytes).ToArray().CopyTo(newBytes, bytes.Length);
            long p = 10007;
            long q = 10009;

            var keys = RSAService.ZnajdzWykladnikPublicznyiPrywatny(p, q);

            var result = RSAService.Decode(newBytes, keys[1], p * q);

            result.CopyTo(FileBytes, segment.SegmentStartByteIndexInFile + 20);
            byte[] newFileBytes = new byte[FileBytes.Length - excessBytes];
            FileBytes.Take(FileBytes.Length - excessBytes).ToArray().CopyTo(newFileBytes, 0);
            FileBytes = newFileBytes;
            Modified = true;
            await SaveFile();
        }

        public void EncodeRSABuiltin()
        {

        }
    }
}
