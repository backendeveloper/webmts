using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Contract.Dtos;
using TransactionService.Contract.Requests;
using TransactionService.Contract.Responses;
using TransactionService.Data;

namespace TransactionService.Business.Handlers;

public class
    GetTransactionByIdHandler : IRequestHandler<GetTransactionByIdRequest, GetTransactionByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTransactionByIdHandler> _logger;

    public GetTransactionByIdHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTransactionByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetTransactionByIdResponse> Handle(GetTransactionByIdRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Getting transaction details for transaction {request.TransactionId}");
                
            var transaction = await _unitOfWork.Transactions.GetTransactionWithHistoryAsync(request.TransactionId);
            if (transaction == null)
                return new GetTransactionByIdResponse
                {
                    Success = false,
                    Message = $"Transaction with id {request.TransactionId} not found"
                };

            var transactionDetailDto = new TransactionDetailDto
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
                
            return new GetTransactionByIdResponse
            {
                Success = true,
                Message = "Transaction retrieved successfully",
                Transaction = transactionDetailDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving transaction {request.TransactionId}");
                
            return new GetTransactionByIdResponse
            {
                Success = false,
                Message = $"Failed to retrieve transaction: {ex.Message}"
            };
        }
    }
}