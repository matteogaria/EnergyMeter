﻿using System;
using System.IO;
using System.Threading;

using NLog;

using EnergyMeter.Interfaces;
using EnergyMeter.Models;

namespace EnergyMeter
{

    public class Pzem004V3 : IDisposable
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
        public void AutoRead()
        {          
            autoread = true;
            new Thread(() =>
            {
                autoreadThreadRunning = true;
                while(autoread)
                {
                    ReadAll();
                    Thread.Sleep(RefreshMs);
                }
                autoreadThreadRunning = false;
            }).Start();
        }

        public void ReadAll()
        {
            using MemoryStream txStream = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(txStream);

            byte[] answer = ModbusExchange(slaveAddress, 0x04, 0, 10, 25);

            if (CheckCrc(answer))
            {
                using MemoryStream rxStream = new MemoryStream(answer);
                using BinaryReader br = new BinaryReader(rxStream);
                br.ReadBytes(3); // removing useless header
                Measurements.Voltage = Reverse(br.ReadUInt16()) / 10.0f;
                Measurements.Current = Reverse(br.ReadUInt32()) / 1000.0f;
                Measurements.Power = Reverse(br.ReadUInt32()) / 10.0f;
                Measurements.Energy = Reverse(br.ReadUInt32());
                Measurements.Frequency = Reverse(br.ReadUInt16()) / 10.0f;
                Measurements.PowerFactor = Reverse(br.ReadUInt16()) / 100.0f;
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
                bw.Write(Reverse(start));
                bw.Write(Reverse(count));
            }
            ushort crc = ModbusRtuCrc(ms.GetBuffer(), (int)ms.Length);
            bw.Write(crc);

            byte[] tx = ms.ToArray();
            log.Trace($"TX: {tx.ToHex()}");
            port.Write(tx);

            byte[] rxData = port.Read(expectedLen);
            log.Trace($"RX: {rxData.ToHex()}");
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
            // Note, this number has low and high bytes swapped, so use it accordingly (or swap bytes)
            return crc;
        }

        static bool CheckCrc(byte[] buffer)
        {
            ushort rxCrc = ModbusRtuCrc(buffer, buffer.Length - 2);
            return buffer[^1] == (rxCrc >> 8) && buffer[^2] == (rxCrc & 0xff);
        }

        private static ushort Reverse(ushort x) => (ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff));

        private static uint Reverse(uint x)
        {
            // swap adjacent 16-bit blocks
            x = (x >> 16) | (x << 16);
            // swap adjacent 8-bit blocks
            return (uint)((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }
    }
}
