﻿using System.IO;
using Google.Protobuf;
using AElf.Kernel.ABI;
using AElf.Runtime.CSharp.Core.ABI;
using Xunit;

// ReSharper disable once CheckNamespace
namespace AElf.Runtime.CSharp.Tests
{
    // ReSharper disable once InconsistentNaming
    public class ABITest
    {
        [Fact]
        public void Test()
        {
            string filePath = "../../../../AElf.Runtime.CSharp.Tests.TestContractForABI/bin/Debug/netstandard2.0/AElf.Runtime.CSharp.Tests.TestContractForABI.dll";

            string expected = @"{ ""Name"": ""AElf.ABI.CSharp.Tests.UserContract"", ""Methods"": [ { ""Name"": ""GetTotalSupply"", ""Params"": [ ], ""ReturnType"": ""uint"", ""IsView"": true, ""IsAsync"": false, ""Fee"": ""1"" }, { ""Name"": ""GetBalanceOf"", ""Params"": [ { ""Type"": ""AElf.Common.Address"", ""Name"": ""account"" } ], ""ReturnType"": ""uint"", ""IsView"": true, ""IsAsync"": false, ""Fee"": ""1"" }, { ""Name"": ""Transfer"", ""Params"": [ { ""Type"": ""AElf.Common.Address"", ""Name"": ""to"" }, { ""Type"": ""uint"", ""Name"": ""value"" } ], ""ReturnType"": ""bool"", ""IsView"": false, ""IsAsync"": false, ""Fee"": ""99"" }, { ""Name"": ""SetAccount"", ""Params"": [ { ""Type"": ""string"", ""Name"": ""name"" }, { ""Type"": ""AElf.Common.Address"", ""Name"": ""address"" } ], ""ReturnType"": ""bool"", ""IsView"": false, ""IsAsync"": true, ""Fee"": ""1"" }, { ""Name"": ""GetAccountName"", ""Params"": [ ], ""ReturnType"": ""string"", ""IsView"": true, ""IsAsync"": true, ""Fee"": ""1"" } ], ""Events"": [ { ""Name"": ""AElf.ABI.CSharp.Tests.Transfered"", ""Indexed"": [ ], ""NonIndexed"": [ { ""Type"": ""AElf.Common.Address"", ""Name"": ""From"" }, { ""Type"": ""AElf.Common.Address"", ""Name"": ""To"" }, { ""Type"": ""uint"", ""Name"": ""Value"" } ] }, { ""Name"": ""AElf.ABI.CSharp.Tests.AccountName"", ""Indexed"": [ { ""Type"": ""string"", ""Name"": ""Name"" } ], ""NonIndexed"": [ { ""Type"": ""string"", ""Name"": ""Dummy"" } ] } ], ""Types"": [ { ""Name"": ""AElf.ABI.CSharp.Tests.Account"", ""Fields"": [ { ""Type"": ""string"", ""Name"": ""Name"" }, { ""Type"": ""AElf.Common.Address"", ""Name"": ""Address"" } ] } ] }";
            Module module = Generator.GetABIModule(GetCode(filePath));
            string actual = new JsonFormatter(new JsonFormatter.Settings(true)).Format(module);
            /*
             * Generate an abi file
            using (var stream = File.Open("../../../AElf.ABI.CSharp.Tests.TestContract.abi.json", FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                stream.Write(actual);
            }
            */
            Assert.Equal(expected, actual);
        }

        public static byte[] GetCode(string filePath)
        {
            return File.ReadAllBytes(Path.GetFullPath(filePath));
        }
    }
}
