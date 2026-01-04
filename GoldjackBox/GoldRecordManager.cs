using System.Collections.Generic;

namespace UltimateGoldJackBoxZombieMod
{
    public class GoldRecordManager
    {
        private HashSet<Zombie> _records;
        private Dictionary<Zombie, GoldBoxStateRecord> _stateRecords;

        public GoldRecordManager()
        {
            _records = new HashSet<Zombie>();
            _stateRecords = new Dictionary<Zombie, GoldBoxStateRecord>();
        }

        public void AddOrUpdatetStateRecord(Zombie zombie, GoldBoxStateRecord record)
        {
            if (!_stateRecords.ContainsKey(zombie))
            {
                _stateRecords.Add(zombie, record);
            }
            record.zombie = zombie;
            _stateRecords[zombie] = record;
        }

        public bool GetJumperStateRecord(Zombie zombie)
        {
            return _stateRecords.ContainsKey(zombie) && _stateRecords[zombie].isLoseJumper;
        }

        public void removeStateRecord(Zombie zombie)
        {
            if (!_stateRecords.ContainsKey(zombie)) return;
            _stateRecords.Remove(zombie);
        }

        public int StateRecordCount()
        {
            return _stateRecords.Count;
        }

        public bool AddRecord(Zombie newRecord)
        {
            if (_records.Contains(newRecord)) return false;
            _records.Add(newRecord);
            return true;
        }

        public int GetRecordCounts()
        {
            return _records.Count;
        }

        public HashSet<Zombie> GetRecords()
        {
            return _records;
        }

        public void setRecords(HashSet<Zombie> records)
        {
            _records = records;
        }

        public bool vaildRecord(Zombie zombie)
        {
            return _records.Contains(zombie);
        }

        public void RemoveRecord(Zombie zombie)
        {
            if (!_records.Contains(zombie)) return;
            _records.Remove(zombie);
        }

        public void Clear()
        {
            _records.Clear();
        }
    }
}
