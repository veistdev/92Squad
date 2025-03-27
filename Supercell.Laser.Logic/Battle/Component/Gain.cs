namespace Supercell.Laser.Logic.Battle.Component
{
    public class Gain
    {
        private int m_ticks;
        private int m_gainBoost;

        public Gain(int ticks, int gainBoost)
        {
            m_ticks = ticks;
            m_gainBoost = gainBoost;
        }

        public int GetGainBoost()
        {
            return m_gainBoost;
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
