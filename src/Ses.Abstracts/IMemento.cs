namespace Ses.Abstracts
{
    public interface IMemento : IEvent
    {
        int Version { get; }
    }
}