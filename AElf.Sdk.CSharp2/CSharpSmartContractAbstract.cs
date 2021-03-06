using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.SmartContract;
using AElf.SmartContract.Contexts;

namespace AElf.Sdk.CSharp
{
    public abstract class CSharpSmartContractAbstract : CSharpSmartContract
    {
        internal abstract void SetSmartContractContext(ISmartContractContext smartContractContext);
        internal abstract void SetTransactionContext(ITransactionContext transactionContext);
        internal abstract void SetStateProviderFactory(IStateProviderFactory stateProviderFactory);
        internal abstract void SetContractAddress(Address address);
        internal abstract Dictionary<StatePath, StateValue> GetChanges();
        internal abstract void Cleanup();

        public void Assert(bool asserted, string message = "Assertion failed!")
        {
            if (!asserted)
            {
                throw new AssertionError(message);
            }
        }
    }
}