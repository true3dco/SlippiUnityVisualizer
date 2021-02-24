using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            // NOTE: FileShare.ReadWrite is important here because we must be able to open the .slp file
            // as it's being written to, in the case of a live-stream.
            fileStream = new FileStream(input.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

            if (buffer[0] != '{')
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
            using (var reader = new SlpPayloadReader(payload))
            {
                switch (command)
                {
                    case Command.GAME_START:
                        Func<int, PlayerType> getPlayerObject = (playerIndex) =>
                        {
                            // Controller fix stuff
                            var cfOffset = playerIndex * 0x8;
                            var dashback = reader.ReadUint32(0x141 + cfOffset);
                            var shieldDrop = reader.ReadUint32(0x145 + cfOffset);
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
                                .Split(new char[] { '\0' })
                                .FirstOrDefault();
                            var nametag = nameTagString != null ? FullWidth.ToHalfWidth(nameTagString) : "";

                            var offset = playerIndex * 0x24;
                            return new PlayerType
                            {
                                PlayerIndex = playerIndex,
                                Port = playerIndex + 1,
                                CharacterId = reader.ReadUint8(0x65 + offset),
                                CharacterColor = reader.ReadUint8(0x68 + offset),
                                StartStocks = reader.ReadUint8(0x67 + offset),
                                Type = reader.ReadUint8(0x66 + offset),
                                TeamId = reader.ReadUint8(0x6e + offset),
                                ControllerFix = cfOption,
                                NameTag = nametag
                            };
                        };

                        return new GameStartType
                        {
                            SlpVersion = $"{reader.ReadUint8(0x1)}.{reader.ReadUint8(0x2)}.{reader.ReadUint8(0x3)}",
                            IsTeams = reader.ReadBool(0xd),
                            IsPAL = reader.ReadBool(0x1a1),
                            StageId = reader.ReadUint16(0x13),
                            Players = new List<int> { 0, 1, 2, 3 }.Select(getPlayerObject).ToList(),
                            Scene = reader.ReadUint8(0x1a3),
                            GameMode = (GameMode)reader.ReadUint8(0x1a4),
                        };
                    case Command.PRE_FRAME_UPDATE:
                        return new PreFrameUpdateType
                        {
                            Frame = reader.ReadInt32(0x1),
                            PlayerIndex = reader.ReadUint8(0x5),
                            IsFollower = reader.ReadBool(0x6),
                            Seed = reader.ReadUint32(0x7),
                            ActionStateId = reader.ReadUint16(0xb),
                            PositionX = reader.ReadFloat(0xd),
                            PositionY = reader.ReadFloat(0x11),
                            FacingDirection = reader.ReadFloat(0x15),
                            JoystickX = reader.ReadFloat(0x19),
                            JoystickY = reader.ReadFloat(0x1d),
                            CStickX = reader.ReadFloat(0x21),
                            CStickY = reader.ReadFloat(0x25),
                            Trigger = reader.ReadFloat(0x29),
                            Buttons = reader.ReadUint32(0x2d),
                            PhysicalButtons = reader.ReadUint16(0x31),
                            PhysicalLTrigger = reader.ReadFloat(0x33),
                            PhysicalRTrigger = reader.ReadFloat(0x37),
                            Percent = reader.ReadFloat(0x3c)
                        };
                    case Command.POST_FRAME_UPDATE:
                        var selfInducedSpeeds = new SelfInducedSpeedsType
                        {
                            AirX = reader.ReadFloat(0x35),
                            Y = reader.ReadFloat(0x39),
                            AttackX = reader.ReadFloat(0x3d),
                            AttackY = reader.ReadFloat(0x41),
                            GroundX = reader.ReadFloat(0x45)
                        };
                        return new PostFrameUpdateType
                        {
                            Frame = reader.ReadInt32(0x1),
                            PlayerIndex = reader.ReadUint8(0x5),
                            IsFollower = reader.ReadBool(0x6),
                            InternalCharacterId = reader.ReadUint8(0x7),
                            ActionStateId = reader.ReadUint16(0x8),
                            PositionX = reader.ReadFloat(0xa),
                            PositionY = reader.ReadFloat(0xe),
                            FacingDirection = reader.ReadFloat(0x12),
                            Percent = reader.ReadFloat(0x16),
                            ShieldSize = reader.ReadFloat(0x1a),
                            LastAttackLanded = reader.ReadUint8(0x1e),
                            CurrentComboCount = reader.ReadUint8(0x1f),
                            LastHitBy = reader.ReadUint8(0x20),
                            StocksRemaining = reader.ReadUint8(0x21),
                            ActionStateCounter = reader.ReadFloat(0x22),
                            MiscActionState = reader.ReadFloat(0x2b),
                            IsAirborne = reader.ReadBool(0x2f),
                            LastGroundId = reader.ReadUint16(0x30),
                            JumpsRemaining = reader.ReadUint8(0x32),
                            LCancelStatus = reader.ReadUint8(0x33),
                            HurtboxCollisionState = reader.ReadUint8(0x34),
                            SelfInducedSpeeds = selfInducedSpeeds
                        };
                    case Command.ITEM_UPDATE:
                        return new ItemUpdateType
                        {
                            Frame = reader.ReadInt32(0x1),
                            TypeId = reader.ReadUint16(0x5),
                            State = reader.ReadUint8(0x7),
                            FacingDirection = reader.ReadFloat(0x8),
                            VelocityX = reader.ReadFloat(0xc),
                            VelocityY = reader.ReadFloat(0x10),
                            PositionX = reader.ReadFloat(0x14),
                            PositionY = reader.ReadFloat(0x18),
                            DamageTaken = reader.ReadUint16(0x1c),
                            ExpirationTimer = reader.ReadFloat(0x1e),
                            SpawnId = reader.ReadUint32(0x22),
                            MissileType = reader.ReadUint8(0x26),
                            TurnipFace = reader.ReadUint8(0x27),
                            ChargeShotLaunched = reader.ReadUint8(0x28),
                            ChargePower = reader.ReadUint8(0x29),
                            Owner = reader.ReadInt8(0x2a)
                        };
                    case Command.FRAME_BOOKEND:
                        return new FrameBookendType
                        {
                            Frame = reader.ReadInt32(0x1),
                            LatestFinalizedFrame = reader.ReadInt32(0x5),
                        };
                    case Command.GAME_END:
                        return new GameEndType
                        {
                            GameEndMethod = reader.ReadUint8(0x1),
                            LrasInitiatorIndex = reader.ReadUint8(0x2),
                        };
                    default:
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
        private class SlpPayloadReader : IDisposable
        {
            private readonly BinaryReader reader;
            public SlpPayloadReader(byte[] payload)
            {
                reader = new BinaryReader(new MemoryStream(payload));
            }

            public void Dispose()
            {
                reader.Dispose();
            }

            public float? ReadFloat(int offset)
            {
                if (!CanRead(offset, 4))
                {
                    return null;
                }
                reader.BaseStream.Position = offset;
                var raw = reader.ReadBytes(4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(raw);
                }
                return BitConverter.ToSingle(raw, 0);
            }

            public int? ReadInt32(int offset)
            {
                if (!CanRead(offset, 4))
                {
                    return null;
                }
                reader.BaseStream.Position = offset;
                var raw = reader.ReadBytes(4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(raw);
                }
                return BitConverter.ToInt32(raw, 0);
            }

            public int? ReadInt8(int offset)
            {
                if (!CanRead(offset, 1))
                {
                    return null;
                }
                reader.BaseStream.Position = offset;
                return reader.ReadSByte();
            }

            public uint? ReadUint32(int offset)
            {
                if (!CanRead(offset, 4))
                {
                    return null;
                }
                reader.BaseStream.Position = offset;
                var raw = reader.ReadBytes(4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(raw);
                }
                return BitConverter.ToUInt32(raw, 0);
            }

            public int? ReadUint16(int offset)
            {
                if (!CanRead(offset, 2))
                {
                    return null;
                }
                reader.BaseStream.Position = offset;
                var raw = reader.ReadBytes(2);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(raw);
                }
                return BitConverter.ToUInt16(raw, 0);
            }

            public int? ReadUint8(int offset) {
                if (!CanRead(offset, 1))
                {
                    return null;
                }
                reader.BaseStream.Position = offset;
                return reader.ReadByte();
            }

            public bool? ReadBool(int offset)
            {
                if (!CanRead(offset, 1))
                {
                    return null;
                }
                reader.BaseStream.Position = offset;
                return reader.ReadBoolean();
            }

            private bool CanRead(int offset, int length) => offset + length <= reader.BaseStream.Length;
        }
    }
}
