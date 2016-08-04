namespace Ses.MsSql
{
    public interface IMsSqlPersistorBuilder
    {
        void Destroy(bool ignoreErrors = false);
        void Initialize();
    }
}