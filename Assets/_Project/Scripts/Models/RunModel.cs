namespace Mergistry.Models
{
    /// <summary>
    /// Tracks linear run state: which fight we're on and the player's persistent HP.
    /// HP is saved on exit from CombatState and restored on enter.
    /// </summary>
    public class RunModel
    {
        public int  CurrentFight { get; set; } = 0;   // 0 = fight 1, 1 = fight 2, 2 = fight 3
        public int  PersistentHP { get; set; } = 5;
        public bool LastVictory  { get; set; } = false;

        public void Reset()
        {
            CurrentFight = 0;
            PersistentHP = 5;
            LastVictory  = false;
        }
    }
}
