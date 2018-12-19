﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Fiffi
{
	public static class MetaExtensions
	{
		public static void AddTypeInfo(this IDictionary<string, string> meta, IEvent e)
		{
			var t = e.GetType();
			var typeProperties = new Dictionary<string, string>
			{
				{ "name", t.Name },
				{ "eventname", t.Name },
				{ "type.name", t.Name },
				{ "type.assemblyqualifiedname", t.AssemblyQualifiedName },
				{ "type.fullname", t.FullName },
				{ "type.version", e.GetType().Assembly.GetName().Version.ToString() }
			};
			typeProperties.ForEach(x => meta.TryAdd(x.Key, x.Value));
		}

		public static string GetStreamName(this IEvent @event) => @event.Meta[nameof(EventMetaData.StreamName).ToLower()];

		public static long GetVersion(this IEvent @event) => long.Parse(@event.Meta[nameof(EventMetaData.Version).ToLower()]);

		public static Guid EventId(this IEvent e) => Guid.Parse(e.Meta[nameof(EventMetaData.EventId).ToLower()]);

		public static bool HasCorrelation(this IEvent @event) => @event.Meta.ContainsKey(nameof(EventMetaData.CorrelationId).ToLower());
		public static Type GetEventType(this IDictionary<string, string> meta, Func<string, Type> f)
			=> f(meta["type.name"]);

		public static Guid GetCorrelation(this IEvent @event) =>
			 Guid.Parse(@event.Meta[nameof(EventMetaData.CorrelationId)]);

		public static void AddMetaData(this IDictionary<string, string> meta , long newVersion, string streamName, string aggregateName, Guid correlationId)
		{
			meta[nameof(EventMetaData.Version).ToLower()] = newVersion.ToString();
			meta[nameof(EventMetaData.StreamName).ToLower()] = streamName;
			meta[nameof(EventMetaData.AggregateName).ToLower()] = aggregateName;
			meta[nameof(EventMetaData.EventId).ToLower()] = Guid.NewGuid().ToString();
			meta[nameof(EventMetaData.CorrelationId).ToLower()] = correlationId.ToString();
		}

	}

	public class EventMetaData
	{
		public static readonly object CorrelationId;
		public static readonly object EventId;
		public static readonly object StreamName;
		public static readonly object AggregateName;
		public static readonly object Version;
	}
}
