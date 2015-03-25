// ======================================================== IEquivalent.cs
namespace Kerosene.Tools
{
	using System;
	using System.Collections;
	using System.Linq;

	// ==================================================== 
	/// <summary>
	/// Represents the ability of an object to verify if it can be considered as equivalent to
	/// a target instance of the given type, based upon any arbitrary criteria it implements.
	/// </summary>
	/// <typeparam name="T">The type of the target objects this one will be tested against.</typeparam>
	public interface IEquivalent<T>
	{
		/// <summary>
		/// Returns true if this object can be considered as equivalent to the target one given.
		/// </summary>
		/// <param name="target">The target object this one will be tested for equivalence.</param>
		/// <returns>True if this object can be considered as equivalent to the target one given.</returns>
		bool EquivalentTo(T target);
	}

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="Kerosene.Tools.IEquivalent"/> objects.
	/// </summary>
	public static class EquivalentEx
	{
		/// <summary>
		/// Returns whether this source object can be considered as equivalent to the target one
		/// given, using a set of common criteria.
		/// </summary>
		/// <param name="source">This source object.</param>
		/// <param name="target">The target object the source one will be tested for equivalence.</param>
		/// <returns>True if this source object can be considered as equivalent to the target one.</returns>
		public static bool IsEquivalentTo(this object source, object target)
		{
			if (object.ReferenceEquals(source, target)) return true;
			if (object.ReferenceEquals(source, null) && object.ReferenceEquals(target, null)) return true;
			if (object.ReferenceEquals(source, null) && !object.ReferenceEquals(target, null)) return false;
			if (object.ReferenceEquals(target, null) && !object.ReferenceEquals(source, null)) return false;

			if (source.Equals(target)) return true;
			if (WithIEquivalent(source, target)) return true;
			if (WithIDictionary(source, target)) return true;
			if (WithIEnumerable(source, target)) return true;

			return false;
		}

		private static bool WithIEquivalent(object source, object target)
		{
			Type sourceType = source.GetType();
			Type targetType = target.GetType();

			var ifaces = sourceType
				.GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEquivalent<>));

			foreach (var iface in ifaces)
			{
				var ifaceType = iface.GetGenericArguments()[0];
				if (!ifaceType.IsAssignableFrom(targetType)) continue;

				var method = sourceType.GetMethod("EquivalentTo", new[] { ifaceType });
				var r = (bool)method.Invoke(source, new[] { target });
				return r;
			}

			return false;
		}

		private static bool WithIDictionary(object source, object target)
		{
			var sourceDic = source as IDictionary; if (sourceDic == null) return false;
			var targetDic = target as IDictionary; if (targetDic == null) return false;

			if (sourceDic.Count != targetDic.Count) return false;

			foreach (DictionaryEntry kvp in sourceDic)
			{
				if (!targetDic.Contains(kvp.Key)) return false;

				var value = targetDic[kvp.Key];
				if (!kvp.Value.IsEquivalentTo(value)) return false;
			}

			return true;
		}

		private static bool WithIEnumerable(object source, object target)
		{
			var sourceEnum = source as IEnumerable; if (sourceEnum == null) return false;
			var targetEnum = target as IEnumerable; if (targetEnum == null) return false;

			var sourceIter = sourceEnum.GetEnumerator();
			var targetIter = targetEnum.GetEnumerator();

			try
			{
				while (sourceIter.MoveNext())
				{
					if (!targetIter.MoveNext()) return false;
					if (!sourceIter.Current.IsEquivalentTo(targetIter.Current)) return false;
				}
				if (targetIter.MoveNext()) return false;
			}
			finally
			{
				if (sourceIter is IDisposable) ((IDisposable)sourceIter).Dispose();
				if (targetIter is IDisposable) ((IDisposable)targetIter).Dispose();
			}

			return true;
		}
	}
}
// ======================================================== 
