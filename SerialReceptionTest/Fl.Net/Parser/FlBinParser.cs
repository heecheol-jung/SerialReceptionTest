using Fl.Net.Message;
using Fl.Net.PInvoke;
using Serilog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Fl.Net.Parser
{
    public class FlBinParser
    {
        enum ReceiveState
        {
            Stx,

            Uid,

            Length,

            Payload
        }

        #region Private Constants
        #endregion

        #region Private Data
        private byte[] _buf = new byte[FlConstant.FL_BIN_MSG_MAX_LENGTH];
        GCHandle _bufPinned;
        IntPtr _bufPtr;
        private int _bufPos = 0;
        private int _count = 0;
        private int _payloadLength = 0;
        private ReceiveState _receiveState;
        //private Stopwatch _stopWatch = new Stopwatch();
        private StringBuilder _sb = new StringBuilder();
        UInt16 _crc16 = 0;
        #endregion

        #region Public Properties
        public object Context { get; set; } = null;
        public FlParserRole Role { get; set; } = FlParserRole.Host;
        #endregion

        #region Constructors
        public FlBinParser()
        {
            _bufPinned = GCHandle.Alloc(_buf, GCHandleType.Pinned);
            _bufPtr = _bufPinned.AddrOfPinnedObject();
        }
        #endregion

        #region Destructors
        ~FlBinParser()
        {
            if (_bufPinned.IsAllocated == true)
            {
                _bufPinned.Free();
            }
        }
        #endregion

        #region Public Methods
        public FlParseState Parse(byte data, out IFlMessage message)
        {
            FlParseState ret = FlParseState.Parsing;

            message = null;

            switch (_receiveState)
            {
                case ReceiveState.Stx:
                    if (data == FlConstant.FL_BIN_MSG_STX)
                    {
                        _buf[_bufPos++] = data;
                        _receiveState = ReceiveState.Uid;
                        _count = 0;
                    }
                    else
                    {
                        ret = FlParseState.ParseFail;
                        Log.Error("STX fail");
                    }
                    break;

                case ReceiveState.Uid:
                    if (_count < FlConstant.FL_BIN_MSG_DEVICE_ID_LENGTH)
                    {
                        _buf[_bufPos++] = data;
                        _count++;
                        if (_count == FlConstant.FL_BIN_MSG_DEVICE_ID_LENGTH)
                        {
                            _count = 0;
                            _receiveState = ReceiveState.Length;
                        }
                    }
                    else
                    {
                        ret = FlParseState.ParseFail;
                        Log.Error("UID fail");
                    }
                    break;

                case ReceiveState.Length:
                    if (_count < FlConstant.FL_BIN_MSG_LENGTH_FIELD_LENGTH)
                    {
                        _buf[_bufPos++] = data;
                        _count++;
                        if (_count == FlConstant.FL_BIN_MSG_LENGTH_FIELD_LENGTH)
                        {
                            _count = 0;
                            _payloadLength = BitConverter.ToUInt16(_buf, _bufPos-2);
                            _receiveState = ReceiveState.Payload;
                        }
                    }
                    else
                    {
                        ret = FlParseState.ParseFail;
                        Log.Error("Length fail");
                    }
                    break;

                case ReceiveState.Payload:
                    // Flag1 ~ FL_BIN_MSG_ETX
                    if (_count < _payloadLength)
                    {
                        // Check buffer overflow.
                        if (_bufPos < _buf.Length)
                        {
                            _buf[_bufPos++] = data;
                            _count++;
                            if (_count == _payloadLength)
                            {
                                _count = 0;
                                ret = FlParseState.ParseOk;
                            }
                        }
                        else
                        {
                            ret = FlParseState.ParseFail;
                            Log.Error("Buffer overflow fail");
                        }
                    }
                    else
                    {
                        FlBinMsgFullStruct fullMsg = Marshal.PtrToStructure<FlBinMsgFullStruct>(_bufPtr);
                        ret = FlParseState.ParseFail;
                        Log.Error($"Payload length fail, Message : {fullMsg.header.message_id}");
                    }
                    break;
            }

            if (ret != FlParseState.Parsing)
            {
                if (ret == FlParseState.ParseOk)
                {
                    if (Role == FlParserRole.Host)
                    {
                        // Process response.
                        //if (MakeResponseObject2(out message) != true)
                        if (MakeResponseEventObject(ref message) != true)
                        {
                            ret = FlParseState.ParseFail;
                        }
                    }
                    else if (Role == FlParserRole.Device)
                    {
                        // Process command.
                        if (MakeCommandObject(ref message) != true)
                        {
                            ret = FlParseState.ParseFail;
                        }
                    }
                }

                // Clear parser data.
                Clear();
            }

            return ret;
        }

        public void Clear()
        {
            Array.Clear(_buf, 0, _buf.Length);
            _bufPos = 0;
            _count = 0;
            _payloadLength = 0;
            _receiveState = ReceiveState.Stx;
            //private Stopwatch _stopWatch = new Stopwatch();
            _sb.Clear();
            _crc16 = 0;
        }
        #endregion

        #region Private Methods
        private bool CompareCrc(UInt16 expectedValue, UInt16 actualValue)
        {
            if (expectedValue == actualValue)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private unsafe bool MakeCommandObject(ref IFlMessage message)
        {
            bool ret = false;
            int crcDataLen = _bufPos - 4;
            int crcStartIndex = _bufPos - 3;
            List<object> arguments = null;
            FlMessageId messageId = FlMessageId.Unknown;
            ushort receivedCrc = 0;
            FlBinMsgFullStruct fullMsg = Marshal.PtrToStructure<FlBinMsgFullStruct>(_bufPtr);
            
            messageId = (FlMessageId)fullMsg.header.message_id;
            switch (messageId)
            {
                case FlMessageId.ReadHardwareVersion:
                case FlMessageId.ReadFirmwareVersion:
                case FlMessageId.Reset:
                    {
                        _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
                        receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);
                        if ((CompareCrc(_crc16, receivedCrc) == true) &&
                            (FlConstant.FL_BIN_MSG_ETX == _buf[_bufPos-1]))
                        {
                            ret = true;
                        }
                    }
                    break;

                case FlMessageId.ReadGpio:
                    {
                        _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
                        receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);
                        if ((CompareCrc(_crc16, receivedCrc) == true) &&
                            (FlConstant.FL_BIN_MSG_ETX == _buf[_bufPos - 1]))
                        {
                            arguments = new List<object>
                            {
                                fullMsg.payload[0] // GPIO number
                            };
                            ret = true;
                        }
                    }
                    break;

                case FlMessageId.WriteGpio:
                    {
                        _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
                        receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);
                        if ((CompareCrc(_crc16, receivedCrc) == true) &&
                            (FlConstant.FL_BIN_MSG_ETX == _buf[_bufPos - 1]))
                        {
                            arguments = new List<object>
                            {
                                fullMsg.payload[0],  // GPIO number
                                fullMsg.payload[1]   // GPIO value
                            };
                            ret = true;
                        }
                    }
                    break;

                case FlMessageId.ReadTemperature:
                case FlMessageId.ReadHumidity:
                case FlMessageId.ReadTempAndHum:
                    {
                        _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
                        receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);
                        if ((CompareCrc(_crc16, receivedCrc) == true) &&
                            (FlConstant.FL_BIN_MSG_ETX == _buf[_bufPos - 1]))
                        {
                            arguments = new List<object>
                            {
                                fullMsg.payload[0] // Sensor number
                            };
                            ret = true;
                        }
                    }
                    break;

                case FlMessageId.BootMode:
                    {
                        _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
                        receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);
                        if ((CompareCrc(_crc16, receivedCrc) == true) &&
                            (FlConstant.FL_BIN_MSG_ETX == _buf[_bufPos - 1]))
                        {
                            arguments = new List<object>
                            {
                                fullMsg.payload[0]  // Boot mode
                            };
                            ret = true;
                        }
                    }
                    break;

                case FlMessageId.ReadWriteI2C:
                    {
                        _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
                        receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);
                        if ((CompareCrc(_crc16, receivedCrc) == true) &&
                            (FlConstant.FL_BIN_MSG_ETX == _buf[_bufPos - 1]))
                        {
                            if ((fullMsg.header.length - 6) >= 8)
                            {
                                byte i2cNumber = fullMsg.payload[0];
                                ushort targetDevAddr = (ushort)((fullMsg.payload[2] << 8) | fullMsg.payload[1]);
                                byte i2cRwMode = fullMsg.payload[3];
                                ushort dataLength = (ushort)((fullMsg.payload[5] << 8) | fullMsg.payload[4]);
                                ushort dataLength2 = (ushort)((fullMsg.payload[7] << 8) | fullMsg.payload[6]);
                                if (dataLength > 0)
                                {
                                    byte[] data = new byte[dataLength];
                                    fixed (byte* pDest = data)
                                    {
                                        for (int i = 0; i < dataLength; i++)
                                        {
                                            data[i] = fullMsg.payload[i + 8];
                                        }
                                    }

                                    arguments = new List<object>
                                    {
                                        i2cNumber,
                                        targetDevAddr,
                                        i2cRwMode,
                                        dataLength,
                                        dataLength2,
                                        data
                                    };

                                    ret = true;
                                }
                            }
                        }
                    }
                    break;

                case FlMessageId.ReadAccelGyro:
                    {
                        _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
                        receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);
                        if ((CompareCrc(_crc16, receivedCrc) == true) &&
                            (FlConstant.FL_BIN_MSG_ETX == _buf[_bufPos - 1]))
                        {
                            ushort actualSamples = (ushort)((fullMsg.payload[2] << 8) | fullMsg.payload[1]);
                            arguments = new List<object>
                            {
                                fullMsg.payload[0], // Sensor number
                                actualSamples
                            };
                            ret = true;
                        }
                    }
                    break;

                case FlMessageId.StartAccelGyro:
                    {
                        _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
                        receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);
                        if ((CompareCrc(_crc16, receivedCrc) == true) &&
                            (FlConstant.FL_BIN_MSG_ETX == _buf[_bufPos - 1]))
                        {
                            arguments = new List<object>
                            {
                                fullMsg.payload[0], // Sensor number
                                fullMsg.payload[1]  // Start/stop
                            };
                            ret = true;
                        }
                    }
                    break;
            }

            if (ret == true)
            {
                FlBinMessageCommand command = new FlBinMessageCommand
                {
                    Arguments = arguments
                };
                command.Header.device_id = fullMsg.header.device_id;
                command.Header.length = fullMsg.header.length;
                command.Header.message_id = (FlMessageId)fullMsg.header.message_id;
                command.Header.flag1 = fullMsg.header.flag1;
                command.Header.flag2 = fullMsg.header.flag2;

                message = command;
            }

            return ret;
        }

        private bool MakeResponseEventObject(ref IFlMessage message)
        {
            FlBinMsgFullStruct fullMsg = Marshal.PtrToStructure<FlBinMsgFullStruct>(_bufPtr);
            
            switch (fullMsg.header.flag1.message_type)
            {
                case (byte)FlMessageCategory.Response:
                    return MakeResponseObject(fullMsg, ref message);

                case (byte)FlMessageCategory.Event:
                    return MakeEventObject(fullMsg, ref message);
            }
            
            return false;
        }

        private unsafe bool MakeEventObject(FlBinMsgFullStruct fullMsg, ref IFlMessage message)
        {
            bool ret = false;
            ushort receivedCrc = 0;
            int crcDataLen = _bufPos - 4;
            int crcStartIndex = _bufPos - 3;
            List<object> arguments = null;
            FlMessageId messageId = (FlMessageId)fullMsg.header.message_id;

            switch (messageId)
            {
                case FlMessageId.ButtonEvent:
                    {
                        _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
                        receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);

                        if ((CompareCrc(_crc16, receivedCrc) == true) &&
                            (FlConstant.FL_BIN_MSG_ETX == _buf[_bufPos - 1]))
                        {
                            arguments = new List<object>()
                            {
                                fullMsg.payload[0], // Button number
                                fullMsg.payload[1]  // Button value
                            };
                            ret = true;
                        }
                    }
                    break;

                case FlMessageId.AccelGyroEvent:
                    {
                        _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
                        receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);

                        if ((CompareCrc(_crc16, receivedCrc) == true) &&
                            (FlConstant.FL_BIN_MSG_ETX == _buf[_bufPos - 1]))
                        {
                            arguments = new List<object>()
                            {
                                fullMsg.payload[0], // Sensor number
                                fullMsg.payload[1]  // Sample count
                            };

                            int pos = 2;
                            int ii = 0;
                            short rawValue = 0;
                            List<FlAccelGyroRawData> accelGyroData = new List<FlAccelGyroRawData>();
                            int length = (byte)arguments[1] * 12;
                            FlAccelGyroRawData rawAccelGyroData = new FlAccelGyroRawData();

                            for (int i = 0; i < length; i += 2)
                            {
                                if (i != 0 && i % 12 == 0)
                                {
                                    rawAccelGyroData = new FlAccelGyroRawData();
                                }

                                rawValue = (short)(fullMsg.payload[pos + 1] << 8 | fullMsg.payload[pos]);

                                if (ii == 0)
                                {
                                    rawAccelGyroData.Ax = rawValue;
                                }
                                else if (ii == 1)
                                {
                                    rawAccelGyroData.Ay = rawValue;
                                }
                                else if (ii == 2)
                                {
                                    rawAccelGyroData.Az = rawValue;
                                }
                                else if (ii == 3)
                                {
                                    rawAccelGyroData.Gx = rawValue;
                                }
                                else if (ii == 4)
                                {
                                    rawAccelGyroData.Gy = rawValue;
                                }
                                else if (ii == 5)
                                {
                                    rawAccelGyroData.Gz = rawValue;
                                    accelGyroData.Add(rawAccelGyroData);
                                }

                                pos += 2;
                                ii++;
                                if (ii > 5)
                                {
                                    ii = 0;
                                }
                            }
                            arguments.Add(accelGyroData);
                            ret = true;
                        }
                    }
                    break;
            }

            if (ret == true)
            {
                FlBinMessageEvent evt = new FlBinMessageEvent()
                {
                    Arguments = arguments,
                    ReceiveTime = DateTime.UtcNow
                };
                evt.Header.device_id = fullMsg.header.device_id;
                evt.Header.length = fullMsg.header.length;
                evt.Header.message_id = (FlMessageId)fullMsg.header.message_id;
                evt.Header.flag1 = fullMsg.header.flag1;
                evt.Header.flag2 = fullMsg.header.flag2;
                evt.Buffer = new byte[_bufPos];
                Array.Copy(_buf, evt.Buffer, _bufPos);

                message = evt;
            }
            else
            {
                Log.Error("MakeEventObject fail");
            }

            return ret;
        }

        private unsafe bool MakeResponseObject(FlBinMsgFullStruct fullMsg, ref IFlMessage message)
        {
            bool ret = false;
            ushort receivedCrc = 0;
            int crcDataLen = _bufPos - 4;
            int crcStartIndex = _bufPos - 3;
            List<object> arguments = null;
            FlMessageId messageId = (FlMessageId)fullMsg.header.message_id;

            // Payload size = header.length - 6;

            // stx header payload crc etx
            // crc : header payload, 
            // crc data length = received bytes - stx(1) - crc(2) - etx(1)

            _crc16 = FlUtil.CRC16(_buf, 1, crcDataLen);
            receivedCrc = BitConverter.ToUInt16(_buf, crcStartIndex);
            if ((CompareCrc(_crc16, receivedCrc) != true) ||
                (FlConstant.FL_BIN_MSG_ETX != _buf[_bufPos - 1]))
            {
                return ret;
            }

            if (fullMsg.header.flag2.error == FlConstant.FL_OK)
            {
                switch (messageId)
                {
                    case FlMessageId.ReadHardwareVersion:
                    case FlMessageId.ReadFirmwareVersion:
                        {
                            string hwVerString = Encoding.ASCII.GetString(fullMsg.payload, fullMsg.header.length - 6);
                            arguments = new List<object>()
                            {
                                hwVerString
                            };
                            ret = true;
                        }
                        break;

                    case FlMessageId.ReadGpio:
                        {
                            arguments = new List<object>()
                            {
                                fullMsg.payload[0], // GPIO number
                                fullMsg.payload[1]  // GPIO value
                            };
                            ret = true;
                        }
                        break;

                    case FlMessageId.WriteGpio:
                    case FlMessageId.BootMode:
                    case FlMessageId.Reset:
                    case FlMessageId.StartAccelGyro:
                        {
                            ret = true;
                        }
                        break;

                    case FlMessageId.ReadTemperature:
                    case FlMessageId.ReadHumidity:
                        {
                            arguments = new List<object>()
                            {
                                fullMsg.payload[0], // Sensor number
                                BitConverter.ToDouble(_buf, _bufPos - 3 - sizeof(double))   // Sensor value
                            };
                            ret = true;
                        }
                        break;

                    case FlMessageId.ReadTempAndHum:
                        {
                            arguments = new List<object>()
                            {
                                fullMsg.payload[0], // Sensor number
                                BitConverter.ToDouble(_buf, _bufPos - 3 - sizeof(double)*2),    // Temperature
                                BitConverter.ToDouble(_buf, _bufPos - 3 - sizeof(double))       // Humidity
                            };
                            ret = true;
                        }
                        break;

                    case FlMessageId.ReadWriteI2C:
                        {
                            if (fullMsg.header.flag1.reserved == 0)
                            {
                                if ((fullMsg.header.length - 6) >= 7)
                                {
                                    byte i2cNumber = fullMsg.payload[0];
                                    ushort targetDevAddr = (ushort)((fullMsg.payload[2] << 8) | fullMsg.payload[1]);
                                    ushort dataLength = (ushort)((fullMsg.payload[4] << 8) | fullMsg.payload[3]);
                                    ushort dataLength2 = (ushort)((fullMsg.payload[6] << 8) | fullMsg.payload[5]);
                                    if (dataLength > 0)
                                    {
                                        byte[] data = new byte[dataLength];
                                        fixed (byte* pDest = data)
                                        {
                                            for (int i = 0; i < dataLength; i++)
                                            {
                                                data[i] = fullMsg.payload[i + 7];
                                            }
                                        }

                                        arguments = new List<object>
                                        {
                                            i2cNumber,
                                            targetDevAddr,
                                            dataLength,
                                            dataLength2,
                                            data
                                        };

                                        ret = true;
                                    }
                                }
                            }
                            else if (fullMsg.header.flag1.reserved == 1) // No payload flag
                            {
                                ret = true;
                            }
                        }
                        break;

                    case FlMessageId.ReadAccelGyro:
                        {
                            ushort actualSamples = (ushort)((fullMsg.payload[2] << 8) | fullMsg.payload[1]);
                            arguments = new List<object>()
                            {
                                fullMsg.payload[0], // Sensor number
                                actualSamples
                            };
                            ret = true;
                        }
                        break;

                }
            }
            else if (fullMsg.header.flag2.error == FlConstant.FL_ERROR)
            {
                ret = true;
            }

            if (ret == true)
            {
                FlBinMessageResponse response = new FlBinMessageResponse()
                {
                    Arguments = arguments,
                    ReceiveTime = DateTime.UtcNow
                };
                response.Header.device_id = fullMsg.header.device_id;
                response.Header.length = fullMsg.header.length;
                response.Header.message_id = (FlMessageId)fullMsg.header.message_id;
                response.Header.flag1 = fullMsg.header.flag1;
                response.Header.flag2 = fullMsg.header.flag2;
                response.Buffer = new byte[_bufPos];
                Array.Copy(_buf, response.Buffer, _bufPos);

                message = response;
            }

            return ret;
        }
        #endregion
    }
}
