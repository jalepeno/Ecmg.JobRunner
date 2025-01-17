
#region Using

using Documents.Exceptions;
using Operations;
using Projects;
using JRConfig = JobRunner.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Documents.Utilities;
using Projects.Configuration;
using Microsoft.Data.SqlClient;
using System.Configuration;
using ProgressSpace;

#endregion

namespace JobRunner
{
  public class JobRunner
  {

    #region Class Constants

    const int FIRST_AVAILABLE_ROW = 3;
    const int FIRST_MESSAGE_ROW = FIRST_AVAILABLE_ROW + 3;

    #endregion

    #region Class Variables

    Project _project;
    Job _currentJob;
    string _currentProjectName = string.Empty;
    string _currentJobName = string.Empty;
    int _currentConsoleRow;
    int _maxThreads;
    ProgressTask _progressTask;
    IProgress<ProgressInfo> _progressMonitor;
    long _contentCounter = 1;
    private readonly JRConfig.Settings? _jrSettings;
    private readonly object _jrSettingsLock = new();
    private readonly string _execConfigFileName = string.Empty;

    #endregion

    #region Public Properties


    #endregion

    #region Constructors

    public JobRunner(JRConfig.Settings settings, string executionFileName)
    {    
      _jrSettings = settings;
      _maxThreads = _jrSettings.MaxBatchConcurrentThreads;
      _execConfigFileName = executionFileName;
    }

    #endregion

    #region Public Methods

    public void Run()
    {
      try
      {

        Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);

        //System.Console.SetCursorPosition(0, GetNextCursorRow());
        DisplayAppHeader();

        // Get the Execution File Path
        string execConfigPath = _jrSettings.JobRunnerExecutionFilePath;

        //  Get max threads
        _maxThreads = _jrSettings.MaxBatchConcurrentThreads;

        string executionConfigurationFilePath = Path.Combine(_jrSettings.JobRunnerExecutionFilePath, _execConfigFileName);

        if (!File.Exists(executionConfigurationFilePath)) { throw new FileDoesNotExistException(executionConfigurationFilePath); }

        ExecutionConfiguration executionConfiguration = ExecutionConfiguration.Open(executionConfigurationFilePath);

        string errorMessage = string.Empty;

        foreach (JobSet jobSet in executionConfiguration.JobSets)
        {
          //AnsiConsole.MarkupInterpolated($"[italic]Loading Project [[{jobSet.ProjectLocation.DatabaseName}[/]");
          //AnsiConsole.WriteLine();

          try
          {
            _project = Project.OpenProject(jobSet.ProjectLocation, ref errorMessage);
            _currentProjectName = _project.Name;

            _project.BatchStarted += Project_BatchStarted;
            _project.BatchItemCompleted += Project_BatchItemCompleted;
            _project.BatchCompleted += Project_BatchCompleted;

          }
          catch (SqlException sqlEx)
          {
            ApplicationLogging.LogException(sqlEx, MethodBase.GetCurrentMethod());
            //AnsiConsole.WriteException(sqlEx);
            ConsoleUtils.ErrorLine(sqlEx.Message);
            if (sqlEx.Message.Contains("network"))
            {
              errorMessage = $"Please check that the project database '{jobSet.ProjectLocation.DatabaseName}' is available at '{jobSet.ProjectLocation.ServerName}'.";
              ConsoleUtils.WarningLine(errorMessage);
            }
            AnsiConsole.MarkupLine("[bold red]Failed To Load Project![/]");
            continue;
          }
          catch (Exception ex)
          {
            ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
            AnsiConsole.WriteException(ex);
            //AnsiConsole.MarkupLine("[bold red]Failed To Load Project![/]");
            ConsoleUtils.ErrorLine("Failed To Load Project!");
            continue;
          }

          long itemCount;

          foreach (string jobName in jobSet.Jobs)
          {
            try
            {
              _currentJobName = jobName;
              //ConsoleUtils.InfoLine($"Running Job [{jobName}] in Project [{_currentProjectName}] with a maximum of {_maxThreads} concurrent threads.");

              _currentJob = _project.Jobs.GetItemByName(jobName);

              if (_currentJob.WorkSummary == null)
              {
                _currentJob.GetWorkSummaryCounts();
              }

              itemCount = _currentJob.WorkSummary.TotalItemsCount;

              //DisplayAppHeader();

              //  This call bopth initializes the progress bar and starts the job
              //_progress = InitializeProgress(jobName, itemCount, _currentJob.WorkSummary.ProcessedCount);
              long processedCount;
              if (itemCount > _currentJob.WorkSummary.ProcessedCount)
              {
                processedCount = _currentJob.WorkSummary.ProcessedCount;
              }
              else
              {
                processedCount = 0;
              }

              Task runJobTask = StartJob(jobName, itemCount, processedCount);

              switch (runJobTask.Status)
              {
              case TaskStatus.Running:
                {
                  break;
                  ConsoleUtils.WarningLine($"Job '{jobName}' task is already running...");
                }
              case TaskStatus.RanToCompletion:
                {
                  ConsoleUtils.WarningLine($"Job '{jobName}' completed...");
                  break; 
                }

              case TaskStatus.Created:
              case TaskStatus.WaitingToRun:
              {
                runJobTask.Start();
                break;
              }

              default:
                break;
              }

              

              //if (_contentCounter > 0) { _progressTask.Value = _currentJob.WorkSummary.ProcessedCount; }

              //_currentConsoleRow = FIRST_MESSAGE_ROW;

              //System.Console.SetCursorPosition(0, _currentConsoleRow);

              //_project.RunJob(jobName, _maxThreads);

            }
            catch (Exception ex)
            {
              ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
              AnsiConsole.WriteException(ex);
            }
          }
        }

      }
      catch (Exception ex) 
      {
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.WriteException(ex);
        throw;
      }
    }

