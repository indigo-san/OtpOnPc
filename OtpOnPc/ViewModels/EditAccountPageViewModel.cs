using Avalonia;

using OtpOnPc.Models;
using OtpOnPc.Services;
using OtpOnPc.Views;

using Reactive.Bindings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpOnPc.ViewModels;

public class EditAccountPageViewModel
{
    public EditAccountPageViewModel(TotpModel model)
    {
        Model = model;
    }

    public TotpModel Model { get; }

    public ReactiveProperty<string> Name { get; } = new();

    public ReactiveProperty<ImageIconType> IconType { get; } = new();

    public async Task Apply()
    {
        var manager = AvaloniaLocator.Current.GetRequiredService<TotpModelManager>();
        await manager.UpdateItem(Model with
        {
            Name = Name.Value,
            IconType = IconType.Value,
        })
            .ConfigureAwait(false);
    }
}
