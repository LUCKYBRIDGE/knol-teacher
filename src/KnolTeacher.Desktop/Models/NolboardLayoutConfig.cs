using System.Collections.Generic;

namespace KnolTeacher.Desktop.Models;

public class NolboardWidgetState
{
    public string Tag { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class NolboardLayoutConfig
{
    // v3: default widget sizes were enlarged for classroom displays and legacy v2
    // layouts are reflowed once so old tiny dimensions do not keep coming back.
    public const int CurrentSchemaVersion = 3;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public bool HasCustomLayout { get; set; } = false;
    public double CanvasWidth { get; set; }
    public double CanvasHeight { get; set; }
    public List<NolboardWidgetState> Widgets { get; set; } = new();
}
