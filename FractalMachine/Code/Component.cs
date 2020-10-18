using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code
{
    public class Component
    {
        Machine machine;
        Dictionary<string, Component> components = new Dictionary<string, Component>();

        public Component(Machine Machine)
        {
            machine = Machine;
        }

        public void ReadLinear(Linear lin)
        {
            foreach(var instr in lin.Instructions)
            {
                switch (instr.Op)
                {
                    case "import":
                        machine.Import(instr.Attributes[0]);
                        break;
                }

            }
           
        }
    }
}
