using OtpNet;

using OtpOnPc.Models;

using System;

namespace OtpOnPc.Services;

public record AccountInfo(Guid Id, string Name, OtpHashMode HashMode = OtpHashMode.Sha1, int Size = 6)
{
    public static AccountInfo FromModel(TotpModel model)
    {
        return new AccountInfo(model.Id, model.Name, model.HashMode, model.Size);
    }
}
