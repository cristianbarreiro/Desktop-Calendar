namespace CalendarWidget.IntegrationTests.Helpers;

/// <summary>
/// Definition for tests that require serialized execution because they interact with WPF
/// global state, <see cref="System.Windows.Application"/>, or UI Dispatcher lifecycles.
/// </summary>
[CollectionDefinition(Name)]
public sealed class WpfTestCollection
{
    /// <summary>
    /// The unique name of the WPF test collection.
    /// </summary>
    public const string Name = "WpfTestCollection";
}
