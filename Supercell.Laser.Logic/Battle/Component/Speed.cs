namespace Supercell.Laser.Logic.Battle.Component
{
    public class Speed
    {
        private int m_ticks;
        private int m_speedBoost;

        public Speed(int ticks, int speedBoost)
        {
            m_ticks = ticks;
            m_speedBoost = speedBoost;
        }

        public int GetSpeedBoost()
        {
            return m_speedBoost;
        }

        public bool Tick(int a2)
        {
            m_ticks -= a2;
            return m_ticks < 1;
        }

        public void Destruct()
        {
            m_ticks = 0;
        }
    }
}
