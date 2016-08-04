namespace Ses.Abstracts
{
    public sealed class RestoredMemento : IRestoredMemento
    {
        public RestoredMemento(int version, IMemento state)
        {
            Version = version;
            State = state;
        }

        public IMemento State { get; }
        public int Version { get; }
    }
}