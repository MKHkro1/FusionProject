using UnityEngine;

namespace WaterPot.BepInEx
{
    internal static class WaterPotGridUtility
    {
        public static bool ContainsWaterPotAt(int column, int row)
        {
            if (column < 0 || row < 0)
            {
                return false;
            }

            Il2CppSystem.Collections.Generic.List<Plant>? plants = null;

            try
            {
                plants = Lawnf.Get1x1Plants(column, row);
            }
            catch
            {
                return false;
            }

            if (plants == null || plants.Count == 0)
            {
                return false;
            }

            for (int i = plants.Count - 1; i >= 0; i--)
            {
                var plant = plants[i];
                if (plant == null)
                {
                    continue;
                }

                if (plant.thePlantType == (PlantType)Core.PlantID)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasWaterPotNearby(int column, int row, int detectionRange = 1)
        {
            if (Board.Instance == null)
            {
                return false;
            }

            if (detectionRange < 0)
            {
                detectionRange = 0;
            }

            for (int dc = -detectionRange; dc <= detectionRange; dc++)
            {
                for (int dr = -detectionRange; dr <= detectionRange; dr++)
                {
                    if (ContainsWaterPotAt(column + dc, row + dr))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
