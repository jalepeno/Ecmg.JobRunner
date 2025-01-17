// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");


//Build a config object, using env vars and JSON providers.
using Microsoft.Extensions.Configuration;
using JRConfig = JobRunner.Configuration;
using JobRunner;
using Spectre.Console;
using Documents.Configuration;
using Documents.Utilities;
using System.Reflection;

try
{
  int argumentCount = args.Length;

  if ((argumentCount == 0) || (argumentCount == 1 && String.IsNullOrEmpty(args[0])))
  {
    DisplayUsage(IssueEnum.MissingConfigFile);
    return;
  }

  string execConfigFile = args[0];

  JRConfig.Settings? _jrSettings;
  string basePath = Directory.GetCurrentDirectory();

  IConfigurationRoot jobRunnerConfig = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("jobrunnersettings.json")
    .AddEnvironmentVariables()
    .Build();

  // Get values from the config given their key and their target type.
  _jrSettings = jobRunnerConfig.GetRequiredSection("Settings").Get<JRConfig.Settings>();

  JobRunner.JobRunner jobRunner = new(_jrSettings, execConfigFile);

  jobRunner.Run();

}
catch (Exception ex)
{
  ApplicationLogging.LogException(ex, MethodBase.GetCurrentMethod());
  AnsiConsole.WriteException(ex);
  Console.ReadKey();
}

static void DisplayUsage(IssueEnum issue, Exception? exception = null)
{
  ConsoleUtils.DisplayAppHeader();
  AnsiConsole.WriteLine();

  switch (issue)
  {
    case IssueEnum.MissingExecutionFilePath:
      ConsoleUtils.ErrorLine("Value not specified for 'ExecutionFilePath' in the application config file.");
      ConsoleUtils.ErrorInstructions("Please supply a value indicating the folder for the ecf files.");
      break;

    case IssueEnum.MissingConfigFile:
      ConsoleUtils.ErrorLine("You must pass a file name on the command line.");
      //ConsoleUtils.ErrorInstructions($"The file needs to be located in this folder: {ConnectionSettings.Instance.JobRunnerExecutionFilePath}");
      break;

    case IssueEnum.ExceptionOccured:
      if (exception != null)
      {
        AnsiConsole.WriteException(exception, ExceptionFormats.Default);
      }
      else
      {
        ConsoleUtils.ErrorLine("An unspecified exception occured.");
      }
      break;

    default:
      break;
  }

}

#region "Enumerations"

enum IssueEnum
{
  None = 0,
  MissingExecutionFilePath = 1,
  MissingConfigFile = 2,
  ExceptionOccured = 3
}

#endregion