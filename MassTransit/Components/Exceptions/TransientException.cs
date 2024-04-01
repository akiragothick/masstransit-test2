namespace Components.Exceptions;

[Serializable]
public class TransientException :
    Exception
{
    public TransientException()
    {
    }

    public TransientException(string message)
        : base(message)
    {
    }
}