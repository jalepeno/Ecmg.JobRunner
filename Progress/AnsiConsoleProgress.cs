using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressSpace
{
  public class AnsiConsoleProgress
  {
    private readonly ProgressTask _task1;
    private readonly ProgressTask? _task2;

    private readonly object _consoleLock = new();

    public AnsiConsoleProgress(ProgressTask task)
    {
      _task1 = task;
    }
    public AnsiConsoleProgress(ProgressTask task1, ProgressTask task2)
    {
      _task1 = task1;
      _task2 = task2;
    }

    public void ReportProgress(ProgressInfo info)
    {
      lock (_consoleLock)
      {
        //_task1.Increment(info.Task1Increase);
        _task1.Value = info.Task1Value;
        _task2?.Increment(info.Task2Increase);
      }
    }
  }
}
