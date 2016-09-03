using System;
using System.Threading;

namespace Ses.Subscriptions
{
    public class InterlockedDateTime
    {
        private long _value;

        /// <summary>
        /// Current value
        /// </summary>
        public DateTime Value => DateTime.FromBinary(Interlocked.Read(ref _value));

        /// <summary>
        /// Initializes a new instance of <see cref="T:InterlockedDateTime"/>
        /// </summary>
        /// <param name="initialValue">initial value</param>
        public InterlockedDateTime(DateTime initialValue)
        {
            _value = initialValue.ToBinary();
        }

        /// <summary>
        /// Sets a new value
        /// </summary>
        /// <param name="newValue">new value</param>
        public void Set(DateTime newValue)
        {
            Interlocked.Exchange(ref _value, newValue.ToBinary());
        }

        /// <summary>
        /// Compares the current value and the comparand for equality and, if they are equal, 
        /// replaces the current value with the new value in an atomic/thread-safe operation.
        /// </summary>
        /// <param name="newValue">new value</param>
        /// <param name="comparand">value to compare the current value with</param>
        /// <returns>the original value before any operation was performed</returns>
        public DateTime CompareExchange(DateTime newValue, DateTime comparand)
        {
            var oldValue = Interlocked.CompareExchange(ref _value, newValue.ToBinary(), comparand.ToBinary());
            return DateTime.FromBinary(oldValue);
        }
    }
}