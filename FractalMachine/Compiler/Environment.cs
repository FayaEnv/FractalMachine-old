using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Compiler
{
    public class Environment
    {
        #region Static

        static Environment current;
        public static Environment GetEnvironment
        {
            get
            {
                if (current == null)
                    current = new Environment();

                return current;
            }
        }

        #endregion

        public Environment()
        {
            var osVersion = System.Environment.OSVersion;
        }
    }
}
