﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using Kontract.Interface;
using Komponent.IO;

namespace archive_nintendo.GAR
{
    [FilePluginMetadata(Name = "GAR", Description = "Ganondorf ARchive", Extension = "*.gar", Author = "onepiecefreak", About = "This is the GAR archive manager for Karameru.")]
    [Export(typeof(IArchiveManager))]
    public class GarManager : IArchiveManager
    {
        private GAR _gar = null;

        #region Properties
        // Feature Support
        public bool FileHasExtendedProperties => false;
        public bool CanAddFiles => false;
        public bool CanRenameFiles => false;
        public bool CanReplaceFiles => true;
        public bool CanDeleteFiles => false;
        public bool CanSave => true;
        public bool CanCreateNew => false;

        public FileInfo FileInfo { get; set; }

        #endregion

        public Identification Identify(Stream stream, string filename)
        {
            using (var br = new BinaryReaderX(stream, true))
            {
                if (br.BaseStream.Length < 3) return Identification.False;
                if (br.ReadString(3) == "GAR") return Identification.True;
            }

            return Identification.False;
        }

        public void Load(string filename)
        {
            FileInfo = new FileInfo(filename);

            if (FileInfo.Exists)
                _gar = new GAR(FileInfo.OpenRead());
        }

        public void Save(string filename = "")
        {
            if (!string.IsNullOrEmpty(filename))
                FileInfo = new FileInfo(filename);

            // Save As...
            if (!string.IsNullOrEmpty(filename))
            {
                _gar.Save(FileInfo.Create());
                _gar.Close();
            }
            else
            {
                // Create the temp file
                _gar.Save(File.Create(FileInfo.FullName + ".tmp"));
                _gar.Close();
                // Delete the original
                FileInfo.Delete();
                // Rename the temporary file
                File.Move(FileInfo.FullName + ".tmp", FileInfo.FullName);
            }

            // Reload the new file to make sure everything is in order
            Load(FileInfo.FullName);
        }

        public void New()
        {

        }

        public void Unload()
        {
            _gar?.Close();
        }

        // Files
        public IEnumerable<ArchiveFileInfo> Files => _gar.Files;

        public bool AddFile(ArchiveFileInfo afi) => false;

        public bool DeleteFile(ArchiveFileInfo afi) => false;

        // Features
        public bool ShowProperties(Icon icon) => false;
    }
}
