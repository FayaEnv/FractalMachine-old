using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Ambiance.Environments.Windows
{
    class Windows : Environment
    {
        public Windows()
        {
            compiler = new Compilers.MSVC(this);
            shell = "cmd.exe";

            checkMSYS2();

            if (SelectSubsystem("msys2") == null)
                throw new Exception("In this moment Windows environmnet is not supported without MSYS2");
        }

        #region Subsystems

        public void checkMSYS2()
        {         
            var path = Resources.SearchFile("msys2.exe", "");

            if(String.IsNullOrEmpty(path)) 
                path = Resources.SearchFile("msys2.exe");

            if (!String.IsNullOrEmpty(path))
            {
                var subsys = new MSYS2(this, path);
                subsystems.Add("msys2", subsys);
            }
            else
            {
                //todo: do you want install it?
            }
        }

        #endregion

    }
}
