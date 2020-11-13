using FractalMachine.Code;
using FractalMachine.Code.Langs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Schema;

namespace FractalMachine.Develop
{
    public class LinearAssert
    {
        string assertDir = "";
        public LinearAssert()
        {
            var assetsDir = Resources.Solve("Assets");
            assertDir = assetsDir + "/LinearAssert/";
        }

        public void Execute()
        {
            var dirInfo = new DirectoryInfo(assertDir);
            var files = dirInfo.GetFiles();

            foreach(var file in files)
            {
                if (file.Extension == ".light")
                {
                    if(file.Name == "13-class.light" || true) // Debug purposes
                        elaborateFile(file.FullName);
                }
            }
        }

        void elaborateFile(string fn)
        {
            var script = Light.OpenFile(fn);
            var linear = script.GetLinear();
            string res = ExplodeLinear(linear);
            File.WriteAllText(fn + ".linear.txt", res);
        }

        delegate void Space(int level);

        string ExplodeLinear(Linear lin, int level=0, int number=0)
        {
            string ret = "";

            Space spaces = delegate(int level)
            {
                for (int l = 0; l < level; l++) ret += "=="; ret += "|";
            };

            spaces(level);
            ret += " " + number + ". " + lin.Op;
            if (lin.Type != null)
            {
                ret += "\n";
                spaces(level+1);
                ret += " Type: " + lin.Type;
            }
            if (lin.Name != null)
            {
                ret += "\n";
                spaces(level + 1);
                ret += " Name: " + lin.Name;
            }
            if (lin.Return != null)
            {
                ret += "\n";
                spaces(level + 1);
                ret += " Return: " + lin.Return;
            }
          
            if (lin.Attributes.Count > 0)
            {
                ret += "\n";
                spaces(level + 1);
                ret += " Attributes: ";
                foreach (var attr in lin.Attributes)
                    ret += attr + "  |  ";
            }

            ret += "\r\n";

            if (lin.Settings.Count > 0)
            {
                int i = 0;
                spaces(level + 1);
                ret += " Settings:\r\n";
                foreach (var sett in lin.Settings)
                {
                    spaces(level+2);
                    ret += sett.Key+"\r\n";
                    ret += ExplodeLinear(sett.Value, level + 2, i++);
                }
            }

            if(lin.Instructions.Count > 0)
            {
                int i = 0;
                spaces(level+1);
                ret += " Instructions:\r\n";
                foreach(var instr in lin.Instructions)
                {
                    ret += ExplodeLinear(instr, level + 2, i++);
                }

            }

            //spaces(level);
            //ret += " END\n";

            return ret;
        }
    }
}
