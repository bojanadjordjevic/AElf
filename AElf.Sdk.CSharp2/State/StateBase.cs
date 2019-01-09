using System;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.Managers;

namespace AElf.Sdk.CSharp.State
{
    public class StateBase
    {
        private IStateManager _stateManager;
        private StatePath _path;

        internal IStateManager Manager
        {
            get => _stateManager;
            set
            {
                _stateManager = value;
                OnStateManagerSet();
            }
        }

        internal StatePath Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPathSet();
            }
        }

        internal virtual void OnStateManagerSet()
        {
        }

        internal virtual void OnPathSet()
        {
        }

        internal virtual void Clear()
        {
        }

        internal virtual Dictionary<StatePath, StateValue> GetChanges()
        {
            return new Dictionary<StatePath, StateValue>();
        }
    }
}