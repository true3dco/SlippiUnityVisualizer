using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlippiCS
{
    class SlpParser
    {
        private bool settingsComplete = false;
        private GameStartType settings = null;
        private GameEndType gameEnd = null;

        public GameStartType GetSettings() => settingsComplete ? settings : null;

        public void HandleCommand(object command, object payload)
        {
            throw new NotImplementedException();
        }

        public GameEndType GetGameEnd() => gameEnd;
    }
}
