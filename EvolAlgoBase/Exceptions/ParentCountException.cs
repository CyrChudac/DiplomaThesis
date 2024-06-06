namespace EvolAlgoBase.Exceptions
{
    public class ParentCountException : System.ArgumentException
    {
        public ParentCountException(int expectedCount, int actualCount)
            : base($"Expected number of <u>parents</u> is {expectedCount} but got {actualCount} instead.") { }
    }
}