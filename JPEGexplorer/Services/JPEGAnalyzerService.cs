using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;

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
                    segment.Name = SegmentNameDictionary[fileBytes[cntr]];
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception thrown during Segment name dictionary lookup: {e}");
                    throw new FileFormatException();
                }

                if (RemovableSegments.Contains(fileBytes[cntr]))
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

                while (cntr + excessCntr + 1 < effectiveLength && !(fileBytes[cntr + excessCntr] == 0xFF && SegmentNameDictionary.ContainsKey(fileBytes[cntr + excessCntr + 1])))
                    excessCntr++;

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

        public static Dictionary<byte, string> SegmentNameDictionary = new Dictionary<byte, string>()
        {
            {0xC0, "SOF0 - Baseline DCT"},
            {0xC1, "SOF1 - Extended Sequential DCT"},
            {0xC2, "SOF2 - Progressive DCT"},
            {0xC3, "SOF3 - Lossless (sequential)"},
            {0xC4, "DHT -	Define Huffman Table	"},
            {0xC5, "SOF5 - Differential sequential DCT"},
            {0xC6, "SOF6 - Differential progressive DCT"},
            {0xC7, "SOF7 - Differential lossless (sequential)"},
            {0xC8, "JPG - JPEG Extensions	"},
            {0xC9, "SOF9 - Extended sequential DCT, Arithmetic coding"},
            {0xCA, "SOF10 - Progressive DCT, Arithmetic coding"},
            {0xCB, "SOF11 - Lossless (sequential), Arithmetic coding"},
            {0xCC, "DAC - Define Arithmetic Coding	"},
            {0xCD, "SOF13 - Differential sequential DCT, Arithmetic coding"},
            {0xCE, "SOF14 - Differential progressive DCT, Arithmetic coding"},
            {0xCF, "SOF15 - Differential lossless (sequential), Arithmetic coding"},
            {0xD0, "RST0 - Restart Marker 0	"},
            {0xD1, "RST1 - Restart Marker 1	"},
            {0xD2, "RST2 - Restart Marker 2	"},
            {0xD3, "RST3 - Restart Marker 3	"},
            {0xD4, "RST4 - Restart Marker 4	"},
            {0xD5, "RST5 - Restart Marker 5	"},
            {0xD6, "RST6 - Restart Marker 6	"},
            {0xD7, "RST7 - Restart Marker 7	"},
            {0xD8, "SOI - Start of Image	"},
            {0xD9, "EOI - End of Image	"},
            {0xDA, "SOS - Start of Scan	"},
            {0xDB, "DQT - Define Quantization Table	"},
            {0xDC, "DNL - Define Number of Lines"},
            {0xDD, "DRI - Define Restart Interval	"},
            {0xDE, "DHP - Define Hierarchical Progression"},
            {0xDF, "EXP - Expand Reference Component"},
            {0xE0, "APP0 - Application Segment 0, JFIF JPEG image"},
            {0xE1, "APP1 - Application Segment 1, EXIF Metadata, TIFF IFD format, JPEG Thumbnail (160×120), Adobe XMP"},
            {0xE2, "APP2 - Application Segment 2, ICC color profile, FlashPix"},
            {0xE3, "APP3 - Application Segment 3, JPS Tag for Stereoscopic JPEG images"},
            {0xE4, "APP4 - Application Segment 4 "},
            {0xE5, "APP5 - Application Segment 5 "},
            {0xE6, "APP6 - Application Segment 6, NITF Lossles profile"},
            {0xE7, "APP7 - Application Segment 7 "},
            {0xE8, "APP8 - Application Segment 8 "},
            {0xE9, "APP9 - Application Segment 9 "},
            {0xEA, "APP10 - Application Segment 10, PhoTags, ActiveObject (multimedia messages / captions)"},
            {0xEB, "APP11 - Application Segment 11, HELIOS JPEG Resources (OPI Postscript)"},
            {0xEC, "APP12 - Application Segment 12, Picture Info (older digicams), Photoshop Save for Web: Ducky"},
            {0xED, "APP13 - Application Segment 13, Photoshop Save As: IRB, 8BIM, IPTC"},
            {0xEE, "APP14 - Application Segment 14 "},
            {0xEF, "APP15 - Application Segment 15 "},
            {0xF0, "JPG0 - JPEG Extension 0"},
            {0xF1, "JPG1 - JPEG Extension 1"},
            {0xF2, "JPG2 - JPEG Extension 2"},
            {0xF3, "JPG3 - JPEG Extension 3"},
            {0xF4, "JPG4 - JPEG Extension 4"},
            {0xF5, "JPG5 - JPEG Extension 5"},
            {0xF6, "JPG6 - JPEG Extension 6"},
            {0xF7, "JPG7 - JPEG Extension 7"},
            {0xF8, "JPG8 - JPEG Extension 8"},
            {0xF9, "JPG9 - JPEG Extension 9"},
            {0xFA, "JPG10 - JPEG Extension 10"},
            {0xFB, "JPG11 - JPEG Extension 11"},
            {0xFC, "JPG12 - JPEG Extension 12"},
            {0xFD, "JPG13 - JPEG Extension 13"},
            {0xFE, "COM	Comment"}
        };

        public static HashSet<byte> RemovableSegments = new HashSet<byte>()
        {
            0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
            0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE
        };

    }
}
