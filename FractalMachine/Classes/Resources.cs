using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Used for accessing to messy resources
public static class Resources
{
    static private bool inited = false;
    static private string exDir;
    static private List<string> lookDirs = new List<string>();

    private static void init()
    {
        if (inited) return;

        exDir = System.Reflection.Assembly.GetExecutingAssembly().Location;
        exDir = Path.GetDirectoryName(exDir);

        lookDirs.Add(exDir);

        var projDir = lookForProjectDir();
        if (projDir != null)
            lookDirs.Add(projDir);

        inited = true;
    }

    private static string lookForProjectDir()
    {
        var dir = exDir;

        try
        {
            while (dir.Replace('\\', '/').Contains('/'))
            {
                dir = Path.GetDirectoryName(dir);
                var files = Directory.GetFiles(dir);
                var csproj = files.Where(f => f.EndsWith(".csproj"));

                if (csproj.Count() > 0)
                    return dir;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception, debug");
        }

        return null;
    }

    public static void CreateDirIfNotExists(string dir)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public static int FilesWriteTimeCompare(string file1, string file2)
    {
        if (!File.Exists(file2))
            return 1;

        DateTime f1 = File.GetLastWriteTime(file1);
        DateTime f2 = File.GetLastWriteTime(file2);

        var sec = f1.Subtract(f2).TotalSeconds;

        if (sec > 0) return 1;
        else if (sec < 0) return -1;
        return 0;
    }

    public static bool IsFileReady(string filename)
    {
        // If the file can be opened for exclusive access it means that the file
        // is no longer locked by another process.
        try
        {
            using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                return inputStream.Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string SearchFile(string searchedFile, int maxDepth = -1, int level=0, string path="")
    {
        if(level == 0)
        {
            var drives = System.IO.DriveInfo.GetDrives();
            foreach(var drive in drives)
            {
                if (drive.DriveType == DriveType.Fixed)
                {
                    var res = SearchFile(searchedFile, maxDepth, level + 1, drive.Name);
                    if (!String.IsNullOrEmpty(res)) return res;
                }
            }
        }
        else 
        { 
            DirectoryInfo d = new DirectoryInfo(path);//Assuming Test is your Folder

            if (d.Attributes == FileAttributes.Hidden || d.Attributes == FileAttributes.System || d.Attributes == FileAttributes.Temporary)
                return null;

            try
            {
                FileInfo[] files = d.GetFiles();
                foreach (var file in files)
                {
                    if (file.Name == searchedFile)
                        return file.FullName;
                }

                if (level < maxDepth)
                {
                    DirectoryInfo[] dirs = d.GetDirectories();
                    foreach (var dir in dirs)
                    {
                        var res = SearchFile(searchedFile, maxDepth, level + 1, dir.FullName);
                        if (!String.IsNullOrEmpty(res)) return res;
                    }
                }
            }
            catch { /* eh vabbè */ }
        }

        return null;
    }


    #region Public

    public static string Solve(string Path)
    {
        init();

        foreach (var dir in lookDirs)
        {
            var look = dir + "/" + Path;

            if (Directory.Exists(look) || File.Exists(look))
                return look;
        }

        return null;
    }

    #endregion

    #region Properties

    public static string BinPath
    {
        get
        {
            return exDir;
        }
    }

    #endregion
}

