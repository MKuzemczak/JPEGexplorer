using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;

using JPEGexplorer.Helpers;
using JPEGexplorer.Models;
using Windows.Storage.Streams;

namespace JPEGexplorer.Services
{
    public static class JPEGAnalyzerService
    {
        

        public static async Task<JPEGByteFile> GetFileSegmentsAsync(StorageFile file)
        {
            List<Segment> segments = new List<Segment>();

            byte[] fileBytes = new byte[] { };

            bool fileReadSucceeded = true;

            try
            {
                IBuffer buffer = await FileIO.ReadBufferAsync(file);
                fileBytes = buffer.ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Excepion while reading file bytes: {e}");
                fileReadSucceeded = false;
            }

            if (!fileReadSucceeded)
            {
                throw new IOException();
            }

            JPEGByteFile ret = new JPEGByteFile()
            {
                File = file,
                FileBytes = fileBytes
            };

            if (fileBytes[0] != 0xFF ||
                fileBytes[1] != 0xD8 )
            {
                throw new FileFormatException();
            }

            Int32 cntr = 2;
            Int32 effectiveLength = (Int32)fileBytes.Length;

            while (cntr < effectiveLength)
            {
                if (fileBytes[cntr++] != 0xFF)
                {
                    throw new FileFormatException();
                }

                if (fileBytes[cntr] == 0xD9) // End of image
                {
                    break;
                }

                Segment segment = new Segment()
                {
                    SegmentStartByteIndexInFile = cntr - 1
                };

                Int32 segmentDataLength = 0;

                try
                {
                    segment.Name = JPEGResources.SegmentNameDictionary[fileBytes[cntr]];
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception thrown during Segment name dictionary lookup: {e}");
                    throw new FileFormatException();
                }

                if (JPEGResources.RemovableSegments.Contains(fileBytes[cntr]))
                {
                    segment.Removable = true;
                }

                if (fileBytes[cntr] == 0xDA) // case there's only SOS segment left (it doesn't include segment length, so it needs special treatment)
                {
                    int compressedImageSegmentLength = 0;

                    while (!(fileBytes[cntr] == 0xFF && fileBytes[cntr + 1] == 0xD9))
                    {
                        cntr++;
                        compressedImageSegmentLength++;
                    }

                    segment.Length = compressedImageSegmentLength - 1;
                    segment.ExcessBytesAfterSegment = effectiveLength - cntr - 2; // two bytes for the 0xFFD9 EOI marker
                    segment.SegmentIndexInFile = segments.Count;
                    segment.SegmentEndByteIndexInFile = cntr + 2;
                    segments.Add(segment);

                    break;
                }

                cntr++;

                try
                {
                    byte[] lengthBytes = (byte[])fileBytes.Skip(cntr).Take(2).ToArray();
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(lengthBytes);
                    segmentDataLength = (Int32)BitConverter.ToUInt16(lengthBytes, 0);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception thrown during Segment length exctraction: {e}");
                    throw new FileFormatException();
                }

                segment.Length = segmentDataLength;
                cntr += 2;

                try
                {
                    byte[] rawBytes = fileBytes.Skip(cntr - 4).Take(segmentDataLength + 2).ToArray();
                    byte[] midBytes = new byte[rawBytes.Length];
                    int startPivot = 0, endPivot = 0, midBytesPivot = 0, minReadableTextLength = 8;
                    byte minAscii = 0x20, maxAscii = 0x7f;

                    while (endPivot < rawBytes.Length)
                    {
                        byte currentEndPivotByte = rawBytes[endPivot], currentStartPivotByte = rawBytes[startPivot];
                        if (currentEndPivotByte >= minAscii && currentEndPivotByte < maxAscii)
                        {
                            endPivot++;
                            continue;
                        }

                        if (endPivot - startPivot > minReadableTextLength)
                        {
                            while (startPivot < endPivot)
                            {
                                midBytes[midBytesPivot++] = rawBytes[startPivot++];
                            }
                        }
                        else
                        {
                            startPivot = endPivot;
                        }

                        if (midBytesPivot > 0 && midBytes[midBytesPivot - 1] != (byte)'\n')
                        {
                            midBytes[midBytesPivot++] = (byte)'\n';
                        }

                        startPivot = ++endPivot;
                    }

                    byte[] resultBytes = new byte[midBytesPivot];
                    midBytes.Take(midBytesPivot).ToArray().CopyTo(resultBytes, 0);
                    segment.Content = resultBytes;

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception thrown during Segment data exctraction: {e}");
                    throw new FileFormatException();
                }

                cntr += segmentDataLength - 2; // without the two length bytes which were already added to cntr
                segment.SegmentEndByteIndexInFile = cntr;
                int excessCntr = 0;

                while (cntr + excessCntr + 1 < effectiveLength &&
                    !(fileBytes[cntr + excessCntr] == 0xFF &&
                    JPEGResources.SegmentNameDictionary.ContainsKey(fileBytes[cntr + excessCntr + 1])))
                {
                    excessCntr++;
                }

                cntr += excessCntr;
                segment.ExcessBytesAfterSegment = excessCntr;
                segment.SegmentIndexInFile = segments.Count;
                segments.Add(segment);
            }

            ret.Segments = segments;

            return ret;
        }

        public static async Task<JPEGByteFile> GetFileSegmentsAsync(ImageItem image)
        {
            return await GetFileSegmentsAsync(image.File);
        }

        
    }
}
