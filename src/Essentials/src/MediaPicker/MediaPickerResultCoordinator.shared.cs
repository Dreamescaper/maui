using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Maui.Media
{
	// Shared on purpose: this type only coordinates TaskCompletionSource state transitions and
	// does not depend on UIKit/PHPicker APIs, which keeps the iOS race-handling logic easier to test.
	internal sealed class MediaPickerResultCoordinator<TResult>
	{
		// Pending: the picker is still on screen and dismissal is allowed to complete as cancellation.
		// CompletionStarted: the user already selected media and async materialization is in flight.
		// Completed: a result/exception/cancellation already won and later callbacks must be ignored.
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
			// Cancellation is only valid while the picker is still pending. Once a real completion path
			// has started, dismissal/disposal callbacks must not override the in-flight selection.
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
				// This coordinator only arbitrates between a dismissal callback and a completion callback,
				// so state transitions are one-way and contention stays very small.
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
