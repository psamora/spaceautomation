using System.Collections;
using System.Collections.Generic;

public static class DictionaryExtensions {
  public static T2 GetOrNull<T1, T2>(this Dictionary<T1, T2> dictionary, T1 key) where T2 : class {
    T2 output = null;
    dictionary.TryGetValue(key, out output);
    return output;
  }

  public static T2 GetOrDefault<T1, T2>(this Dictionary<T1, T2> dictionary, T1 key, T2 defaultResponse) {
    if (dictionary.TryGetValue(key, out T2 output)) {
      return output;
    }
    return defaultResponse;
  }

  public static T2 GetOrDefault<T1, T2>(this Dictionary<T1, T2> dictionary, T1 key) {
    T2 output = default(T2);
    dictionary.TryGetValue(key, out output);
    return output;
  }
}
