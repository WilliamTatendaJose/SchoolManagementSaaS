using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Library.Queries;

/// <summary>Lists a student's loan history, most recent first.</summary>
public record GetStudentLoansQuery(Guid StudentId) : IRequest<Result<List<BookLoanDto>>>;
