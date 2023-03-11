using OtpOnPc.Models;

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OtpOnPc.Services;

public interface ITotpRepository
{
    event EventHandler<TotpModel>? Added;
    event EventHandler<TotpModel>? Deleted;
    event EventHandler<TotpModel>? Updated;
    event EventHandler<(int OldIndex, int NewIndex)>? Moved;

    Task<IEnumerable<TotpModel>> GetItems();

    Task<TotpModel?> FindItem(Guid id);

    Task UpdateItem(TotpModel item);

    Task DeleteItem(Guid id);

    Task AddItem(TotpModel item);

    Task Move(int oldIndex, int newIndex);
}
