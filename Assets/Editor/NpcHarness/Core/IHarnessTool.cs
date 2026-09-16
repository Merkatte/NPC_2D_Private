internal interface IHarnessTool
{
    string Id { get; }

    HarnessToolResult Validate(HarnessToolContext context, HarnessStep step);

    HarnessToolResult Execute(HarnessToolContext context, HarnessStep step);
}
