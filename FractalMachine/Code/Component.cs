using FractalMachine.Classes;
using FractalMachine.Code.Langs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FractalMachine.Code
{
    public class Component
    {
        Linear _linear;
        Context context;

        internal string FileName, outFileName;
        internal Component parent;
        internal Lang script;
        internal bool called = false;

        public Dictionary<string, Component> components = new Dictionary<string, Component>();
        internal Dictionary<string, string> parameters = new Dictionary<string, string>();
        internal Dictionary<string, Component> importLink = new Dictionary<string, Component>();

        public Component(Component parent)
        {
            this.context = parent.context;
            this.parent = parent;
        }

        public Component(Component parent, Linear linear) : this(parent)
        {
            this.Linear = linear;
        }

        public Component(Context context, Linear linear)
        {
            this.context = context;
            this.parent = linear.component;
            this.Linear = linear;       
        }

        bool linearRead = false;
        public void ReadLinear()
        {
            if (linearRead)
                return;

            AnalyzeParameters();

            int i = 0;
            int pushNum = 0;
            Linear callParameters = null;

            foreach (var instr in _linear.Instructions)
            {
                instr.component = this;

                Component comp;             

                switch (instr.Op)
                {
                    case "import":
                        Import(instr.Attributes[0], instr.Parameters);
                        break;

                    case "declare":
                        addComponent(instr);

                        break;

                    case "function":
                        comp = addComponent(instr);
                        break;

                    case "namespace":
                        comp = addComponent(instr.Name);
                        comp.Linear = instr;
                        comp.ReadLinear();
                        break;

                    case "push":
                        if(callParameters == null)
                        {
                            int j = 0;
                            while (_linear[i + j].Op != "call") j++;
                            var call = _linear.Instructions[i + j];
                            var function = Solve(call.Name).Linear;
                            callParameters = function.Settings["parameters"];
                            pushNum = 0;
                        }

                        // Check parameter
                        var par = callParameters[pushNum];
                        CheckType(instr.Name, par.Return, i);

                        string read = "";
                        
                        pushNum++;

                        break;

                    case "call":
                        comp = Solve(instr.Name);
                        comp.called = true;
                        callParameters = null;
                        
                        break;
                }

                i++;
            }

            linearRead = true;
        }

        //tothink: is it so important that this parameters are instanced when strictly necessary?
        internal void IsNested()
        {
            components = new Dictionary<string, Component>();
            parameters = new Dictionary<string, string>();
            importLink = new Dictionary<string, Component>();
        }

        internal void AnalyzeParameters()
        {
            string parAs;

            if (parameters.TryGetValue("as", out parAs))
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

            if (_linear != null)
            {             
                switch (_linear.Op)
                {
                    case "function":
                        Linear sett;
                        if (!_linear.Settings.TryGetValue("parameters", out sett))
                            throw new Exception("Missing function parameters");

                        foreach(var param in sett.Instructions)
                        {
                            addComponent(param);
                        }

                        break;
                }
            }

        }

        #region Components

        internal Component addComponent(Linear instr)
        {
            var comp = addComponent(instr.Name);
            comp.Linear = instr;
            comp.ReadLinear();
            return comp;
        }

        internal Component addComponent(string Name)
        {           
            var names = Name.Split('.');
            var parent = this;
            for (int i=0; i<names.Length; i++)
            {
                parent = parent.getComponentOrCreate(names[i]);
            }

            return parent;
        }

        internal Component getComponentOrCreate(string Name)
        {
            Component comp;
            if (!components.TryGetValue(Name, out comp))
            {
                comp = new Component(this);
                components.Add(Name, comp);
            }

            return comp;
        }

        #endregion

        #region Types

        public void CheckType(string subject, string request, int linearPos)
        {
            Type reqType = Type.Get(request);
            Type subjType;

            var attrType = Type.GetAttributeType(subject);
            
            if(attrType == Type.AttributeType.Invalid)
            {
                throw new Exception("Invalid type");
            }

            if (attrType == Type.AttributeType.Name)
            {
                // get component info    
                var comp = Solve(subject);
                subjType = Type.Get(comp.Linear.Return);
                subjType.Solve(this); // or comp?

                if (subjType.Name != reqType.Name)
                {
                    //todo
                }
            }
            else
            {
                if (attrType != reqType.MyAttributeType)
                {
                    subject = Type.Convert(subject, reqType);
                    Linear[linearPos].Name = subject;
                }
            }   

            string done = "";
        }

        #endregion

        #region Properties

        static string[] NestedOperations = new string[] { "namespace", "function" };

        internal Linear Linear
        {
            get { return _linear; }
            set
            {
                if (value == null)
                    return;

                _linear = value;
                _linear.component = this;

                if (NestedOperations.Contains(_linear.Op))
                    IsNested();
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
                var dir = context.libsDir+"/"+ fname;
                var c = importFileIntoComponent(dir, Parameters);
                importLink.Add(fname, c);
                //todo: importLink.Add(ResultingNamespace, dir);
            }
            else
            {
                // Is namespace
                var fname = findNamespaceDirectory(ToImport);
                var dir = context.libsDir + fname;

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
            var comp = context.ExtractComponent(file);
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

                if (!(dirExists = Directory.Exists(context.libsDir + dir)))
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
                while (!File.Exists(context.libsDir + dir + ".light") && s >= 0)
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
                writer = new CPP.Writer.Main(Linear);

            Component comp;

            foreach(var lin in _linear.Instructions)
            {
                //lin.component = this;

                switch (lin.Op)
                {
                    case "import":
                        new CPP.Writer.Import(writer, lin, this);

                        break;

                    case "function":                     
                        new CPP.Writer.Function(writer, lin);
                        
                        break;

                    case "push":
                        push.Add(lin.Name);
                        break;

                    case "call":
                        new CPP.Writer.Call(writer, lin);
                        push.Clear();

                        break;

                    case "namespace":                 
                        new CPP.Writer.Namespace(writer, lin);

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
                    outFileName = context.tempDir + Misc.DirectoryNameToFile(FileName) + ".hpp";
                    outFileName = Path.GetFullPath(outFileName);
                    if (Resources.FilesWriteTimeCompare(FileName, outFileName) >= 0)
                    {
                        var output = WriteToCpp();
                        File.WriteAllText(outFileName, output);
                    }
                }
                else
                    outFileName = FileName;
            }

            // non so se l'AssertPath metterlo qui o direttamente in WriteCPP
            return context.env.AssertPath(outFileName);
        }

        #endregion
    }
}
