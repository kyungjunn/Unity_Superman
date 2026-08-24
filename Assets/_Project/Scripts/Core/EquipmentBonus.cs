namespace GrowNa.Core
{
    public struct EquipmentBonus
    {
        public double attackMultiplier;
        public double healthMultiplier;
        public double defenseMultiplier;

        public static EquipmentBonus None => new EquipmentBonus
        {
            attackMultiplier = 1.0,
            healthMultiplier = 1.0,
            defenseMultiplier = 1.0,
        };
    }
}
