#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Mochineko.Result;
using Mochineko.UncertainResult;

namespace Mochineko.Resilience
{
    internal sealed class RetryPolicy<TResult>
        : IPolicy<TResult>
    {
        private readonly int permittedRetryCount;
        private readonly Func<int, TimeSpan> waitDurationProvider;

        public RetryPolicy(int permittedRetryCount)
        {
            this.permittedRetryCount = permittedRetryCount;
            waitDurationProvider = retryAttempt => TimeSpan.Zero;
        }

        public RetryPolicy(int permittedRetryCount, TimeSpan duration)
        {
            this.permittedRetryCount = permittedRetryCount;
            waitDurationProvider = retryAttempt => duration;
        }

        public RetryPolicy(int permittedRetryCount, Func<int, TimeSpan> durationProvider)
        {
            this.permittedRetryCount = permittedRetryCount;
            waitDurationProvider = durationProvider ?? throw new ArgumentNullException(nameof(durationProvider));
        }

        public async Task<IResult<TResult>> ExecuteAsync(
            Func<CancellationToken, Task<IUncertainResult<TResult>>> execute,
            CancellationToken cancellationToken)
        {
            var retryCount = 0;

            while (retryCount < permittedRetryCount)
            {
                var result = await execute.Invoke(cancellationToken);
                if (result is IUncertainSuccessResult<TResult> success)
                {
                    return ResultFactory.Succeed(success.Result);
                }
                else if (result is IUncertainRetryableResult<TResult> retryable)
                {
                    retryCount++;

                    var waitResult = await CertainWait
                        .Wait(waitDurationProvider.Invoke(retryCount), cancellationToken);
                    if (waitResult is IFailureResult cancelled)
                    {
                        return ResultFactory.Fail<TResult>(cancelled.Message);
                    }
                }
                else if (result is IUncertainFailureResult<TResult> failure)
                {
                    return ResultFactory.Fail<TResult>(failure.Message);
                }
                else
                {
                    // Unexpected
                    throw new UncertainResultPatternMatchException(nameof(result));
                }
            }

            // Over permitted retry count
            return ResultFactory.Fail<TResult>(
                "Failed to retry because retry count was over permitted count.");
        }
    }
}