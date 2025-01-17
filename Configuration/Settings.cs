using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobRunner.Configuration
{
  [method: SetsRequiredMembers]
  public sealed class Settings()
  {
    public required int ConsoleFontSize { get; set; } = 0;
    public int MaxBatchConcurrentThreads { get; set; } = 1;
    public bool DisplayProcessedMessage { get; set; } = false;
    public string JobRunnerExecutionFilePath { get; set; } = "C:\\Users\\Public\\Documents\\Cts\\Jobrunner";

  }
}
