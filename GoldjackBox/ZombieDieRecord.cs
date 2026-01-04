using System;

namespace UltimateGoldJackBoxZombieMod
{
	public class ZombieDieRecord
	{
		public ZombieDieRecord(ZombieType type, float time, float health, int row, int col)
		{
			this.zombieType = type;
			this.dieTime = time;
			this.health = health;
			this.row = row;
			this.col = col;
		}

		public override string ToString()
		{
			return $"[{this.row},{this.col}] Type:{this.zombieType}, Health:{this.health}, Time:{this.dieTime}";
		}

		public ZombieType zombieType;
		public float dieTime;
		public float health;
		public int row;
		public int col;
	}
}
