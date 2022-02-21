using System;
using System.IO;
using System.Threading;

using NLog;

using EnergyMeter.Interfaces;
using EnergyMeter.Models;
using EnergyMeter.Enums;

namespace EnergyMeter
{

    public class Pzem004V3 : IEnergyMeter, IDisposable
    {
        private const byte slaveAddress = 0x01;
        private readonly ITransport port;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private bool autoread;
        private bool autoreadThreadRunning;
        public Pzem004V3(ITransport port)
        {
            this.port = port;
        }

        public ElectricalMeasurements Measurements { get; protected set; } = new();

        public int RefreshMs { get; set; } = 500;

        public event EventHandler<ElectricalMeasurements> NewReading;
        public event EventHandler<ErrorCode> Error;

        public void AutoRead()
        {
            autoread = true;
            new Thread(() =>
            {
                autoreadThreadRunning = true;
                while (autoread)
                {
                    ReadAll();
                    Thread.Sleep(RefreshMs);
                }
                autoreadThreadRunning = false;
            }).Start();
        }

        public void Stop()
        {
            autoread = false;
        }

        public void ReadAll()
        {
            using MemoryStream txStream = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(txStream);

            int answerLen = 25;

            byte[] answer = ModbusExchange(slaveAddress, 0x04, 0, 10, answerLen);

            if (answer.Length == answerLen)
            {
                using MemoryStream rxStream = new MemoryStream(answer);
                using BinaryReader br = new BinaryReader(rxStream);
                br.ReadBytes(3); // removing useless header
                Measurements.Voltage = br.ReadUInt16Reverse() / 10.0f;
                Measurements.Current = br.ReadUInt32Reverse() / 1000.0f;
                Measurements.Power = br.ReadUInt32Reverse() / 10.0f;
                Measurements.Energy = br.ReadUInt32Reverse();
                Measurements.Frequency = br.ReadUInt16Reverse() / 10.0f;
                Measurements.PowerFactor = br.ReadUInt16Reverse() / 100.0f;

                log.Debug(Measurements.ToString());
                NewReading?.Invoke(this, Measurements);
            }
        }

        public void EnergyReset()
        {
            ModbusExchange(slaveAddress, 0x42, 0, 0, 4);
        }

        public void Dispose()
        {
            autoread = false;
            SpinWait.SpinUntil(() => autoreadThreadRunning == false, RefreshMs * 3);
            port.Dispose();
        }

        private byte[] ModbusExchange(byte slave, byte funct, ushort start, ushort count, int expectedLen = 0)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(slave);
            bw.Write(funct);
            if (count > 0)
            {
                bw.WriteUInt16Reverse(start);
                bw.WriteUInt16Reverse(count);
            }
            ushort crc = ModbusRtuCrc(ms.GetBuffer(), (int)ms.Length);
            bw.Write(crc);

            byte[] tx = ms.ToArray();
            byte[] rxData = Array.Empty<byte>();
            log.Trace($"TX: {tx.ToHex()}");
            try
            {
                port.Write(tx);
                rxData = port.Read(expectedLen);
                log.Trace($"RX: {rxData.ToHex()}");
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                Error?.Invoke(this, ErrorCode.Communication);
                return rxData;
            }

            if (rxData.Length == 0)
            {
                log.Warn("Timeout during communication");
                Error?.Invoke(this, ErrorCode.Timeout);
            }
            else if (!CheckCrc(rxData))
            {
                log.Warn("Invalid crc received");
                Error?.Invoke(this, ErrorCode.Crc);
                rxData = Array.Empty<byte>();
            }

            return rxData;
        }

        // Compute the MODBUS RTU CRC
        static ushort ModbusRtuCrc(byte[] buf, int len)
        {
            ushort crc = 0xFFFF;

            for (int pos = 0; pos < len; pos++)
            {
                crc ^= buf[pos];

                for (int i = 8; i != 0; i--)
                {    // Loop over each bit
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                        crc >>= 1;
                }
            }
            return crc;
        }

        static bool CheckCrc(byte[] buffer)
        {
            ushort rxCrc = ModbusRtuCrc(buffer, buffer.Length - 2);
            return buffer[^1] == (rxCrc >> 8) && buffer[^2] == (rxCrc & 0xff);
        }
    }
}
