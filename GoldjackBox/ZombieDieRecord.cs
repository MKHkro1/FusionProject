namespace UltimateGoldJackBoxZombieMod
{
    public class ZombieDieRecord
    {
        public ZombieType zombieType;
        public float dieTime;
        public float health;
        public int row;
        public int col;

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
            return $"[{row},{col}] Type:{zombieType}, Health:{health}, Time:{dieTime}";
        }
    }
}
