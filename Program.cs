using System;
using System.IO;
using System.Security.Cryptography;
using System.Timers;

class Program
{
    static void Main()
    {
        // Get user input
        Console.Write("Enter source folder path: ");
        string sourcePath = Console.ReadLine();

        Console.Write("Enter replica folder path (new folder will be created if it does not exist): ");
        string replicaPath = Console.ReadLine();

        Console.Write("Enter log file path (new file will be created if it does not exist): ");
        string logFilePath = Console.ReadLine();

        Console.WriteLine("Enter the synchronization interval in seconds:");
        int syncInterval = int.Parse(Console.ReadLine());

        //Check if paths are valid or not
        if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(replicaPath))
        {
            Console.WriteLine("Invalid folder paths. Please provide valid paths.");
            return;
        }

        //Run synchronization
        FolderSynchronizer synchronizer = new FolderSynchronizer(sourcePath, replicaPath, syncInterval, logFilePath);
        synchronizer.StartSynchronization();


        Console.ReadLine();// Keep console open
    }
}


   

    


class FolderSynchronizer
{
    private string sourcePath;
    private string replicaPath;
    static string logFilePath;
    private Timer syncTimer;

    public FolderSynchronizer(string source, string replica, int syncInterval, string logFile)
    {
        sourcePath = source;
        replicaPath = replica;
        logFilePath = logFile;
        syncTimer = new Timer();
        syncTimer.Interval = TimeSpan.FromSeconds(syncInterval).TotalMilliseconds; // Set synchronization interval 
        syncTimer.Elapsed += SyncTimerElapsed;
    }

    //Start and Stop program
    public void StartSynchronization()
    {
        syncTimer.Start();
        Console.WriteLine("Periodic synchronization started. Press any key to stop.");
        Console.ReadKey();
        syncTimer.Stop();
        Console.WriteLine("Periodic synchronization stopped.");
    }

    private void SyncTimerElapsed(object sender, ElapsedEventArgs e)
    {
        LogAction("");
        SyncFolders();
        Console.WriteLine("Folders synchronized at " + DateTime.Now);
    }

    public void SyncFolders()
    {
        try { 
            // Check if replica folder exists, create if not
            if (!Directory.Exists(replicaPath))
            {
                Directory.CreateDirectory(replicaPath);
            }


            // Synchronize folders using MD5 hashes
            SynchronizeFolders(sourcePath, replicaPath);

            
            }
            catch (Exception ex)
            {
            Console.WriteLine("Error synchronizing folders: " + ex.Message);
        }
    }

       

    static void SynchronizeFolders(string sourcePath, string replicaPath)
    {
        
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            if (!Directory.Exists(dirPath.Replace(sourcePath, replicaPath)))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, replicaPath));
                LogAction($"Directory created: {dirPath.Replace(sourcePath, replicaPath)}"); ;
            }
            
            
        }

        string[] sourceFiles = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
        string[] replicaFiles = Directory.GetFiles(replicaPath, "*.*", SearchOption.AllDirectories);

        foreach (string sourceFile in sourceFiles)
        {
            string relativePath = sourceFile.Replace(sourcePath, "");

            string replicaFile = Path.Join(replicaPath, relativePath);

            if (File.Exists(replicaFile))
            {
                string sourceHash = GetMD5HashFromFile(sourceFile);
                string replicaHash = GetMD5HashFromFile(replicaFile);

                if (sourceHash != replicaHash)
                {
                    File.Copy(sourceFile, replicaFile, true);
                    LogAction($"File overwriting: {sourceFile} to {replicaFile}");
                }
            }
            else
            {
                File.Copy(sourceFile, replicaFile);
                LogAction($"File copied: {sourceFile} to {replicaFile}");
            }
        }

        // Delete files in replica not present in source
        foreach (string replicaFile in replicaFiles)
        {
            string relativePath = replicaFile.Replace(replicaPath, "");
            string sourceFile = Path.Join(sourcePath, relativePath);

            if (!File.Exists(sourceFile))
            {
                File.Delete(replicaFile);
                LogAction($"File deleted from replica: {replicaFile}");
            }
        }

        // Delete folders in replica not present in source
        string[] replicaDirs = Directory.GetDirectories(replicaPath, "*", SearchOption.AllDirectories);

        foreach (string replicaDir in replicaDirs)
        {
            string relativePath = replicaDir.Replace(replicaPath, "");
            string sourceDir = Path.Join(sourcePath, relativePath);

            if (!Directory.Exists(sourceDir))
            {
                Directory.Delete(replicaDir, true);
                LogAction($"Directory deleted from replica: {replicaDir}");
            }
        }

    }

    static string GetMD5HashFromFile(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    static void LogAction(string message)
    {
        Console.WriteLine(message);

        if (!File.Exists(logFilePath))
        {
            // Create the log file if it doesn't exist
            using (StreamWriter sw = File.CreateText(logFilePath))
            {
                sw.WriteLine($"{DateTime.Now}: Log file created.");
                Console.WriteLine($"{DateTime.Now}: Log file created.");
            }
        }
        
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}\n");
    }

}