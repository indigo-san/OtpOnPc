using Avalonia;

using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.DataProtection;

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

public sealed class AesTotpRepository : ITotpRepository
{
    private const string FileName = "aes\\protected-account";
    private const string IndexFileName = "aes\\account-index.json";
    private const string DirectoryName = "aes";
    private const string SecretFileName = "salt";

    private static readonly byte[] s_dummy = new byte[16];
    private readonly IsolatedStorageFile _storageFile;
    private readonly IDataProtector _dataProtector;
    private readonly string _secretpath;
    private readonly string _directoryPath;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly ReactivePropertySlim<bool> _isPasswordSet = new();
    private byte[]? _salt;
    private byte[]? _hash;

    public AesTotpRepository()
    {
        _dataProtector = AvaloniaLocator.Current.GetRequiredService<IDataProtectionProvider>().CreateProtector("SecretKey.v1");
        _directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OtpOnPc");
        _secretpath = Path.Combine(_directoryPath, SecretFileName);
        _storageFile = IsolatedStorageFile.GetUserStoreForApplication();
        if (File.Exists(_secretpath))
        {
            try
            {
                _salt = File.ReadAllBytes(_secretpath);
                _isPasswordSet.Value = true;
            }
            catch
            {
            }
        }
    }

    public IReadOnlyReactiveProperty<bool> IsPasswordSet => _isPasswordSet;

    public async Task<TotpModel[]> Restore(byte[] key)
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
                    using var input = _storageFile.OpenFile(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);

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

    public async Task<TotpModel[]> Restore()
    {
        if (_hash != null)
        {
            return await Restore(_hash).ConfigureAwait(false);
        }
        else
        {
            return Array.Empty<TotpModel>();
        }
    }

    public async Task Store(byte[] key, IEnumerable<TotpModel> items, RepositoryStoreTrigger trigger)
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
                using ICryptoTransform encryptor = aes.CreateEncryptor(key, aes.IV);

                using var output = _storageFile.CreateFile(FileName);
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
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task Store(IEnumerable<TotpModel> items, RepositoryStoreTrigger trigger)
    {
        if (_hash != null)
        {
            await Store(_hash, items, trigger).ConfigureAwait(false);
        }
    }

    public async Task<bool> UpdatePassword(string? oldPassword, string newPassword)
    {
        var result = await UpdatePasswordCore(oldPassword, newPassword);
        if (result)
        {
            var manager = AvaloniaLocator.Current.GetRequiredService<TotpModelManager>();
            var items = await manager.GetItems();
            await Store(items, RepositoryStoreTrigger.OnAdded);
        }

        return result;
    }

    public async Task<TotpModel[]?> Unlock(string password)
    {
        if (_salt == null)
            return null;

        byte[] hash = GenHash(password, _salt);

        var items = await Restore(hash).ConfigureAwait(false);
        _hash = hash;
        return items;
    }

    private async Task<bool> UpdatePasswordCore(string? oldPassword, string newPassword)
    {
        if (_hash != null && _salt != null)
        {
            if (oldPassword == null)
                return false;

            byte[] oldHash = GenHash(oldPassword, _salt);

            if (!_hash.AsSpan().SequenceEqual(oldHash))
            {
                return false;
            }
        }

        byte[] salt = new byte[128 / 8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        byte[] hash = GenHash(newPassword, salt);

        _salt = salt;
        _hash = hash;

        if (!Directory.Exists(_directoryPath))
        {
            Directory.CreateDirectory(_directoryPath);
        }

        await File.WriteAllBytesAsync(_secretpath, _salt);
        _isPasswordSet.Value = true;

        return true;
    }

    private static byte[] GenHash(string password, byte[] salt)
    {
        return KeyDerivation.Pbkdf2(
            password,
            salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);
    }

    public Task Clear()
    {
        if (_storageFile.FileExists(FileName))
            _storageFile.DeleteFile(FileName);

        if (_storageFile.FileExists(IndexFileName))
            _storageFile.DeleteFile(IndexFileName);

        if (_storageFile.DirectoryExists(DirectoryName))
            _storageFile.DeleteDirectory(DirectoryName);

        if (File.Exists(_secretpath))
        {
            File.Delete(_secretpath);
            _isPasswordSet.Value = false;
            _salt = null;
            _hash = null;
        }

        return Task.CompletedTask;
    }
}
