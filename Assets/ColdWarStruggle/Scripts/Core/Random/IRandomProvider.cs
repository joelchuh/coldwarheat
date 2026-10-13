namespace ColdWarStruggle.Core
{
    public interface IRandomProvider
    {
        int NextInt(int minInclusive,int maxExclusive);
        RandomSnapshot Capture();
        IRandomProvider Fork();
    }
}
