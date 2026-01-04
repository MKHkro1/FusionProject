using System;
using System.Collections.Generic;

namespace UltimateGoldJackBoxZombieMod
{
    public class PlantRecordManager
    {
        private Dictionary<Plant, Zombie> _records;

        public PlantRecordManager()
        {
            _records = new Dictionary<Plant, Zombie>();
        }

        public bool AddRecord(Plant newRecord, Zombie z)
        {
            if (_records.ContainsKey(newRecord))
            {
                return false;
            }
            _records[newRecord] = z;
            return true;
        }

        public int GetRecordCounts()
        {
            return _records.Count;
        }

        public Zombie? GetRecord(Plant p)
        {
            if (!_records.ContainsKey(p))
            {
                return null;
            }
            return _records[p];
        }

        public void RemoveRecord(Plant p)
        {
            if (!_records.ContainsKey(p))
            {
                return;
            }
            _records.Remove(p);
        }
    }
}
