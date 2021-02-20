using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlippiCS
{
    public enum SlpInputSource
    {
        FILE,
        BUFFER,
    }
    public class SlpReadInput
    {
        public SlpInputSource source;
        public string filePath;
        // Don't care about buffer shit for now.
    }
    public class SlpReader
    {
    }
}
