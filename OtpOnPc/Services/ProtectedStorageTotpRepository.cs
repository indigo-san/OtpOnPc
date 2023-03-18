﻿#if WINDOWS10_0_17763_0_OR_GREATER

using Avalonia;

using Microsoft.AspNetCore.DataProtection;

using OtpOnPc.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DataProtectionProvider = Windows.Security.Cryptography.DataProtection.DataProtectionProvider;

namespace OtpOnPc.Services;

public sealed class ProtectedStorageTotpRepository : ITotpRepository
{
    private const string FileName = "wscd\\protected-account";
    private const string IndexFileName = "wscd\\account-index.json";
    private const string Directory = "wscd";
    private readonly IsolatedStorageFile _storageFile;
    private readonly IDataProtector _dataProtector;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public ProtectedStorageTotpRepository()
    {
        _dataProtector = AvaloniaLocator.Current.GetRequiredService<IDataProtectionProvider>().CreateProtector("SecretKey.v1");
        _storageFile = IsolatedStorageFile.GetUserStoreForApplication();
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
            if (_storageFile.FileExists(IndexFileName))
            {
                using var infoStream = _storageFile.OpenFile(IndexFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var infos = await JsonSerializer.DeserializeAsync<AccountInfo[]>(infoStream).ConfigureAwait(false);
                _ = infos ?? throw new Exception("Failed to load accounts.");

                if (_storageFile.FileExists(FileName))
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
                    _ = dict ?? throw new Exception("Failed to restore secret keys.");

                    if (dict.Count != infos.Length)
                        throw new Exception("The number of accounts does not match the number of secret keys.");

                    var result = new TotpModel[dict.Count];
                    for (int i = 0; i < infos.Length; i++)
                    {
                        var account = infos[i];
                        if (dict.TryGetValue(account.Id, out var secretKey))
                        {
                            result[i] = account.ToModel(_dataProtector.Protect(secretKey));
                            Random.Shared.NextBytes(secretKey);
                        }
                        else
                        {
                            throw new Exception("Secret key does not exist.");
                        }
                    }

                    return result;
                }
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
                
                var dict = items.ToDictionary(x => x.Id, x => _dataProtector.Unprotect(x.ProtectedSecretKey));
                await JsonSerializer.SerializeAsync(unprotectedStream, dict)
                    .ConfigureAwait(false);
                foreach (var item in dict.Values)
                {
                    Random.Shared.NextBytes(item);
                }
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