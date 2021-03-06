using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Kernel.Managers.Another
{
    [Flags]
    public enum BlockAttachOperationStatus
    {
        None = 0,
        NewBlockNotLinked = 1 << 1,
        NewBlockLinked = 1 << 2,
        BestChainFound = 1 << 3 | NewBlockLinked,
        NewBlocksLinked = 1 << 4 | NewBlockLinked
    }

    public interface IChainManager
    {
    }


    public class ChainManager : IChainManager, ISingletonDependency
    {
        private readonly IBlockchainStore<Chain> _chains;
        private readonly IBlockchainStore<ChainBlockLink> _chainBlockLinks;
        private readonly IBlockchainStore<ChainBlockIndex> _chainBlockIndexes;

        public ChainManager(IBlockchainStore<Chain> chains,
            IBlockchainStore<ChainBlockLink> chainBlockLinks,
            IBlockchainStore<ChainBlockIndex> chainBlockIndexes)
        {
            _chains = chains;
            _chainBlockLinks = chainBlockLinks;
            _chainBlockIndexes = chainBlockIndexes;
        }

        public async Task<Chain> CreateAsync(int chainId, Hash genesisBlock)
        {
            var chain = await _chains.GetAsync(chainId.ToHex());
            if (chain != null)
                throw new InvalidOperationException("chain already exists");

            chain = new Chain()
            {
                BestChainHash = genesisBlock,
                GenesisBlockHash = genesisBlock,
                Branches =
                {
                    {genesisBlock.ToHex(), 0}
                }
            };

            await SetChainBlockLinkAsync(chainId, new ChainBlockLink()
            {
                BlockHash = genesisBlock,
                Height = 0,
                IsLinked = true
            });
            await _chains.SetAsync(chain.Id.ToHex(), chain);

            return chain;
        }

        public async Task<Chain> GetAsync(int chainId)
        {
            var chain = await _chains.GetAsync(chainId.ToHex());
            return chain;
        }

        public async Task<ChainBlockLink> GetChainBlockLinkAsync(int chainId, Hash blockHash)
        {
            return await GetChainBlockLinkAsync(chainId, blockHash.ToHex());
        }

        public async Task<ChainBlockLink> GetChainBlockLinkAsync(int chainId, string blockHash)
        {
            return await _chainBlockLinks.GetAsync(chainId.ToHex() + blockHash);
        }

        public async Task SetChainBlockLinkAsync(int chainId, ChainBlockLink chainBlockLink)
        {
            await _chainBlockLinks.SetAsync(chainId.ToHex() + chainBlockLink.BlockHash.ToHex(), chainBlockLink);
        }

        private async Task SetChainBlockIndexAsync(int chainId, long blockHeight, Hash blockHash)
        {
            await _chainBlockIndexes.SetAsync(chainId.ToHex() + blockHeight.ToHex(),
                new ChainBlockIndex() {BlockHash = blockHash});
        }

        public async Task<ChainBlockIndex> GetChainBlockIndexAsync(int chainId, long blockHeight)
        {
            return await _chainBlockIndexes.GetAsync(chainId.ToHex() + blockHeight.ToHex());
        }

        public async Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain,
            ChainBlockLink chainBlockLink)
        {
            BlockAttachOperationStatus status = BlockAttachOperationStatus.None;

            while (true)
            {
                var previousHash = chainBlockLink.PreviousBlockHash.ToHex();
                var blockHash = chainBlockLink.BlockHash.ToHex();

                if (chain.Branches.ContainsKey(previousHash))
                {
                    chain.Branches[blockHash] = chainBlockLink.Height;
                    chain.Branches.Remove(previousHash);

                    if (chainBlockLink.Height > chain.BestChainHeight)
                    {
                        chain.BestChainHeight = chainBlockLink.Height;
                        chain.BestChainHash = chainBlockLink.BlockHash;
                        status |= BlockAttachOperationStatus.BestChainFound;
                    }


                    if (chainBlockLink.IsLinked)
                        throw new Exception("chain block link should not be linked");

                    chainBlockLink.IsLinked = true;

                    await SetChainBlockLinkAsync(chain.Id, chainBlockLink);

                    if (!chain.NotLinkedBlocks.ContainsKey(blockHash))
                    {
                        status |= BlockAttachOperationStatus.NewBlockLinked;
                        break;
                    }

                    chainBlockLink = await GetChainBlockLinkAsync(
                        chain.Id, chain.NotLinkedBlocks[blockHash]);

                    chain.NotLinkedBlocks.Remove(blockHash);

                    status |= BlockAttachOperationStatus.NewBlocksLinked;
                }
                else
                {
                    if (chainBlockLink.Height <= chain.BestChainHeight)
                    {
                        //check database to ensure whether it can be a branch
                        var previousChainBlockLink =
                            await this.GetChainBlockLinkAsync(chain.Id, chainBlockLink.PreviousBlockHash);
                        if (previousChainBlockLink != null && previousChainBlockLink.IsLinked)
                        {
                            chain.Branches[previousChainBlockLink.BlockHash.ToHex()] = previousChainBlockLink.Height;
                            continue;
                        }
                    }

                    chain.NotLinkedBlocks[previousHash] = blockHash;

                    if (status != BlockAttachOperationStatus.None)
                        throw new Exception("invalid status");

                    status = BlockAttachOperationStatus.NewBlockNotLinked;
                    await SetChainBlockLinkAsync(chain.Id, chainBlockLink);
                    break;
                }
            }

            await _chains.SetAsync(chain.Id.ToHex(), chain);

            return status;
        }

        public async Task SetIrreversibleBlockAsync(Chain chain, Hash irreversibleBlockHash)
        {
            Stack<ChainBlockLink> links = new Stack<ChainBlockLink>();

            while (true)
            {
                if (irreversibleBlockHash == null)
                    break;
                var chainBlockLink = await GetChainBlockLinkAsync(chain.Id, irreversibleBlockHash);
                if (chainBlockLink==null || chainBlockLink.IsIrreversibleBlock)
                    break;
                if(!chainBlockLink.IsLinked)
                    throw new InvalidOperationException("should not set an unlinked block as irreversible block");
                chainBlockLink.IsIrreversibleBlock = true;
                links.Push(chainBlockLink);
                irreversibleBlockHash = chainBlockLink.PreviousBlockHash;
            }

            while (links.Count > 0)
            {
                var chainBlockLink = links.Pop();
                await SetChainBlockIndexAsync(chain.Id, chainBlockLink.Height, chainBlockLink.BlockHash);
                await SetChainBlockLinkAsync(chain.Id, chainBlockLink);
                chain.LastIrreversibleBlockHash = chainBlockLink.BlockHash;
                chain.LastIrreversibleBlockHeight = chainBlockLink.Height;
                await _chains.SetAsync(chain.Id.ToHex(), chain);
                
            }
        }
    }
}