using OtpNet;

using OtpOnPc.Models;
using OtpOnPc.Views;

using System;

namespace OtpOnPc.Services;

public record AccountInfo(
    Guid Id,
    string Name,
    OtpHashMode HashMode = OtpHashMode.Sha1,
    int Size = 6,
    ImageIconType IconType = ImageIconType.Initial,
    int Step = 30)
{
    public TotpModel ToModel(byte[] secretKey)
    {
        return new TotpModel(Id, secretKey, Name, HashMode, Size, IconType, Step);
    }

    public static AccountInfo FromModel(TotpModel model)
    {
        return new AccountInfo(model.Id, model.Name, model.HashMode, model.Size, model.IconType, model.Step);
    }
}
