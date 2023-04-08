#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Mochineko.Relent.Resilience.Bulkhead;
using Mochineko.Relent.UncertainResult;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mochineko.Relent.Resilience.Tests
{
    [TestFixture]
    internal sealed class BulkheadTest
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        [RequiresPlayMode(false)]
        public async Task PrimitiveBulkheadWithNoValueTest(int maxParallelization)
        {
            IBulkheadPolicy policy = BulkheadFactory.Bulkhead(maxParallelization);

            var taskList = new List<Task<IUncertainResult>>();
            for (var i = 0; i < maxParallelization + 1; i++)
            {
                var task = policy.ExecuteAsync(
                    execute: cancellationToken => WaitUtility.WaitAndSucceed(
                        TimeSpan.FromSeconds(0.1d),
                        cancellationToken),
                    cancellationToken: CancellationToken.None);

                taskList.Add(task);

                // Does not wait to complete.
#pragma warning disable CS4014
                Task.Run(() => task.ConfigureAwait(false));
#pragma warning restore CS4014
            }

            policy.RemainingParallelizationCount.Should().Be(0,
                because: "Bulkhead is fulfilled.");

            await Task.Delay(TimeSpan.FromSeconds(0.11d));

            policy.RemainingParallelizationCount.Should().Be(maxParallelization - 1);

            for (var i = 0; i < maxParallelization; i++)
            {
                taskList[i].Status.Should().Be(TaskStatus.RanToCompletion);
            }

            taskList[maxParallelization].Status.Should().Be(TaskStatus.WaitingForActivation);

            await Task.Delay(TimeSpan.FromSeconds(1d));

            taskList[maxParallelization].Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        [RequiresPlayMode(false)]
        public async Task PrimitiveBulkheadTest(int maxParallelization)
        {
            IBulkheadPolicy<int> policy = BulkheadFactory.Bulkhead<int>(maxParallelization);

            var taskList = new List<Task<IUncertainResult<int>>>();
            for (var i = 0; i < maxParallelization + 1; i++)
            {
                var task = policy.ExecuteAsync(
                    execute: cancellationToken => WaitUtility.WaitAndSucceed(
                        TimeSpan.FromSeconds(0.1d),
                        cancellationToken,
                        1),
                    cancellationToken: CancellationToken.None);

                taskList.Add(task);

                // Does not wait to complete.
#pragma warning disable CS4014
                Task.Run(() => task.ConfigureAwait(false));
#pragma warning restore CS4014
            }

            policy.RemainingParallelizationCount.Should().Be(0,
                because: "Bulkhead is fulfilled.");

            await Task.Delay(TimeSpan.FromSeconds(0.11d));

            policy.RemainingParallelizationCount.Should().Be(maxParallelization - 1);

            for (var i = 0; i < maxParallelization; i++)
            {
                taskList[i].Status.Should().Be(TaskStatus.RanToCompletion);
            }

            taskList[maxParallelization].Status.Should().Be(TaskStatus.WaitingForActivation);

            await Task.Delay(TimeSpan.FromSeconds(1d));

            taskList[maxParallelization].Status.Should().Be(TaskStatus.RanToCompletion);
        }
    }
}