using System;
using EnergyMeter.Enums;
using EnergyMeter.Models;

namespace EnergyMeter.Interfaces
{
    public interface IEnergyMeter
    {
        ElectricalMeasurements Measurements { get; }

        event EventHandler<ElectricalMeasurements> NewReading;
        event EventHandler<ErrorCode> Error;
    }
}
