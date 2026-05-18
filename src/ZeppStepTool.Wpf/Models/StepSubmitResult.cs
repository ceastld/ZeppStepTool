namespace ZeppStepTool.Wpf.Models
{
    public sealed class StepSubmitResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Steps { get; set; }
    }
}
