/*
   Copyright 2020 (c) Riccardo Cecchini
   
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using FractalMachine.Classes;
using FractalMachine.Code.Components;
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
    abstract public class Component
    {
        internal Types type;
        internal Linear _linear;
        internal Component parent;

        public Dictionary<string, Component> components = new Dictionary<string, Component>();
        internal Dictionary<string, string> parameters = new Dictionary<string, string>();

        public Component(Component parent, Linear Linear) 
        {
            this.parent = parent;
            _linear = Linear;
        }


        #region ComponentTypes

        public enum Types
        {
            Container,
            Function,
            Overload,
            Member
        }

        #endregion

        #region ReadLinear

        public virtual void ReadLinear()
        {
            for (int i = 0; i < _linear.Instructions.Count; i++)
            {
                var instr = _linear[i];

                switch (instr.Op)
                {
                    case "import":
                        readLinear_import(instr);
                        break;

                    case "declare":
                        readLinear_declare(instr);
                        break;

                    case "function":
                        readLinear_function(instr);
                        break;

                    case "namespace":
                        readLinear_namespace(instr);
                        break;

                    case "call":
                        readLinear_call(instr);
                        break;

                    default:
                        if (instr.Type == "operation")
                            readLinear_operation(instr);
                        break;
                }
            }
        }

        internal virtual void readLinear_import(Linear instr)
        {
            throw new Exception("import not expected");
        }

        internal virtual void readLinear_declare(Linear instr)
        {

        }

        internal virtual void readLinear_operation(Linear instr)
        {

        }

        internal virtual void readLinear_function(Linear instr)
        {
            Function function;

            try
            {
                function = (Function)getComponent(instr.Name);
            }
            catch(Exception ex)
            {
                throw new Exception("Name used for another variable");
            }
            
            if (function == null)
            {
                function = new Function(this, null);
                addComponent(instr.Name, function);
            }

            function.addOverload(instr);
        }

        internal virtual void readLinear_namespace(Linear instr)
        {
            Namespace ns;

            try
            {
                ns = (Namespace)getComponent(instr.Name);
            }
            catch (Exception ex)
            {
                throw new Exception("Name used for another variable");
            }

            if(ns == null)
            {
                ns = new Namespace(this, instr);
                addComponent(instr.Name, ns);
            }
        }

        internal virtual void readLinear_call(Linear instr)
        {

        }


        #endregion

        bool linearRead = false;
        /*public void ReadLinear()
        {
            if (linearRead)
                return;

            AnalyzeParameters();

            int pushNum = 0;
            Linear callParameters = null;

            bool afterIncludes = false;

            for(int i=0; i<_linear.Instructions.Count; i++)
            {
                var instr = _linear[i];
                instr.component = this;

                bool closesIncludes = afterIncludes;
                Component comp;             

                switch (instr.Op)
                {
                    case "import":
                        Import(instr.Name, instr.Parameters);
                        break;

                    case "declare":
                        addComponent(instr);
                        closesIncludes = true;

                        break;

                    case "function":
                        comp = addComponent(instr);
                        comp.Type = Types.Function;
                        closesIncludes = true;
                        break;

                    case "namespace":
                        comp = addComponent(instr.Name);
                        comp.Linear = instr;
                        comp.Type = Types.Namespace;
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
                        pushNum++;
                        
                        break;

                    case "call":
                        if (instr.Type != null)
                        {
                            //todo
                        }
                        else
                        {
                            comp = Solve(instr.Name);
                            comp.called = true;
                            callParameters = null;
                        }
                        
                        break;
                }

                if(type == Types.File && !afterIncludes && closesIncludes)
                {
                    // Inser linear advisor
                    var lin = new Linear(instr.ast);
                    lin.Op = "compiler";
                    lin.Name = "endIncluse";
                    _linear.Instructions.Insert(i, lin);
                    i++;

                    afterIncludes = true;
                }
            }

            linearRead = true;
        }*/

        //tothink: is it so important that this parameters are instanced when strictly necessary?

        /*internal void AnalyzeParameters()
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

        }*/

        #region AddComponents

        internal Component getBaseComponent(string Name, out string toCreate)
        {
            var names = Name.Split('.').ToList();
            toCreate = names.Pull();

            Component baseComp = this;
            if (names.Count > 0)
                baseComp = Solve(String.Join(".", names));

            return baseComp;
        } 

        internal void addComponent(string Name, Component comp)
        {
            string toCreate;
            var baseComp = getBaseComponent(Name, out toCreate);

            baseComp.components[toCreate] = comp;
        }

        internal Component getComponent(string Name)
        {
            string toCreate;
            var baseComp = getBaseComponent(Name, out toCreate);
            Component comp;
            baseComp.components.TryGetValue(Name, out comp);
            return comp;
        }

        /*internal Component addComponent(Linear instr)
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
        }*/

        #endregion



        #region Properties

        /*static string[] NestedOperations = new string[] { "namespace", "function" };

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
        }*/

        #endregion

        #region SubComponents

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

        #endregion

        #region Called

        internal bool called = false;

        public bool Called
        {
            get
            {
                if (called) return true;
                foreach (var comp in components)
                {
                    if (comp.Value.Called) return true;
                }

                return false;
            }
        }

        public Linear Linear
        {
            get
            {
                return _linear;
            }
        }

        #endregion

        #region Writer

        internal string wtCont = "";

        virtual public string WriteTo(Lang.Settings LangSettings)
        {
            wtCont = "";

            foreach (var lin in _linear.Instructions)
            {
                //lin.component = this;

                switch (lin.Op)
                {
                    case "import":
                        writeTo_import(LangSettings, lin);
                        break;

                    case "function":
                        writeTo_function(LangSettings, lin);
                        break;

                    case "call":
                        writeTo_call(LangSettings, lin);
                        break;

                    case "namespace":
                        writeTo_namespace(LangSettings, lin);
                        break;

                    case "compiler":
                        writeTo_compiler(LangSettings, lin);
                        break;
                }
            }

            return wtCont;
        }

        virtual internal int writeToNewLine()
        {
            wtCont += "\r\n";
            return parent.writeToNewLine();
        }

        virtual internal void writeToCont(string str)
        {
            wtCont += str;
        }

        virtual public void writeTo_import(Lang.Settings LangSettings, Linear instr)
        {
            
        }

        virtual public void writeTo_function(Lang.Settings LangSettings, Linear instr)
        {
            Overload fun = (Overload)Solve(instr.Name);
            var res = fun.WriteTo(LangSettings);
            writeToCont(res);
        }

        virtual public void writeTo_call(Lang.Settings LangSettings, Linear instr)
        {
            
        }

        virtual public void writeTo_namespace(Lang.Settings LangSettings, Linear instr)
        {
            
        }

        virtual public void writeTo_compiler(Lang.Settings LangSettings, Linear instr)
        {
            
        }


        /*public string WriteToCpp(CPP.Writer writer = null)
        {        
            if(writer == null)
                writer = new CPP.Writer.Main(this, Linear);

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

                    case "call":
                        new CPP.Writer.Call(writer, lin);
                        push.Clear();
                        break;

                    case "namespace":
                        new CPP.Writer.Namespace(writer, lin);
                        break;

                    ///
                    /// Compiler instructions
                    /// 

                    case "compiler":

                        switch (lin.Name) {
                            case "endIncluse":
                                // Write usings
                                while (usings.Count > 0)
                                {
                                    new CPP.Writer.Using(writer, lin, usings[0]);
                                    usings.RemoveAt(0);
                                }
                                break;
                        }

                        break;
                }
            }

            return writer.Compose();
        }*/



        #endregion
    }
}
