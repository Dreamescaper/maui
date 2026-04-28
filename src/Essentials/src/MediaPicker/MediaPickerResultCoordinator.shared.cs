using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Maui.Media
{
	internal sealed class MediaPickerResultCoordinator<TResult>
	{
		const int Pending = 0;
		const int CompletionStarted = 1;
		const int Completed = 2;

		int _state = Pending;

		internal bool TryBeginCompletion() =>
			Interlocked.CompareExchange(ref _state, CompletionStarted, Pending) == Pending;

		internal bool TrySetResult(TaskCompletionSource<TResult> taskCompletionSource, TResult result) =>
			TryComplete(taskCompletionSource.TrySetResult, result);

		internal bool TrySetException(TaskCompletionSource<TResult> taskCompletionSource, Exception exception) =>
			TryComplete(taskCompletionSource.TrySetException, exception);

		internal bool TrySetCanceled(TaskCompletionSource<TResult> taskCompletionSource, TResult canceledResult)
		{
			if (Interlocked.CompareExchange(ref _state, Completed, Pending) != Pending)
			{
				return false;
			}

			return taskCompletionSource.TrySetResult(canceledResult);
		}

		bool TryComplete<T>(Func<T, bool> completionAction, T value)
		{
			while (true)
			{
				var state = Volatile.Read(ref _state);

				if (state == Completed)
				{
					return false;
				}

				if (Interlocked.CompareExchange(ref _state, Completed, state) == state)
				{
					return completionAction(value);
				}
			}
		}
	}
}
