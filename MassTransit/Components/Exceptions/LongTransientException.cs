namespace Components.Exceptions;

[Serializable]
public class LongTransientException :
    Exception
{
    public LongTransientException()
    {
    }

    public LongTransientException(string message)
        : base(message)
    {
    }
}