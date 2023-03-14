using OtpNet;

using OtpOnPc.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OtpOnPc.Services;

public sealed class InMemoryTotpRepository : ITotpRepository
{
    private TotpModel[] _items =
    {
        new(Guid.NewGuid(), KeyGeneration.GenerateRandomKey(20), "XXX App"),
        new(Guid.NewGuid(), KeyGeneration.GenerateRandomKey(20), "YYY App"),
        new(Guid.NewGuid(), KeyGeneration.GenerateRandomKey(20), "ZZZ App"),
    };

    public Task Clear()
    {
        return Task.CompletedTask;
    }

    public Task<TotpModel[]> Restore()
    {
        return Task.FromResult(_items);
    }

    public Task Store(IEnumerable<TotpModel> items, RepositoryStoreTrigger trigger)
    {
        _items = items.ToArray();
        return Task.CompletedTask;
    }
}
