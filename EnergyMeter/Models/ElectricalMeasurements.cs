
namespace EnergyMeter.Models
{
    public class ElectricalMeasurements
    {
        public float Voltage { get; set; }
        public float Current { get; set; }
        public float Power { get; set; }
        public uint Energy { get; set; }
        public float Frequency { get; set; }
        public float PowerFactor { get; set; }

        public override string ToString()
         => $"V: {Voltage:0.0}V | I: {Current:0.000}A | P: {Power:0.0}W | E: {Energy}Wh | F: {Frequency:0.0}Hz | PF: {PowerFactor:0.00}";
    }
}
