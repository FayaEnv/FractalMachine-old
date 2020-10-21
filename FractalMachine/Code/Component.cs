using FractalMachine.Classes;
using FractalMachine.Code.Langs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FractalMachine.Code
{
    public class Component
    {
        Linear _linear;
        Machine machine;

        public Dictionary<string, Component> components = new Dictionary<string, Component>();

        internal string FileName, outFileName;
        internal Component parent;
        internal Lang script;
        internal bool called = false;

        internal Dictionary<string, string> parameters = new Dictionary<string, string>();
        internal Dictionary<string, Component> importLink = new Dictionary<string, Component>();

        public Component(Component parent)
        {
            this.machine = parent.machine;
            this.parent = parent;
        }

        public Component(Machine machine, Linear linear)
        {
            this.machine = machine;
            this.Linear = linear;
            this.parent = linear.component;
        }

        bool linearRead = false;
        public void ReadLinear()
        {
            if (linearRead)
                return;

            AnalyzeParameters();

            foreach(var instr in _linear.Instructions)
            {
                instr.component = this;

                Component comp;

                switch (instr.Op)
                {
                    case "import":
                        Import(instr.Attributes[0], instr.Parameters);
                        break;

                    case "function":
                        addComponent(instr);
                        break;

                    case "namespace":
                        comp = addComponent(instr.Name);
                        comp.Linear = instr;
                        comp.ReadLinear();
                        break;

                    case "call":
                        comp = Solve(instr.Name);
                        comp.called = true;

                        break;
                }
            }

            linearRead = true;
        }

        internal void AnalyzeParameters()
        {
            string par;

            if (parameters.TryGetValue("as", out par))
            {
                // Depends if CPP or Light
                if (Top.script.Language == Language.CPP)
                {
                    //Check for last import
                    int l = 0;
                    for (; l < _linear.Instructions.Count; l++)
                    {
                        if (_linear.Instructions[l].Op != "#include") 
                            break;
                    }

                    //todo: add namespace here
                    string read = "";
                }
            }
        }

        internal void addComponent(Linear instr)
        {
            var comp = new Component(machine, instr);
            comp.parent = parent;
            components.Add(instr.Name, comp);
            comp.ReadLinear();
        }

        internal Component addComponent(string Name)
        {           
            var names = Name.Split('.');
            var parent = this;
            for (int i=0; i<names.Length; i++)
            {
                parent = parent.enterComponentOrCreate(names[i]);
            }

            return parent;
        }

        internal Component enterComponentOrCreate(string Name)
        {
            Component comp;
            if (!components.TryGetValue(Name, out comp))
            {
                comp = new Component(this);
                components.Add(Name, comp);
            }

            return comp;
        }

        #region Properties

        internal Linear Linear
        {
            get { return _linear; }
            set
            {
                _linear = value;
                _linear.component = this;
            }
        }

        #endregion

        #region Import

        public void Import(string ToImport, Dictionary<string, string> Parameters)
        {
            if (ToImport.HasMark())
            {
                // Is file
                //todo: ToImport.HasStringMark() || (angularBrackets = ToImport.HasAngularBracketMark())
                var fname = ToImport.NoMark();
                var dir = machine.libsDir+"/"+ fname;
                var c = importFileIntoComponent(dir, Parameters);
                importLink.Add(fname, c);
                //todo: importLink.Add(ResultingNamespace, dir);
            }
            else
            {
                // Is namespace
                var fname = findNamespaceDirectory(ToImport);
                var dir = machine.libsDir + fname;

                if (Directory.Exists(dir))
                    importDirectoryIntoComponent(dir);

                dir += ".light";
                if (File.Exists(dir))
                {
                    var c = importFileIntoComponent(dir, Parameters);
                    importLink.Add(ToImport, c);
                }
            }
        }

        internal Component importFileIntoComponent(string file, Dictionary<string, string> parameters)
        {
            var comp = machine.Compile(file);
            //comp.parent = this; // ???

            foreach (var c in comp.components)
            {
                //todo: file name yet exists
                this.components.Add(c.Key, c.Value);
            }

            comp.parameters = parameters;

            return comp;
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

            for(int p=1; p<parts.Length; p++)
            {
                var part = parts[p];
                if (!comp.components.TryGetValue(part, out comp))
                    throw new Exception("Error, " + tot + part + " not found");

                tot += part + ".";
            }

            return comp;
        }

        #endregion

        #region Properties

        public Component Top
        {
            get
            {
                return (parent!=null && parent != this) ? parent.Top : this;
            }
        }

        public bool Called
        {
            get
            {
                if (called) return true;
                foreach(var comp in components)
                {
                    if (comp.Value.Called) return true;
                }

                return false;
            }
        }

        #endregion

        #region Writer

        internal List<string> push = new List<string>();

        public string WriteToCpp(CPP.Writer writer = null)
        {        
            if(writer == null)
                writer = new CPP.Writer.Main();

            Component comp;

            foreach(var lin in _linear.Instructions)
            {
                //lin.component = this;

                switch (lin.Op)
                {
                    case "import":
                        writer.Add(new CPP.Writer.Import(lin, this));

                        break;

                    case "function":                     
                        writer.Add(new CPP.Writer.Function(lin));
                        
                        break;

                    case "push":
                        push.Add(lin.Name);
                        break;

                    case "call":
                        writer.Add(new CPP.Writer.Call(lin));
                        push.Clear();

                        break;

                    case "namespace":                 
                        writer.Add(new CPP.Writer.Namespace(lin));

                        break;
                }
            }

            return writer.Compose();
        }

        public string WriteLibrary()
        {
            if (outFileName == null)
            {
                if (script.Language == Language.Light)
                {
                    var output = WriteToCpp();
                    outFileName = machine.tempDir + Path.GetFileNameWithoutExtension(FileName.Replace("/", "-")) + ".hpp";
                    File.WriteAllText(outFileName, output);
                }
            }

            return outFileName;
        }

        #endregion
    }
}
