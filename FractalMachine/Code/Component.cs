﻿using FractalMachine.Classes;
using FractalMachine.Code.Langs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FractalMachine.Code
{
    public class Component
    {
        Machine machine;

        public Dictionary<string, Component> components = new Dictionary<string, Component>();

        internal string FileName, outFileName;
        internal Component parent;
        internal Linear linear;
        internal bool called = false;

        internal Dictionary<string, Component> importedComponents = new Dictionary<string, Component>();
        internal Dictionary<string, string> importLink = new Dictionary<string, string>();

        public Component(Machine machine, Linear linear)
        {
            this.machine = machine;
            this.linear = linear;
            ReadLinear();
        }

        public void ReadLinear()
        {
            foreach(var instr in linear.Instructions)
            {
                instr.component = this;

                switch (instr.Op)
                {
                    case "import":
                        Import(instr.Attributes[0]);
                        break;

                    case "function":
                        addComponent(instr);
                        break;
                }                
            }
           
        }

        internal void addComponent(Linear instr)
        {
            var comp = new Component(machine, instr);
            comp.parent = this;
            components.Add(instr.Name, comp);
        }

        #region Import

        public void Import(string ToImport)
        {
            if (ToImport.HasMark())
            {
                // Is file
                //todo: ToImport.HasStringMark() || (angularBrackets = ToImport.HasAngularBracketMark())
                var dir = machine.libsDir+"/"+ToImport.NoMark();
                importFileIntoComponent(dir);
                //todo: importLink.Add(ResultingNamespace, dir);
            }
            else
            {
                // Is namespace
                var dir = findNamespaceDirectory(ToImport);
                dir = machine.libsDir + dir;

                if (Directory.Exists(dir))
                    importDirectoryIntoComponent(dir);

                dir += ".light";
                if (File.Exists(dir))
                {
                    importLink.Add(ToImport, dir);
                    importFileIntoComponent(dir);
                }
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

            importedComponents.Add(file, comp);
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

        #region Components

        public Component Solve(string Name)
        {
            var parts = Name.Split('.');
            Component comp = this, bcomp = this;
            var tot = "";

            while (!comp.components.TryGetValue(parts[0], out comp))
            {
                comp = bcomp.parent;
                if(comp == null)
                    throw new Exception("Error, " + parts[0] + " not found");
                bcomp = comp;
            }

            comp = bcomp;

            foreach(var p in parts)
            {
                if (!comp.components.TryGetValue(p, out comp))
                    throw new Exception("Error, " + tot + p + " not found");

                tot += p + ".";
            }

            return comp;
        }

        #endregion

        #region Properties

        public Component Top
        {
            get
            {
                return (parent!=null) ? parent.Top : this;
            }
        }

        #endregion

        #region Writer

        internal List<string> push = new List<string>();

        public string WriteToCpp(CPP.Writer writer = null)
        {
            if(writer == null)
                writer = new CPP.Writer.Main();
        
            foreach(var lin in linear.Instructions)
            {
                CPP.Writer o;

                switch (lin.Op)
                {
                    case "import":
                        o = writer.Add(new CPP.Writer.Import(lin, this));

                        break;

                    case "function":
                        var comp = components[lin.Name];

                        // Calculate parameters                       
                        o = writer.Add(new CPP.Writer.Function(lin));
                        comp.WriteToCpp(o);

                        break;

                    case "push":
                        push.Add(lin.Name);
                        break;

                    case "call":
                        writer.Add(new CPP.Writer.Call(lin));
                        push.Clear();

                        push.Clear();

                        break;
                }
            }

            return writer.Compose();
        }

        public string WriteLibrary()
        {
            if (outFileName == null)
            {
                var output = WriteToCpp();
                outFileName = machine.tempDir + FileName.Replace("/", "-");
                File.WriteAllText(outFileName, output);
            }

            return outFileName;
        }

        #endregion
    }
}
