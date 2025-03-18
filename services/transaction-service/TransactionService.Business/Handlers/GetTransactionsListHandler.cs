using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Contract.Dtos;
using TransactionService.Contract.Requests;
using TransactionService.Contract.Responses;
using TransactionService.Data;

namespace TransactionService.Business.Handlers;

public class
    GetTransactionsListHandler : IRequestHandler<GetTransactionsListRequest, GetTransactionsListResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTransactionsListHandler> _logger;

    public GetTransactionsListHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTransactionsListHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetTransactionsListResponse> Handle(GetTransactionsListRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting transactions list");

            var allTransactions = await _unitOfWork.Transactions.GetAllAsync();
            var totalCount = allTransactions.Count();

            var paginatedTransactions = allTransactions
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var transactionDtos = paginatedTransactions.Select(paginatedTransaction => new TransactionDto
                {
                    Id = paginatedTransaction.Id,
                    TransactionNumber = paginatedTransaction.TransactionNumber,
                    Amount = paginatedTransaction.Amount,
                    Status = paginatedTransaction.Status,
                    Currency = paginatedTransaction.Currency,
                    CustomerId = paginatedTransaction.CustomerId,
                    SourceAccountId = paginatedTransaction.SourceAccountId,
                    DestinationAccountId = paginatedTransaction.DestinationAccountId,
                    Description = paginatedTransaction.Description
                })
                .ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return new GetTransactionsListResponse
            {
                Success = true,
                Message = "Transactions retrieved successfully",
                TotalCount = totalCount,
                PageSize = request.PageSize,
                PageNumber = request.PageNumber,
                TotalPages = totalPages,
                Transactions = transactionDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions list");

            return new GetTransactionsListResponse
            {
                Success = false,
                Message = $"Failed to retrieve transactions: {ex.Message}"
            };
        }
    }
}