using OtpOnPc.Models;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace OtpOnPc.Services;

public enum RepositoryStoreTrigger
{
    OnAdded,
    OnDeleted,
    OnUpdated,
    OnMoved
}

public interface ITotpRepository
{
    Task<TotpModel[]> Restore();

    Task Store(IEnumerable<TotpModel> items, RepositoryStoreTrigger trigger);
}
