﻿using System.IO;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using AElf.Types.CSharp;
using AElf.Common;
using Volo.Abp.DependencyInjection;

namespace AElf.Sdk.CSharp.Tests
{
    public class TestContractShim : ITransientDependency
    {
        private MockSetup _mock;
        public Address ContractAddres;
        public IExecutive Executive { get; set; }

        public byte[] Code
        {
            get
            {
                var filePath = "../../../../AElf.Sdk.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/AElf.Sdk.CSharp.Tests.TestContract.dll";
                return File.ReadAllBytes(Path.GetFullPath(filePath));
            }
        }

        public TestContractShim(MockSetup mock)
        {
            _mock = mock;
            Initialize();
        }

        private void Initialize()
        {
            ContractAddres = _mock.DeployContractAsync(Code).Result;
            var task = _mock.GetExecutiveAsync(ContractAddres);
            task.Wait();
            Executive = task.Result;
        }

        public uint GetTotalSupply()
        {
            var tx = new Transaction
            {
                From = Address.Generate(),
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetTotalSupply",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            tc.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return tc.Trace.RetVal.Data.DeserializeToUInt32();
        }

        public bool SetAccount(string name, Hash address)
        {
            var tx = new Transaction
            {
                From = Address.Generate(),
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "SetAccount",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(name, address))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            tc.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return tc.Trace.RetVal.Data.DeserializeToBool();
        }

        public string GetAccountName()
        {
            var tx = new Transaction
            {
                From = Address.Generate(),
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetAccountName",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            tc.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return tc.Trace.RetVal.Data.DeserializeToString();
        }

        public TransactionTrace InlineCallToZero()
        {
            // This is not a standard way of writing shim method
            var tx = new Transaction
            {
                From = Address.Zero,
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "InlineCallToZero",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            tc.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return tc.Trace;
        }
    }
}