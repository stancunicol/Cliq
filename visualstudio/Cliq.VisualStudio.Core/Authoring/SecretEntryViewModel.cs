namespace Cliq.VisualStudio.Core.Authoring;

public sealed class SecretEntryViewModel
{
    public SecretEntryViewModel(SecretDescriptor descriptor)
    {
        Key = descriptor.Key;
        HasValue = descriptor.HasValue;
    }

    public string Key { get; }

    public bool HasValue { get; }

    public string MaskedValue => HasValue ? "••••••••" : "Not configured";

    public string Status => HasValue ? "Configured" : "Missing";
}
