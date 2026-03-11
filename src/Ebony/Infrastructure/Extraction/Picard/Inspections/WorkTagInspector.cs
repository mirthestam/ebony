using Ebony.Core.Extraction;
using Ebony.Infrastructure.Inspection;

namespace Ebony.Infrastructure.Extraction.Picard.Inspections;

public class WorkTagInspector : TagInspector
{
    public override IEnumerable<Diagnostic> Inspect(IReadOnlyList<Tag> tags)
    {
        var workTags  = tags.Where(t => t.Name.Equals(PicardTags.WorkTags.Work, StringComparison.OrdinalIgnoreCase));
        var movementTags  = tags.Where(t => t.Name.Equals(PicardTags.WorkTags.Movement, StringComparison.OrdinalIgnoreCase));
        var showMovementTags  = tags.Where(t => t.Name.Equals(PicardTags.WorkTags.ShowMovement, StringComparison.OrdinalIgnoreCase));

        // Movement number is optional, and therefore not inspected.
        
        var work = workTags.FirstOrDefault()?.Value;
        var movement = movementTags.FirstOrDefault()?.Value;
        var showMovement = showMovementTags.FirstOrDefault()?.Value;
        
        if (showMovement == "1" && (string.IsNullOrWhiteSpace(work) || string.IsNullOrWhiteSpace(movement)))
        {
            yield return new Diagnostic(
                Severity.Warning,
                "Movement information missing",
                "The track is configured to show movement information, However the movement information is missing. This track can appear incorrect or confusing in the UI."
            );                
        }
    }
}