﻿using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel.Managers;
using AElf.Configuration.Config.Consensus;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable MemberCanBeMadeStatic.Local
    // ReSharper disable UnusedMember.Global
    public class ConsensusHelper
    {

        public ConsensusHelper()
        {
            Logger=NullLogger<ConsensusHelper>.Instance;
        }
        private readonly IMinersManager _minersManager;
        private readonly ConsensusDataReader _reader;        
        public ILogger<ConsensusHelper> Logger { get; set; }

        public List<string> Miners
        {
            get
            {
                if (ChainConfig.Instance.ChainId == GlobalConfig.DefaultChainId)
                {
                    return _minersManager.GetMiners(CurrentTermNumber.Value).Result.PublicKeys.ToList();
                }
            
                return _minersManager.GetMiners(GetCurrentRoundInfo().MinersTermNumber).Result.PublicKeys.ToList();
            }
        }

        public UInt64Value CurrentRoundNumber
        {
            get
            {
                try
                {
                    return UInt64Value.Parser.ParseFrom(
                        _reader.ReadFiled<UInt64Value>(GlobalConfig.AElfDPoSCurrentRoundNumber));
                }
                catch (Exception)
                {
                    return new UInt64Value {Value = 0};
                }
            }
        }

        public UInt64Value CurrentTermNumber
        {
            get
            {
                try
                {
                    return UInt64Value.Parser.ParseFrom(
                        _reader.ReadFiled<UInt64Value>(GlobalConfig.AElfDPoSCurrentTermNumber));
                }
                catch (Exception)
                {
                    return new UInt64Value {Value = 0};
                }
            }
        }

        public UInt64Value BlockchainAge
        {
            get
            {
                try
                {
                    return UInt64Value.Parser.ParseFrom(
                        _reader.ReadFiled<UInt64Value>(GlobalConfig.AElfDPoSAgeFieldString));
                }
                catch (Exception)
                {
                    return new UInt64Value {Value = 0};
                }
            }
        }

        public Timestamp BlockchainStartTimestamp
        {
            get
            {
                try
                {
                    return Timestamp.Parser.ParseFrom(
                        _reader.ReadFiled<Timestamp>(GlobalConfig.AElfDPoSBlockchainStartTimestamp));
                }
                catch (Exception)
                {
                    return DateTime.UtcNow.ToTimestamp();
                }
            }
        }

        public Candidates Candidates
        {
            get
            {
                try
                {
                    var candidates = Candidates.Parser.ParseFrom(
                        _reader.ReadFiled<Candidates>(GlobalConfig.AElfDPoSCandidatesString));
                    if (candidates.PublicKeys.Count < GlobalConfig.BlockProducerNumber)
                    {
                        throw new Exception();
                    }

                    return candidates;
                }
                catch (Exception)
                {
                    Logger.LogTrace("No candidate, so the miners of next term will still be the initial miners.");
                    var initialMiners = _minersManager.GetMiners(0).Result.PublicKeys.ToCandidates();
                    initialMiners.IsInitialMiners = true;
                    return initialMiners;
                }
            }
        }

        private Round CurrentRoundInformation
        {
            get
            {
                try
                {
                    return Round.Parser.ParseFrom(_reader.ReadMap<Round>(CurrentRoundNumber,
                        GlobalConfig.AElfDPoSRoundsMapString));
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to get DPoS information of current round.\n");
                    return new Round();
                }
            }
        }

        private SInt32Value MiningInterval
        {
            get
            {
                try
                {
                    return SInt32Value.Parser.ParseFrom(
                        _reader.ReadFiled<SInt32Value>(GlobalConfig.AElfDPoSMiningIntervalString));
                }
                catch (Exception)
                {
                    return new SInt32Value {Value = ConsensusConfig.Instance.DPoSMiningInterval};
                }
            }
        }

        public ConsensusHelper(IMinersManager minersManager, ConsensusDataReader reader):this()
        {
            _minersManager = minersManager;
            _reader = reader;
        }

        /// <summary>
        /// Get block producer information of current round.
        /// </summary>
        /// <param name="accountAddressHex"></param>
        public MinerInRound this[string accountAddressHex]
        {
            get
            {
                try
                {
                    var bytes = _reader.ReadMap<Round>(CurrentRoundNumber, GlobalConfig.AElfDPoSRoundsMapString);
                    var round = Round.Parser.ParseFrom(bytes);
                    if (round.RealTimeMinersInfo.ContainsKey(accountAddressHex))
                        return round.RealTimeMinersInfo[accountAddressHex];

                    Logger.LogError("No such Block Producer in current round.");
                    return default(MinerInRound);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to get Block Producer information of current round.");
                    return default(MinerInRound);
                }
            }
        }

        public MinerInRound this[byte[] pubKey] => this[pubKey.ToPlainBase58()];

        private Round this[UInt64Value roundNumber]
        {
            get
            {
                try
                {
                    var bytes = _reader.ReadMap<Round>(roundNumber, GlobalConfig.AElfDPoSRoundsMapString);
                    var round = Round.Parser.ParseFrom(bytes);
                    return round;
                }
                catch (Exception ex)
                {
                    Logger.LogTrace(ex, $"Error while getting Round information of round {roundNumber.Value}.");
                    return default(Round);
                }
            }
        }

        public bool TryToGetVictories(out List<string> victories)
        {
            var ticketsMap = new Dictionary<string, ulong>();
            victories = new List<string>();
            var candidates = Candidates;
            if (candidates.PublicKeys.Count < GlobalConfig.BlockProducerNumber)
            {
                return false;
            }

            foreach (var candidate in candidates.PublicKeys)
            {
                var tickets = GetTickets(candidate);
                if (tickets.ObtainedTickets != 0)
                {
                    ticketsMap[candidate] = tickets.ObtainedTickets;
                }
            }

            if (ticketsMap.Keys.Count < GlobalConfig.BlockProducerNumber)
            {
                return false;
            }

            victories = ticketsMap.OrderByDescending(tm => tm.Value).Take(GlobalConfig.BlockProducerNumber)
                .Select(tm => tm.Key)
                .ToList();
            return !candidates.IsInitialMiners;
        }

        private Tickets GetTickets(string candidatePublicKey)
        {
            var bytes = _reader.ReadMap<Tickets>(candidatePublicKey.ToStringValue(),
                GlobalConfig.AElfDPoSTicketsMapString);
            return bytes == null ? new Tickets() : Tickets.Parser.ParseFrom(bytes);
        }

        public StringValue GetDPoSInfoToString()
        {
            ulong count = 1;

            if (CurrentRoundNumber.Value != 0)
            {
                count = CurrentRoundNumber.Value;
            }

            var infoOfOneRound = "";

            ulong i = 1;
            while (i <= count)
            {
                var roundInfoStr = GetRoundInfoToString(new UInt64Value {Value = i});
                infoOfOneRound += $"\n[Round {i}]\n" + roundInfoStr;
                i++;
            }

            var res = new StringValue
            {
                Value = infoOfOneRound + "Current round: " + CurrentRoundNumber?.Value
            };

            return res;
        }

        private string GetDPoSInfoToStringOfLatestRounds(ulong countOfRounds)
        {
            try
            {
                if (CurrentRoundNumber.Value == 0)
                {
                    return "Somehow current round number is 0";
                }

                if (countOfRounds == 0)
                {
                    return "";
                }

                var currentRoundNumber = CurrentRoundNumber.Value;
                ulong startRound;
                if (countOfRounds >= currentRoundNumber)
                {
                    startRound = 1;
                }
                else
                {
                    startRound = currentRoundNumber - countOfRounds + 1;
                }

                var infoOfOneRound = "";
                var i = startRound;
                while (i <= currentRoundNumber)
                {
                    if (i <= 0)
                    {
                        continue;
                    }

                    var roundInfoStr = GetRoundInfoToString(new UInt64Value {Value = i});
                    infoOfOneRound += $"\n[Round {i}]\n" + roundInfoStr;
                    i++;
                }

                return infoOfOneRound + $"Current round: {CurrentRoundNumber.Value}";
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to get dpos info");
                return "";
            }
        }

        /// <summary>
        /// This method should return true if all the BPs restarted (and missed their time slots).
        /// </summary>
        /// <returns></returns>
        public bool CanRecoverDPoSInformation()
        {
            return GlobalConfig.BlockProducerNumber == 1;
        }

        public ulong CalculateBlockchainAge()
        {
            return (ulong) (DateTime.UtcNow - BlockchainStartTimestamp.ToDateTime()).TotalMinutes + 1;
        }

        public void SyncMiningInterval()
        {
            ConsensusConfig.Instance.DPoSMiningInterval = MiningInterval.Value;
            Logger.LogInformation($"Set AElf DPoS mining interval to: {ConsensusConfig.Instance.DPoSMiningInterval} ms.");
        }

        public void LogDPoSInformation(ulong height)
        {
            Logger.LogTrace("Log dpos information - Start");
            Logger.LogTrace(GetDPoSInfoToStringOfLatestRounds(GlobalConfig.AElfDPoSLogRoundCount) +
                           $". Current height: {height}. Current term: {CurrentTermNumber.Value}. Current age: {BlockchainAge.Value}");
            Logger.LogTrace(GetCurrentElectionInformation());
            Logger.LogTrace("Log dpos information - End");
        }

        /// <summary>
        /// Valid candidate means someone has voted him.
        /// </summary>
        /// <param name="validCandidates"></param>
        /// <returns></returns>
        private bool TryToGetValidCandidates(out List<string> validCandidates)
        {
            validCandidates = new List<string>();
            foreach (var candidate in Candidates.PublicKeys)
            {
                if (GetTickets(candidate).ObtainedTickets > 0)
                {
                    validCandidates.Add(candidate);
                }
            }

            return validCandidates.Any();
        }

        private string GetCurrentElectionInformation()
        {
            var result = "";
            var dictionary = new Dictionary<string, ulong>();
            if (!TryToGetValidCandidates(out var candidates))
                return result;

            foreach (var candidatePublicKey in candidates)
            {
                var tickets = GetTickets(candidatePublicKey);

                dictionary.Add(GetAlias(candidatePublicKey), tickets.ObtainedTickets);
            }

            result += "\nElection information:\n";

            return dictionary.OrderByDescending(kv => kv.Value)
                .Aggregate(result, (current, pair) => current + $"[{pair.Key}]\n{pair.Value}\n");
        }

        public Round GetCurrentRoundInfo()
        {
            return CurrentRoundNumber.Value != 0 ? this[CurrentRoundNumber] : null;
        }

        public Miners GetCurrentMiners()
        {
            Logger.LogTrace($"Current term number: {CurrentTermNumber.Value}");
            var bytes = _reader.ReadMap<Miners>(CurrentTermNumber, GlobalConfig.AElfDPoSMinersMapString);
            var miners = AElf.Kernel.Miners.Parser.ParseFrom(bytes);
            return miners;
        }

        public TermSnapshot GetLatestTermSnapshot()
        {
            var bytes = _reader.ReadMap<TermSnapshot>(CurrentTermNumber, GlobalConfig.AElfDPoSSnapshotMapString);
            var snapshot = TermSnapshot.Parser.ParseFrom(bytes);
            return snapshot;
        }

        public bool TryGetRoundInfo(ulong roundNumber, out Round roundInfo)
        {
            if (roundNumber == 0)
            {
                roundInfo = null;
                return false;
            }

            var info = this[roundNumber.ToUInt64Value()];
            if (info != null)
            {
                roundInfo = info;
                return true;
            }

            roundInfo = null;
            return false;
        }

        private string GetRoundInfoToString(UInt64Value roundNumber)
        {
            try
            {
                var result = "";

                var roundInfo = this[roundNumber];
                foreach (var minerInfo in roundInfo.RealTimeMinersInfo.OrderBy(m => m.Value.Order))
                {
                    result += GetAlias(minerInfo.Key) +
                              (minerInfo.Value.IsExtraBlockProducer ? " [Current EBP]:\n" : ":\n");
                    result += "Order:\t\t" + minerInfo.Value.Order + "\n";
                    result += "Mining Time:\t" +
                              minerInfo.Value.ExpectedMiningTime.ToDateTime().ToLocalTime().ToString("u") + "\n";
                    result += "Signature:\t" +
                              minerInfo.Value.Signature?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                    result += "Out Value:\t" + minerInfo.Value.OutValue?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                    result += "In Value:\t" + minerInfo.Value.InValue?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                    result += "Mined Blocks:\t" + minerInfo.Value.ProducedBlocks + "\n";
                    // TODO: `IsForked` Not implemented yet, maybe useless.
                    result += "Is Forked:\t" + minerInfo.Value.IsForked + "\n";
                    result += "Missed Slots:\t" + minerInfo.Value.MissedTimeSlots + "\n";
                    result += "Latest Missed:\t" + minerInfo.Value.LatestMissedTimeSlots + "\n";
                }

                return result +
                       $"\nEBP TimeSlot of current round: {roundInfo.GetEBPMiningTime(MiningInterval.Value).ToLocalTime():u}\n";
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to get dpos info of round {roundNumber.Value}");
                return "";
            }
        }

        private string GetAlias(string publicKey)
        {
            var bytes = _reader.ReadMap<StringValue>(new StringValue {Value = publicKey},
                GlobalConfig.AElfDPoSAliasesMapString);
            return bytes == null
                ? publicKey.Substring(0, GlobalConfig.AliasLimit)
                : StringValue.Parser.ParseFrom(bytes).Value;
        }
    }
}