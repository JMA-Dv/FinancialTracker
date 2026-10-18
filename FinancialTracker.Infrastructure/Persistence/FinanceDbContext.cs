using FinancialTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Infrastructure.Persistence;

public class FinanceDbContext(DbContextOptions<FinanceDbContext> options)
    : IdentityUserContext<User, Guid>(options)
{
}
