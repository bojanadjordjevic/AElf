using System;
using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Sdk.CSharp2.Tests.TestContract
{
    public partial class TokenContract : CSharpSmartContract<TokenContractState>
    {
        public void Initialize(string symbol, string tokenName, ulong totalSupply, uint decimals)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            // TODO: Add back this assert
            // Api.Assert(Api.GetContractOwner().Equals(Api.GetFromAddress()), "Only owner can initialize the contract state.");

            // Set token info
            State.TokenInfo.Symbol.Value = symbol;
            State.TokenInfo.TokenName.Value = tokenName;
            State.TokenInfo.TotalSupply.Value = totalSupply;
            State.TokenInfo.Decimals.Value = decimals;

            // Assign total supply to owner
            State.Balances[Context.Sender] = totalSupply;

            // Set initialized flag
            State.Initialized.Value = true;
        }

        public void Transfer(Address to, ulong amount)
        {
            var from = Context.Sender;
            DoTransfer(from, to, amount);
        }

        public void TransferFrom(Address from, Address to, ulong amount)
        {
            var allowance = State.Allowances[from][Context.Sender];
            Assert(allowance >= amount, "Insufficient allowance.");

            DoTransfer(from, to, amount);
            State.Allowances[from][Context.Sender] = allowance.Sub(amount);
        }

        public void Approve(Address spender, ulong amount)
        {
            State.Allowances[Context.Sender][spender].Add(amount);
            Context.FireEvent(new Approved()
            {
                Owner = Context.Sender,
                Spender = spender,
                Amount = amount
            });
        }

        public void UnApprove(Address spender, ulong amount)
        {
            var oldAllowance = State.Allowances[Context.Sender][spender];
            var amountOrAll = Math.Min(amount, oldAllowance);
            State.Allowances[Context.Sender][spender] = oldAllowance.Sub(amountOrAll);
            Context.FireEvent(new UnApproved()
            {
                Owner = Context.Sender,
                Spender = spender,
                Amount = amountOrAll
            });
        }

        public void Burn(ulong amount)
        {
            var existingBalance = State.Balances[Context.Sender];
            Assert(existingBalance >= amount, "Burner doesn't own enough balance.");
            State.Balances[Context.Sender] = existingBalance.Sub(amount);
            State.TokenInfo.TotalSupply.Value = State.TokenInfo.TotalSupply.Value.Sub(amount);
            Context.FireEvent(new Burned()
            {
                Burner = Context.Sender,
                Amount = amount
            });
        }
    }
}