using System.Collections;

namespace QuitQQ.App.Collections;
/// <summary>
/// A HashSet with a timeout for each element.
/// Warning: the timeout is an approximate value when you are iterating through this set.
/// </summary>
/// <typeparam name="T">Item type</typeparam>
internal class TimeoutHashSet<T> where T : notnull
{
    private readonly Dictionary<T, DateTime> _set = new();

    public TimeoutHashSet(TimeSpan timeout)
    {
        Timeout = timeout;
    }

    public TimeSpan Timeout { get; }

    public int Count
    {
        get
        {
            Clean();
            return _set.Count;
        }
    }

    public void Clear() => _set.Clear();

    public bool Contains(T k)
    {
        if (_set.ContainsKey(k))
        {
            if (_set[k] + Timeout >= DateTime.Now) return true;
            _set.Remove(k);
            return false;
        }

        return false;
    }

    public void Add(T k)
    {
        _set[k] = DateTime.Now;
    }

    private void Clean()
    {
        foreach (var (k, v) in _set)
        {
            if (DateTime.Now > v + this.Timeout)
            {
                _set.Remove(k);
            }
        }
    }
}
