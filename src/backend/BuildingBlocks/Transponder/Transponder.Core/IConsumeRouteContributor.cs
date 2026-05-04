namespace Dialysis.BuildingBlocks.Transponder;

internal interface IConsumeRouteContributor
{
    void Contribute(Dictionary<string, TransponderConsumeRouteEntry> routes);
}
