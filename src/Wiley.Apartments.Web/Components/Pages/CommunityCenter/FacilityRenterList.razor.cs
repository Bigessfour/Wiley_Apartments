#nullable enable
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Grids;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Web.Components.Pages.CommunityCenter;

public partial class FacilityRenterList
{
    [Inject] private IFacilityRenterService RenterService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<FacilityRenterList> Logger { get; set; } = default!;

    private SfGrid<FacilityRenter>? GridRef { get; set; }
    private List<FacilityRenter> Rows { get; set; } = [];
    private string? ErrorMessage { get; set; }

    private static readonly List<IdTypeOption> IdTypeOptions =
    [
        new(string.Empty, "(none)"),
        new("Driver's license", "Driver's license"),
        new("State ID", "State ID"),
        new("Passport", "Passport"),
        new("Military ID", "Military ID"),
        new("Tribal ID", "Tribal ID"),
        new("Other", "Other")
    ];

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            Rows = [.. await RenterService.SearchAsync()];
            Logger.LogInformation("FacilityRenterList loaded {Count} renters.", Rows.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "FacilityRenterList load failed.");
            ErrorMessage = ex.Message;
        }
    }

    private async Task ReloadGridAsync()
    {
        await LoadAsync();
        if (GridRef is not null)
        {
            await GridRef.Refresh();
        }
    }

    private void OnActionBeginAsync(ActionEventArgs<FacilityRenter> args) => ErrorMessage = null;

    private async Task OnActionCompleteAsync(ActionEventArgs<FacilityRenter> args)
    {
        try
        {
            switch (args.RequestType)
            {
                case Syncfusion.Blazor.Grids.Action.Save when args.Action == "Add" && args.Data is not null:
                    Logger.LogInformation(
                        "FacilityRenterList creating renter {LastName}, {FirstName}.",
                        args.Data.LastName, args.Data.FirstName);
                    await RenterService.CreateAsync(args.Data);
                    Logger.LogInformation("FacilityRenterList created renter {RenterId}.", args.Data.Id);
                    break;
                case Syncfusion.Blazor.Grids.Action.Save when args.Action == "Edit" && args.Data is not null:
                    Logger.LogInformation("FacilityRenterList updating renter {RenterId}.", args.Data.Id);
                    await RenterService.UpdateAsync(args.Data);
                    Logger.LogInformation("FacilityRenterList updated renter {RenterId}.", args.Data.Id);
                    break;
                case Syncfusion.Blazor.Grids.Action.Delete when args.Data is not null:
                    Logger.LogInformation("FacilityRenterList soft-deleting renter {RenterId}.", args.Data.Id);
                    await RenterService.SoftDeleteAsync(args.Data.Id);
                    Logger.LogInformation("FacilityRenterList soft-deleted renter {RenterId}.", args.Data.Id);
                    break;
                default:
                    return;
            }

            await ReloadGridAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "FacilityRenterList mutation failed action={Action} request={RequestType}.",
                args.Action, args.RequestType);
            ErrorMessage = ex.Message;
            await ReloadGridAsync();
        }
    }

    private void OnDoubleClick(RecordDoubleClickEventArgs<FacilityRenter> args)
    {
        if (args.RowData is not null)
        {
            Logger.LogInformation("FacilityRenterList open detail {RenterId}.", args.RowData.Id);
            Navigation.NavigateTo($"/community-center/renters/{args.RowData.Id}");
        }
    }

    private sealed record IdTypeOption(string Value, string Text);
}
