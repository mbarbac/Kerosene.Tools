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

		/// <summary>
		/// Testing using IEquivalent capabilities...
		/// </summary>
		static bool WithIEquivalent(object source, object target)
		{
#if OPTION1
			Type sourceType = source.GetType();
			Type targetType = target.GetType();

			var ifaces = sourceType.GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEquivalent<>))
				.ToArray();

			// From the last one assuming is the most specific one...
			for (int i = ifaces.Length - 1; i >= 0; i--)
			{
				var iface = ifaces[i];
				var ifaceType = iface.GetGenericArguments()[0];
				if (!ifaceType.IsAssignableFrom(targetType)) break;

				var method = sourceType.GetMethod("EquivalentTo", new[] { ifaceType });
				var r = (bool)method.Invoke(source, new[] { target });
				if (r) return true;
			}

			return false;
#else
			Type sourceType = source.GetType();
			Type targetType = target.GetType();

			var ifaces = sourceType.GetInterfaces(); for (int i = ifaces.Length - 1; i >= 0; i--)
			{
				var iface = ifaces[i];
				if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IEquivalent<>)) continue;

				var ifaceType = iface.GetGenericArguments()[0];
				if (!ifaceType.IsAssignableFrom(targetType)) continue;

				var method = sourceType.GetMethod("EquivalentTo", new[] { ifaceType });
				var r = (bool)method.Invoke(source, new[] { target });
				return r;
			}

			return false;
#endif
		}

		/// <summary>
		/// Testing using IDictionary capabilities...
		/// </summary>
		static bool WithIDictionary(object source, object target)
		{
#if OPTION1
			Type sourceType = source.GetType();
			Type targetType = target.GetType();

			if (!typeof(IDictionary).IsAssignableFrom(sourceType)) return false;
			if (!typeof(IDictionary).IsAssignableFrom(targetType)) return false;

			var sourceDic = source as IDictionary;
			var targetDic = target as IDictionary;

			if (sourceDic.Count != targetDic.Count) return false;

			foreach (DictionaryEntry kvp in sourceDic)
			{
				if (!targetDic.Contains(kvp.Key)) return false;

				var value = targetDic[kvp.Key];
				if (!kvp.Value.IsEquivalentTo(value)) return false;
			}

			return true;
#else
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
#endif
		}

		/// <summary>
		/// Testing using IEnumerable capabilities...
		/// </summary>
		static bool WithIEnumerable(object source, object target)
		{
#if OPTION1
			Type sourceType = source.GetType();
			Type targetType = target.GetType();

			if (!typeof(IEnumerable).IsAssignableFrom(sourceType)) return false;
			if (!typeof(IEnumerable).IsAssignableFrom(targetType)) return false;

			var sourceEnum = source as IEnumerable; var sourceList = new List<object>(); foreach (var entry in sourceEnum) sourceList.Add(entry);
			var targetEnum = target as IEnumerable; var targetList = new List<object>(); foreach (var entry in targetEnum) targetList.Add(entry);

			if (sourceList.Count != targetList.Count) return false;

			for (int i = 0; i < sourceList.Count; i++)
			{
				var sourceItem = sourceList[i];
				var targetItem = targetList[i];
				if (!sourceItem.IsEquivalentTo(targetItem)) return false;
			}

			return true;
#else
			var sourceEnum = source as IEnumerable; if (sourceEnum == null) return false;
			var targetEnum = target as IEnumerable; if (targetEnum == null) return false;

			var sourceIter = sourceEnum.GetEnumerator();
			var targetIter = targetEnum.GetEnumerator();

			while (sourceIter.MoveNext())
			{
				if (!targetIter.MoveNext()) return false;
				if (!sourceIter.Current.IsEquivalentTo(targetIter.Current)) return false;
			}
			if (targetIter.MoveNext()) return false;

			return true;
#endif
		}
	}
}
// ======================================================== 
