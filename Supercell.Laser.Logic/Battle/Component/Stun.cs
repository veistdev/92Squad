namespace Supercell.Laser.Logic.Battle.Component
{
    public class Stun
    {
        private int m_ticks;

        public Stun(int ticks)
        {
            m_ticks = ticks;
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
