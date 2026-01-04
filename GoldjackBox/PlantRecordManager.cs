using System;
using System.Collections.Generic;

namespace UltimateGoldJackBoxZombieMod
{
	public class PlantRecordManager
	{
		public PlantRecordManager()
		{
			this._records = new Dictionary<Plant, Zombie>();
		}

		public bool AddRecord(Plant newRecord, Zombie z)
		{
			if (this._records.ContainsKey(newRecord))
			{
				return false;
			}
			this._records[newRecord] = z;
			return true;
		}

		public int GetRecordCounts()
		{
			return this._records.Count;
		}

		public Zombie GetRecord(Plant p)
		{
			if (!this._records.ContainsKey(p))
			{
				return null;
			}
			return this._records[p];
		}

		public void RemoveRecord(Plant p)
		{
			if (!this._records.ContainsKey(p))
			{
				return;
			}
			this._records.Remove(p);
		}

		private Dictionary<Plant, Zombie> _records;
	}
}
