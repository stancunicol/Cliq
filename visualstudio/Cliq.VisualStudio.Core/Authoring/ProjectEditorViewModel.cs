using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public sealed class ProjectEditorViewModel : EditableViewModelBase
{
    private string _name;
    private string _baseUrl;
    private BrowserKind _defaultBrowser;
    private string _pageObjectsDirectory;
    private string _testsDirectory;
    private string _pageObjectsNamespace;
    private string _testsNamespace;

    public ProjectEditorViewModel(ProjectManifest project)
    {
        Id = project.Id;
        SchemaVersion = project.SchemaVersion;
        _name = project.Name;
        _baseUrl = project.BaseUrl;
        _defaultBrowser = project.DefaultBrowser;
        _pageObjectsDirectory = project.Output.PageObjectsDirectory;
        _testsDirectory = project.Output.TestsDirectory;
        _pageObjectsNamespace = project.Output.PageObjectsNamespace;
        _testsNamespace = project.Output.TestsNamespace;
    }

    public int SchemaVersion { get; }

    public string Id { get; }

    public string Name
    {
        get => _name;
        set => SetEditable(ref _name, value ?? string.Empty);
    }

    public string BaseUrl
    {
        get => _baseUrl;
        set => SetEditable(ref _baseUrl, value ?? string.Empty);
    }

    public BrowserKind DefaultBrowser
    {
        get => _defaultBrowser;
        set => SetEditable(ref _defaultBrowser, value);
    }

    public string PageObjectsDirectory
    {
        get => _pageObjectsDirectory;
        set => SetEditable(ref _pageObjectsDirectory, value ?? string.Empty);
    }

    public string TestsDirectory
    {
        get => _testsDirectory;
        set => SetEditable(ref _testsDirectory, value ?? string.Empty);
    }

    public string PageObjectsNamespace
    {
        get => _pageObjectsNamespace;
        set => SetEditable(ref _pageObjectsNamespace, value ?? string.Empty);
    }

    public string TestsNamespace
    {
        get => _testsNamespace;
        set => SetEditable(ref _testsNamespace, value ?? string.Empty);
    }

    public override string ValidationSummary => string.Join(Environment.NewLine, Validate());

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Project name is required.");
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("Base URL must be an absolute HTTP or HTTPS URL.");
        }

        ValidateRelativeDirectory(PageObjectsDirectory, "Page objects directory", errors);
        ValidateRelativeDirectory(TestsDirectory, "Tests directory", errors);
        ValidateNamespace(PageObjectsNamespace, "Page objects namespace", errors);
        ValidateNamespace(TestsNamespace, "Tests namespace", errors);
        return errors;
    }

    public ProjectManifest ToModel()
    {
        return new ProjectManifest(
            SchemaVersion,
            Id,
            Name.Trim(),
            BaseUrl.Trim(),
            DefaultBrowser,
            new OutputSettings(
                NormalizeDirectory(PageObjectsDirectory),
                NormalizeDirectory(TestsDirectory),
                PageObjectsNamespace.Trim(),
                TestsNamespace.Trim()),
            DateTimeOffset.UtcNow);
    }

    public void AcceptChanges(ProjectManifest project)
    {
        _name = project.Name;
        _baseUrl = project.BaseUrl;
        _defaultBrowser = project.DefaultBrowser;
        _pageObjectsDirectory = project.Output.PageObjectsDirectory;
        _testsDirectory = project.Output.TestsDirectory;
        _pageObjectsNamespace = project.Output.PageObjectsNamespace;
        _testsNamespace = project.Output.TestsNamespace;
        OnPropertyChanged(string.Empty);
        IsDirty = false;
    }

    private static void ValidateRelativeDirectory(
        string value,
        string label,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{label} is required.");
            return;
        }

        var normalized = value.Replace('\\', '/');
        if (Path.IsPathRooted(value) ||
            normalized.Split('/').Any(segment => segment == ".."))
        {
            errors.Add($"{label} must be a relative workspace path without '..'.");
        }
    }

    private static void ValidateNamespace(
        string value,
        string label,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Regex.IsMatch(value, @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$"))
        {
            errors.Add($"{label} must be a valid dotted .NET namespace.");
        }
    }

    private static string NormalizeDirectory(string value)
    {
        return value.Trim().Replace('\\', '/').TrimEnd('/');
    }
}
