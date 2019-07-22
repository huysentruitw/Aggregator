using System;

namespace Aggregator.Testing
{
    [Serializable]
    public class AggregatorTestingException : Exception
    {
        public AggregatorTestingException(string message)
            : base(message)
        {
        }

        public AggregatorTestingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
