namespace Trainer.Web.Core;

/// <summary>A lab with <see cref="LabTool.ModeratorView"/> tells the page where the simulated moderator screen lives.</summary>
public interface IHasModeratorView
{
    /// <summary>URL of the moderator page, including its secret key.</summary>
    string ModeratorViewUrl { get; }
}
