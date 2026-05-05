using System.Collections.ObjectModel;
using System.Linq;
using DrivingSchoolApp.Localization;
using DrivingSchoolApp.Models;
using DrivingSchoolApp.Services;

namespace DrivingSchoolApp.Pages;

public partial class SavedRoutesPage : ContentPage
{
    private readonly ObservableCollection<SavedRouteListItem> _routes = new();
    private readonly ToolbarItem _deleteToolbarItem;
    private bool _isEditing;

    public SavedRoutesPage()
    {
        InitializeComponent();

        RoutesCollectionView.ItemsSource = _routes;

        _deleteToolbarItem = new ToolbarItem
        {
            Text = AppText.SavedRoutesDelete,
            Order = ToolbarItemOrder.Primary,
            Priority = 0
        };

        _deleteToolbarItem.Clicked += DeleteToolbarItem_Clicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        RouteSnapBackgroundProcessor.StatesChanged += RouteSnapBackgroundProcessor_StatesChanged;

        await RouteSnapBackgroundProcessor.EnsureWorkQueuedForAllRoutesAsync();
        await LoadRoutesAsync();
    }

    protected override void OnDisappearing()
    {
        RouteSnapBackgroundProcessor.StatesChanged -= RouteSnapBackgroundProcessor_StatesChanged;
        base.OnDisappearing();
    }

    private void RouteSnapBackgroundProcessor_StatesChanged()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadRoutesAsync();
        });
    }

    private async Task LoadRoutesAsync()
    {
        var sessions = await RouteStorage.ListAsync();

        _routes.Clear();

        foreach (var session in sessions)
        {
            var item = SavedRouteListItem.FromSession(session);
            item.IsEditing = _isEditing;

            var state = await RouteSnapBackgroundProcessor.GetStateAsync(session.Id);
            item.ApplyStatus(state);

            _routes.Add(item);
        }
    }

    private void EditToolbarItem_Clicked(object? sender, EventArgs e)
    {
        _isEditing = !_isEditing;
        EditToolbarItem.Text = _isEditing ? AppText.SavedRoutesDone : AppText.SavedRoutesEdit;

        if (_isEditing)
        {
            if (!ToolbarItems.Contains(_deleteToolbarItem))
                ToolbarItems.Add(_deleteToolbarItem);
        }
        else
        {
            ToolbarItems.Remove(_deleteToolbarItem);

            foreach (var item in _routes)
            {
                item.IsSelected = false;
                item.IsEditing = false;
            }

            return;
        }

        foreach (var item in _routes)
            item.IsEditing = true;
    }

    private async void DeleteToolbarItem_Clicked(object? sender, EventArgs e)
    {
        var selected = _routes.Where(x => x.IsSelected).ToList();

        if (selected.Count == 0)
        {
            await DisplayAlertAsync(AppText.SavedRoutesDeleteTitle, AppText.SavedRoutesDeleteSelectMessage, AppText.CommonOk);
            return;
        }

        var confirmed = await DisplayAlertAsync(
            AppText.SavedRoutesDeleteConfirmTitle,
            AppText.FormatSavedRoutesDeleteCount(selected.Count),
            AppText.SavedRoutesDelete,
            AppText.CommonCancel);

        if (!confirmed)
            return;

        foreach (var item in selected)
        {
            await RouteStorage.DeleteAsync(item.SessionId);
            _routes.Remove(item);
        }

        if (_routes.Count == 0)
        {
            _isEditing = false;
            EditToolbarItem.Text = AppText.SavedRoutesEdit;
            ToolbarItems.Remove(_deleteToolbarItem);
        }
    }

    private async void RouteItem_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not SavedRouteListItem item)
            return;

        if (_isEditing)
        {
            item.IsSelected = !item.IsSelected;
            return;
        }

        if (!item.IsFinalized)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(RouteConfirmationPage)}?sessionId={Uri.EscapeDataString(item.SessionId)}");
            return;
        }

        await Shell.Current.GoToAsync(
            $"{nameof(RouteDetailPage)}?sessionId={Uri.EscapeDataString(item.SessionId)}");
    }
}
