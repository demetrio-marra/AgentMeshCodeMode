using AgentMesh.Models;
using System.Collections.Concurrent;

namespace AgentMesh.Services
{
    /// <summary>
    /// Thread-safe, atomic parameter value store with optimistic concurrency control.
    /// 
    /// Design:
    /// - Each parameter value is stored by its Type as key
    /// - Version number increments on every successful commit
    /// - Steps create snapshots (versioned read) before execution
    /// - Steps commit mutations with expected version; fails if stale
    /// - On commit failure, caller can retry step (deterministically re-read parameters)
    /// 
    /// This ensures consistency even when steps run in parallel.
    /// </summary>
    public class ParameterStore : IParameterStore
    {
        private readonly ConcurrentDictionary<Type, ParameterStoreItem> _parameterValues = new();
        private readonly object _versionLock = new();

        public ParameterStore(IEnumerable<IEWParameterConfiguration> parameterConfigurations)
        {
            foreach (var config in parameterConfigurations)
            {
                _parameterValues.TryAdd(config.GetType(), new ParameterStoreItem { Value = config.GetDefaultValue(), Version = 1 });
            }
        }

        public void SetInitialValues(IDictionary<Type, object?> initialValues)
        {
            foreach (var kvp in initialValues)
            {
                if (_parameterValues.ContainsKey(kvp.Key))
                {
                    _parameterValues[kvp.Key] = new ParameterStoreItem { Value = kvp.Value, Version = 1 };
                }
                else
                {
                    throw new ArgumentException($"Parameter type {kvp.Key.Name} is not registered in the store.");
                }
            }
        }

        /// <summary>
        /// Creates an immutable snapshot of specified parameters at the current version.
        /// </summary>
        public ParametersSnapshot CreateSnapshot(IEnumerable<Type> parameterTypes)
        {
            lock (_versionLock)
            {
                var snapshot = parameterTypes.ToDictionary(
                    t => t,
                    t => _parameterValues.TryGetValue(t, out var value) ? value : throw new KeyNotFoundException($"Parameter type {t.Name} not found in store.")
                );

                return new ParametersSnapshot(
                    Values: snapshot.AsReadOnly(),
                    CapturedAtUtc: DateTime.UtcNow
                );
            }
        }

        /// <summary>
        /// Atomically commits a batch of mutations.
        /// Returns success only if expectedVersion matches current version.
        /// On success, increments version and returns display diffs.
        /// </summary>
        public IReadOnlyCollection<CommitResultItem> TryCommit(
            string stepName,
            IReadOnlyCollection<ParameterMutation> mutations)
        {
            lock (_versionLock)
            {
                // before committing anything ensure versions match for all mutations
                foreach (var mutation in mutations)
                {
                    if (mutation.ParameterVersion != _parameterValues[mutation.ParameterType].Version)
                    {
                        throw new InvalidOperationException($"Parameter version conflict for {mutation.ParameterType.Name}: " +
                                                            $"Expected version {mutation.ParameterVersion}, " +
                                                            $"but current version is {_parameterValues[mutation.ParameterType].Version}, " +
                                                            $"Step: '{stepName}'.");
                    }
                }

                // Apply all mutations
                var committedDiffs = new List<CommitResultItem>();

                foreach (var mutation in mutations)
                {
                    _parameterValues.TryGetValue(mutation.ParameterType, out var oldValue);
                    _parameterValues[mutation.ParameterType] = new ParameterStoreItem
                    {
                        Version = _parameterValues[mutation.ParameterType].Version + 1,
                        Value = mutation.NewValue
                    };

                    // Build diff for audit trail
                    committedDiffs.Add(new CommitResultItem(
                        ParameterType: mutation.ParameterType,
                        OldValue: oldValue.Value,
                        NewValue: mutation.NewValue
                    ));
                }

                return committedDiffs;
            }
        }
    }
}
