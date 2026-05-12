using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NUnit.Framework;
using Windows.UI.Text;

namespace UnoPropertyGrid.Tests;

[TestFixture]
#if WINDOWS_APP_SDK
[Apartment(System.Threading.ApartmentState.STA)]
#endif
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

        Assert.Throws<InvalidOperationException>(() => property.SetValue("changed"));

        Assert.That(target.ReadOnlyName, Is.EqualTo("fixed"));
    }

    [Test]
    public void EventProvider_DiscoversPublicInstanceEvents()
    {
        var target = new SampleComponent();
        var events = new ReflectionEventProvider().GetEvents(target).ToList();

        var changed = events.Single(e => e.Name == nameof(SampleComponent.Changed));
        Assert.That(changed.HandlerType, Is.EqualTo(typeof(EventHandler)));
        Assert.That(changed.Component, Is.SameAs(target));
    }

    [Test]
    public void EventViewModel_ValidatesHandlerName()
    {
        var target = new SampleComponent();
        var descriptor = new ReflectionEventProvider()
            .GetEvents(target)
            .Single(e => e.Name == nameof(SampleComponent.Changed));
        var viewModel = new PropertyGridEventViewModel(descriptor);

        viewModel.HandlerName = "OnChanged";
        Assert.That(viewModel.HasError, Is.False);

        viewModel.HandlerName = "not valid";
        Assert.That(viewModel.HasError, Is.True);
    }

    [Test]
    public void ViewModel_WritesFontFamilyValue()
    {
#if WINDOWS_APP_SDK
        if (Environment.GetEnvironmentVariable(StandaloneWinUITestEnvironmentVariable) != "1")
        {
            RunFontFamilyTestInStandaloneProcess();
            return;
        }
#endif

        RunWithWinUIApplication(Test);

        static void Test()
        {
        var target = new SampleComponent();
        target.FontFamily = new FontFamily("Segoe UI");
        Assert.That(target.FontFamily, Is.Not.Null);
        Assert.That(target.FontFamily!.Source, Is.EqualTo("Segoe UI"));
        target.FontFamily = null;

        var property = new TypeDescriptorPropertyProvider()
            .GetProperties(target)
            .Single(p => p.Name == nameof(SampleComponent.FontFamily));
        var viewModel = new PropertyGridPropertyViewModel(property);

        Assert.That(viewModel.EditorKind, Is.EqualTo(PropertyEditorKind.FontFamily));

        viewModel.FontFamilyValue = "Consolas";

        Assert.That(viewModel.HasError, Is.False, viewModel.Error);
        Assert.That(target.FontFamily, Is.Not.Null);
        Assert.That(target.FontFamily!.Source, Is.EqualTo("Consolas"));
        }
    }

    [Test]
    public void ViewModel_WritesFontWeightValue()
    {
        var target = new SampleComponent();
        var property = new TypeDescriptorPropertyProvider()
            .GetProperties(target)
            .Single(p => p.Name == nameof(SampleComponent.FontWeight));
        var viewModel = new PropertyGridPropertyViewModel(property);

        Assert.That(viewModel.EditorKind, Is.EqualTo(PropertyEditorKind.FontWeight));

        viewModel.FontWeightValue = "Bold";

        Assert.That(target.FontWeight.Weight, Is.EqualTo(700));
    }

    [Test]
    public void ViewModel_WritesBrushValue()
    {
        var target = new SampleComponent();
        var property = new TypeDescriptorPropertyProvider()
            .GetProperties(target)
            .Single(p => p.Name == nameof(SampleComponent.Foreground));
        var viewModel = new PropertyGridPropertyViewModel(property);

        Assert.That(viewModel.EditorKind, Is.EqualTo(PropertyEditorKind.Brush));

        Assert.That(viewModel.BrushValue, Is.EqualTo("No brush"));
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

        public FontFamily? FontFamily { get; set; }

        public FontWeight FontWeight { get; set; } = new() { Weight = 400 };

        public Brush? Foreground { get; set; }

        [Browsable(false)]
        public string Hidden { get; set; } = "hidden";

        public event EventHandler? Changed;

        public void RaiseChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    enum SampleMode
    {
        First,
        Second
    }

#if WINDOWS_APP_SDK
    sealed class TestApplication : Application
    {
    }
#endif

    static void RunWithWinUIApplication(Action action)
    {
#if WINDOWS_APP_SDK
        Exception? exception = null;
        Application.Start(_ =>
        {
            var app = new TestApplication();
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Application.Current.Exit();
            }
        });

        if (exception != null)
            throw exception;
#else
        action();
#endif
    }

#if WINDOWS_APP_SDK
    const string StandaloneWinUITestEnvironmentVariable = "UNOPROPERTYGRID_STANDALONE_WINUI_TEST";

    static void RunFontFamilyTestInStandaloneProcess()
    {
        var assemblyPath = typeof(PropertyProviderTests).Assembly.Location;
        var executablePath = Path.ChangeExtension(assemblyPath, ".exe");
        Assert.That(File.Exists(executablePath), Is.True, $"Test executable was not found at '{executablePath}'.");

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(executablePath)!
        };
        process.StartInfo.ArgumentList.Add("--where");
        process.StartInfo.ArgumentList.Add($"test == '{typeof(PropertyProviderTests).FullName}.{nameof(ViewModel_WritesFontFamilyValue)}'");
        process.StartInfo.Environment[StandaloneWinUITestEnvironmentVariable] = "1";

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        Assert.That(process.WaitForExit(60000), Is.True, "Standalone WinUI test process timed out.");
        Assert.That(process.ExitCode, Is.EqualTo(0), output + error);
    }
#endif
}
