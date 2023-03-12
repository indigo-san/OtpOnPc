using Avalonia;

using Nito.AsyncEx;

using OtpOnPc.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OtpOnPc.Services;

public sealed class TotpModelManager
{
    private readonly Task _initTask;
    private readonly List<TotpModel> _items = new();
    private readonly AsyncLock _asyncLock = new();

    public TotpModelManager()
    {
        async Task Init()
        {
            var items = await AvaloniaLocator.Current.GetRequiredService<ITotpRepository>().Restore();
            _items.AddRange(items);
        }

        _initTask = Init();
    }

    public event EventHandler<TotpModel>? Added;

    public event EventHandler<TotpModel>? Deleted;

    public event EventHandler<TotpModel>? Updated;

    public event EventHandler<(int OldIndex, int NewIndex)>? Moved;

    private async Task Save(RepositoryStoreTrigger trigger)
    {
        await AvaloniaLocator.Current.GetRequiredService<ITotpRepository>().Store(_items, trigger).ConfigureAwait(false);
    }

    public async Task AddItem(TotpModel item)
    {
        using (await _asyncLock.LockAsync())
        {
            await _initTask;

            _items.Add(item);
            await Save(RepositoryStoreTrigger.OnAdded);
            Added?.Invoke(this, item);
        }
    }

    public async Task DeleteItem(Guid id)
    {
        using (await _asyncLock.LockAsync())
        {
            await _initTask;

            if (await FindItem(id) is { } model)
            {
                _items.Remove(model);
                await Save(RepositoryStoreTrigger.OnDeleted);
                Deleted?.Invoke(this, model);
            }
        }
    }

    public async Task<TotpModel?> FindItem(Guid id)
    {
        using (await _asyncLock.LockAsync())
        {
            await _initTask;

            return _items.FirstOrDefault(o => o.Id == id);
        }
    }

    public async Task<IEnumerable<TotpModel>> GetItems()
    {
        using (await _asyncLock.LockAsync())
        {
            await _initTask;
            return _items;
        }
    }

    public async Task UpdateItem(TotpModel item)
    {
        using (await _asyncLock.LockAsync())
        {
            await _initTask;

            var index = _items.FindIndex(v => v.Id == item.Id);
            if (index >= 0)
            {
                _items[index] = item;
                await Save(RepositoryStoreTrigger.OnUpdated);
                Updated?.Invoke(this, item);
            }
        }
    }

    public async Task Move(int oldIndex, int newIndex)
    {
        using (await _asyncLock.LockAsync())
        {
            await _initTask;

            if (0 <= oldIndex && oldIndex < _items.Count
                && 0 <= newIndex && newIndex < _items.Count)
            {
                var item = _items[oldIndex];
                _items.RemoveAt(oldIndex);
                _items.Insert(newIndex, item);
                await Save(RepositoryStoreTrigger.OnMoved);
                Moved?.Invoke(this, (oldIndex, newIndex));
            }
        }
    }
}
