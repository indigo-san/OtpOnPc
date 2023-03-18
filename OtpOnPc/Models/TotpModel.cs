using OtpNet;

using OtpOnPc.Views;

using System;
using System.Text.Json.Serialization;

namespace OtpOnPc.Models;

public record TotpModel(
    Guid Id,
    byte[] ProtectedSecretKey,
    string Name,
    OtpHashMode HashMode = OtpHashMode.Sha1,
    int Size = 6,
    ImageIconType IconType = ImageIconType.Initial,
    int Step = 30)
{
}
