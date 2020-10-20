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

    public static void CreateDirIfNotExists(string dir)
    {

    }

    public static bool InBinPath(string Path)
    {
        return Path.StartsWith(exDir);
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

