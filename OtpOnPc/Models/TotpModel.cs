﻿using OtpNet;

using System;
using System.Text.Json.Serialization;

namespace OtpOnPc.Models;

public record TotpModel(Guid Id, byte[] SecretKey, string Name, OtpHashMode HashMode = OtpHashMode.Sha1, int Size = 6)
{
    private string? _lazy;

    [JsonIgnore]
    public string Base32
    {
        get
        {
            return _lazy ??= Base32Encoding.ToString(SecretKey);
        }
    }
}
