using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Sdk.CSharp.State;
using Google.Protobuf;
using Newtonsoft.Json;
using Xunit;

namespace AElf.Sdk.CSharp2.Tests
{
    public class StateTest
    {
        internal T GetValue<T>()
        {
            if (typeof(T) == typeof(bool))
            {
                return (T) (object) true;
            }

            if (typeof(T) == typeof(int))
            {
                return (T) (object) (int) -12345;
            }

            if (typeof(T) == typeof(uint))
            {
                return (T) (object) (uint) 12345U;
            }

            if (typeof(T) == typeof(long))
            {
                return (T) (object) (long) -678910L;
            }

            if (typeof(T) == typeof(ulong))
            {
                return (T) (object) (ulong) 678910UL;
            }

            if (typeof(T) == typeof(byte[]))
            {
                return (T) (object) ByteArrayHelpers.FromHexString("302010");
            }

            if (typeof(T) == typeof(string))
            {
                return (T) (object) "aelf";
            }

            if (typeof(T) == typeof(Address))
            {
                return (T) (object) Address.FromString("a");
            }

            throw new Exception("Not supported type.");
        }


        private void SetValues(MockContractState state)
        {
            state.BoolState.Value = GetValue<bool>();
            state.Int32State.Value = GetValue<int>();
            state.UInt32State.Value = GetValue<uint>();
            state.Int64State.Value = GetValue<long>();
            state.UInt64State.Value = GetValue<ulong>();
            state.StringState.Value = GetValue<string>();
            state.BytesState.Value = GetValue<byte[]>();
            state.StructuredState.StringState.Value = GetValue<string>();
            state.MappedState[GetValue<Address>()][GetValue<Address>()] = GetValue<string>();
        }

        private void AssertDefault(MockContractState state)
        {
            Assert.False(state.BoolState.Value);
            Assert.Equal(0, state.Int32State.Value);
            Assert.Equal(0U, state.UInt32State.Value);
            Assert.Equal(0, state.Int64State.Value);
            Assert.Equal(0U, state.UInt64State.Value);
            Assert.Equal("", state.StringState.Value);
            Assert.Empty(state.BytesState.Value);
            Assert.Equal("", state.StructuredState.StringState.Value);
            Assert.Equal("", state.MappedState[GetValue<Address>()][GetValue<Address>()]);
        }

        private void AssertValues(MockContractState state)
        {
            Assert.Equal(GetValue<bool>(), state.BoolState.Value);
            Assert.Equal(GetValue<int>(), state.Int32State.Value);
            Assert.Equal(GetValue<uint>(), state.UInt32State.Value);
            Assert.Equal(GetValue<long>(), state.Int64State.Value);
            Assert.Equal(GetValue<ulong>(), state.UInt64State.Value);
            Assert.Equal(GetValue<string>(), state.StringState.Value);
            Assert.Equal(GetValue<byte[]>(), state.BytesState.Value);
            Assert.Equal(GetValue<string>(), state.StructuredState.StringState.Value);
            Assert.Equal(GetValue<string>(), state.MappedState[GetValue<Address>()][GetValue<Address>()]);
        }

        [Fact]
        public async Task Test1()
        {
            var stateManager = new MockStateManager();
            var path = new StatePath();
            path.Path.Add(ByteString.CopyFromUtf8("dummy_address"));
            var state = new MockContractState
            {
                Manager = stateManager,
                Path = path
            };

            // Initial default value
            AssertDefault(state);

            // Set values
            SetValues(state);
            AssertValues(state);

            // Get changes
            var changes = state.GetChanges();

            // Clear values
            state.Clear();
            AssertDefault(state);

            // Set changes to StateManager
            await stateManager.PipelineSetAsync(
                changes.ToDictionary(x => x.Key, x => x.Value.CurrentValue.ToByteArray()));

            // Need to clear again as AssertDefault reloaded values that have not been committed
            state.Clear();
            AssertValues(state);
        }
    }
}