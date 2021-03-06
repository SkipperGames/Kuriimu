﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Knit.steps;

namespace Knit
{
    [XmlRoot("patch")]
    public sealed class Patch
    {
        #region Properties

        /// <summary>
        /// The list of steps stored in this patch.xml document.
        /// </summary>
        [XmlArray("steps")]
        [XmlArrayItem("step-delay", typeof(StepDelay))]
        [XmlArrayItem("step-select-file", typeof(StepSelectFile))]
        [XmlArrayItem("step-select-directory", typeof(StepSelectDirectory))]
        [XmlArrayItem("step-verify-file-hash", typeof(StepVerifyFileHash))]
        [XmlArrayItem("step-execute-program", typeof(StepExecuteProgram))]
        [XmlArrayItem("step-options", typeof(StepOptions))]
        // DEBUG
        [XmlArrayItem("step-debug-show-variable", typeof(StepDebugShowVariable))]
        [XmlArrayItem("step-debug-show-variables", typeof(StepDebugShowVariables))]
        public List<Step> Steps { get; set; } = new List<Step>();

        [XmlAttribute("debug")]
        public bool Debug { get; set; } = false;

        #endregion

        /// <summary>
        /// Initializes a new instance of the Patch class that is empty.
        /// </summary>
        public Patch() { }

        /// <summary>
        /// Loads a patch.xml document from disk.
        /// </summary>
        /// <param name="filename">The patch.xml to load.</param>
        /// <returns></returns>
        public static Patch Load(string filename)
        {
            try
            {
                using (var fs = File.OpenRead(filename))
                    return (Patch)new XmlSerializer(typeof(Patch)).Deserialize(XmlReader.Create(fs, new XmlReaderSettings { CheckCharacters = false }));
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Saves the patch.xml document to disk.
        /// </summary>
        /// <param name="filename">The filename to save to.</param>
        public void Save(string filename)
        {
            try
            {
                var xmlSettings = new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    NewLineOnAttributes = false,
                    NewLineHandling = NewLineHandling.Entitize,
                    IndentChars = "	",
                    CheckCharacters = false
                };

                using (var xmlIO = new StreamWriter(filename, false, xmlSettings.Encoding))
                {
                    var serializer = new XmlSerializer(typeof(Patch));
                    var namespaces = new XmlSerializerNamespaces();
                    namespaces.Add(string.Empty, string.Empty);
                    serializer.Serialize(XmlWriter.Create(xmlIO, xmlSettings), this, namespaces);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
