using System;
using System.Collections.Generic;
using System.Linq;

namespace UltimateGoldJackBoxZombieMod
{
	public class ZombieRecordManager
	{
		public ZombieRecordManager(int maxRecordsPerPosition = 5)
		{
			this._recordsByPosition = new Dictionary<string, List<ZombieDieRecord>>();
			this._maxRecordsPerPosition = maxRecordsPerPosition;
			this._currentTotalRecords = 0;
		}

		public bool AddRecord(ZombieDieRecord newRecord)
		{
			if (this._currentTotalRecords >= 5000)
			{
				return false;
			}
			string positionKey = this.GetPositionKey(newRecord.row, newRecord.col);
			if (!this._recordsByPosition.ContainsKey(positionKey))
			{
				this._recordsByPosition[positionKey] = new List<ZombieDieRecord>();
				this._recordsByPosition[positionKey].Add(newRecord);
				this._currentTotalRecords++;
				return true;
			}
			if (this._recordsByPosition[positionKey].Count >= this._maxRecordsPerPosition)
			{
				ZombieDieRecord zombieDieRecord = (from r in this._recordsByPosition[positionKey]
				orderby r.health
				select r).First<ZombieDieRecord>();
				if (newRecord.health <= zombieDieRecord.health)
				{
					return false;
				}
			}
			this._recordsByPosition[positionKey].Add(newRecord);
			this._currentTotalRecords++;
			this.MaintainTopRecordsForPosition(positionKey);
			return true;
		}

		public int AddRecords(IEnumerable<ZombieDieRecord> newRecords)
		{
			int num = 0;
			foreach (ZombieDieRecord newRecord in newRecords)
			{
				if (this.AddRecord(newRecord))
				{
					num++;
				}
				if (this._currentTotalRecords >= 5000)
				{
					break;
				}
			}
			return num;
		}

		public static float GetTotalHealth(List<ZombieDieRecord> records)
		{
			float num = 0f;
			foreach (ZombieDieRecord zombieDieRecord in records)
			{
				num += zombieDieRecord.health;
			}
			return num;
		}

		public List<ZombieDieRecord> GetTopRecordsAroundPosition(int row, int col, bool isUltimate, int count)
		{
			List<ZombieDieRecord> list = new List<ZombieDieRecord>();
			int[] array = new int[] { -1, 0, 1 };
			foreach (int num in array)
			{
				foreach (int num2 in array)
				{
					int row2 = row + num;
					int col2 = col + num2;
					string positionKey = this.GetPositionKey(row2, col2);
					List<ZombieDieRecord> collection;
					if (this._recordsByPosition.TryGetValue(positionKey, out collection))
					{
						list.AddRange(collection);
					}
				}
			}
			IOrderedEnumerable<ZombieDieRecord> source = from r in list
			orderby r.health descending
			select r;
			List<ZombieDieRecord> list2;
			if (!isUltimate)
			{
				list2 = (from r in source
				where TypeMgr.UltimateZombie(r.zombieType)
				group r by r.zombieType into g
				select g.First<ZombieDieRecord>()).ToList<ZombieDieRecord>();
			}
			list2 = (from r in source
			group r by r.zombieType into g
			select g.First<ZombieDieRecord>()).ToList<ZombieDieRecord>();
			if (list2.Count <= count)
			{
				return list2;
			}
			return list2.Take(count).ToList<ZombieDieRecord>();
		}

		private void MaintainTopRecordsForPosition(string positionKey)
		{
			if (this._recordsByPosition.ContainsKey(positionKey))
			{
				List<ZombieDieRecord> list = this._recordsByPosition[positionKey];
				if (list.Count > this._maxRecordsPerPosition)
				{
					List<ZombieDieRecord> list2 = (from record in list
					orderby record.health descending
					select record).Take(this._maxRecordsPerPosition).ToList<ZombieDieRecord>();
					int num = list.Count - list2.Count;
					this._recordsByPosition[positionKey] = list2;
					this._currentTotalRecords -= num;
				}
			}
		}

		public List<ZombieDieRecord> GetTopRecordsForPosition(int row, int col)
		{
			string positionKey = this.GetPositionKey(row, col);
			if (this._recordsByPosition.ContainsKey(positionKey))
			{
				return new List<ZombieDieRecord>(this._recordsByPosition[positionKey]);
			}
			return new List<ZombieDieRecord>();
		}

		public Dictionary<string, int> GetPositionStatistics()
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (KeyValuePair<string, List<ZombieDieRecord>> keyValuePair in this._recordsByPosition)
			{
				dictionary[keyValuePair.Key] = keyValuePair.Value.Count;
			}
			return dictionary;
		}

		private string GetPositionKey(int row, int col)
		{
			return $"{row}-{col}";
		}

		public void DisplayAllRecords()
		{
			Console.WriteLine($"=== 各位置最大health记录 (总容量: {this._currentTotalRecords}/{5000}) ===");
			foreach (KeyValuePair<string, List<ZombieDieRecord>> keyValuePair in from x in this._recordsByPosition
			orderby x.Key
			select x)
			{
				Console.WriteLine("位置 " + keyValuePair.Key + ":");
				foreach (ZombieDieRecord value in keyValuePair.Value)
				{
					Console.WriteLine($"  {value}");
				}
			}
		}

		public (int current, int max) GetCapacityInfo()
		{
			return (this._currentTotalRecords, 5000);
		}

		public void Clear()
		{
			this._recordsByPosition.Clear();
			this._currentTotalRecords = 0;
		}

		public bool RemovePositionRecords(int row, int col)
		{
			string positionKey = this.GetPositionKey(row, col);
			if (this._recordsByPosition.ContainsKey(positionKey))
			{
				int count = this._recordsByPosition[positionKey].Count;
				this._recordsByPosition.Remove(positionKey);
				this._currentTotalRecords -= count;
				return true;
			}
			return false;
		}

		private Dictionary<string, List<ZombieDieRecord>> _recordsByPosition;
		private int _maxRecordsPerPosition;
		private const int MAX_CAPACITY = 5000;
		private int _currentTotalRecords;
	}
}
