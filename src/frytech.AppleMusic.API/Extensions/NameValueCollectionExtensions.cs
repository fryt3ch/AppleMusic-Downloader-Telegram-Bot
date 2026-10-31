using System.Collections.Specialized;
using System.Text;

namespace frytech.AppleMusic.API.Extensions;

public static class NameValueCollectionExtensions
{
    /// <summary>
    /// Projects each element of a sequence into a key/value pair and returns a <see cref="NameValueCollection"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">A sequence of values to transform.</param>
    /// <param name="keySelector">A transform function to produce a key from each element.</param>
    /// <param name="valueSelector">A transform function to produce a value from each element.</param>
    /// <returns>A <see cref="NameValueCollection"/> that contains keys and values selected from the input sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/>, <paramref name="keySelector"/>, or <paramref name="valueSelector"/> is null.</exception>
    public static NameValueCollection ToNameValueCollection<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector, Func<TSource, string> valueSelector)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        
        if (keySelector is null)
            throw new ArgumentNullException(nameof(keySelector));
        
        if (valueSelector is null)
            throw new ArgumentNullException(nameof(valueSelector));

        var nameValueCollection = new NameValueCollection();
        
        foreach (var item in source)
            nameValueCollection.Add(keySelector(item), valueSelector(item));

        return nameValueCollection;
    }
    
    public static string ToQueryString(this NameValueCollection nvc)
    {
        var sb = new StringBuilder();

        foreach (string key in nvc.Keys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;

            var values = nvc.GetValues(key);
            if (values is null) continue;

            foreach (var value in values)
            {
                if (sb.Length > 0)
                    sb.Append('&');
                
                sb.AppendFormat("{0}={1}", Uri.EscapeDataString(key), Uri.EscapeDataString(value));
            }
        }

        return sb.ToString();
    }
}