using System;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Xunit;

namespace Tests
{
	public class MediaPickerResultCoordinator_Tests
	{
		[Fact]
		public async Task Cancellation_Does_Not_Win_After_Completion_Starts()
		{
			var tcs = new TaskCompletionSource<string>();
			var coordinator = new MediaPickerResultCoordinator<string>();

			Assert.True(coordinator.TryBeginCompletion());
			Assert.False(coordinator.TrySetCanceled(tcs, "cancelled"));
			Assert.True(coordinator.TrySetResult(tcs, "picked"));

			Assert.Equal("picked", await tcs.Task);
		}

		[Fact]
		public async Task Cancellation_Wins_While_Pending()
		{
			var tcs = new TaskCompletionSource<string>();
			var coordinator = new MediaPickerResultCoordinator<string>();

			Assert.True(coordinator.TrySetCanceled(tcs, "cancelled"));
			Assert.False(coordinator.TrySetResult(tcs, "picked"));

			Assert.Equal("cancelled", await tcs.Task);
		}

		[Fact]
		public async Task Exception_Can_Finish_After_Completion_Starts()
		{
			var tcs = new TaskCompletionSource<string>();
			var coordinator = new MediaPickerResultCoordinator<string>();
			var exception = new InvalidOperationException("boom");

			Assert.True(coordinator.TryBeginCompletion());
			Assert.True(coordinator.TrySetException(tcs, exception));

			var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => tcs.Task);
			Assert.Same(exception, thrown);
		}
	}
}
