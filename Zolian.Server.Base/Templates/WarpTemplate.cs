using Darkages.Enums;
using Darkages.Models;

namespace Darkages.Templates;

public class WarpTemplate : Template
{
    public WarpTemplate()
    {
        Activations = new List<Warp>();
    }

    public int ActivationMapId { get; set; }
    public List<Warp> Activations { get; set; }
    public int LevelRequired { get; set; }
    public Warp To { get; set; }
    public WarpType WarpType { get; set; }
}