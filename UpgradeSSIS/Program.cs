using System;
using System.IO;
using Microsoft.SqlServer.Dts.Runtime;

class Program
{
    static void Main(string[] args)
    {
        // Define paths
        string sourceDirectory = @"C:\Users\dyego\paths";
        string destinationDirectory = @"C:\Users\dyego\source\repos\2019";
        string logFilePath = @"C:\Users\dyego\source\repos\LogFile.log";

        // Ensure the destination directory exists
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        // Ensure the log file is created/reset
        if (!File.Exists(logFilePath))
        {
            File.Create(logFilePath).Close();
        }
        else
        {
            File.WriteAllText(logFilePath, string.Empty);
        }

        // Function to log a message
        void LogMessage(string message, string type)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"{timestamp} [{type}] {message}\n";
            File.AppendAllText(logFilePath, logEntry);
            Console.ForegroundColor = type == "ERROR" ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine(logEntry);
            Console.ResetColor();
        }

        // Function to upgrade the project file using MSBuild
        void UpgradeSSISProject(string projectFilePath, string destinationProjectPath)
        {
            try
            {
                LogMessage($"Starting upgrade for project: {projectFilePath}", "INFO");

                // Define the path to MSBuild.exe
                string msbuildPath = @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe";

                // Copy the project file to the destination directory
                File.Copy(projectFilePath, destinationProjectPath, true);

                // Execute the command to upgrade the project
                System.Diagnostics.Process.Start(msbuildPath, $"\"{destinationProjectPath}\" /t:Rebuild /p:Configuration=Release").WaitForExit();

                LogMessage($"Successfully upgraded project: {projectFilePath}", "INFO");
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to upgrade project: {projectFilePath}. Exception: {ex}", "ERROR");
            }
        }

        // Function to upgrade an SSIS package
        void UpgradeSSISPackage(string sourcePackagePath, string destinationPackagePath)
        {
            try
            {
                LogMessage($"Starting upgrade for package: {sourcePackagePath}", "INFO");

                // Create an instance of the SSIS Application
                Application app = new Application();

                // Load the package from the source path
                Package package = app.LoadPackage(sourcePackagePath, null);

                // Save and update the package version to SQLServer2019
                app.SaveAndUpdateVersionToXml(destinationPackagePath, package, DTSTargetServerVersion.SQLServer2019, null);

                LogMessage($"Successfully upgraded package: {sourcePackagePath} to {destinationPackagePath}", "INFO");
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to upgrade package: {sourcePackagePath}. Exception: {ex}", "ERROR");
            }
        }

        // Get all .dtproj files in the source directory
        string[] projectFiles = Directory.GetFiles(sourceDirectory, "*.dtproj");

        foreach (string projectFile in projectFiles)
        {
            string destinationProjectPath = Path.Combine(destinationDirectory, Path.GetFileName(projectFile));
            UpgradeSSISProject(projectFile, destinationProjectPath);
        }

        // Get all .dtsx files in the source directory
        string[] packageFiles = Directory.GetFiles(sourceDirectory, "*.dtsx");

        foreach (string packageFile in packageFiles)
        {
            string destinationPackagePath = Path.Combine(destinationDirectory, Path.GetFileName(packageFile));
            UpgradeSSISPackage(packageFile, destinationPackagePath);
        }
    }
}
