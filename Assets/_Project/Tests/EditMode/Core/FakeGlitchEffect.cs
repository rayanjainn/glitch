using System;
using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Glitches;

namespace BugReport.Core.Tests
{
    /// <summary>Sets an int channel per target to <see cref="AppliedValue"/>; every behaviour is switchable.</summary>
    internal sealed class FakeGlitchEffect : IGlitchEffect
    {
        public readonly Dictionary<GameEntityId, ModifierStack<int>> Channels = new Dictionary<GameEntityId, ModifierStack<int>>();
        public readonly List<(OperationId, GlitchEndReason)> Ended = new List<(OperationId, GlitchEndReason)>();
        public int AppliedValue = 1;
        public string RejectReason;
        public bool FailAfterFirstLease;
        public bool ThrowOnApply;
        public bool ThrowOnEnded;
        public string BlockEnd;
        public Action<GlitchOperation, GlitchEndReason> OnEndedHook;
        /// <summary>Optional: called during Apply, e.g. to register a product.</summary>
        public Action<GlitchOperation> OnApplyHook;

        public ModifierStack<int> Channel(GameEntityId id)
        {
            if (!Channels.TryGetValue(id, out var stack))
            {
                stack = new ModifierStack<int>(0, ConflictPolicy.Exclusive);
                Channels.Add(id, stack);
            }
            return stack;
        }

        public string Validate(GlitchRequest request)
        {
            if (RejectReason != null) return RejectReason;
            for (int i = 0; i < request.Targets.Count; i++)
            {
                if (!Channel(request.Targets[i]).CanAdd(request.PendingId)) return "ALREADY GLITCHED";
            }
            return null;
        }

        public bool Apply(GlitchOperation operation, out string failure)
        {
            if (ThrowOnApply) throw new InvalidOperationException("boom");
            for (int i = 0; i < operation.TargetCount; i++)
            {
                Channel(operation.Target(i)).TryAdd(operation.Id, AppliedValue, out var lease);
                operation.AddLease(lease);
                if (FailAfterFirstLease)
                {
                    failure = "HALF APPLIED";
                    return false;
                }
            }
            OnApplyHook?.Invoke(operation);
            failure = null;
            return true;
        }

        public string CanEnd(GlitchOperation operation, GlitchEndReason reason) => BlockEnd;

        public void OnEnded(GlitchOperation operation, GlitchEndReason reason)
        {
            Ended.Add((operation.Id, reason));
            OnEndedHook?.Invoke(operation, reason);
            if (ThrowOnEnded) throw new InvalidOperationException("ended boom");
        }
    }
}
