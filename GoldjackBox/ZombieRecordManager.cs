using System;
using System.Collections.Generic;
using System.Linq;

namespace UltimateGoldJackBoxZombieMod
{
    public class ZombieRecordManager
    {
        private Dictionary<string, List<ZombieDieRecord>> _recordsByPosition;
        private int _maxRecordsPerPosition;
        private const int MAX_CAPACITY = 5000;
        private int _currentTotalRecords;

        public ZombieRecordManager(int maxRecordsPerPosition = 5)
        {
            _recordsByPosition = new Dictionary<string, List<ZombieDieRecord>>();
            _maxRecordsPerPosition = maxRecordsPerPosition;
            _currentTotalRecords = 0;
        }

        public bool AddRecord(ZombieDieRecord newRecord)
        {
            if (_currentTotalRecords >= MAX_CAPACITY) return false;
            string positionKey = GetPositionKey(newRecord.row, newRecord.col);
            if (!_recordsByPosition.ContainsKey(positionKey))
            {
                _recordsByPosition[positionKey] = new List<ZombieDieRecord>();
                _recordsByPosition[positionKey].Add(newRecord);
                _currentTotalRecords++;
                return true;
            }
            if (_recordsByPosition[positionKey].Count >= _maxRecordsPerPosition)
            {
                ZombieDieRecord minRecord = _recordsByPosition[positionKey].OrderBy(r => r.health).First();
                if (newRecord.health <= minRecord.health) return false;
            }
            _recordsByPosition[positionKey].Add(newRecord);
            _currentTotalRecords++;
            MaintainTopRecordsForPosition(positionKey);
            return true;
        }

        public int AddRecords(IEnumerable<ZombieDieRecord> newRecords)
        {
            int num = 0;
            foreach (ZombieDieRecord newRecord in newRecords)
            {
                if (AddRecord(newRecord)) num++;
                if (_currentTotalRecords >= MAX_CAPACITY) break;
            }
            return num;
        }

        public static float GetTotalHealth(List<ZombieDieRecord> records)
        {
            float num = 0f;
            foreach (ZombieDieRecord record in records)
            {
                num += record.health;
            }
            return num;
        }

        public List<ZombieDieRecord> GetTopRecordsAroundPosition(int row, int col, bool isUltimate, int count)
        {
            List<ZombieDieRecord> list = new List<ZombieDieRecord>();
            int[] offsets = new int[] { -1, 0, 1 };
            foreach (int rowOffset in offsets)
            {
                foreach (int colOffset in offsets)
                {
                    int row2 = row + rowOffset;
                    int col2 = col + colOffset;
                    string positionKey = GetPositionKey(row2, col2);
                    if (_recordsByPosition.TryGetValue(positionKey, out List<ZombieDieRecord>? collection))
                    {
                        list.AddRange(collection);
                    }
                }
            }
            var source = list.OrderByDescending(r => r.health);
            List<ZombieDieRecord> list2;
            if (!isUltimate)
            {
                // 非究极模式：只复活究极僵尸
                list2 = source.Where(r => TypeMgr.UltimateZombie(r.zombieType))
                    .GroupBy(r => r.zombieType)
                    .Select(g => g.First())
                    .ToList();
            }
            else
            {
                // 究极模式：复活所有类型僵尸（每种类型只复活一个）
                list2 = source.GroupBy(r => r.zombieType)
                    .Select(g => g.First())
                    .ToList();
            }
            if (list2.Count <= count) return list2;
            return list2.Take(count).ToList();
        }

        private void MaintainTopRecordsForPosition(string positionKey)
        {
            if (_recordsByPosition.ContainsKey(positionKey))
            {
                List<ZombieDieRecord> list = _recordsByPosition[positionKey];
                if (list.Count > _maxRecordsPerPosition)
                {
                    List<ZombieDieRecord> list2 = list.OrderByDescending(record => record.health)
                        .Take(_maxRecordsPerPosition)
                        .ToList();
                    int num = list.Count - list2.Count;
                    _recordsByPosition[positionKey] = list2;
                    _currentTotalRecords -= num;
                }
            }
        }

        public List<ZombieDieRecord> GetTopRecordsForPosition(int row, int col)
        {
            string positionKey = GetPositionKey(row, col);
            if (_recordsByPosition.ContainsKey(positionKey))
            {
                return new List<ZombieDieRecord>(_recordsByPosition[positionKey]);
            }
            return new List<ZombieDieRecord>();
        }

        public Dictionary<string, int> GetPositionStatistics()
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            foreach (KeyValuePair<string, List<ZombieDieRecord>> kvp in _recordsByPosition)
            {
                dictionary[kvp.Key] = kvp.Value.Count;
            }
            return dictionary;
        }

        private string GetPositionKey(int row, int col)
        {
            return $"{row}-{col}";
        }

        public void DisplayAllRecords()
        {
            Console.WriteLine($"=== 各位置最大health记录 (总容量: {_currentTotalRecords}/{MAX_CAPACITY}) ===");
            foreach (var kvp in _recordsByPosition.OrderBy(x => x.Key))
            {
                Console.WriteLine($"位置 {kvp.Key}:");
                foreach (ZombieDieRecord value in kvp.Value)
                {
                    Console.WriteLine($"  {value}");
                }
            }
        }

        public (int current, int max) GetCapacityInfo()
        {
            return (_currentTotalRecords, MAX_CAPACITY);
        }

        public void Clear()
        {
            _recordsByPosition.Clear();
            _currentTotalRecords = 0;
        }

        public bool RemovePositionRecords(int row, int col)
        {
            string positionKey = GetPositionKey(row, col);
            if (_recordsByPosition.ContainsKey(positionKey))
            {
                int count = _recordsByPosition[positionKey].Count;
                _recordsByPosition.Remove(positionKey);
                _currentTotalRecords -= count;
                return true;
            }
            return false;
        }
    }
}
