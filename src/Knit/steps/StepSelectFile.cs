﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Knit.steps
{
    public class StepSelectFile : Step
    {
        // Properties
        [XmlAttribute("openFileTitle")]
        public string OpenFileTitle { get; set; } = "Select a file...";

        [XmlAttribute("openFileFilter")]
        public string OpenFileFilter { get; set; } = "All files (*.*)|*.*";

        // Methods
        public override async Task<StepResults> Perform(Dictionary<string, object> variableCache, IProgress<ProgressReport> progress)
        {
            var progressReport = new ProgressReport();
            var stepResults = new StepResults();

            if (Variable == string.Empty)
            {
                stepResults.Status = StepStatus.Error;
                stepResults.Message = $"{nameof(StepSelectFile)} requires a variable but none was provided.";
            }

            if (stepResults.Status == StepStatus.Success)
                try
                {
                    var ofd = new OpenFileDialog
                    {
                        Title = Common.ProcessVariableTokens(OpenFileTitle, variableCache),
                        Filter = OpenFileFilter
                    };

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        variableCache[Variable] = ofd.FileName;
                        progressReport.Percentage = 100;
                        stepResults.Message = $"File selected: \"{ofd.FileName}\".";
                    }
                    else
                    {
                        stepResults.Status = StepStatus.Cancel;
                        stepResults.Message = "File selection cancelled.";
                    }
                }
                catch (Exception ex)
                {
                    stepResults.Status = StepStatus.Error;
                    stepResults.Message = ex.ToString();
                }

            progress.Report(progressReport);
            return stepResults;
        }
    }
}
