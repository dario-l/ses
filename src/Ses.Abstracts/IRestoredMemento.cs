namespace Ses.Abstracts
{
    public interface IRestoredMemento : IEvent
    {
        IMemento State { get; }
        int Version { get; }
    }
}