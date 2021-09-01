﻿using FinancialTransactions.Entities;
using FinancialTransactions.Databases.Abstractions;
using FinancialTransactions.Services.Abstractions;
using System;
using System.Linq;
using FinancialTransactions.Inputs.Abstractions;
using FinancialTransactions.Validation;
using System.Threading.Tasks;
using FinancialTransactions.Entities.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace FinancialTransactions
{
    internal class TransactionService : ITransactionService
    {
        readonly IFinancialTransactionsDatabase _financialTransactionsDatabase;
        readonly IAccountService _accountService;

        public TransactionService(IFinancialTransactionsDatabase financialTransactionsDatabase,
            IAccountService accountService)
        {
            _financialTransactionsDatabase = financialTransactionsDatabase;
            _accountService = accountService;
        }

        public async Task<ITransaction> RequestAsync(TransactionInput transactionInput)
        {
            var transaction = AddTransaction(transactionInput);
            await _financialTransactionsDatabase.CommitAsync();
            return transaction;
        }

        public async Task<ITransaction> PromiseAsync(int transactionId)
        {
            var transaction = _financialTransactionsDatabase.Query<Transaction>().FirstOrDefault(e => e.Id == transactionId);
            EntityValidator.ValidateNotNullable(transaction);
            if (transaction.Status != TransactionStatus.Requested)
            {
                throw new ValidationException("The transaction must be requested.");
            }
            transaction.Status = TransactionStatus.Promised;
            transaction.PromisedTime = DateTime.Now.Date.AddDays(1).AddHours(13);//parametrizável

            _financialTransactionsDatabase.Update(transaction);
            await _financialTransactionsDatabase.CommitAsync();

            return transaction;
        }
        public async Task<ITransaction> TransferAsync(int transactionId)
        {
            var transaction = _financialTransactionsDatabase.Query<Transaction>().FirstOrDefault(e => e.Id == transactionId);
            EntityValidator.ValidateNotNullable(transaction);
            if (transaction.Status == TransactionStatus.Transferred)
            {
                throw new ValidationException("The transaction has already been transferred.");
            }

            await TransferAsync(transaction);
            return transaction;
        }

        public async Task<ITransaction> TransferAsync(TransactionInput transactionInput)
        {
            var transaction = AddTransaction(transactionInput);
            await _financialTransactionsDatabase.CommitAsync();

            await TransferAsync(transaction);
            return transaction;
        }

        private Transaction AddTransaction(TransactionInput transactionInput)
        {
            if (transactionInput.Value <= 0)
            {
                var message = $"Not allowed to transfer less or equal to 0.";
                throw new ValidationException(message);
            }
            var transaction = new Transaction
            {
                Status = TransactionStatus.Requested,
                Value = transactionInput.Value,
                FromId = transactionInput.FromId,
                ToId = transactionInput.ToId,
                CreationTime = DateTime.Now
            };
            _financialTransactionsDatabase.Add(transaction);
            return transaction;
        }

        private async Task TransferAsync(Transaction transaction)
        {
            _accountService.Debit(transaction.FromId, transaction.Value);
            _accountService.Credit(transaction.ToId, transaction.Value);

            transaction.Status = TransactionStatus.Transferred;
            transaction.TransferTime = DateTime.Now;

            _financialTransactionsDatabase.Update(transaction);

            await _financialTransactionsDatabase.CommitAsync();
        }
    }
}
