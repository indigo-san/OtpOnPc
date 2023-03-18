using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using Microsoft.AspNetCore.DataProtection;

using OtpOnPc.Models;
using OtpOnPc.Services;

using Reactive.Bindings;
using Reactive.Bindings.TinyLinq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OtpOnPc.ViewModels;

public class MainPageViewModel
{
    private readonly IDataProtector _dataProtector;
    private readonly TotpModelManager _totpManager;
    private readonly Dictionary<int, StepManager> _steps = new();
    private CancellationTokenSource? _cts;
    internal readonly Task _initializeTask;

    public MainPageViewModel()
    {
        _dataProtector = AvaloniaLocator.Current.GetRequiredService<IDataProtectionProvider>().CreateProtector("SecretKey.v1");
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
            OnTotpReposAdded(null, item);
        }
    }

    public ReactiveCollection<TotpItemViewModel> Items { get; } = new();

    public ReactiveCommand<TotpItemViewModel> CopySelected { get; }

    public ReactivePropertySlim<string> Message { get; } = new();

    public void UpdateCode()
    {
        foreach (var item in _steps.Values)
        {
            item.UpdateCode();
        }
    }

    public void UpdateSweepAngle(long unixTime)
    {
        foreach (var item in _steps.Values)
        {
            item.UpdateSweepAngle(unixTime);
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

            RemoveStep(e.Step, viewModel);
            viewModel.StepChanged -= OnItemStepChanged;
            viewModel.Dispose();
        }
    }

    private void OnTotpReposAdded(object? sender, TotpModel e)
    {
        var viewModel = new TotpItemViewModel(e, _dataProtector);
        Items.Add(viewModel);

        AddStep(e.Step, viewModel);
        viewModel.StepChanged += OnItemStepChanged;
    }

    private void OnItemStepChanged(object? sender, (int OldStep, int NewStep) e)
    {
        if (sender is TotpItemViewModel viewModel)
        {
            RemoveStep(e.OldStep, viewModel);
            AddStep(e.NewStep, viewModel);
        }
    }

    private void AddStep(int step, TotpItemViewModel viewModel)
    {
        FindOrAddStepManager(step).AddItem(viewModel);
    }

    private void RemoveStep(int step, TotpItemViewModel viewModel)
    {
        FindOrAddStepManager(step).RemoveItem(viewModel);
    }

    private StepManager FindOrAddStepManager(int step)
    {
        ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(_steps, step, out _);
        value ??= new StepManager(step);

        return value;
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
        try
        {
            await _totpManager.Move(oldIndex, newIndex);
        }
        catch (Exception ex)
        {
            if (await ExceptionDialog.Handle(ex) == ExceptionDialogResult.Shutdown)
            {
                var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                lifetime?.Shutdown((int)ExitCodes.FailedToMoveAccount);
            }
        }
    }

    public async Task DeleteItem(TotpItemViewModel item)
    {
        try
        {
            await _totpManager.DeleteItem(item.Model.Value.Id);
        }
        catch (Exception ex)
        {
            if (await ExceptionDialog.Handle(ex) == ExceptionDialogResult.Shutdown)
            {
                var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                lifetime?.Shutdown((int)ExitCodes.FailedToDeleteAccount);
            }
        }
    }
}
