using Avalonia;

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
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public TotpModelManager()
    {
        async Task Init()
        {
            var items = await AvaloniaLocator.Current.GetRequiredService<IUnlockScreen>().WaitUnlocked().ConfigureAwait(false);
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
        await _semaphoreSlim.WaitAsync();
        try
        {
            await _initTask;

            _items.Add(item);
            await Save(RepositoryStoreTrigger.OnAdded);
            Added?.Invoke(this, item);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task DeleteItem(Guid id)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            await _initTask;

            if (_items.FirstOrDefault(o => o.Id == id) is { } model)
            {
                _items.Remove(model);
                await Save(RepositoryStoreTrigger.OnDeleted);
                Deleted?.Invoke(this, model);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<TotpModel?> FindItem(Guid id)
    {
        await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            await _initTask.ConfigureAwait(false);

            return _items.FirstOrDefault(o => o.Id == id);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<IEnumerable<TotpModel>> GetItems()
    {
        await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            await _initTask.ConfigureAwait(false);
            return _items;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task UpdateItem(TotpModel item)
    {
        await _semaphoreSlim.WaitAsync();
        try
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
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task Move(int oldIndex, int newIndex)
    {
        await _semaphoreSlim.WaitAsync();
        try
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
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task Reset()
    {
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            TotpModel? item = _items[i];
            _items.RemoveAt(i);
            Deleted?.Invoke(this, item);
        }

        await AvaloniaLocator.Current.GetRequiredService<ITotpRepository>().Clear().ConfigureAwait(false);
    }
}
