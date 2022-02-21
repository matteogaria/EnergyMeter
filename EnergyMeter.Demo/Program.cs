using EnergyMeter.Interfaces;
using EnergyMeter.Transport;
using NLog;
using System;

namespace EnergyMeter.Demo
{
    class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            ITransport port = new SerialTransport("COM10", 9600);
            port.Open();
            Pzem004V3 meter = new Pzem004V3(port);
            meter.AutoRead();

            meter.Error += (m, e) => log.Error($"error notification: {e}");
            meter.NewReading += (m, r) => log.Info("Reading");

            Console.ReadKey();
        }
    }
}
