namespace Generated.PageObjects;

// Pairs a locator with typed read/write access to its current value - navigate to
// the property on a generated Page Object ({page}.{ElementName}) to reach both.
public sealed class ElementWrapper<TData>
{
    private readonly Action<TData> _upload;
    private readonly Func<TData> _read;

    public ElementWrapper(Action<TData> upload, Func<TData> read)
    {
        _upload = upload;
        _read = read;
    }

    public void Upload(TData value) => _upload(value);

    public TData Read() => _read();
}
