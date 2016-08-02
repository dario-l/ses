namespace Ses.Abstracts.Converters
{
    public interface IUpConvertEvent { }

    public interface IUpConvertEvent<in TSource, out TTarget> : IUpConvertEvent
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