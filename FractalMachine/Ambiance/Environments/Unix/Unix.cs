using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Ambiance.Environments.Unix
{
    public class Unix : Environment
    {
        public Unix()
        {
            syspath = "/";
            shell = "bin/bash";
        }
    }
}
