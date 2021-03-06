using AElf.Kernel;
using AElf.Modularity;
using AElf.Contracts.TestBase;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Authorization.Tests
{
    [DependsOn(
        typeof(AElf.ChainController.ChainControllerAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(KernelAElfModule),
        typeof(ContractTestAElfModule)
        
    )]
    public class AuthroizationContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<AuthroizationContractTestAElfModule>();
        }
    }
}