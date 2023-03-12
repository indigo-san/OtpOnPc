using Avalonia;

using OtpOnPc.Models;
using OtpOnPc.Services;

using Reactive.Bindings;
using Reactive.Bindings.TinyLinq;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OtpOnPc.ViewModels;

public class MainPageViewModel
{
    private readonly TotpModelManager _totpManager;
    private CancellationTokenSource? _cts;
    internal readonly Task _initializeTask;

    public MainPageViewModel()
    {
        _totpManager = AvaloniaLocator.Current.GetRequiredService<TotpModelManager>();
        _initializeTask = Init();

        _totpManager.Updated += OnTotpReposUpdated;
        _totpManager.Added += OnTotpReposAdded;
        _totpManager.Deleted += OnTotpReposDeleted;
        _totpManager.Moved += OnTotpReposMoved;

        CopySelected = new ReactiveCommand<TotpItemViewModel>();
        CopySelected.Where(x => x != null)
            .Subscribe(async o =>
            {
                var task = Application.Current?.Clipboard?.SetTextAsync(o.OriginalCode.Value);
                if (task != null)
                {
                    await task;
                    Message.Value = "コードをコピーしました";

                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();

                    try
                    {
                        await Task.Delay(2000, _cts.Token);
                        Message.Value = "";
                    }
                    catch
                    {
                    }
                }
            });
    }

    private async Task Init()
    {
        foreach (var item in await _totpManager.GetItems())
        {
            Items.Add(new TotpItemViewModel(item));
        }
    }

    public ReactiveCollection<TotpItemViewModel> Items { get; } = new();

    public ReactiveCommand<TotpItemViewModel> CopySelected { get; }

    public ReactivePropertySlim<string> Message { get; } = new();

    public void UpdateCode()
    {
        foreach (var item in Items)
        {
            item.UpdateCode();
        }
    }

    private void OnTotpReposMoved(object? sender, (int OldIndex, int NewIndex) e)
    {
        Items.Move(e.OldIndex, e.NewIndex);
    }

    private void OnTotpReposDeleted(object? sender, TotpModel e)
    {
        var viewModel = Items.FirstOrDefault(x => x.Model.Value.Id == e.Id);
        if (viewModel != null)
        {
            Items.Remove(viewModel);
        }
    }

    private void OnTotpReposAdded(object? sender, TotpModel e)
    {
        Items.Add(new TotpItemViewModel(e));
    }

    private void OnTotpReposUpdated(object? sender, TotpModel e)
    {
        var viewModel = Items.FirstOrDefault(x => x.Model.Value.Id == e.Id);
        if (viewModel != null)
        {
            viewModel.Model.Value = e;
        }
    }

    public async Task MoveItem(int oldIndex, int newIndex)
    {
        await _totpManager.Move(oldIndex, newIndex);
    }

    public async Task DeleteItem(TotpItemViewModel item)
    {
        await _totpManager.DeleteItem(item.Model.Value.Id);
    }
}
