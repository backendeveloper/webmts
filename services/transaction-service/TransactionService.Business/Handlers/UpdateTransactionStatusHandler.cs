using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Contract.Dtos;
using TransactionService.Contract.Enums;
using TransactionService.Contract.Requests;
using TransactionService.Contract.Responses;
using TransactionService.Data;
using TransactionService.Data.Entities;

namespace TransactionService.Business.Handlers;

public class
    UpdateTransactionStatusHandler : IRequestHandler<UpdateTransactionStatusRequest, UpdateTransactionStatusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTransactionStatusHandler> _logger;

    public UpdateTransactionStatusHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateTransactionStatusHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UpdateTransactionStatusResponse> Handle(UpdateTransactionStatusRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Updating transaction status for transaction {request.TransactionId}");

        try
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(request.TransactionId);
            if (transaction == null)
            {
                return new UpdateTransactionStatusResponse
                {
                    Success = false,
                    Message = $"Transaction with id {request.TransactionId} not found"
                };
            }

            var oldStatus = transaction.Status;

            if (!IsValidStatusTransition(oldStatus, request.NewStatus))
            {
                return new UpdateTransactionStatusResponse
                {
                    Success = false,
                    Message = $"Invalid status transition from {oldStatus} to {request.NewStatus}"
                };
            }

            transaction.Status = request.NewStatus;
            transaction.UpdatedAt = DateTime.UtcNow;
            transaction.UpdatedBy = "system";

            if (request.NewStatus == TransactionStatusType.Completed)
                transaction.CompletedAt = DateTime.UtcNow;

            var transactionHistory = new TransactionHistory
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                OldStatus = oldStatus,
                NewStatus = request.NewStatus,
                Notes = request.Notes,
                CreatedBy = "system"
            };

            await _unitOfWork.BeginTransactionAsync();

            _unitOfWork.Transactions.Update(transaction);
            await _unitOfWork.TransactionHistories.AddAsync(transactionHistory);

            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync();

            // Event yayınlanabilir burada
            // await _eventBus.PublishAsync(new TransactionStatusChangedEvent 
            // { 
            //     TransactionId = transaction.Id,
            //     OldStatus = oldStatus,
            //     NewStatus = request.NewStatus
            // });

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

            return new UpdateTransactionStatusResponse
            {
                Success = true,
                Message = "Transaction status updated successfully",
                Transaction = transactionDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating transaction status for transaction {request.TransactionId}");
            await _unitOfWork.RollbackTransactionAsync();

            return new UpdateTransactionStatusResponse
            {
                Success = false,
                Message = $"Failed to update transaction status: {ex.Message}"
            };
        }
    }

    private static bool IsValidStatusTransition(TransactionStatusType currentStatus, TransactionStatusType newStatus)
    {
        switch (currentStatus)
        {
            case TransactionStatusType.New:
                return newStatus is TransactionStatusType.Processing or TransactionStatusType.Cancelled;
            case TransactionStatusType.Processing:
                return newStatus is TransactionStatusType.Completed or TransactionStatusType.Failed;
            case TransactionStatusType.Completed:
            case TransactionStatusType.Failed:
            case TransactionStatusType.Cancelled:
            default:
                return false;
        }
    }
}