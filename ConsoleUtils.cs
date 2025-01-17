using Documents.Configuration;
using Microsoft.Identity.Client;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JobRunner
{
  public static  class ConsoleUtils
  {

    //public static void DisplayUsage(IssueEnum issue, Exception? exception = null)
    //{
    //  DisplayAppHeader();
    //  AnsiConsole.WriteLine();

    //  switch (issue)
    //  {
    //    case IssueEnum.MissingExecutionFilePath:
    //      ErrorLine("Value not specified for 'ExecutionFilePath' in the application config file.");
    //      ErrorInstructions("Please supply a value indicating the folder for the ecf files.");
    //      break;

    //    case IssueEnum.MissingConfigFile:
    //      ErrorLine("You must pass a file name on the command line.");
    //      ErrorInstructions($"The file needs to be located in this folder: {ConnectionSettings.Instance.JobRunnerExecutionFilePath}");
    //      break;

    //    case IssueEnum.ExceptionOccured:
    //      if (exception != null)
    //      {
    //        AnsiConsole.WriteException(exception, ExceptionFormats.Default);
    //      }
    //      else
    //      {
    //        ErrorLine("An unspecified exception occured.");
    //      }
    //      break;

    //    default:
    //      break;
    //  }

    //}

    public static void DisplayAppHeader()
    {
      AnsiConsole.Clear();
      string appLabelPart1 = $"JobRunner {Assembly.GetEntryAssembly().GetName().Version}";
      string appLabelPart21 = $"Copyright 2012-{DateTime.Today.Year} Conteage Corp";

      //AnsiConsole.Markup("[green]{appLabelPart1}[/]");
      AnsiConsole.Write(new Columns(
        new Text(appLabelPart1, new Style(Color.Green)).LeftJustified(),
        new Text(appLabelPart21, new Style(Color.White)).RightJustified()
        ));

      AnsiConsole.Write(new Rule());

    }

    //public static Progress InitializeProgress(string jobName, int maxValue, int initialCount, Task workTask, out ProgressTask progressTask)
    //{
    //  Progress progress = AnsiConsole.Progress()
    //    .AutoRefresh(false)
    //    .AutoClear(false)
    //    .HideCompleted(false)        
    //    .Columns(new ProgressColumn[]
    //    {
    //      new TaskDescriptionColumn(),
    //      new ProgressBarColumn(),
    //      new PercentageColumn(),
    //      new RemainingTimeColumn(),
    //      new SpinnerColumn()
    //    });

    //  ProgressTask proxyTask = null;

    //  progress
    //    .StartAsync(async ctx =>
    //    {
    //      await Task.Run(async item =>
    //      {
    //        var proxyTask = ctx.AddTask($"[green]{jobName}[/]", new ProgressTaskSettings { AutoStart = false, MaxValue = maxValue});
    //        proxyTask.Value = initialCount;
    //      }
    //      );
    //      await (await workTask)(CancellationToken.None);
    //    });

    //  progressTask = proxyTask;

    //  return progress;

    //}

    public static void DisplayAppHeader(string projectInfo, int batchCount)
    {
      AnsiConsole.Clear();
      string appLabelPart1 = $"JobRunner {Assembly.GetEntryAssembly().GetName().Version}";
      string appLabelPart21 = $"Copyright 2012-{DateTime.Today.Year} Conteage Corp";

      //AnsiConsole.Markup("[green]{appLabelPart1}[/]");
      AnsiConsole.Write(new Columns(
        new Text(appLabelPart1, new Style(Color.Green)).LeftJustified(),
        new Text(appLabelPart21, new Style(Color.White)).RightJustified()
        ));

      StringBuilder batchStringBuilder = new();
      if (batchCount == 1)
      {
        batchStringBuilder.Append("1 Batch");
      }
      else
      {
        batchStringBuilder.Append($"{batchCount} Batches");
      }

      AnsiConsole.Write(new Columns(
  new Text(projectInfo, new Style(Color.Yellow)).LeftJustified(),
  new Text(batchStringBuilder.ToString(), new Style(Color.White)).RightJustified()
  ));

      AnsiConsole.Write(new Rule());
    }

    public static void ErrorLine(string errorMessage)
    { AnsiConsole.MarkupLineInterpolated($"[red]{errorMessage}[/]"); }

    public static void SuccessLine(string errorMessage)
    { AnsiConsole.MarkupLineInterpolated($"[darkgreen]{errorMessage}[/]"); }

    public static void ErrorInstructions(string errorInstructions)
    { AnsiConsole.MarkupLineInterpolated($"[italic yellow]{errorInstructions}[/]"); }

    public static void WarningLine(string warningMessage)
    {
      AnsiConsole.MarkupLineInterpolated($"[italic yellow]{warningMessage}[/]");
    }

    public static void InfoLine(string infoMessage)
    {
      AnsiConsole.MarkupLineInterpolated($"{infoMessage}");
    }

  }
}
