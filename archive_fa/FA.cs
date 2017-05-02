﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cetera.Hash;
using Kuriimu.Contract;
using Kuriimu.IO;

namespace archive_fa
{
    public sealed class FA
    {
        public List<FAFileInfo> Files = new List<FAFileInfo>();
        Stream _stream = null;

        private Header header;
        private byte[] unk1;
        private byte[] unk2;

        private List<Entry> entries;
        private List<string> fileNames;
        private List<string> dirStruct = new List<string>();
        private List<int> folderCounts = new List<int>();
        private List<uint> hashes;

        public FA(Stream input)
        {
            _stream = input;
            using (BinaryReaderX br = new BinaryReaderX(input, true))
            {
                //Header
                header = br.ReadStruct<Header>();

                //unknown lists
                unk1 = br.ReadBytes(header.offset1 - header.offset0);
                unk2 = br.ReadBytes(header.entryOffset - header.offset1);

                //Entries
                entries = br.ReadMultiple<Entry>(header.entryCount);

                //Names
                br.BaseStream.Position = header.nameOffset;
                br.BaseStream.Position++;

                string currentFolder = "";
                string tmp = br.ReadCStringA();
                fileNames = new List<string>();
                folderCounts.Add(0);

                while (tmp != "" && br.BaseStream.Position < header.dataOffset)
                {
                    if (tmp.Last() == '/')
                    {
                        folderCounts.Add(0);
                        dirStruct.Add(tmp);
                        currentFolder = tmp;
                    }
                    else
                    {
                        dirStruct.Add(currentFolder + tmp);
                        fileNames.Add(currentFolder + tmp);
                        folderCounts[folderCounts.Count - 1] += 1;
                    }

                    tmp = br.ReadCStringA();
                }

                //FileData
                int pos = 0;
                foreach (var folderCount in folderCounts)
                {
                    var tmpFiles = new List<NameEntry>();
                    for (int i = 0; i < folderCount; i++)
                        tmpFiles.Add(new NameEntry {
                            name = fileNames[pos + i],
                            crc32 = Crc32.Create(Encoding.GetEncoding("SJIS").GetBytes(fileNames[pos + i].Split('/').Last().ToLower()))
                        });
                    tmpFiles = tmpFiles.OrderBy(x => x.crc32).ToList();

                    foreach (var nameEntry in tmpFiles)
                    {
                        if (nameEntry.crc32 == entries[pos].crc32)
                            Files.Add(new FAFileInfo
                            {
                                State = ArchiveFileState.Archived,
                                FileName = nameEntry.name,
                                FileData = new SubStream(br.BaseStream, entries[pos].fileOffset + header.dataOffset, entries[pos].fileSize),
                                crc32 = entries[pos++].crc32
                            });
                    }
                }
            }
        }

        public void Save(Stream output)
        {
            using (BinaryWriterX bw = new BinaryWriterX(output))
            {
                bw.BaseStream.Position = 0x48;

                //first unknown half of info section
                bw.Write(unk1);
                bw.Write(unk2);

                //entryList and Data
                uint movDataOffset = (uint)(0x48 + unk1.Length + unk2.Length + Files.Count * 0x10);
                foreach (var name in dirStruct) movDataOffset += 1 + (uint)Encoding.GetEncoding("SJIS").GetBytes((name.Last() != '/') ? name.Split('/').Last() : name).Length;
                while (movDataOffset % 4 != 0) movDataOffset++;

                int pos = 0;
                foreach (var folderCount in folderCounts)
                {
                    var nameSorted = new List<NameEntry>();
                    for (int i = 0; i < folderCount; i++) nameSorted.Add(new NameEntry { name = Files[pos + i].FileName, crc32 = Files[pos + i].crc32, size = (uint)Files[pos + i].FileSize });
                    nameSorted = nameSorted.OrderBy(x => x.name).ToList();

                    var entriesTmp = new List<Entry>();
                    uint nameOffset = 0;
                    for (int i = 0; i < folderCount; i++)
                    {
                        entriesTmp.Add(new Entry { crc32 = Files[pos + i].crc32 });
                    }
                    for (int i = 0; i < folderCount; i++)
                    {
                        var foundEntry = entriesTmp.Find(x => x.crc32 == nameSorted[i].crc32);
                        foundEntry.nameOffsetInFolder = nameOffset;
                        foundEntry.fileOffset = movDataOffset;
                        foundEntry.fileSize = nameSorted[i].size;

                        nameOffset += 1 + (uint)nameSorted[i].name.Length;

                        long bk = bw.BaseStream.Position;
                        bw.BaseStream.Position = movDataOffset;
                        Files.Find(x => x.FileName == nameSorted[i].name).FileData.CopyTo(bw.BaseStream);
                        bw.BaseStream.Position++;
                        while (bw.BaseStream.Position % 4 != 0) bw.BaseStream.Position++;
                        movDataOffset = (uint)bw.BaseStream.Position;
                        bw.BaseStream.Position = bk;
                    }
                    for (int i = 0; i < folderCount; i++)
                    {
                        bw.WriteStruct(entriesTmp[i]);
                    }

                    pos += folderCount;
                }
                

                //nameList
                foreach(var name in dirStruct)
                {
                    bw.Write((byte)0);
                    if (name.Last() != '/')
                        bw.Write(Encoding.GetEncoding("SJIS").GetBytes(name.Split('/').Last().ToLower()));
                    else
                        bw.Write(Encoding.GetEncoding("SJIS").GetBytes(name));
                }
                bw.BaseStream.Position++;
                while (bw.BaseStream.Position % 4 != 0) bw.BaseStream.Position++;

                //Write Header
                bw.BaseStream.Position = 0;
                bw.WriteStruct(header);
            }
        }

        /*public List<string> GetDirNameList()
        {
            List<string> list = new List<string>();
            int pos = 0;
            string lastFolder = "";

            foreach (var file in filenames)
            {
                if (!file.Contains('/'))
                {
                    list.Add(file);
                }
                else
                {
                    bool found = false;

                    while (!found)
                    {
                        if (lastFolder != foldernames[pos])
                        {
                            list.Add(foldernames[pos]);
                        }

                        if (!file.Contains(foldernames[pos])) pos++;
                        else
                            if (pos + 1 < foldernames.Count)
                            if (!file.Contains(foldernames[pos + 1]))
                            {
                                lastFolder = foldernames[pos];
                                found = true;
                            }
                            else pos++;
                        else
                        {
                            lastFolder = foldernames[pos];
                            found = true;
                        }
                    }

                    list.Add(file.Split('/').Last());
                }
            }

            for (int i = pos + 1; i < foldernames.Count; i++)
            {
                list.Add(foldernames[i]);
            }

            return list;
        }*/

        public void Close()
        {
            _stream?.Close();
            _stream = null;
        }
    }
}
