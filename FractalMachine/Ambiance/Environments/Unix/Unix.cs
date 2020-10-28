using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Ambiance.Environments.Unix
{
    public class Unix : Environment
    {
        public Unix()
        {
            sysPath = "/";
            binPath = ""; // because it yet has environment PATH
            shell = "bash";
        }

        public override char PathChar
        {
            get { return '/'; }
        }
    }
}
