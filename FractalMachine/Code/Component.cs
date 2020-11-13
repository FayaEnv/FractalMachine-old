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
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FractalMachine.Code
{
    abstract public class Component
    {
        internal Component attached;
        internal string name;
        internal Type returnType;
        internal Types type;
        internal Linear _linear;
        internal Component parent;

        /// Compiling purposes
        internal string lastPath;

        public Dictionary<string, Component> components = new Dictionary<string, Component>();
        internal Dictionary<string, string> parameters = new Dictionary<string, string>();

        public Component(Component parent, string Name, Linear Linear) 
        {
            this.parent = parent;
            _linear = Linear;

            if (parent != null && Name != null)
                parent.addComponent(Name, this);

            if (_linear != null)
            {
                _linear.component = this;
                if(!(this is Components.Container)) ReadLinear();
            }
        }

        #region ReadLinear

        public virtual void ReadLinear() { } 

        #endregion

        #region ComponentTypes

        public enum Types
        {
            Container,
            Function,
            Member,
            Operation
        }

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
            comp.name = toCreate;
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

        public virtual Component Solve(string Name, bool DontPanic = false)
        {
            var parts = Name.Split('.');
            return Solve(parts, DontPanic);
        }

        public Component Solve(string[] Names, bool DontPanic = false)
        {
            Component comp = this, bcomp = this;
            var tot = "";

            ///
            /// Native functions
            ///
            if (Names.Length == 1)
            {
                ///
                /// Check native function call
                /// 
                var name = Names[0];
                if (name.StartsWith(Properties.NativeFunctionPrefix))
                {
                    // Is C function
                    if (name.StartsWith(Properties.NativeFunctionPrefix+"c_"))
                    {
                        name = name.Substring(5);
                        var spl = name.Split('_');
                        var lib = spl[0];
                        name = spl[1];

                        TopFile.IncludeDefault(lib);

                        //Create dummy component function
                        var fun = new Function(null, null);
                        fun.name = name;
                        return fun;
                    }
                }

                ///
                /// Check internal var reference
                ///
                if (this is Components.Container)
                {
                    var cont = (Components.Container)this;
                    var ivar = cont.ivarMan.Get(name);
                    if (ivar != null)
                        return new Member(ivar);
                }
            }

            ///
            /// Normal
            ///
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

            string path = Names[0];

            for (int p = 1; p < Names.Length; p++)
            {
                /// Validate component
                if(comp is Components.File)
                    ((Components.File)comp).Load();

                if(comp is Components.Member)
                {
                    var retType = comp.returnType;
                    if (retType.IsDataStructure)
                        comp = retType.Component;
                    //todo: handle extensions properties for value types
                }

                /// Seek component
                var part = Names[p];
                if (!comp.components.TryGetValue(part, out comp))
                {
                    if (!DontPanic) throw new Exception("Error, " + tot + part + " not found");
                    return null;
                }

                comp.lastPath = path;
                path += "." + part;

                tot += part + ".";
            }

            return comp;
        }

        #endregion

        #region Modifiers

        public virtual bool IsPublic
        {
            get
            {
                // Light modifiers pespective
                return _linear.Attributes.Contains("public");
            }
        }

        public virtual bool CanAccess(Component from)
        {
            if (parent == from)
                return true;

            throw new Exception("todo");
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

        public virtual Components.File TopFile
        {
            get
            {
                return parent.TopFile;
            }
        }

        public Components.Container TopContainer
        {
            get
            {
                if (this is Components.Container)
                    return (Components.Container)this;
                else
                    return parent.TopContainer;
            }
        }

        public virtual Project GetProject
        {
            get
            {
                return parent.GetProject;
            }
        }

        #endregion

        #region Methods

        public virtual string GetRealName(Component relativeTo = null)
        {
            string topName = null;

            bool hasCommonDescending = relativeTo != null && HasCommonDescending(relativeTo);

            /*
                This is a delicate part. The type should be specified if part of a static class or namespace
                The code below, pratically, is not working for the moment
            */
            if (TopFile != parent && !hasCommonDescending && !(this is DataStructure))
                topName = parent?.GetRealName(relativeTo);

            return (topName != null ? topName + '.' : "") + name;
        }

        public string GetPath(string Delimiter = ".", Component RelativeTo = null)
        {
            var n = "";
            if (parent != null && parent != TopFile && !HasCommonDescending(RelativeTo)) n = parent.GetPath(Delimiter) + Delimiter;
            return n + name;
        }

        public bool HasCommonDescending(Component comp)
        {
            var p = comp;
            while(p != null)
            {
                if (p == this)
                    return true;

                p = p.parent;
            }

            return false;
        }

        #endregion

        #region Called

        internal bool _called = false;

        public virtual bool Called
        {
            get
            {
                if (!(this is Components.Container))
                    return true;

                if (_called) return true;
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

        internal bool written = false;
        internal int writeContLength = 0;
        internal List<string> writeCont = new List<string>();
        internal Component writeRedirectTo;

        abstract public string WriteTo(Lang Lang);

        internal virtual void writeReset()
        {
            writeCont.Clear();
            writeContLength = 0;

            if (written) {
                written = false;
                foreach (var i in components)
                    i.Value.writeReset();
            }
        }

        internal virtual int writeNewLine(Linear instr, bool isBase = true)
        {
            if(isBase) writeToCont("\n");
            return parent.writeNewLine(instr, false);
        }

        internal void writeToCont(string str)
        {
            writeCont.Add(str);
            writeContLength += str.Length;
        }

        internal string writeReturn()
        {
            var strBuild = new StringBuilder(writeContLength);
            strBuild.AppendJoin("", writeCont.ToArray());

            if(writeRedirectTo != null)
            {
                writeRedirectTo.writeCont.Add(strBuild.ToString());
                return "";
            }

            return strBuild.ToString();
        }
        
        internal void writeRedirect(Component to)
        {
            writeRedirectTo = to;
        }

        #endregion
    }
}
