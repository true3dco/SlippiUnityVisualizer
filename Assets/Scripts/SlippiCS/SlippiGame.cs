using System;
using System.Threading;
using System.Collections.Generic;

namespace SlippiCS
{
    public class SlippiGame : IDisposable
    {
        private readonly SlpReadInput input;
        private int? readPosition;
        private readonly Mutex processMtx = new Mutex();
        // NOTE: T3D Addition changing this to public
        public readonly SlpParser Parser;
        public int ProcessWaitTimeoutMs = 500;

        public SlippiGame(string filePath)
        {
            input = new SlpReadInput
            {
                Source = SlpInputSource.FILE,
                FilePath = filePath
            };
            Parser = new SlpParser();
        }

        public void Dispose()
        {
            processMtx.Dispose();
        }

        public GameStartType GetSettings()
        {
            Process(true);
            return Parser.GetSettings();
        }

        public Dictionary<int, FrameEntryType> GetFrames()
        {
            Process();
            return Parser.GetFrames();
        }

        public GameEndType GetGameEnd()
        {
            Process();
            return Parser.GetGameEnd();
        }

        public void Process(bool settingsOnly = false)
        {
            if (processMtx.WaitOne(ProcessWaitTimeoutMs))
            {
                try
                {
                    ProcessInternal(settingsOnly);
                } finally
                {
                    processMtx.ReleaseMutex();
                }
            }
        }

        private void ProcessInternal(bool settingsOnly)
        {
            if (Parser.GetGameEnd() != null)
            {
                return;
            }

            using (var slpFile = SlpReader.OpenSlpFile(input))
            {
                readPosition = SlpReader.IterateEvents(slpFile, (command, payload) =>
                {
                    if (payload == null)
                    {
                        // See: https://github.com/project-slippi/slippi-js/blob/a4041e7b3fb00be1b6143e7d45eefa697a4be35d/src/SlippiGame.ts#L81
                        return false;
                    }
                    Parser.HandleCommand(command, payload);
                    return settingsOnly && Parser.GetSettings() != null;
                }, readPosition);
            }
        }
    }
}
