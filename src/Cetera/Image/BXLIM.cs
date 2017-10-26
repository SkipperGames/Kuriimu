using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Kontract.Image.Format;
using Kontract.Interface;
using Kontract.Image.Swizzle;
using Cetera.IO;
using Kontract.IO;
using System.Linq;

namespace Cetera.Image
{
    public sealed class BXLIM
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class BCLIMImageHeader
        {
            public short width;
            public short height;
            public byte format;
            public byte orientation;
            public short alignment;
            public int datasize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class BFLIMImageHeaderLE
        {
            public short width;
            public short height;
            public short alignment;
            public byte format;
            public byte orientation;
            public int datasize;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class BFLIMImageHeaderBE
        {
            public short width;
            public short height;
            public short alignment;
            public byte format;
            private byte tmp;
            public int datasize;

            public Orientation orientation => (Orientation)(tmp >> 5);
            public int tileMode => tmp & 0x1F;
        }

        public Dictionary<byte, IImageFormat> DSFormat = new Dictionary<byte, IImageFormat>
        {
            [0] = new LA(8, 0),
            [1] = new LA(0, 8),
            [2] = new LA(4, 4),
            [3] = new LA(8, 8),
            [4] = new HL(8, 8),
            [5] = new RGBA(5, 6, 5),
            [6] = new RGBA(8, 8, 8),
            [7] = new RGBA(5, 5, 5, 1),
            [8] = new RGBA(4, 4, 4, 4),
            [9] = new RGBA(8, 8, 8, 8),
            [10] = new Kontract.Image.Format.ETC1(),
            [11] = new Kontract.Image.Format.ETC1(true),
            [18] = new LA(4, 0),
            [19] = new LA(0, 4),
        };

        public Dictionary<byte, IImageFormat> WiiUFormat = new Dictionary<byte, IImageFormat>
        {
            [0] = new LA(8, 0, ByteOrder.BigEndian),
            [1] = new LA(0, 8, ByteOrder.BigEndian),
            [2] = new LA(4, 4, ByteOrder.BigEndian),
            [3] = new LA(8, 8, ByteOrder.BigEndian),
            [4] = new HL(8, 8, ByteOrder.BigEndian),
            [5] = new RGBA(5, 6, 5, 0, ByteOrder.BigEndian),
            [6] = new RGBA(8, 8, 8, 0, ByteOrder.BigEndian),
            [7] = new RGBA(5, 5, 5, 1, ByteOrder.BigEndian),
            [8] = new RGBA(4, 4, 4, 4, ByteOrder.BigEndian),
            [9] = new RGBA(8, 8, 8, 8, ByteOrder.BigEndian),
            [10] = new Kontract.Image.Format.ETC1(false, ByteOrder.BigEndian),
            [11] = new Kontract.Image.Format.ETC1(true, ByteOrder.BigEndian),
            [12] = new Kontract.Image.Format.DXT(Kontract.Image.Format.DXT.Version.DXT1, false, ByteOrder.BigEndian),
            [13] = new Kontract.Image.Format.DXT(Kontract.Image.Format.DXT.Version.DXT3, false, ByteOrder.BigEndian),
            [14] = new Kontract.Image.Format.DXT(Kontract.Image.Format.DXT.Version.DXT5, false, ByteOrder.BigEndian),
            [15] = new Kontract.Image.Format.ATI(Kontract.Image.Format.ATI.Format.ATI1L, ByteOrder.BigEndian),
            [16] = new Kontract.Image.Format.ATI(Kontract.Image.Format.ATI.Format.ATI1A, ByteOrder.BigEndian),
            [17] = new Kontract.Image.Format.ATI(Kontract.Image.Format.ATI.Format.ATI2, ByteOrder.BigEndian),
            [18] = new LA(4, 0, ByteOrder.BigEndian),
            [19] = new LA(0, 4, ByteOrder.BigEndian),
            [20] = null,
            [21] = null,
            [22] = null,
            [23] = null,
            [24] = new RGBA(10, 10, 10, 2, ByteOrder.BigEndian)
        };

        public enum Orientation : byte
        {
            Default = 0,
            Rotate90 = 4,
            Transpose = 8,
        }

        NW4CSectionList sections;

        private ByteOrder byteOrder { get; set; }

        public BCLIMImageHeader BCLIMHeader { get; private set; }
        public BFLIMImageHeaderLE BFLIMHeaderLE { get; private set; }
        public BFLIMImageHeaderBE BFLIMHeaderBE { get; private set; }

        public Bitmap Image { get; set; }

        public Kontract.Image.ImageSettings Settings { get; set; }

        public BXLIM(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                var tex = br.ReadBytes((int)br.BaseStream.Length - 40);
                sections = br.ReadSections();
                byteOrder = br.ByteOrder;

                switch (sections.Header.magic)
                {
                    case "CLIM":
                        BCLIMHeader = sections[0].Data.BytesToStruct<BCLIMImageHeader>(byteOrder);

                        CreateSwizzleLists(BCLIMHeader.orientation, byteOrder, out var innerS, out var outerS);

                        Settings = new Kontract.Image.ImageSettings
                        {
                            Width = BCLIMHeader.width,
                            Height = BCLIMHeader.height,
                            Format = DSFormat[BCLIMHeader.format],
                            InnerSwizzle = innerS,
                            OuterSwizzle = outerS,
                        };
                        Image = Kontract.Image.Image.Load(tex, Settings);
                        break;
                    case "FLIM":
                        if (byteOrder == ByteOrder.LittleEndian)
                        {
                            BFLIMHeaderLE = sections[0].Data.BytesToStruct<BFLIMImageHeaderLE>(byteOrder);

                            CreateSwizzleLists(BFLIMHeaderLE.orientation, byteOrder, out innerS, out outerS);

                            Settings = new Kontract.Image.ImageSettings
                            {
                                Width = BFLIMHeaderLE.width,
                                Height = BFLIMHeaderLE.height,
                                Format = DSFormat[BFLIMHeaderLE.format],
                                InnerSwizzle = innerS,
                                OuterSwizzle = outerS,
                            };
                            Image = Kontract.Image.Image.Load(tex, Settings);
                        }
                        else
                        {
                            BFLIMHeaderBE = sections[0].Data.BytesToStruct<BFLIMImageHeaderBE>(byteOrder);

                            Settings = new Kontract.Image.ImageSettings
                            {
                                Width = BFLIMHeaderBE.width,
                                Height = BFLIMHeaderBE.height,
                                Format = WiiUFormat[BFLIMHeaderBE.format],
                            };
                            Image = Kontract.Image.Image.Load(tex, Settings);
                        }
                        break;
                    default:
                        throw new NotSupportedException($"Unknown image format {sections.Header.magic}");
                }
            }
        }

        void CreateSwizzleLists(byte orient, ByteOrder byteOrder, out List<IImageSwizzle> inner, out List<IImageSwizzle> outer)
        {
            inner = null;
            outer = null;

            if (byteOrder == ByteOrder.LittleEndian)
                inner = new List<IImageSwizzle> { new ZOrder() };

            if (orient != 0)
            {
                byte count = 8;
                while (count != 0)
                {
                    switch (orient & (int)Math.Pow(2, --count))
                    {
                        case 0x80:
                            break;
                        case 0x40:
                            break;
                        case 0x20:
                            break;
                        case 0x10:
                            break;
                        //Transpose
                        case 0x8:
                            if (inner == null)
                                inner = new List<IImageSwizzle>();
                            if (outer == null)
                                outer = new List<IImageSwizzle>();
                            inner.Add(new Transpose());
                            outer.Add(new Transpose(true));
                            break;
                        //Rotated by 90
                        case 0x4:
                            if (inner == null)
                                inner = new List<IImageSwizzle>();
                            if (outer == null)
                                outer = new List<IImageSwizzle>();
                            inner.Add(new Rotate(270));
                            outer.Add(new Rotate(270, true));
                            break;
                        case 0x2:
                            break;
                        case 0x1:
                            break;
                    }
                }
            }
        }

        private Bitmap SwizzleTiles(Bitmap tex, int padWidth, int padHeight, int origWidth, int origHeight, int tileSize, int tileMode)
        {
            var newImage = new Bitmap(padWidth, padHeight);

            var oldG = Graphics.FromImage(tex);
            var newG = Graphics.FromImage(newImage);

            switch (tileMode)
            {
                case 4:
                    var swizzleY = new int[] { tileSize, 0, tileSize, 0 };
                    var swizzleX = new int[] { 0, 0, tileSize, tileSize };
                    var xValues = new int[] { 0, 0, padWidth, padWidth };
                    var xValuesPos = 0;

                    var newPosX = 0;
                    var newPosY = 0;

                    for (int y = 0; y < padHeight; y += 2 * tileSize)
                    {
                        if (xValues[xValuesPos] == 0)
                            for (int x = 0; x < padWidth / 2; x += 2 * tileSize)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    var tmpTile = new Bitmap(tileSize, tileSize);
                                    Graphics.FromImage(tmpTile).DrawImage(tex, 0, 0, new Rectangle(new Point(x + swizzleX[i], y + swizzleY[i]), new Size(tileSize, tileSize)), GraphicsUnit.Pixel);

                                    newG.DrawImage(tmpTile, new Point(newPosX, newPosY));
                                    newPosX += tileSize;
                                    if (newPosX >= padWidth)
                                    {
                                        newPosX = 0;
                                        newPosY += tileSize;
                                    }

                                    tmpTile = new Bitmap(tileSize, tileSize);
                                    Graphics.FromImage(tmpTile).DrawImage(tex, 0, 0, new Rectangle(new Point(padWidth - ((x + 2 * tileSize)) + swizzleX[i], y + swizzleY[i]), new Size(tileSize, tileSize)), GraphicsUnit.Pixel);
                                    newG.DrawImage(tmpTile, new Point(newPosX, newPosY));
                                    newPosX += tileSize;
                                    if (newPosX >= padWidth)
                                    {
                                        newPosX = 0;
                                        newPosY += tileSize;
                                    }
                                }
                            }
                        else
                            for (int x = padWidth / 2 - 2 * tileSize; x >= 0; x -= 2 * tileSize)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    var tmpTile = new Bitmap(tileSize, tileSize);
                                    Graphics.FromImage(tmpTile).DrawImage(tex, 0, 0, new Rectangle(new Point(padWidth - ((x + 2 * tileSize)) + swizzleX[i], y + swizzleY[i]), new Size(tileSize, tileSize)), GraphicsUnit.Pixel);
                                    newG.DrawImage(tmpTile, new Point(newPosX, newPosY));
                                    newPosX += tileSize;
                                    if (newPosX >= padWidth)
                                    {
                                        newPosX = 0;
                                        newPosY += tileSize;
                                    }

                                    tmpTile = new Bitmap(tileSize, tileSize);
                                    Graphics.FromImage(tmpTile).DrawImage(tex, 0, 0, new Rectangle(new Point(x + swizzleX[i], y + swizzleY[i]), new Size(tileSize, tileSize)), GraphicsUnit.Pixel);

                                    newG.DrawImage(tmpTile, new Point(newPosX, newPosY));
                                    newPosX += tileSize;
                                    if (newPosX >= padWidth)
                                    {
                                        newPosX = 0;
                                        newPosY += tileSize;
                                    }
                                }
                            }

