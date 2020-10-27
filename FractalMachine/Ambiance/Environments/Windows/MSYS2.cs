using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Ambiance.Environments.Windows
{
    public class MSYS2 : Unix.Unix
    {
        public MSYS2 (Windows parent, string path) : base()
        {
            this.parent = parent;
            sysPath = path;
            binPath = "usr/bin/";
            exeFormat = parent.exeFormat;

            compiler = new Compilers.GCC(this);
        }

        public override string Path(string Path)
        {
            Path = "/" + System.IO.Path.GetFullPath(Path);
            Path = Path.Replace('\\', '/').Replace(":", "");
            return Path;
        }

        // repo https://repo.msys2.org/msys/x86_64/
    }
}
