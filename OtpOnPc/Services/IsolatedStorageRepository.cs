using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;

namespace OtpOnPc.Services;

public abstract class IsolatedStorageRepository
{
    public IsolatedStorageRepository()
    {
        StorageFile = IsolatedStorageFile.GetUserStoreForApplication();
    }

    protected IsolatedStorageFile StorageFile { get; }

    protected async ValueTask<string?> BackupToTempFile(string name, bool isolated = true)
    {
        bool Exists()
        {
            return isolated ? StorageFile.FileExists(name) : File.Exists(name);
        }

        Stream Open()
        {
            return isolated
                ? StorageFile.OpenFile(name, FileMode.Open, FileAccess.Read, FileShare.Read)
                : File.OpenRead(name);
        }

        if (!Exists())
            return null;
        var tempFile = Path.GetTempFileName();

        using var srcStream = Open();
        using var dstStream = File.OpenWrite(tempFile);

        await srcStream.CopyToAsync(dstStream).ConfigureAwait(false);
        return tempFile;
    }

    protected async ValueTask RevertBackup(string name, string? backupName, bool isolated = true)
    {
        Stream Open()
        {
            return isolated
                ? StorageFile.OpenFile(name, FileMode.Create, FileAccess.Write, FileShare.None)
                : File.Create(name);
        }

        if (backupName != null)
        {
            using (var stream = Open())
            using (var backupStream = File.OpenRead(backupName))
            {
                await backupStream.CopyToAsync(stream).ConfigureAwait(false);
            }

            File.Delete(backupName);
        }
    }

    protected static void DeleteBackup(string? backupName)
    {
        if (File.Exists(backupName))
        {
            File.Delete(backupName);
        }
    }

    protected void CreateDirectoryIfNotExists(string name)
    {
        if (!StorageFile.DirectoryExists(name))
        {
            StorageFile.CreateDirectory(name);
        }
    }
}