    public async Task StartJob(string jobName, double maxValue, double initialCount)
    {
      //Progress progress = AnsiConsole.Progress()
      //  .AutoRefresh(false)
      //  .AutoClear(false)
      //  .HideCompleted(false)
      //  .Columns(new ProgressColumn[]
      //  {
      //    new TaskDescriptionColumn(),
      //    new ProgressBarColumn(),
      //    new PercentageColumn(),
      //    new RemainingTimeColumn(),
      //    new SpinnerColumn()
      //  });

      //ProgressTask proxyTask = null;

      //  ProgressContext progressContext = {
      //      var task = ctx.AddTask($"[green]{jobName}[/]", new ProgressTaskSettings { AutoStart = false, MaxValue = maxValue });
      //  progressTask = task;
      //  await RunJob(_project, jobName);
      //});
      try
      {
        await AnsiConsole.Progress()
          .Columns(
          [
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new RemainingTimeColumn(),
            new SpinnerColumn(Spinner.Known.SimpleDotsScrolling) //,
            //new DownloadedColumn(),
            //new TransferSpeedColumn()
          ])
          .StartAsync(async ctx =>
          {
            //  Define task
            var task = ctx.AddTask($"[green]{jobName}[/]", new ProgressTaskSettings { AutoStart = true, MaxValue = maxValue });
            task.Value = initialCount;

            AnsiConsoleProgress ansiProgress = new(task);
            var progress2 = new Progress<ProgressInfo>(ansiProgress.ReportProgress);

            await RunJob(_project, jobName, progress2);
          });
      }
      catch (Exception ex)
      {
        // An error occured
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.MarkupLine($"[red]Error:[/] {ex}");
      }
      //return progress;

    }

    async Task RunJob(Project project, string jobName, IProgress<ProgressInfo> progress)
    {
      try
      {
        _progressMonitor = progress;
        project.RunJob(jobName, _maxThreads);
      }
      catch (Exception ex)
      {
        // An error occured
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.MarkupLine($"[red]Error:[/] {ex}");
      }
    }

    async Task RunJob(Project project, string jobName, ProgressTask progressTask)
    {
      try
      {
        _progressTask = progressTask;
        project.RunJob(jobName, _maxThreads);
      }
      catch (Exception ex)
      {
        // An error occured
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.MarkupLine($"[red]Error:[/] {ex}");
      }
    }

    #endregion

    #region Private Methods

    private void Cancel(string reason)
    {
      try
      {
        ConsoleUtils.WarningLine(reason);

        foreach (Job runningJob in _project.RunningJobs)
        {
          runningJob.CancelJob(reason);
          Thread.Sleep(10000);
        }
      }
      catch (Exception ex)
      {
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.WriteException(ex);
        throw;
      }
    }

    private int GetNextCursorRow()
    {
      try
      {
        int nextCursorRow = 0;

        if (_currentConsoleRow >= System.Console.WindowHeight - 2)
        {
          lock (_jrSettingsLock)
          {
            DisplayAppHeader();
            if (_currentJob.WorkSummary != null)
            {
              //_progressTask.Value += _contentCounter;
              nextCursorRow = FIRST_MESSAGE_ROW;
            }
          }
        }
        else
        {
          nextCursorRow = _currentConsoleRow + 1;
        }

        _currentConsoleRow = nextCursorRow;

        return nextCursorRow;

      }
      catch (Exception ex)
      {
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.WriteException(ex);
        throw;
      }
    }

    private void DisplayAppHeader()
    {
      ConsoleUtils.DisplayAppHeader(GetCurrentScope(), _maxThreads);
      _currentConsoleRow = 4;
    }

