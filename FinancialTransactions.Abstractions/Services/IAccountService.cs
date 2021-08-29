﻿using FinancialTransactions.Entities.Abstractions;
using FinancialTransactions.Inputs.Abstractions;
using System.Threading.Tasks;

namespace FinancialTransactions.Services.Abstractions
{
    public interface IAccountService
    {
        Task SignInAsync(int accountId, string jwToken, AuthenticationInput authenticationInput);
        Task<ILegalPerson> CreateAsync(string email);
        Task<ILegalPerson> GetOrCreateAsync(string email);
        Task CreditAsync(int accountId, decimal value);
        void Debit(int accountId, decimal value);
        void Credit(int accountId, decimal value);
    }
}