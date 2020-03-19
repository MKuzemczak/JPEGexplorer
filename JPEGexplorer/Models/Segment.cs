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
        public string Content { get; set; }
        public int ExcessBytesAfterSegment { get; set; }
        public int SegmentStartFileIndex { get; set; }
        public int SegmentEndFileIndex { get; set; }
    }
}
