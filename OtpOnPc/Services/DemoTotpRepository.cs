using OtpNet;

using OtpOnPc.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OtpOnPc.Services;

public sealed class DemoTotpRepository : ITotpRepository
{
    private readonly List<TotpModel> _items = new();

    public DemoTotpRepository()
    {
        AddItem(new(Guid.NewGuid(), KeyGeneration.GenerateRandomKey(20), "XXX App"));
        AddItem(new(Guid.NewGuid(), KeyGeneration.GenerateRandomKey(20), "YYY App"));
        AddItem(new(Guid.NewGuid(), KeyGeneration.GenerateRandomKey(20), "ZZZ App"));
    }

    public event EventHandler<TotpModel>? Added;
    public event EventHandler<TotpModel>? Deleted;
    public event EventHandler<TotpModel>? Updated;
    public event EventHandler<(int OldIndex, int NewIndex)>? Moved;

    public Task AddItem(TotpModel item)
    {
        _items.Add(item);
        Added?.Invoke(this, item);
        return Task.CompletedTask;
    }

    public async Task DeleteItem(Guid id)
    {
        if (await FindItem(id) is { } model)
        {
            _items.Remove(model);
            Deleted?.Invoke(this, model);
        }
    }

    public Task<TotpModel?> FindItem(Guid id)
    {
        return Task.FromResult(_items.FirstOrDefault(o => o.Id == id));
    }

    public Task<IEnumerable<TotpModel>> GetItems()
    {
        return Task.FromResult<IEnumerable<TotpModel>>(_items);
    }

    public Task UpdateItem(TotpModel item)
    {
        var index = _items.FindIndex(v => v.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
            Updated?.Invoke(this, item);
        }
        return Task.CompletedTask;
    }

    public Task Move(int oldIndex, int newIndex)
    {
        if (0 <= oldIndex && oldIndex < _items.Count
            && 0 <= newIndex && newIndex < _items.Count)
        {
            var item = _items[oldIndex];
            _items.RemoveAt(oldIndex);
            _items.Insert(newIndex, item);
            Moved?.Invoke(this, (oldIndex, newIndex));
        }
        return Task.CompletedTask;
    }
}
