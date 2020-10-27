using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Ambiance.Environments.Windows
{
    public class MSYS2 : Unix.Unix
    {
        public MSYS2 (Environment parent, string path) : base()
        {
            this.parent = parent;
            syspath = path;
        }
    }
}
