#if WINDOWS10_0_17763_0_OR_GREATER

using OtpOnPc.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;

namespace OtpOnPc.Services;

public sealed class ProtectedStorageTotpRepository : ITotpRepository
{
    private const string FileName = "protected-user-content.json";
    private readonly IsolatedStorageFile _storageFile;
    private readonly List<TotpModel> _items;
    private Task _initTask;

    public ProtectedStorageTotpRepository()
    {
        _storageFile = IsolatedStorageFile.GetUserStoreForApplication();
        _items = new List<TotpModel>();
        _initTask = Init();
    }

    private async Task Init()
    {
        if (_storageFile.FileExists(FileName))
        {
            var prov = new DataProtectionProvider();

            using var input = _storageFile.OpenFile(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var inputStream = input.AsInputStream();
            using var output = new MemoryStream();
            using var outputStream = output.AsOutputStream();

            await prov.UnprotectStreamAsync(inputStream, outputStream);

            output.Position = 0;
            var array = JsonSerializer.Deserialize<TotpModel[]>(output)!;
            if (array != null)
            {
                _items.AddRange(array);
            }
        }
    }

    public event EventHandler<TotpModel>? Added;
    public event EventHandler<TotpModel>? Deleted;
    public event EventHandler<TotpModel>? Updated;
    public event EventHandler<(int OldIndex, int NewIndex)>? Moved;

    private async Task Save()
    {
        var prov = new DataProtectionProvider("LOCAL=user");

        using var unprotectedStream = new MemoryStream();
        JsonSerializer.Serialize(unprotectedStream, _items);
        unprotectedStream.Position = 0;

        using var inputStream = unprotectedStream.AsInputStream();
        using var output = _storageFile.CreateFile(FileName);
        using var outputStream = output.AsOutputStream();

        await prov.ProtectStreamAsync(inputStream, outputStream);
    }

    public async Task AddItem(TotpModel item)
    {
        await _initTask;

        _items.Add(item);
        await Save();
        Added?.Invoke(this, item);
    }

    public async Task DeleteItem(Guid id)
    {
        await _initTask;

        if (await FindItem(id) is { } model)
        {
            _items.Remove(model);
            await Save();
            Deleted?.Invoke(this, model);
        }
    }

    public async Task<TotpModel?> FindItem(Guid id)
    {
        await _initTask;

        return _items.FirstOrDefault(o => o.Id == id);
    }

    public async Task<IEnumerable<TotpModel>> GetItems()
    {
        await _initTask;

        return _items;
    }

    public async Task UpdateItem(TotpModel item)
    {
        await _initTask;

        var index = _items.FindIndex(v => v.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
            await Save();
            Updated?.Invoke(this, item);
        }
    }

    public async Task Move(int oldIndex, int newIndex)
    {
        await _initTask;

        if (0 <= oldIndex && oldIndex < _items.Count
            && 0 <= newIndex && newIndex < _items.Count)
        {
            var item = _items[oldIndex];
            _items.RemoveAt(oldIndex);
            _items.Insert(newIndex, item);
            await Save();
            Moved?.Invoke(this, (oldIndex, newIndex));
        }
    }
}
#endif