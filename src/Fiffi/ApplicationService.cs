﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi
{
	public static class ApplicationService
	{
		public static async Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub)
			where TState : class, new()
		{
			var aggregateName = typeof(TState).Name.Replace("State", "Aggregate").ToLower();
			var streamName = $"{aggregateName}-{command.AggregateId}";
			var happend = await store.LoadEventStreamAsync(streamName, 0);
			var state = happend.Events.Rehydrate<TState>();
			var events = action(state);

			if (!events.Any())
				return;

			events
				.Where(x => x.Meta == null)
				.ForEach(x => x.Meta = new Dictionary<string, string>());

			events
				.ForEach(x => x
						.Tap(e => e.Meta.AddMetaData(happend.Version + 1, streamName, aggregateName, Guid.NewGuid())) //TODO correlation from command
						.Tap(e => e.Meta.AddTypeInfo(e))
					);

			await store.AppendToStreamAsync(streamName, events.Last().GetVersion(), events);
			await pub(events);
		}

		public static async Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
			where TState : class, new()
			=> await aggregateLocks.UseLockAsync(command.AggregateId, command.CorrelationId, pub, async (publisher) =>
				 await ExecuteAsync(store, command, action, publisher)
			);
	}
}
