using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachineLib.Classes
{
    public class Tetris<T>
    {
        public delegate bool OnAnalyze(T ast);

        public List<OnAnalyze> onAnalyzes = new List<OnAnalyze>();

        public void Check(OnAnalyze checker)
        {
            onAnalyzes.Add(checker);
        }

        public bool Ensure(List<T> list)
        {
            return Ensure(list.ToArray());
        }

        public bool Ensure(T[] array)
        {
            int minPos = 0;

            foreach(var an in onAnalyzes)
            {
                bool validated = false;

                for(int i=minPos; i<array.Length; i++)
                {
                    if (an(array[i]))
                    {
                        validated = true;
                        minPos = i;
                        break;
                    }
                }

                if (!validated)
                    return false;
            }

            return true;
        }
    }
}
