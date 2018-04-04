using System;
using Aggregator.Internal;

namespace Aggregator.Command
{
    internal static class CommandHandlingContextExtensions
    {
        internal static readonly string UnitOfWorkKey = $"UnitOfWork.{Guid.NewGuid():N}";

        public static void SetUnitOfWork<TIdentifier, TEventBase>(this CommandHandlingContext context, UnitOfWork<TIdentifier, TEventBase> unitOfWork)
            where TIdentifier : IEquatable<TIdentifier>
            => context.Set(UnitOfWorkKey, unitOfWork);

        public static UnitOfWork<TIdentifier, TEventBase> GetUnitOfWork<TIdentifier, TEventBase>(this CommandHandlingContext context)
            where TIdentifier : IEquatable<TIdentifier>
            => context.Get<UnitOfWork<TIdentifier, TEventBase>>(UnitOfWorkKey);
    }
}
