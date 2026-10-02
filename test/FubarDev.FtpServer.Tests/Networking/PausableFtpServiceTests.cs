// <copyright file="PausableFtpServiceTests.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.Networking;

using Xunit;

namespace FubarDev.FtpServer.Tests.Networking
{
    public class PausableFtpServiceTests
    {
        [Fact]
        public async Task StartThenStop()
        {
            var service = new IdleService();

            await service.StartAsync(CancellationToken.None);
            Assert.Equal(FtpServiceStatus.Running, service.Status);

            await service.StopAsync(CancellationToken.None);
            Assert.Equal(FtpServiceStatus.Stopped, service.Status);
        }

        [Fact]
        public async Task StartWithCancelledTokenLeavesServiceStoppable()
        {
            var service = new IdleService();

            await InvokeThenRunLateCallbacksAsync(() => service.StartAsync(new CancellationToken(true)));

            await AssertStopsCleanlyAsync(service);
        }

        [Fact]
        public async Task PauseThenContinue()
        {
            var service = new IdleService();
            await service.StartAsync(CancellationToken.None);

            await service.PauseAsync(CancellationToken.None);
            Assert.Equal(FtpServiceStatus.Paused, service.Status);

            await service.ContinueAsync(CancellationToken.None);
            Assert.Equal(FtpServiceStatus.Running, service.Status);

            await service.StopAsync(CancellationToken.None);
            Assert.Equal(FtpServiceStatus.Stopped, service.Status);
        }

        [Fact]
        public async Task ContinueWithCancelledTokenLeavesServiceStoppable()
        {
            var service = new IdleService();
            await service.StartAsync(CancellationToken.None);
            await service.PauseAsync(CancellationToken.None);

            await InvokeThenRunLateCallbacksAsync(() => service.ContinueAsync(new CancellationToken(true)));

            await AssertStopsCleanlyAsync(service);
        }

        private static async Task InvokeThenRunLateCallbacksAsync(Func<Task> action)
        {
            // Holding back posted callbacks until the call has returned makes the late-report race deterministic.
            var deferred = new DeferringSynchronizationContext();
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(deferred);
            Task task;
            try
            {
                task = action();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Either outcome is fine; only a late callback throwing is the bug.
            }

            deferred.RunPending();
        }

        private static async Task AssertStopsCleanlyAsync(PausableFtpService service)
        {
            Assert.Contains(service.Status, new[] { FtpServiceStatus.Running, FtpServiceStatus.Stopped });

            await service.StopAsync(CancellationToken.None);
            Assert.Equal(FtpServiceStatus.Stopped, service.Status);
        }

        private sealed class IdleService : PausableFtpService
        {
            public IdleService()
                : base(CancellationToken.None)
            {
            }

            protected override Task ExecuteAsync(CancellationToken cancellationToken)
                => Task.Delay(Timeout.Infinite, cancellationToken);
        }

        private sealed class DeferringSynchronizationContext : SynchronizationContext
        {
            private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _pending =
                new ConcurrentQueue<(SendOrPostCallback Callback, object? State)>();

            public override void Post(SendOrPostCallback d, object? state) => _pending.Enqueue((d, state));

            public void RunPending()
            {
                while (_pending.TryDequeue(out var item))
                {
                    item.Callback(item.State);
                }
            }
        }
    }
}
