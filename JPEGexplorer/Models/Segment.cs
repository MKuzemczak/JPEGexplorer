using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPEGexplorer.Models
{
    public class Segment
    {
        public string Name { get; set; }
        public int Length { get; set; }
        public byte[] Content { get; set; }
        public int ExcessBytesAfterSegment { get; set; }
        public int SegmentStartByteIndexInFile { get; set; }
        public int SegmentEndByteIndexInFile { get; set; }
        public bool Removable { get; set; } = false;
        public int SegmentIndexInFile { get; set; }
    }
}
