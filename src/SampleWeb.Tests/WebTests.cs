﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ServiceFabric.Mocks;
using Xunit;
using System.Threading.Tasks;
using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.AspNetCore.Builder;
using System.Linq;
using Xunit.Abstractions;
using System.Diagnostics;
using System.IO;
using SampleWeb.Order;

namespace SampleWeb.Tests
{
	public class WebTests
	{
		private HttpClient client;
		private TestContext context;
		private ITestOutputHelper output;

		public WebTests(ITestOutputHelper output)
		=> this.context = TestContextBuilder.Create((stateManager, storeFactory, queue) =>
		   {
			   this.output = output;

			   var orderModule = OrderModule.Initialize(stateManager, storeFactory, queue.Enqueue, events => Task.CompletedTask);
			   var module = CartModule.Initialize(stateManager, storeFactory, queue.Enqueue, events => Task.CompletedTask);

			   var server = new TestServer(
				  new WebHostBuilder()
				  .UseEnvironment("Development")
				  .UseStartup<Startup>()
				  .ConfigureTestServices(services =>
				  {
					  services.AddMvc();
					  services.AddSingleton<IReliableStateManager>(new MockReliableStateManager());
					  services.AddSingleton(module);
				  }));

			   this.client = server.CreateClient();

			   return new TestContext(given => stateManager.UseTransactionAsync(tx => given(storeFactory(tx))),
				   module.DispatchAsync, queue, module.WhenAsync, orderModule.WhenAsync);
		   });

		[Fact]
		public async Task HelloAsync()
		{
			var req = new HttpRequestMessage(new HttpMethod("GET"), "/");

			var res = await client.SendAsync(req);

			res.EnsureSuccessStatusCode();
			Assert.Contains("Hello", await res.Content.ReadAsStringAsync());
		}


		[Fact]
		public async Task AddItemAsync()
		{
			var req = new HttpRequestMessage(HttpMethod.Post, "/api/cart")
			{
				Content = new AddItemCommand(Guid.NewGuid()) { ItemId = Guid.NewGuid() }.ToContent(),
			};

			await this.context.WhenAsync(() => client.SendAsync(req));

			this.context.Then(events => Assert.True(events.OfType<ItemAddedEvent>().Happened()));
		}

		[Fact]
		public async Task CheckoutAsync()
		{
			var req = new HttpRequestMessage(HttpMethod.Post, "/api/cart")
			{
				Content = new AddItemCommand(Guid.NewGuid()) { ItemId = Guid.NewGuid() }.ToContent(),
			};

			await this.context.WhenAsync(() => client.SendAsync(req));

			this.context.Then((events, table) =>
			{
				this.output.WriteLine(table);
				events.OfType<ItemAddedEvent>().Happened();
			});
		}
	}
}
