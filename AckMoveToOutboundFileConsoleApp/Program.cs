using System;
using System.Configuration;
using System.IO;

namespace FileMoverApp
{
    class Program
    {
        private static string sourceFolder;
        private static string destinationFolder;
        private static string logFilePath;

        static void Main(string[] args)
        {
            // Read the paths from App.config
            sourceFolder = ConfigurationManager.AppSettings["SourceFolder"];
            destinationFolder = ConfigurationManager.AppSettings["DestinationFolder"];
            logFilePath = ConfigurationManager.AppSettings["LogFilePath"];

            // Check if the paths are not null or empty
            if (string.IsNullOrEmpty(sourceFolder) || string.IsNullOrEmpty(destinationFolder) || string.IsNullOrEmpty(logFilePath))
            {
                Console.WriteLine("Source, Destination, or Log file paths are not set in the App.config.");
                return;
            }

            FileSystemWatcher watcher = new FileSystemWatcher
            {
                Path = sourceFolder,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = "*.*"
            };

            // Event handlers for when a file is created
            watcher.Created += OnNewFileDetected;

            // Start watching the folder
            watcher.EnableRaisingEvents = true;

            Console.WriteLine($"Watching folder: {sourceFolder}");
            Console.WriteLine("Press 'q' to quit the application.");

            // Keep the application running until the user quits
            while (Console.Read() != 'q') ;
        }

        private static void OnNewFileDetected(object source, FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath);
            string destFile = Path.Combine(destinationFolder, fileName);

            try
            {
                // Wait for the file to be fully written
                WaitForFile(e.FullPath);

                // If the file already exists, rename the file
                int count = 1;
                string fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                string newDestFile = destFile;

                while (File.Exists(newDestFile))
                {
                    string tempFileName = $"{fileNameOnly} ({count++}){extension}";
                    newDestFile = Path.Combine(destinationFolder, tempFileName);
                }

                // Move (cut) the file to the destination folder
                File.Move(e.FullPath, newDestFile);

                // Log the successful file move
                Log($"Successfully moved file: {fileName} to {newDestFile}");
            }
            catch (Exception ex)
            {
                // Log any errors that occur
                Log($"Error moving file: {fileName}. Exception: {ex.Message}");
            }
        }

        private static void WaitForFile(string fullPath)
        {
            int retryCount = 10;
            while (retryCount > 0)
            {
                try
                {
                    using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        if (stream.Length > 0)
                        {
                            break;
                        }
                    }
                }
                catch (IOException)
                {
                    retryCount--;
                    System.Threading.Thread.Sleep(500);  // Wait a bit before retrying
                }
            }
        }

        private static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("M/d/yyyy h:mm:ss tt");
            string logEntry = $"-------------{timestamp}:----------------\n\n {message}\n\n-------------{timestamp}:----------------\n";

            try
            {
                // Write to console
                Console.WriteLine(logEntry);

                // Write to log file
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging message: {ex.Message}");
            }
        }

    }
}
