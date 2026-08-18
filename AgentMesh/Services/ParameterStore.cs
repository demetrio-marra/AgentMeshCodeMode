using AgentMesh.Models;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

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
        private readonly ConcurrentDictionary<Type, object?> _parameterValues = new();
        private long _currentVersion = 0;
        private readonly object _versionLock = new();

        /// <summary>
        /// Registers a parameter definition in the store.
        /// Called during DI setup for each concrete EWParameter<T>.
        /// </summary>
        public void RegisterParameterDefinition(Type parameterType, object? initialValue = null)
        {
            _parameterValues.TryAdd(parameterType, initialValue);
        }

        /// <summary>
        /// Creates an immutable snapshot of specified parameters at the current version.
        /// </summary>
        public ParameterSnapshot CreateSnapshot(IEnumerable<Type> parameterTypes)
        {
            lock (_versionLock)
            {
                var snapshot = parameterTypes.ToDictionary(
                    t => t,
                    t => _parameterValues.TryGetValue(t, out var value) ? value : null
                );

                return new ParameterSnapshot(
                    Version: _currentVersion,
                    Values: new ReadOnlyDictionary<Type, object?>(snapshot),
                    CapturedAtUtc: DateTime.UtcNow
                );
            }
        }

        /// <summary>
        /// Atomically commits a batch of mutations.
        /// Returns success only if expectedVersion matches current version.
        /// On success, increments version and returns display diffs.
        /// </summary>
        public CommitResult TryCommit(
            string stepName,
            long expectedVersion,
            IReadOnlyCollection<ParameterMutation> mutations)
        {
            lock (_versionLock)
            {
                // Optimistic concurrency check
                if (_currentVersion != expectedVersion)
                {
                    return new CommitResult(
                        Success: false,
                        NewVersion: _currentVersion,
                        CommittedMutations: [],
                        ConflictReason: $"Version conflict: expected {expectedVersion}, current {_currentVersion}. " +
                                       $"Another step committed changes. Retry step '{stepName}'."
                    );
                }

                // Apply all mutations
                var committedDiffs = new List<CommitResultItem>();

                foreach (var mutation in mutations)
                {
                    _parameterValues.TryGetValue(mutation.ParameterType, out var oldValue);
                    _parameterValues[mutation.ParameterType] = mutation.NewValue;

                    // Build diff for audit trail
                    committedDiffs.Add(new CommitResultItem(
                        ParameterType: mutation.ParameterType,
                        OldValue:oldValue,
                        NewValue: mutation.NewValue
                    ));
                }

                // Increment version atomically
                _currentVersion++;

                return new CommitResult(
                    Success: true,
                    NewVersion: _currentVersion,
                    CommittedMutations: committedDiffs.AsReadOnly()
                );
            }
        }
       

        /// <summary>
        /// Gets the current version number.
        /// </summary>
        public long GetCurrentVersion()
        {
            lock (_versionLock)
            {
                return _currentVersion;
            }
        }
    }
}
