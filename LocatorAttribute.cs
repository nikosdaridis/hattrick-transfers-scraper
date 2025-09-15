namespace HattrickTransfersScraper
{
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class LocatorAttribute(string locator) : Attribute
    {
        internal enum LocatorType
        {
            Select,
            Input
        }

        internal string Locator { get; } = locator;
    }
}
