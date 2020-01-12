using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Aggregator.Testing
{
    public sealed class ThrowsContinuation<TAggregateRoot, TEventBase, TException>
        where TAggregateRoot : AggregateRoot<TEventBase>
        where TException : Exception
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new ExceptionContractResolver()
        };

        private readonly TAggregateRoot _aggregateRoot;
        private readonly Action _action;
        private readonly Exception _expectedException;

        internal ThrowsContinuation(TAggregateRoot aggregateRoot, Action action, Exception exception)
        {
            _aggregateRoot = aggregateRoot;
            _action = action;
            _expectedException = exception;
        }

        public void Assert()
        {
            var expectedExceptionType = _expectedException.GetType();

            try
            {
                _action();
                throw new AggregatorTestingException($"Expected an exception of type {expectedExceptionType} to be thrown, but no exception was thrown instead");
            }
            catch (Exception ex) when (!(ex is AggregatorTestingException))
            {
                var exceptionType = ex.GetType();

                if (exceptionType != expectedExceptionType)
                {
                    throw new AggregatorTestingException($"Expected an exception of type {expectedExceptionType} to be thrown, but got an exception of type {exceptionType} instead", ex);
                }

                string expectedJson = JsonConvert.SerializeObject(_expectedException, Formatting.Indented, JsonSettings);
                string json = JsonConvert.SerializeObject(ex, Formatting.Indented, JsonSettings);
                if (json != expectedJson)
                {
                    throw new AggregatorTestingException($"Expected exception:{Environment.NewLine}{expectedJson}{Environment.NewLine}to be thrown, but got exception:{Environment.NewLine}{json} ");
                }
            }
        }
    }

    internal sealed class ExceptionContractResolver : DefaultContractResolver
    {
        private static readonly string[] IgnoredProperties = new[]
        {
            nameof(Exception.InnerException),
            nameof(Exception.StackTrace),
            nameof(Exception.HelpLink),
            nameof(Exception.Source),
            nameof(Exception.HResult),
        };

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var allProperties = base.CreateProperties(type, memberSerialization);
            return allProperties.Where(x => !IgnoredProperties.Contains(x.PropertyName)).ToList();
        }
    }
}
