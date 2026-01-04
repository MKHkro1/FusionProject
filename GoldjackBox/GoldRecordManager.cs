using System;
using System.Collections.Generic;

namespace UltimateGoldJackBoxZombieMod
{
	public class GoldRecordManager
	{
		public GoldRecordManager()
		{
			this._records = new HashSet<Zombie>();
			this._stateRecords = new Dictionary<Zombie, GoldBoxStateRecord>();
		}

		public void AddOrUpdatetStateRecord(Zombie zombie, GoldBoxStateRecord record)
		{
			if (!this._stateRecords.ContainsKey(zombie))
			{
				this._stateRecords.Add(zombie, record);
			}
			record.zombie = zombie;
			this._stateRecords[zombie] = record;
		}

		public bool GetJumperStateRecord(Zombie zombie)
		{
			return this._stateRecords.ContainsKey(zombie) && this._stateRecords[zombie].isLoseJumper;
		}

		public void removeStateRecord(Zombie zombie)
		{
			if (!this._stateRecords.ContainsKey(zombie))
			{
				return;
			}
			this._stateRecords.Remove(zombie);
		}

		public int StateRecordCount()
		{
			return this._stateRecords.Count;
		}

		public bool AddRecord(Zombie newRecord)
		{
			if (this._records.Contains(newRecord))
			{
				return false;
			}
			this._records.Add(newRecord);
			return true;
		}

		public int GetRecordCounts()
		{
			return this._records.Count;
		}

		public HashSet<Zombie> GetRecords()
		{
			return this._records;
		}

		public void setRecords(HashSet<Zombie> records)
		{
			this._records = records;
		}

		public bool vaildRecord(Zombie zombie)
		{
			return this._records.Contains(zombie);
		}

		public void RemoveRecord(Zombie zombie)
		{
			if (!this._records.Contains(zombie))
			{
				return;
			}
			this._records.Remove(zombie);
		}

		public void Clear()
		{
			this._records.Clear();
		}

		private HashSet<Zombie> _records;
		private Dictionary<Zombie, GoldBoxStateRecord> _stateRecords;
	}
}
