using System;
namespace ColdWarStruggle.Core
{
    public static class ControlRules
    {
        public static PlayerSide? GetController(int us, int ussr, int stability)
        {
            if (us < 0 || ussr < 0 || stability < 1) throw new ArgumentOutOfRangeException(nameof(stability));
            if ((long)us - ussr >= stability) return PlayerSide.US;
            if ((long)ussr - us >= stability) return PlayerSide.USSR;
            return null;
        }
    }
}
