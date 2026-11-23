using Microsoft.Maui;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Xunit;

namespace RateZero.Tests;

public class RateZeroTests
{
	// Stands in for CADisplayLink / ValueAnimator / CompositionTarget.Rendering: the test fires frames by hand.
	sealed class ManualTicker : ITicker
	{
		public bool IsRunning { get; private set; }
		public bool SystemEnabled => true;
		public int MaxFps { get; set; } = 60;
		public Action Fire { get; set; }
		public void Start() => IsRunning = true;
		public void Stop() => IsRunning = false;
	}

	sealed class Animatable : IAnimatable
	{
		public void BatchBegin() { }
		public void BatchCommit() { }
	}

	sealed class ImmediateDispatcher : IDispatcher
	{
		public bool IsDispatchRequired => false;
		public bool Dispatch(Action action) { action(); return true; }
		public bool DispatchDelayed(TimeSpan delay, Action action) { action(); return true; }
		public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
	}

	sealed class ImmediateDispatcherProvider : IDispatcherProvider
	{
		public IDispatcher GetForCurrentThread() => new ImmediateDispatcher();
	}

	// Fires exactly one frame by hand; the exception (if any) is captured, not rethrown.
	static (Exception Thrown, int ValueUpdates) AnimateAndFireOneFrame(uint rate)
	{
		DispatcherProvider.SetCurrent(new ImmediateDispatcherProvider());

		var ticker = new ManualTicker();
		using var manager = new AnimationManager(ticker);
		var owner = new Animatable();
		var valueUpdates = 0;

		// The call site must succeed: rate 0 is accepted without any exception.
		owner.Animate("RateZero", progress => progress, _ => valueUpdates++, rate: rate, length: 250, animationManager: manager);
		Assert.True(ticker.IsRunning, "Animate should have started the ticker.");

		Thread.Sleep(20);

		try
		{
			ticker.Fire();
			return (null, valueUpdates);
		}
		catch (Exception ex)
		{
			return (ex, valueUpdates);
		}
	}

	// A passing test prints nothing, so markers and the exception text go to files that the CI script asserts on and shows.
	static void WriteMarker(string line) => AppendResultFile("markers.txt", line + Environment.NewLine);

	static void WriteResultFile(string fileName, string content) => File.WriteAllText(ResultPath(fileName), content);

	static void AppendResultFile(string fileName, string content) => File.AppendAllText(ResultPath(fileName), content);

	static string ResultPath(string fileName)
	{
		var dir = Environment.GetEnvironmentVariable("RESULTS_DIR") ?? "results";
		Directory.CreateDirectory(dir);
		return Path.Combine(dir, fileName);
	}

	[Fact]
	public void Bug_RateZero_ThrowsDivideByZeroFromTickerCallback()
	{
		var (thrown, valueUpdates) = AnimateAndFireOneFrame(rate: 0);

		Assert.NotNull(thrown);
		Assert.IsType<DivideByZeroException>(thrown);
		Assert.Contains("Microsoft.Maui.Controls.Tweener.Step", thrown.StackTrace);
		Assert.Equal(1, valueUpdates);
		WriteResultFile("bug-exception.txt", thrown.ToString());
		WriteMarker($"MARKER BUG rate=0 threw {thrown.GetType().FullName} valueUpdates={valueUpdates}");
	}

	[Fact]
	public void Control_RateOne_DoesNotThrow()
	{
		var (thrown, valueUpdates) = AnimateAndFireOneFrame(rate: 1);

		Assert.Null(thrown);
		Assert.Equal(2, valueUpdates);
		WriteMarker($"MARKER CONTROL rate=1 clean valueUpdates={valueUpdates}");
	}

	[Fact]
	public void Fix_RateZeroClampedToOne_DoesNotThrow()
	{
		// The recommended fix (Rate = Math.Max(rate, 1u) in Tweener's constructor) cannot be applied from user code,
		// because Tweener is internal. The caller-side equivalent is clamping before calling Animate.
		const uint requestedRate = 0;
		var (thrown, valueUpdates) = AnimateAndFireOneFrame(rate: Math.Max(requestedRate, 1u));

		Assert.Null(thrown);
		Assert.Equal(2, valueUpdates);
		WriteMarker($"MARKER FIX clamped rate clean valueUpdates={valueUpdates}");
	}
}
