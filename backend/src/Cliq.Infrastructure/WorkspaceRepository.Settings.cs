using System.Text.RegularExpressions;
using Cliq.Application;
using Cliq.Domain;

namespace Cliq.Infrastructure;

public sealed partial class WorkspaceRepository
{
    public async Task<CliqSettings> GetSettingsAsync(CancellationToken cancellationToken)
    {
        EnsureOpen();
        var settings = await ReadOptionalAsync<CliqSettings>(SettingsPath, cancellationToken) ?? DefaultSettings();
        ValidateVersion(settings.SchemaVersion, CliqSettings.CurrentSchemaVersion, "settings");

        // A settings.json written before a field was introduced deserializes that field as null
        // (not an empty collection) via constructor binding - backfill so older files upgrade
        // cleanly instead of crashing whatever first reads the missing value.
        if (settings.ElementIdentification.IgnoreIdPatterns is null)
        {
            settings = settings with
            {
                ElementIdentification = settings.ElementIdentification with { IgnoreIdPatterns = [] },
            };
        }

        return settings;
    }

    public async Task<CliqSettings> SaveSettingsAsync(CliqSettings settings, CancellationToken cancellationToken)
    {
        EnsureOpen();
        ValidateVersion(settings.SchemaVersion, CliqSettings.CurrentSchemaVersion, "settings");
        ValidateSettings(settings);
        await WriteAtomicAsync(SettingsPath, settings, cancellationToken);
        return settings;
    }

    // Cliq.Domain.IdentificationStrategies.All is the single source of truth this validates
    // against - see its own remarks for why (the pre-2026-08-03 and "css" drifts).
    private static readonly HashSet<string> KnownIdentificationStrategies =
        new(IdentificationStrategies.All, StringComparer.Ordinal);

    private static void ValidateSettings(CliqSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ElementIdentification.TestIdAttribute))
        {
            throw new WorkspaceValidationException("elementIdentification.testIdAttribute must not be empty.");
        }

        if (settings.ElementIdentification.StrategyOrder.Count == 0)
        {
            throw new WorkspaceValidationException("elementIdentification.strategyOrder must not be empty.");
        }

        foreach (var strategy in settings.ElementIdentification.StrategyOrder)
        {
            if (!KnownIdentificationStrategies.Contains(strategy))
            {
                throw new WorkspaceValidationException(
                    $"elementIdentification.strategyOrder contains unknown strategy '{strategy}'. " +
                    $"Expected one of: {string.Join(", ", IdentificationStrategies.All)}.");
            }
        }

        foreach (var pattern in settings.ElementIdentification.IgnoreIdPatterns)
        {
            try
            {
                _ = new Regex(pattern);
            }
            catch (ArgumentException exception)
            {
                throw new WorkspaceValidationException(
                    $"elementIdentification.ignoreIdPatterns contains an invalid regular expression '{pattern}': {exception.Message}");
            }
        }

        if (settings.TestExecution.DefaultStepTimeoutMs <= 0)
        {
            throw new WorkspaceValidationException("testExecution.defaultStepTimeoutMs must be positive.");
        }

        if (settings.TestExecution.PageReadyTimeoutMs <= 0)
        {
            throw new WorkspaceValidationException("testExecution.pageReadyTimeoutMs must be positive.");
        }

        if (settings.Scanning.LoadTimeoutMs <= 0)
        {
            throw new WorkspaceValidationException("scanning.loadTimeoutMs must be positive.");
        }

        if (settings.Scanning.NetworkIdleGraceMs <= 0)
        {
            throw new WorkspaceValidationException("scanning.networkIdleGraceMs must be positive.");
        }
    }

    // Known framework-instance-generated id shapes: values that look like this are volatile
    // across page loads even without any DOM/data change, so they're untrustworthy as a locator
    // and as a naming source - see ElementIdentificationSettings.IgnoreIdPatterns.
    private static readonly string[] DefaultIgnoreIdPatterns =
    [
        @"^ember\d+$",
        @"^react-select-\d+-",
        @"^mat-[a-z]+(-[a-z]+)*-\d+$",
        @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
    ];

    private static CliqSettings DefaultSettings() => new(
        CliqSettings.CurrentSchemaVersion,
        new ElementIdentificationSettings("data-testid", ["testId", "xpath"], DefaultIgnoreIdPatterns),
        new TestExecutionSettings(DefaultStepTimeoutMs: 10_000, PageReadyTimeoutMs: 15_000, CaptureScreenshotOnFailure: false),
        new ScanningSettings(LoadTimeoutMs: 30_000, NetworkIdleGraceMs: 3_000, CaptureScreenshot: false));
}
