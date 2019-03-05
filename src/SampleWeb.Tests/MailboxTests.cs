﻿using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SampleWeb.Tests
{
	public class MailboxTests
	{
		private IReliableStateManager stateManager;
		private HttpClient client;
		private TestContext context;
		private List<IEvent> events = new List<IEvent>();

		public MailboxTests()
		=> this.context = TestContextBuilder.Create((stateManager, storeFactory, queue) =>
		{
			var server = new TestServer(
		   new WebHostBuilder()
		   .UseEnvironment("Development")
		   .UseStartup<Startup>()
		   .ConfigureTestServices(services =>
		   {
			   services.AddSingleton(stateManager);
			   services.Configure<MailboxOptions>(opt =>
			   {
				   opt.Serializer = Serialization.FabricSerialization(); //TODO JSON
				   opt.Deserializer = Serialization.FabricDeserialization(); 
			   });
			   services.AddMailboxes(sp => new Func<IEvent, Task>[] { e => {
				   events.Add(e);
				   return Task.CompletedTask;
			   } });
			   services.AddMvc();
		   }));

			this.stateManager = stateManager;

			this.client = server.CreateClient();

			return new TestContext(given => stateManager.UseTransactionAsync(tx => given(storeFactory(tx))),
				c => Task.CompletedTask, queue, e => Task.CompletedTask);
		});

		[Fact]
		public async Task InboxProcessorReadsFromInboxAsync()
		{
			await stateManager.EnqueuAsync(new TestEvent(Guid.NewGuid()), Serialization.FabricSerialization(), "inbox");
			await Task.Delay(500); //TODO await a task set by inbox delegate
			Assert.True(this.events.Any());
		}

		public class TestEvent : IEvent
		{
			public TestEvent(Guid id)
			{
				this.AggregateId = id;
				this.Meta["eventid"] = Guid.NewGuid().ToString();
			}

			public Guid AggregateId { get; set; }

			public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
		}
	}
}
