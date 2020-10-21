using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Langs
{
    /// <summary>
    /// This abstract class makes a language Linear compatible
    /// </summary>
    public abstract class Lang
    {
        public abstract Linear GetLinear();
        public abstract Language Language { get; }
    }

    public enum Language
    {
        Light,
        CPP
    }
}
