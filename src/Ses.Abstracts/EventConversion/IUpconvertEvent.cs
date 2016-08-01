namespace Ses.Abstracts.EventConversion
{
    public interface IUpconvertEvent { }

    public interface IUpconvertEvent<in TSource, out TTarget> : IUpconvertEvent
        where TSource : class, IEvent
        where TTarget : class, IEvent
    {
        /// <summary>
        /// Converts an event from one type to another.
        /// </summary>
        /// <param name="sourceEvent">The event to be converted.</param>
        /// <returns>The converted event.</returns>
        TTarget Convert(TSource sourceEvent);
    }
}