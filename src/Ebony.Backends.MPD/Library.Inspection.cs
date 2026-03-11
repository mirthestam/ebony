using Ebony.Core.Extraction;
using Ebony.Infrastructure.Inspection;
using Microsoft.Extensions.Logging;
using MpcNET.Commands.Database;
using MpcNET.Types;
using MpcNET.Types.Filters;
using FindCommand = Ebony.Backends.MPD.Connection.Commands.Find.FindCommand;

namespace Ebony.Backends.MPD;

public partial class Library
{
    // Inspection
    public override async Task InspectLibraryAsync(CancellationToken ct = default)
    {
        using var scope = await client.CreateConnectionScopeAsync(token: ct);

        LogStartingLibraryInspection(logger);
        
        // get tracks
        var listAllCommand = new ListAllCommand();
        var listAllResponse = await scope.SendCommandAsync(listAllCommand).ConfigureAwait(false);
        if (!listAllResponse.IsSuccess) return;
        
        foreach (var dir in listAllResponse.Content!)
        {
            ct.ThrowIfCancellationRequested();
            
            foreach (var file in dir.Files)
            {
                ct.ThrowIfCancellationRequested();
                
                var command = new FindCommand(new FilterFile(file.Path, FilterOperator.Equal));
                var response = await scope.SendCommandAsync(command).ConfigureAwait(false);
                if (!response.IsSuccess) continue;
                var tags = response.Content!.Select(x => new Tag(x.Key, x.Value)).ToList();
                
                // We do NOT have a full album here, only album info based upon this file.
                // So we cannot do inspections yet on FULL albums
                
                var inspectedAlbum = tagInspector.InspectAlbum(tags);

                foreach (var diagnostic in inspectedAlbum.Diagnostics)
                {
                    var message = $": File: {file.Path}: {diagnostic.Message}";
                    
                    switch (diagnostic.Level)
                    {
                        case Severity.Info:
                            logger.LogInformation(message);
                            break;
                        case Severity.Warning:
                            logger.LogWarning(message);
                            break;
                        case Severity.Problem:
                            logger.LogError(message);
                            break;
                    }
                }
            }
        }
        
        LogLibraryInspectionCompleted(logger);
    }

    [LoggerMessage(LogLevel.Information, "Starting library inspection. ")]
    static partial void LogStartingLibraryInspection(ILogger<Library> logger);

    [LoggerMessage(LogLevel.Information, "Library Inspection completed.")]
    static partial void LogLibraryInspectionCompleted(ILogger<Library> logger);
}