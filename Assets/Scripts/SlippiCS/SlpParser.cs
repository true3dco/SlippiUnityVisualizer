using System;
using System.Collections.Generic;
using System.Linq;

namespace SlippiCS
{
    class SlpParser
    {
        public static readonly int MAX_ROLLBACK_FRAMES = 7;

        private Dictionary<int, FrameEntryType> frames = new Dictionary<int, FrameEntryType>();
        private GameStartType settings = null;
        private GameEndType gameEnd = null;
        private int? latestFrameIndex = null;
        private bool settingsComplete = false;
        private int lastFinalizedFrame = (int)Frames.FIRST - 1;
        // NOTE: Not caring about SlpParserOptions for now

        public event EventHandler<FinalizedFrameEventArgs> FinalizedFrame;
        protected virtual void OnFinalizedFrame(FinalizedFrameEventArgs e)
        {
            FinalizedFrame?.Invoke(this, e);
        }

        public event EventHandler<EndEventArgs> End;
        protected virtual void OnEnd(EndEventArgs e)
        {
            End?.Invoke(this, e);
        }

        public event EventHandler<SettingsEventArgs> Settings;
        protected virtual void OnSettings(SettingsEventArgs e)
        {
            Settings?.Invoke(this, e);
        }

        public event EventHandler<FrameEventArgs> Frame;
        protected virtual void OnFrame(FrameEventArgs e)
        {
            Frame?.Invoke(this, e);
        }

        public GameStartType GetSettings() => settingsComplete ? settings : null;

        public Dictionary<int, FrameEntryType> GetFrames() => frames;


        public void HandleCommand(Command command, IEventPayloadType payload)
        {
            switch (command)
            {
                case Command.GAME_START:
                    HandleGameStart(payload as GameStartType);
                    break;
                case Command.POST_FRAME_UPDATE:
                    HandlePostFrameUpdate(payload as PostFrameUpdateType);
                    HandleFrameUpdate(command, payload as PostFrameUpdateType);
                    break;
                case Command.PRE_FRAME_UPDATE:
                    HandleFrameUpdate(command, payload as PreFrameUpdateType);
                    break;
                case Command.ITEM_UPDATE:
                    HandleItemUpdate(payload as ItemUpdateType);
                    break;
                case Command.FRAME_BOOKEND:
                    HandleFrameBookend(payload as FrameBookendType);
                    break;
                case Command.GAME_END:
                    HandleGameEnd(payload as GameEndType);
                    break;
            }
        }

        public GameEndType GetGameEnd() => gameEnd;

        public FrameEntryType GetFrame(int num) => frames.ContainsKey(num) ? frames[num] : null;

        private void HandleGameEnd(GameEndType payload)
        {
            // Finalize remaining frames if necessary
            if (latestFrameIndex.HasValue && latestFrameIndex.Value != lastFinalizedFrame)
            {
                FinalizeFrames(latestFrameIndex.Value);
            }

            gameEnd = payload;
            OnEnd(new EndEventArgs { GameEnd = gameEnd });
        }

        private void HandleGameStart(GameStartType payload)
        {
            settings = payload;
            var players = payload.Players;
            settings.Players = players.Where(player => player.Type != 3).ToList();

            // Check to see if the file was created after the shek fix so we know
            // we don't have to process the first frame of the game for the full settings
            if (payload.SlpVersion != null && Version.Parse(payload.SlpVersion) >= Version.Parse("1.6.0"))
            {
                CompleteSettings(); 
            }
        }

        private void HandlePostFrameUpdate(PostFrameUpdateType payload)
        {
            if (settingsComplete)
            {
                return;
            }

            // Finish calculating settings
            if (payload.Frame.Value <= (int)Frames.FIRST)
            {
                var playerIndex = payload.PlayerIndex.Value;
                var playersByIndex = new Dictionary<int, PlayerType>();
                for (var i = 0; i < settings.Players.Count; i++)
                {
                    playersByIndex[i] = settings.Players[i];
                }

                switch (payload.InternalCharacterId)
                {
                    case 0x7:
                        playersByIndex[playerIndex].CharacterId = 0x13; // Sheik
                        break;
                    case 0x13:
                        playersByIndex[playerIndex].CharacterId = 0x12; // Zelda
                        break;
                }
            }
            if (payload.Frame.Value > (int)Frames.FIRST)
            {
                CompleteSettings();
            }
        }

