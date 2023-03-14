#if WINDOWS10_0_17763_0_OR_GREATER

using OtpOnPc.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Windows.Security.Cryptography.DataProtection;

namespace OtpOnPc.Services;

public sealed class ProtectedStorageTotpRepository : ITotpRepository
{
    private const string FileName = "wscd\\protected-account";
    private const string IndexFileName = "wscd\\account-index.json";
    private const string Directory = "wscd";
    private readonly IsolatedStorageFile _storageFile;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public ProtectedStorageTotpRepository()
    {
        _storageFile = IsolatedStorageFile.GetUserStoreForApplication();
    }

    private static TotpModel ToModel(AccountInfo info, byte[] secretKey)
    {
        return new TotpModel(info.Id, secretKey, info.Name, info.HashMode, info.Size);
    }

    public Task Clear()
    {
        if (_storageFile.FileExists(FileName))
            _storageFile.DeleteFile(FileName);

        if (_storageFile.FileExists(IndexFileName))
            _storageFile.DeleteFile(IndexFileName);

        if (_storageFile.DirectoryExists(Directory))
            _storageFile.DeleteDirectory(Directory);

        return Task.CompletedTask;
    }

    public async Task<TotpModel[]> Restore()
    {
        await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            AccountInfo[]? infos = null;
            if (_storageFile.FileExists(IndexFileName))
            {
                using var infoStream = _storageFile.OpenFile(IndexFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                infos = await JsonSerializer.DeserializeAsync<AccountInfo[]>(infoStream).ConfigureAwait(false);
            }

            if (infos != null && _storageFile.FileExists(FileName))
            {
                var prov = new DataProtectionProvider();

                using var input = _storageFile.OpenFile(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var inputStream = input.AsInputStream();
                using var output = new MemoryStream();
                using var outputStream = output.AsOutputStream();

                await prov.UnprotectStreamAsync(inputStream, outputStream);

                output.Position = 0;
                var dict = await JsonSerializer.DeserializeAsync<Dictionary<Guid, byte[]>>(output)
                    .ConfigureAwait(false);
                if (dict == null)
                {
                    return Array.Empty<TotpModel>();
                }

                return infos.Select(x => dict.TryGetValue(x.Id, out var key) ? ToModel(x, key) : null)
                    .Where(x => x != null)
                    .ToArray()!;
            }

            return Array.Empty<TotpModel>();
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task Store(IEnumerable<TotpModel> items, RepositoryStoreTrigger trigger)
    {
        await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_storageFile.DirectoryExists(Directory))
            {
                _storageFile.CreateDirectory(Directory);
            }

            // account-index.jsonだけ変更
            using var infoStream = _storageFile.CreateFile(IndexFileName);
            await JsonSerializer.SerializeAsync(infoStream, items.Select(AccountInfo.FromModel).ToArray());

            if (trigger is RepositoryStoreTrigger.OnAdded or RepositoryStoreTrigger.OnDeleted)
            {
                var prov = new DataProtectionProvider("LOCAL=user");

                using var unprotectedStream = new MemoryStream();
                await JsonSerializer.SerializeAsync(unprotectedStream, items.ToDictionary(x => x.Id, x => x.SecretKey))
                    .ConfigureAwait(false);
                unprotectedStream.Position = 0;

                using var inputStream = unprotectedStream.AsInputStream();
                using var output = _storageFile.CreateFile(FileName);
                using var outputStream = output.AsOutputStream();

                await prov.ProtectStreamAsync(inputStream, outputStream);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}
#endif