    private string BuildOutputMessage(BatchItemProcessedEventArgs batchItemProcessedEventArgs, BatchWorker worker)
    {
      try
      {

        int batchCount = worker.Batch.Job.BatchCount;

        StringBuilder outputMessageBuilder = new();

        IWorkItem workItem = null;
        if (batchItemProcessedEventArgs.WorkItem != null)
        {
          workItem = batchItemProcessedEventArgs.WorkItem;

          switch (batchCount)
          {
            case < 10:
              {
                outputMessageBuilder.AppendFormat("Batch {0} - {1} ({2})",
                  worker.Batch.Number,
                  workItem.SourceDocId,
                  workItem.ProcessedStatus.ToString());
                break;
              }
            case < 100:
              {
                outputMessageBuilder.AppendFormat("Batch {0,2} - {1} ({2})",
                  worker.Batch.Number,
                  workItem.SourceDocId,
                  workItem.ProcessedStatus.ToString());
                break;
              }
            default:
              {
                outputMessageBuilder.AppendFormat("Batch {0,3} - {1} ({2})",
                  worker.Batch.Number,
                  workItem.SourceDocId,
                  workItem.ProcessedStatus.ToString());
                break;
              }
          }

          if (_jrSettings.DisplayProcessedMessage && workItem.ProcessedStatus == OperationEnumerations.ProcessedStatus.Failed)
          {
            outputMessageBuilder.Append($": {workItem.ProcessedMessage}");
          }

        }
        else
        {
          outputMessageBuilder.AppendFormat("Completed batch item - {0} - Success:{1}    Failed:{2}    Not Processed:{3}",
                            worker.Batch.Name,
                            batchItemProcessedEventArgs.WorkSummary.SuccessCount.ToString(),
                            batchItemProcessedEventArgs.WorkSummary.FailedCount.ToString(),
                            batchItemProcessedEventArgs.WorkSummary.NotProcessedCount.ToString());
        }

        return outputMessageBuilder.ToString();

      }
      catch (Exception ex)
      {
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.WriteException(ex);
        return string.Empty;
      }
    }

    private string GetCurrentScope()
    {
      StringBuilder stringBuilder = new();

      stringBuilder.Append(_currentProjectName);

      if (!string.IsNullOrEmpty(_currentJobName))
      {
        stringBuilder.Append($" - {_currentJobName}");
      }

      return stringBuilder.ToString();

    }

    #region Event Handlers

    void myHandler(object sender, ConsoleCancelEventArgs e)
    {
      try
      {
        if (e.Cancel) { Cancel("Closing!"); }
      }
      catch (Exception ex)
      {
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.WriteException(ex);
        throw;
      }
    }

    void Project_BatchStarted(object sender, ref object e)
    {
      try
      {
        Batch batch = (Batch)e;
        ConsoleUtils.InfoLine(batch.Name);
      }
      catch (Exception ex)
      {
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.WriteException(ex);
        throw;
      }
    }

    void Project_BatchItemCompleted(object sender, ref object e)
    {
      try
      {
        BatchItemProcessedEventArgs batchItemProcessedEventArgs = (BatchItemProcessedEventArgs)e;
        BatchWorker worker = (BatchWorker)sender;

        IWorkItem workItem = null;

        string outputMessage = BuildOutputMessage(batchItemProcessedEventArgs, worker);

        if (batchItemProcessedEventArgs.WorkItem != null)
        {
          workItem = batchItemProcessedEventArgs.WorkItem;
        }

        lock (_jrSettingsLock)
        {
          if (_currentJob.WorkSummary != null)
          {
            _contentCounter = _currentJob.WorkSummary.ProcessedCount;
            //_progress.WorkSummary = _currentJob.WorkSummary;
          }
          else
          {
            _contentCounter = 1;
          }

          //_progressTask.Value = _contentCounter;
          //_progressTask.Increment(1);

          //_progressMonitor.Report(new ProgressInfo { Task1Increase = 1 });
          _progressMonitor.Report(new ProgressInfo { Task1Value = _contentCounter });

          System.Console.SetCursorPosition(0, GetNextCursorRow());

          if (workItem != null && workItem.ProcessedStatus == OperationEnumerations.ProcessedStatus.Failed)
          {
            ConsoleUtils.ErrorLine(outputMessage);
          }
          else
          {
            ConsoleUtils.SuccessLine(outputMessage);
          }
        }

      }
      catch (Exception ex)
      {
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.WriteException(ex);
        throw;
      }
    }

    void Project_BatchCompleted(object sender, ref object e)
    {
      try
      {
        Batch batch = (Batch)e;
        ConsoleUtils.InfoLine($"Completed Batch - {batch.Name}");
      }
      catch (Exception ex)
      {
        ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
        AnsiConsole.WriteException(ex);
        throw;
      }
    }

    #endregion

    #endregion

  }
}
