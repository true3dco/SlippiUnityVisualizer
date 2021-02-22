using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlippiCS
{
    using EventCallbackFunc = Func<Command, IEventPayloadType, bool>;

    public enum SlpInputSource
    {
        FILE,
        BUFFER,
    }

    public class SlpReadInput
    {
        public SlpInputSource Source;
        public string FilePath;
        // Don't care about buffer shit for now.
    }

    public interface ISlpRefType : IDisposable
    {
        SlpInputSource Source { get; }

        int Read(Byte[] buffer, int offset, int count, int position);

        int Size();
    }

    public class SlpFileSourceRef : ISlpRefType
    {
        public SlpInputSource Source => SlpInputSource.FILE;
        public readonly FileStream fileStream; 

        public SlpFileSourceRef(SlpReadInput input)
        {
            if (input.Source != SlpInputSource.FILE)
            {
                throw new ArgumentException("input source must be of type FILE");
            }
            fileStream = File.OpenRead(input.FilePath);
        }

        public void Dispose()
        {
            fileStream.Dispose();
        }

        // Simulates the NodejS API. See: https://nodejs.org/api/fs.html#fs_fs_read_fd_buffer_offset_length_position_callback
        public int Read(byte[] buffer, int offset, int count, int position)
        {
            var hasPosition = position >= 0;
            if (hasPosition)
            {
                fileStream.Seek(position, SeekOrigin.Begin);
            }

            var nBytesRead = fileStream.Read(buffer, offset, count);
            // Resets the seek position, per the NodeJS API:
            // "If position is an integer, the file position will be unchanged."
            if (hasPosition)
            {
                fileStream.Seek(-nBytesRead, SeekOrigin.Current);
            }

            return nBytesRead;
        }

        // NOTE: This may be incorrect as statSync may be different than the length of the stream when it was opened / read.
        public int Size() => (int)fileStream.Length;
    }

    public class SlpFileType : IDisposable
    {
        public readonly ISlpRefType Ref;
        public readonly int RawDataPosition;
        public readonly int RawDataLength;
        public readonly int MetadataPosition;
        public readonly int MetadataLength;
        public readonly Dictionary<int, int> MessageSizes;

        public SlpFileType(ISlpRefType refType)
        {
            Ref = refType;
            RawDataPosition = GetRawDataPosition();
            RawDataLength = GetRawDataLength();
            MetadataPosition = RawDataPosition + RawDataLength + 10; // remove metadata string
            MetadataLength = GetMetadataLength();
            MessageSizes = GetMessageSizes();
        }

        public void Dispose()
        {
            Ref.Dispose();
        }

        private Dictionary<int, int> GetMessageSizes()
        {
            var messageSizes = new Dictionary<int, int>();
            // Support old file format
            if (RawDataPosition == 0)
            {
                messageSizes[0x36] = 0x140;
                messageSizes[0x37] = 0x6;
                messageSizes[0x38] = 0x46;
                messageSizes[0x39] = 0x1;
                return messageSizes;
            }

            var buffer = new byte[2];
            Ref.Read(buffer, 0, buffer.Length, RawDataPosition);
            if (buffer[0] != (byte)Command.MESSAGE_SIZES)
            {
                return messageSizes;
            }

            var payloadLength = buffer[1];
            messageSizes[0x35] = payloadLength;

            var messageSizesBuffer = new byte[payloadLength - 1];
            Ref.Read(messageSizesBuffer, 0, messageSizesBuffer.Length, RawDataPosition + 2);
            for (var i = 0; i < payloadLength - 1; i += 3)
            {
                var command = messageSizesBuffer[i];
                var sizeOfCommand = (messageSizesBuffer[i + 1] << 8) | messageSizesBuffer[i + 2];
                messageSizes[command] = sizeOfCommand;
            }

            return messageSizes;
        }

        private int GetMetadataLength() => Ref.Size() - RawDataPosition - 1;

        private int GetRawDataLength()
        {
            var fileSize = Ref.Size();
            if (RawDataPosition == 0)
            {
                return fileSize;
            }

            var buffer = new byte[4];
            Ref.Read(buffer, 0, buffer.Length, RawDataPosition - 4);

            // TODO: Def a better way to do this in C#
            // See: https://github.com/project-slippi/slippi-js/blob/a4041e7b3fb00be1b6143e7d45eefa697a4be35d/src/utils/slpReader.ts#L149
            var rawDataLen = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
            if (rawDataLen > 0)
            {
                return rawDataLen;
            }

            return fileSize - RawDataPosition;
        }

        private int GetRawDataPosition()
        {
            var buffer = new byte[1];
            Ref.Read(buffer, 0, buffer.Length, 0);

            if (buffer[0] == 0x36)
            {
                return 0;
            }

            if (buffer[0] == '{')
            {
                return 0; // (Note from original impl) return error?
            }

            return 15;
        }
    }

    // NOTE: Don't care about buffer stuff for now.

    public class SlpReader
    {
        public static SlpFileType OpenSlpFile(SlpReadInput input)
        {
            var inputRef = GetRef(input);
            return new SlpFileType(inputRef);
        }

        public static int IterateEvents(SlpFileType slpFile, EventCallbackFunc callback, int? startPos)
        {
            var slpRef = slpFile.Ref;

            var readPosition = startPos.GetValueOrDefault(0) > 0 ? startPos.Value : slpFile.RawDataPosition;
            var stopReadingAt = slpFile.RawDataPosition + slpFile.RawDataLength;

            var commandPayloadBuffers = slpFile.MessageSizes.ToDictionary(kp => kp.Key, kp => new byte[kp.Value + 1]);

            var commandByteBuffer = new byte[1];
            while (readPosition < stopReadingAt)
            {
                slpRef.Read(commandByteBuffer, 0, 1, readPosition);
                var commandByte = commandByteBuffer[0];
                if (!commandPayloadBuffers.TryGetValue(commandByte, out byte[] buffer))
                {
                    // According to original source code, this means we've failed (?)
                    return readPosition;
                }

                if (buffer.Length > stopReadingAt - readPosition)
                {
                    return readPosition;
                }

                slpRef.Read(buffer, 0, buffer.Length, readPosition);
                var parsedPayload = ParseMessage((Command)commandByte, buffer);
                var shouldStop = callback((Command)commandByte, parsedPayload);
                if (shouldStop)
                {
                    break;
                }

                readPosition += buffer.Length;
            }

            return readPosition;
        }

        public static IEventPayloadType ParseMessage(Command command, byte[] payload)
        {
            using (var reader = new BinaryReader(new MemoryStream(payload)))
            {
                try
                {
                    switch (command)
                    {
                        case Command.GAME_START:
                            Func<int, PlayerType> getPlayerObject = (playerIndex) =>
                            {
                                // Controller fix stuff
                                var cfOffset = playerIndex * 0x8;
                                reader.BaseStream.Position = 0x141 + cfOffset;
                                var dashback = reader.ReadUInt32();
                                reader.BaseStream.Position = 0x145 + cfOffset;
                                var shieldDrop = reader.ReadUInt32();
                                var cfOption = "None";
                                if (dashback != shieldDrop)
                                {
                                    cfOption = "Mixed";
                                }
                                else if (dashback == 1)
                                {
                                    cfOption = "UCF";
                                }
                                else if (dashback == 2)
                                {
                                    cfOption = "Dween";
                                }

                                // Nametag stuff
                                var nametagOffset = playerIndex * 0x10;
                                var nametagStart = 0x161 + nametagOffset;
                                var nametagBuf = payload.Skip(nametagOffset).Take(16).ToArray();
                                var nameTagString = Encoding.GetEncoding("shift_jis")
                                    .GetString(nametagBuf)
                                    .Split('\0')
                                    .FirstOrDefault(null);
                                var nametag = nameTagString != null ? FullWidth.ToHalfWidth(nameTagString) : "";

                                var offset = playerIndex * 0x24;
                                reader.BaseStream.Position = 0x65 + offset;
                                var characterId = reader.ReadByte();
                                reader.BaseStream.Position = 0x68 + offset;
                                var characterColor = reader.ReadByte();
                                reader.BaseStream.Position = 0x67 + offset;
                                var startStocks = reader.ReadByte();
                                reader.BaseStream.Position = 0x66 + offset;
                                var type = reader.ReadByte();
                                reader.BaseStream.Position = 0x6e + offset;
                                var teamId = reader.ReadByte();
                                return new PlayerType
                                {
                                    PlayerIndex = playerIndex,
                                    Port = playerIndex + 1,
                                    CharacterId = characterId,
                                    CharacterColor = characterColor,
                                    StartStocks = startStocks,
                                    Type = type,
                                    TeamId = teamId,
                                    ControllerFix = cfOption,
                                    NameTag = nametag
                                };
                            };

                            reader.BaseStream.Position = 0x1;
                            var slpVersion = $"{reader.ReadByte()}.{reader.ReadByte()}.{reader.ReadByte()}";
                            reader.BaseStream.Position = 0xd;
                            var isTeams = reader.ReadBoolean();
                            reader.BaseStream.Position = 0x1a1;
                            var isPAL = reader.ReadBoolean();
                            reader.BaseStream.Position = 0x13;
                            var stageId = reader.ReadUInt16();
                            reader.BaseStream.Position = 0x1a3;
                            var scene = reader.ReadByte();
                            reader.BaseStream.Position = 0x1a4;
                            var gameMode = reader.ReadByte();
                            return new GameStartType
                            {
                                SlpVersion = slpVersion,
                                IsTeams = isTeams,
                                IsPAL = isPAL,
                                StageId = stageId,
                                Players = new List<int>{ 0, 1, 2, 3 }.Select(getPlayerObject).ToList(),
                                Scene = scene,
                                GameMode = (GameMode)gameMode
                            };
                        case Command.PRE_FRAME_UPDATE:
                            break;
                        case Command.POST_FRAME_UPDATE:
                            break;
                        case Command.ITEM_UPDATE:
                            break;
                        case Command.FRAME_BOOKEND:
                            break;
                        case Command.GAME_END:
                            break;
                        default:
                            return null;
                    }
                }
                catch (EndOfStreamException)
                {
                    return null;
                }
            }
        }

        private static ISlpRefType GetRef(SlpReadInput input)
        {
            switch (input.Source)
            {
                case SlpInputSource.FILE:
                    if (input.FilePath == null)
                    {
                        throw new InvalidDataException("FILE input source does not have FilePath set");
                    }
                    return new SlpFileSourceRef(input);
                default:
                    throw new ArgumentException($"Unsupported input source {input.Source}");
            }
        }
    }
}
