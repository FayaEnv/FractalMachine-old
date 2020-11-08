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
        internal Type returnType;
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
            Member,
            Operation
        }

        #endregion

        #region ReadLinear

        //tothink: move this region from Component to Components.Container?
        /*
        public void ReadLinear()
        {
            ReadLinear(_linear);
        }

        public virtual void ReadLinear(Linear lin)
        {
            for (int i = 0; i < lin.Instructions.Count; i++)
            {
                var instr = lin[i];

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
                        else
                            throw new Exception("Unexpected instruction");
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
            throw new Exception("declare not expected");
        }

        internal virtual void readLinear_operation(Linear instr)
        {
            throw new Exception("operation not expected");
        }

        internal virtual void readLinear_function(Linear instr)
        {
            throw new Exception("function not expected");
        }

        internal virtual void readLinear_namespace(Linear instr)
        {
            throw new Exception("namespace not expected");
        }

        internal virtual void readLinear_call(Linear instr)
        {
            throw new Exception("call not expected");
        }*/

        #endregion

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

        #endregion

        #region SubComponents

        public Component Solve(string Name, bool DontPanic = false)
        {
            var parts = Name.Split('.');
            return Solve(parts, DontPanic);
        }

        public Component Solve(string[] Names, bool DontPanic = false)
        {
            Component comp = this, bcomp = this;
            var tot = "";

            while (!comp.components.TryGetValue(Names[0], out comp))
            {
                comp = bcomp.parent;
                if (comp == null)
                {
                    if (!DontPanic) throw new Exception("Error, " + Names[0] + " not found");
                    return null;
                }
                bcomp = comp;
            }

            for (int p = 1; p < Names.Length; p++)
            {
                var part = Names[p];
                if (!comp.components.TryGetValue(part, out comp))
                {
                    if (!DontPanic) throw new Exception("Error, " + tot + part + " not found");
                    return null;
                }

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
      
        #endregion
        }
    }
