namespace SlippiCS
{
    class SlippiGame
    {
        private readonly SlpReadInput input;
        private readonly SlpParser parser;
        private int? readPosition;

        public SlippiGame(string filePath)
        {
            input = new SlpReadInput
            {
                Source = SlpInputSource.FILE,
                FilePath = filePath
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
            if (parser.GetGameEnd() != null)
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
                    parser.HandleCommand(command, payload);
                    return settingsOnly && parser.GetSettings() != null;
                }, readPosition);
            }
        }
    }
}
