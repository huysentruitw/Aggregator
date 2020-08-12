using System;
using System.Diagnostics.CodeAnalysis;
using Aggregator.Internal;

namespace Aggregator.Command
{
    internal static class CommandHandlingContextExtensions
    {
        internal static readonly string UnitOfWorkKey = $"UnitOfWork.{Guid.NewGuid():N}";

        public static UnitOfWork<TIdentifier, TEventBase> CreateUnitOfWork<TIdentifier, TEventBase>(this CommandHandlingContext context)
            where TIdentifier : IEquatable<TIdentifier>
        {
            lock (context)
            {
                if (context.GetUnitOfWork<TIdentifier, TEventBase>() != null)
                    throw new InvalidOperationException("Unit of work already created for this context, make sure CommandHandlingContext is registered as scoped service");

                var unitOfWork = new UnitOfWork<TIdentifier, TEventBase>();
                context.Set(UnitOfWorkKey, unitOfWork);
                return unitOfWork;
            }
        }

        public static UnitOfWork<TIdentifier, TEventBase> GetUnitOfWork<TIdentifier, TEventBase>(this CommandHandlingContext context)
            where TIdentifier : IEquatable<TIdentifier>
            => context.Get<UnitOfWork<TIdentifier, TEventBase>>(UnitOfWorkKey);
    }
}
