using System.ComponentModel;
using Cliq.VisualStudio.Core.Authoring;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class EditableViewModelBaseTests
{
    [Test]
    public void SetProperty_RaisesPropertyChangedOnlyWhenValueChanges()
    {
        var changes = new List<string?>();
        var vm = new BindableStub();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        var first = vm.SetValue("a");
        var unchanged = vm.SetValue("a");
        var second = vm.SetValue("b");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(unchanged, Is.False);
            Assert.That(second, Is.True);
            Assert.That(changes, Is.EqualTo(new[] { "SetValue", "SetValue" }));
        });
    }

    [Test]
    public void SetEditable_MarksDirtyAndRaisesValidationSummary()
    {
        var changes = new List<string?>();
        var vm = new EditableStub();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        vm.Name = "Changed";

        Assert.Multiple(() =>
        {
            Assert.That(vm.IsDirty, Is.True);
            Assert.That(changes, Does.Contain("Name"));
            Assert.That(changes, Does.Contain("ValidationSummary"));
        });
    }

    [Test]
    public void SetEditable_UnchangedValue_DoesNotMarkDirty()
    {
        var vm = new EditableStub();

        vm.Name = "Same";

        Assert.Multiple(() =>
        {
            Assert.That(vm.IsDirty, Is.False);
            Assert.That(vm.Name, Is.EqualTo("Same"));
        });
    }

    [Test]
    public void IsDirty_RaisesDirtyChangedEvent()
    {
        var raised = 0;
        var vm = new EditableStub();
        vm.DirtyChanged += (_, _) => raised++;

        vm.Name = "Changed";

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void OnDirtyChanged_RunsWhenDirtyFlips()
    {
        var vm = new DirtyObservingStub();

        vm.Name = "Changed";

        Assert.That(vm.DirtyHookCount, Is.EqualTo(1));
    }

    private sealed class BindableStub : BindableBase
    {
        private string _value = string.Empty;

        public bool SetValue(string value) => SetProperty(ref _value, value);

        public string Value => _value;
    }

    private sealed class EditableStub : EditableViewModelBase
    {
        private string _name = "Same";

        public string Name
        {
            get => _name;
            set => SetEditable(ref _name, value);
        }

        public override string ValidationSummary => Validate().Length == 0 ? string.Empty : string.Join("\n", Validate());

        private string[] Validate() => _name.Length == 0 ? new[] { "Name is required." } : Array.Empty<string>();
    }

    private sealed class DirtyObservingStub : EditableViewModelBase
    {
        private string _name = "Same";

        public int DirtyHookCount { get; private set; }

        public string Name
        {
            get => _name;
            set => SetEditable(ref _name, value);
        }

        public override string ValidationSummary => string.Empty;

        protected override void OnDirtyChanged() => DirtyHookCount++;
    }
}
