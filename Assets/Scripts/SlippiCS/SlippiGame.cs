using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlippiCS
{
    class SlippiGame
    {
        private readonly SlpReadInput input;
        private readonly SlpParser parser;

        public SlippiGame(string filePath)
        {
            input = new SlpReadInput
            {
                source = SlpInputSource.FILE,
                filePath = filePath
            };
            parser = new SlpParser();
        }

        public GameStartType GetSettings()
        {
            Process(true);
            return parser.GetSettings();
        }

        private void Process(bool settingsOnly = false)
        {
            // TODO!!
        }
    }
}
