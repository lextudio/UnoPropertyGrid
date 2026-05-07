using System.ComponentModel;
using System.Linq;
using NUnit.Framework;

namespace UnoPropertyGrid.Tests;

[TestFixture]
public sealed class PropertyProviderTests
{
    [Test]
    public void Provider_HonorsBrowsableAndMetadata()
    {
        var target = new SampleComponent();
        var provider = new TypeDescriptorPropertyProvider();

        var properties = provider.GetProperties(target).ToList();

        Assert.That(properties.Select(p => p.Name), Does.Contain(nameof(SampleComponent.Title)));
        Assert.That(properties.Select(p => p.Name), Does.Not.Contain(nameof(SampleComponent.Hidden)));
        var title = properties.Single(p => p.Name == nameof(SampleComponent.Title));
        Assert.That(title.DisplayName, Is.EqualTo("Caption"));
        Assert.That(title.Category, Is.EqualTo("Common"));
        Assert.That(title.Description, Is.EqualTo("Shown to users."));
    }

    [Test]
    public void ViewModel_WritesConvertedNumericValue()
    {
        var target = new SampleComponent();
        var property = new TypeDescriptorPropertyProvider()
            .GetProperties(target)
            .Single(p => p.Name == nameof(SampleComponent.Count));
        var viewModel = new PropertyGridPropertyViewModel(property);

        viewModel.NumberValue = "42";

        Assert.That(target.Count, Is.EqualTo(42));
        Assert.That(viewModel.Value, Is.EqualTo(42));
        Assert.That(viewModel.HasError, Is.False);
    }

    [Test]
    public void ViewModel_WritesEnumValue()
    {
        var target = new SampleComponent();
        var property = new TypeDescriptorPropertyProvider()
            .GetProperties(target)
            .Single(p => p.Name == nameof(SampleComponent.Mode));
        var viewModel = new PropertyGridPropertyViewModel(property);

        viewModel.EnumValue = SampleMode.Second;

        Assert.That(target.Mode, Is.EqualTo(SampleMode.Second));
        Assert.That(viewModel.EnumValues, Does.Contain(SampleMode.First));
        Assert.That(viewModel.EnumValues, Does.Contain(SampleMode.Second));
    }

    [Test]
    public void ViewModel_DoesNotWriteReadOnlyProperty()
    {
        var target = new SampleComponent();
        var property = new TypeDescriptorPropertyProvider()
            .GetProperties(target)
            .Single(p => p.Name == nameof(SampleComponent.ReadOnlyName));
        var viewModel = new PropertyGridPropertyViewModel(property);

        viewModel.StringValue = "changed";

        Assert.That(target.ReadOnlyName, Is.EqualTo("fixed"));
        Assert.That(viewModel.HasError, Is.True);
    }

    sealed class SampleComponent
    {
        [System.ComponentModel.Category("Common")]
        [DisplayName("Caption")]
        [System.ComponentModel.Description("Shown to users.")]
        public string Title { get; set; } = "hello";

        [System.ComponentModel.Category("Common")]
        public int Count { get; set; } = 3;

        public SampleMode Mode { get; set; } = SampleMode.First;

        public string ReadOnlyName => "fixed";

        [Browsable(false)]
        public string Hidden { get; set; } = "hidden";
    }

    enum SampleMode
    {
        First,
        Second
    }
}
