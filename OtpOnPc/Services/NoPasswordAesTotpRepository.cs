using Avalonia;

using Microsoft.AspNetCore.DataProtection;

using OtpOnPc.Models;

using Reactive.Bindings;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OtpOnPc.Services;

public sealed class NoPasswordAesTotpRepository : IsolatedStorageRepository, ITotpRepository
{
    private const string FileName = "nopassaes\\protected-account";
    private const string IndexFileName = "nopassaes\\account-index.json";
    private const string DirectoryName = "nopassaes";
    private const string keyFileName = "key";

    private static readonly byte[] s_dummy = new byte[16];
    private readonly IDataProtector _dataProtector;
    private readonly string _keypath;
    private readonly string _directoryPath;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public NoPasswordAesTotpRepository()
    {
        _dataProtector = AvaloniaLocator.Current.GetRequiredService<IDataProtectionProvider>().CreateProtector("SecretKey.v1");
        _directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OtpOnPc");
        _keypath = Path.Combine(_directoryPath, keyFileName);
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

            if (StorageFile.FileExists(IndexFileName))
            {
                using var infoStream = StorageFile.OpenFile(IndexFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var infos = await JsonSerializer.DeserializeAsync<AccountInfo[]>(infoStream).ConfigureAwait(false);
                _ = infos ?? throw new Exception("Failed to load accounts.");

                if (StorageFile.FileExists(FileName))
                {
                    using var input = StorageFile.OpenFile(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                    using Aes aes = Aes.Create();
                    using ICryptoTransform decryptor = aes.CreateDecryptor(key, aes.IV);
                    using var cryptStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
                    await cryptStream.ReadAsync(s_dummy.AsMemory(0, 16))
                        .ConfigureAwait(false);

                    Dictionary<Guid, byte[]>? dict = await JsonSerializer.DeserializeAsync<Dictionary<Guid, byte[]>>(cryptStream)
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
        string? oldAccountsFile = await BackupToTempFile(IndexFileName).ConfigureAwait(false);

        string? oldSecretFile = trigger is RepositoryStoreTrigger.OnAdded or RepositoryStoreTrigger.OnDeleted
            ? await BackupToTempFile(FileName).ConfigureAwait(false)
            : null;

        string? oldKeyFile = trigger is RepositoryStoreTrigger.OnAdded or RepositoryStoreTrigger.OnDeleted
            ? await BackupToTempFile(_keypath, false).ConfigureAwait(false)
            : null;

        try
        {
            CreateDirectoryIfNotExists(DirectoryName);

            // account-index.jsonだけ変更
            using var infoStream = StorageFile.CreateFile(IndexFileName);

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

                using var output = StorageFile.CreateFile(FileName);
                using var cryptStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write);
                Array.Clear(s_dummy);
                await cryptStream.WriteAsync(s_dummy.AsMemory(0, 16)).ConfigureAwait(false);

                var dict = items.ToDictionary(x => x.Id, x => _dataProtector.Unprotect(x.ProtectedSecretKey));
                await JsonSerializer.SerializeAsync(cryptStream, dict)
                    .ConfigureAwait(false);
                foreach (var item in dict.Values)
                {
                    Random.Shared.NextBytes(item);
                }
            }
        }
        catch
        {
            await RevertBackup(FileName, oldSecretFile)
                .ConfigureAwait(false);
            await RevertBackup(IndexFileName, oldAccountsFile)
                .ConfigureAwait(false);
            await RevertBackup(_keypath, oldKeyFile, false)
                .ConfigureAwait(false);
            throw;
        }
        finally
        {
            DeleteBackup(oldSecretFile);
            DeleteBackup(oldAccountsFile);
            DeleteBackup(oldKeyFile);
            _semaphoreSlim.Release();
        }
    }

    public Task Clear()
    {
        if (StorageFile.FileExists(FileName))
            StorageFile.DeleteFile(FileName);

        if (StorageFile.FileExists(IndexFileName))
            StorageFile.DeleteFile(IndexFileName);

        if (StorageFile.DirectoryExists(DirectoryName))
            StorageFile.DeleteDirectory(DirectoryName);

        if (File.Exists(_keypath))
            File.Delete(_keypath);

        return Task.CompletedTask;
    }
}
