using OtpOnPc.Models;

using Reactive.Bindings;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OtpOnPc.Services;

public sealed class NoPasswordAesTotpRepository : ITotpRepository
{
    private const string FileName = "nopassaes\\protected-account";
    private const string IndexFileName = "nopassaes\\account-index.json";
    private const string DirectoryName = "nopassaes";
    private const string keyFileName = "key";

    private static readonly byte[] s_dummy = new byte[16];
    private readonly IsolatedStorageFile _storageFile;
    private readonly string _keypath;
    private readonly string _directoryPath;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public NoPasswordAesTotpRepository()
    {
        _directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OtpOnPc");
        _keypath = Path.Combine(_directoryPath, keyFileName);
        _storageFile = IsolatedStorageFile.GetUserStoreForApplication();
    }

    public async Task<TotpModel[]> Restore()
    {
        await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(_keypath))
            {
                return Array.Empty<TotpModel>();
            }
            var key = await File.ReadAllBytesAsync(_keypath).ConfigureAwait(false);

            AccountInfo[]? infos = null;
            if (_storageFile.FileExists(IndexFileName))
            {
                using var infoStream = _storageFile.OpenFile(IndexFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                infos = await JsonSerializer.DeserializeAsync<AccountInfo[]>(infoStream).ConfigureAwait(false);
            }

            if (infos != null && _storageFile.FileExists(FileName))
            {
                using var input = _storageFile.OpenFile(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                using Aes aes = Aes.Create();
                using ICryptoTransform decryptor = aes.CreateDecryptor(key, aes.IV);
                using var cryptStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
                await cryptStream.ReadAsync(s_dummy.AsMemory(0, 16))
                    .ConfigureAwait(false);

                Dictionary<Guid, byte[]>? dict = null;
                try
                {
                    dict = await JsonSerializer.DeserializeAsync<Dictionary<Guid, byte[]>>(cryptStream)
                        .ConfigureAwait(false);
                }
                catch
                {
                    return Array.Empty<TotpModel>();
                }

                if (dict == null)
                {
                    return Array.Empty<TotpModel>();
                }

                return infos.Select(x => dict.TryGetValue(x.Id, out var key) ? x.ToModel(key) : null)
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
            if (!_storageFile.DirectoryExists(DirectoryName))
            {
                _storageFile.CreateDirectory(DirectoryName);
            }

            // account-index.jsonだけ変更
            using var infoStream = _storageFile.CreateFile(IndexFileName);

            await JsonSerializer.SerializeAsync(infoStream, items.Select(AccountInfo.FromModel).ToArray())
                .ConfigureAwait(false);

            if (trigger is RepositoryStoreTrigger.OnAdded or RepositoryStoreTrigger.OnDeleted)
            {
                using Aes aes = Aes.Create();
                if (!Directory.Exists(_directoryPath))
                {
                    Directory.CreateDirectory(_directoryPath);
                }
                await File.WriteAllBytesAsync(_keypath, aes.Key);

                using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using var output = _storageFile.CreateFile(FileName);
                using var cryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write);
                Array.Clear(s_dummy);
                await cryptStream.WriteAsync(s_dummy.AsMemory(0, 16)).ConfigureAwait(false);

                await JsonSerializer.SerializeAsync(cryptStream, items.ToDictionary(x => x.Id, x => x.SecretKey))
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public Task Clear()
    {
        if (_storageFile.FileExists(FileName))
            _storageFile.DeleteFile(FileName);

        if (_storageFile.FileExists(IndexFileName))
            _storageFile.DeleteFile(IndexFileName);

        if (_storageFile.DirectoryExists(DirectoryName))
            _storageFile.DeleteDirectory(DirectoryName);

        if (File.Exists(_keypath))
            File.Delete(_keypath);

        return Task.CompletedTask;
    }
}
