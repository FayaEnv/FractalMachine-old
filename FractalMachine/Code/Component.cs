using FractalMachine.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachine.Code
{
    public class Component
    {
        Linear linear;
        Machine machine;
        public Dictionary<string, Component> components = new Dictionary<string, Component>();

        public Component(Machine machine, Linear linear)
        {
            this.machine = machine;
            this.linear = linear;
        }

        public void ReadLinear()
        {
            foreach(var instr in linear.Instructions)
            {
                switch (instr.Op)
                {
                    case "import":
                        Import(instr.Attributes[0]);
                        break;

                    case "function":
                        components.Add(instr.Name, new Component(machine, instr));
                        break;
                }

            }
           
        }

        #region Import

        public void Import(string ToImport)
        {
            if (ToImport.HasMark())
            {
                // Is file
                // ToImport.HasStringMark() || (angularBrackets = ToImport.HasAngularBracketMark())
                var dir = machine.libsDir+"/"+ToImport.NoMark();
                importFileIntoComponent(dir);
            }
            else
            {
                // Is namespace
                var dir = findNamespaceDirectory(ToImport);
                dir = machine.libsDir + dir;

                if (Directory.Exists(dir)) importDirectoryIntoComponent(dir);

                dir += ".light";
                if (File.Exists(dir)) importFileIntoComponent(dir);
            }
        }

        internal void importFileIntoComponent(string file)
        {
            var comp = machine.Compile(file);

            foreach (var c in comp.components)
            {
                //todo: file name yet exists
                this.components.Add(c.Key, c.Value);
            }
        }

        internal void importDirectoryIntoComponent(string dir)
        {
            //todo
        }

        string findNamespaceDirectory(string ns)
        {
            var dir = "";
            var split = ns.Split('.');

            bool dirExists = false;

            int s = 0;
            for (; s < split.Length; s++)
            {
                var ss = split[s];
                dir += "/" + ss;

                if (!(dirExists = Directory.Exists(machine.libsDir + dir)))
                {
                    break;
                }
            }

            if (dirExists)
            {
                return dir;
            }
            else
            {
                while (!File.Exists(machine.libsDir + dir + ".light") && s >= 0)
                {
                    dir = dir.Substring(0, dir.Length - (split[s].Length + 1));
                    s--;
                }
            }

            return dir;
        }

        #endregion
    }
}
