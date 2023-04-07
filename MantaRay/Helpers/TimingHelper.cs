using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Helpers
{
    public class TimingHelper
    {
        public string Name { get; set; }
        public DateTime LastTime { get; set; }


        public TimingHelper(string name = "")
        {
            LastTime = DateTime.Now;
            Name = name;
        }

        public void Benchmark(string s = "")
        {
#if DEBUG
            Rhino.RhinoApp.WriteLine($"[ {DateTime.Now:T} {Name} ]: '{s}' in {(DateTime.Now - LastTime).ToShortString()}");
            LastTime = DateTime.Now;
#endif
        }
    }
}