        private void HandleFrameUpdate(Command command, IEventPayloadType payload)
        {
            if (!(payload is PreFrameUpdateType || payload is PostFrameUpdateType))
            {
                throw new ArgumentException("Event payload type must be PreFrameUpdateType or PostFrameUpdateType");
            }
            var frame = payload is PreFrameUpdateType ? (payload as PreFrameUpdateType).Frame : (payload as PostFrameUpdateType).Frame;
            var isFollower = payload is PreFrameUpdateType ? (payload as PreFrameUpdateType).IsFollower : (payload as PostFrameUpdateType).IsFollower;
            var playerIndex = payload is PreFrameUpdateType ? (payload as PreFrameUpdateType).PlayerIndex.Value : (payload as PostFrameUpdateType).PlayerIndex.Value;
            var isPre = command == Command.PRE_FRAME_UPDATE;
            var currentFrameNumber = frame.Value;
            latestFrameIndex = currentFrameNumber;

            if (!frames.TryGetValue(currentFrameNumber, out FrameEntryType frameEntry))
            {
                frameEntry = new FrameEntryType();
                frames[currentFrameNumber] = frameEntry;
            }

            if (isFollower.GetValueOrDefault(false))
            {
                if (frameEntry.Followers == null)
                {
                    frameEntry.Followers = new Dictionary<int, PrePostUpdates>();
                }
                if (!frameEntry.Followers.TryGetValue(playerIndex, out var updates))
                {
                    updates = new PrePostUpdates();
                    frameEntry.Followers[playerIndex] = updates;
                }

                if (isPre)
                {
                    updates.Pre = payload as PreFrameUpdateType;
                }
                else
                {
                    updates.Post = payload as PostFrameUpdateType;
                }
            }
            else
            {
                if (frameEntry.Players == null)
                {
                    frameEntry.Players = new Dictionary<int, PrePostUpdates>();
                }
                if (!frameEntry.Players.TryGetValue(playerIndex, out var updates))
                {
                    updates = new PrePostUpdates();
                    frameEntry.Players[playerIndex] = updates;
                }

                if (isPre)
                {
                    updates.Pre = payload as PreFrameUpdateType;
                }
                else
                {
                    updates.Post = payload as PostFrameUpdateType;
                }
            }
            frameEntry.Frame = currentFrameNumber;

            // If file is from before frame bookending, add frame to stats computer here. Does a little
            // more processing than necessary, but works.
            var settings = GetSettings();
            if (settings != null && (settings.SlpVersion == null || Version.Parse(settings.SlpVersion) <= Version.Parse("2.2.0")))
            {
                OnFrame(new FrameEventArgs { Frame = frameEntry });
                // Finalize the previous frame since no bookending exists
                FinalizeFrames(currentFrameNumber - 1);
            }
            else
            {
                frameEntry.IsTransferComplete = false;
            }
        }

        private void HandleItemUpdate(ItemUpdateType payload)
        {
            var currentFrameNumber = payload.Frame.Value;
            if (!frames.TryGetValue(currentFrameNumber, out var frameEntry))
            {
                frameEntry = new FrameEntryType();
                frames[currentFrameNumber] = frameEntry;
            }
            if (frameEntry.Items == null)
            {
                frameEntry.Items = new List<ItemUpdateType>();
            }
            frameEntry.Items.Add(payload);
        }

        private void HandleFrameBookend(FrameBookendType payload)
        {
            var latestFinalizedFrame = payload.LatestFinalizedFrame.Value;
            var currentFrameNumber = payload.Frame.Value;
            if (!frames.TryGetValue(currentFrameNumber, out var frameEntry))
            {
                frameEntry = new FrameEntryType();
                frames[currentFrameNumber] = frameEntry;
            }
            frameEntry.IsTransferComplete = true;

            // Fire of a normal frame event
            OnFrame(new FrameEventArgs { Frame = frameEntry });

            // Finalize frames if necessary
            var validLatestFrame = settings.GameMode == GameMode.ONLINE;
            if (validLatestFrame && latestFinalizedFrame >= (int)Frames.FIRST)
            {
                // NOTE: Don't care about strict mode for now
                FinalizeFrames(latestFinalizedFrame);
            }
            else
            {
                // Since we don't have a valid finalized frame, just finalize the frame based on MAX_ROLLBACK_FRAMES
                FinalizeFrames(currentFrameNumber - MAX_ROLLBACK_FRAMES);
            }
        }

        private void FinalizeFrames(int num)
        {
            while (lastFinalizedFrame < num)
            {
                var frameToFinalize = lastFinalizedFrame + 1;
                var frame = GetFrame(frameToFinalize);

                // NOTE: Skipping strict stuff

                OnFinalizedFrame(new FinalizedFrameEventArgs { Frame = frame });
                lastFinalizedFrame = frameToFinalize;
            }
        }

        private void CompleteSettings()
        {
            if (!settingsComplete)
            {
                settingsComplete = true;
                OnSettings(new SettingsEventArgs { Settings = settings });
            }
        }
    }

    public class FrameEventArgs : EventArgs
    {
        public FrameEntryType Frame;
    }

    public class SettingsEventArgs : EventArgs
    {
        public GameStartType Settings;
    }

    public class EndEventArgs : EventArgs
    {
        public GameEndType GameEnd;
    }

    public class FinalizedFrameEventArgs : EventArgs
    {
        public FrameEntryType Frame;
    }
}