                        xValuesPos++;
                        swizzleY = swizzleY.Reverse().ToArray();
                    }

                    if (origWidth != padWidth || origHeight != padHeight)
                    {
                        var cropImage = new Bitmap(origWidth, origHeight);
                        Graphics.FromImage(cropImage).DrawImage(newImage, 0, 0, new Rectangle(new Point(0, 0), new Size(origWidth, origHeight)), GraphicsUnit.Pixel);
                        return cropImage;
                    }

                    return newImage;
                default:
                    return tex;
            }
        }

        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output))
            {
                var settings = new ImageSettings();
                byte[] texture;

                switch (sections.Header.magic)
                {
                    case "CLIM":
                        settings.Width = BCLIMHeader.width;
                        settings.Height = BCLIMHeader.height;
                        settings.Orientation = ImageSettings.ConvertOrientation(BCLIMHeader.orientation);
                        settings.Format = ImageSettings.ConvertFormat(BCLIMHeader.format);
                        texture = Common.Save(Image, settings);
                        bw.Write(texture);

                        // We can now change the image width/height/filesize!
                        BCLIMHeader.width = (short)Image.Width;
                        BCLIMHeader.height = (short)Image.Height;
                        BCLIMHeader.datasize = texture.Length;
                        sections[0].Data = BCLIMHeader.StructToBytes();
                        sections.Header.file_size = texture.Length + 40;
                        bw.WriteSections(sections);
                        break;
                    case "FLIM":
                        if (byteOrder == ByteOrder.LittleEndian)
                        {
                            settings.Width = BFLIMHeaderLE.width;
                            settings.Height = BFLIMHeaderLE.height;
                            settings.Orientation = ImageSettings.ConvertOrientation(BFLIMHeaderLE.orientation);
                            settings.Format = ImageSettings.ConvertFormat(BFLIMHeaderLE.format);
                            texture = Common.Save(Image, settings);
                            bw.Write(texture);

                            // We can now change the image width/height/filesize!
                            BFLIMHeaderLE.width = (short)Image.Width;
                            BFLIMHeaderLE.height = (short)Image.Height;
                            BFLIMHeaderLE.datasize = texture.Length;
                            sections[0].Data = BFLIMHeaderLE.StructToBytes();
                            sections.Header.file_size = texture.Length + 40;
                            bw.WriteSections(sections);
                        }
                        else
                        {
                            throw new NotSupportedException($"Big Endian FLIM isn't savable yet!");
                        }
                        break;
                    default:
                        throw new NotSupportedException($"Unknown image format {sections.Header.magic}");
                }
            }
        }
    }
}
