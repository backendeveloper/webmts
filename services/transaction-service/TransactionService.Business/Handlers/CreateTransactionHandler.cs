using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Contract.Dtos;
using TransactionService.Contract.Enums;
using TransactionService.Contract.Requests;
using TransactionService.Contract.Responses;
using TransactionService.Data;
using TransactionService.Data.Entities;

namespace TransactionService.Business.Handlers;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionRequest, CreateTransactionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTransactionHandler> _logger;

    public CreateTransactionHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateTransactionHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateTransactionResponse> Handle(CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating a new transaction");

        try
        {
            var transaction = new TransactionService.Data.Entities.Transaction
            {
                Id = Guid.NewGuid(),
                TransactionNumber = GenerateTransactionNumber(),
                Type = request.Type,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = TransactionStatusType.New,
                SourceAccountId = request.SourceAccountId,
                DestinationAccountId = request.DestinationAccountId,
                Description = request.Description,
                CustomerId = request.CustomerId,
                CreatedBy = "system"
            };

            var transactionHistory = new TransactionHistory
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                OldStatus = TransactionStatusType.New,
                NewStatus = TransactionStatusType.New,
                Notes = "Transaction created",
                CreatedBy = "system"
            };

            await _unitOfWork.BeginTransactionAsync();

            await _unitOfWork.Transactions.AddAsync(transaction);
            await _unitOfWork.TransactionHistories.AddAsync(transactionHistory);

            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync();

            // Event yayınlanabilir burada
            // await _eventBus.PublishAsync(new TransactionCreatedEvent { TransactionId = transaction.Id });

            var transactionDto = new TransactionDto
            {
                Id = transaction.Id,
                TransactionNumber = transaction.TransactionNumber,
                Amount = transaction.Amount,
                Status = transaction.Status,
                Currency = transaction.Currency,
                CustomerId = transaction.CustomerId,
                SourceAccountId = transaction.SourceAccountId,
                DestinationAccountId = transaction.DestinationAccountId,
                Description = transaction.Description
            };

            return new CreateTransactionResponse
            {
                Success = true,
                Message = "Transaction created successfully",
                Transaction = transactionDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            await _unitOfWork.RollbackTransactionAsync();

            return new CreateTransactionResponse
            {
                Success = false,
                Message = $"Failed to create transaction: {ex.Message}"
            };
        }
    }

    private string GenerateTransactionNumber()
    {
        return $"TRX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8)}";
    }
}