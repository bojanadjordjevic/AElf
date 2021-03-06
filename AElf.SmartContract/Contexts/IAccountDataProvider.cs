﻿namespace AElf.SmartContract
{
    /// <summary>
    /// Data is stored associated with Account
    /// </summary>
    public interface IAccountDataProvider
    {
        IAccountDataContext Context { get; set; }
       
        IDataProvider GetDataProvider();
    }
}