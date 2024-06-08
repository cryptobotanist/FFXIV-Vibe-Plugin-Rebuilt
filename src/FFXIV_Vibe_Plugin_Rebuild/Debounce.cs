using System;
using System.Threading;

public static class Debounce
{
	public static Action<T> Create<T>(Action<T> action, int milliseconds)
	{
		CancellationTokenSource cancelToken = null;
		T lastArg = default(T);
		object lockObj = new object();
		return delegate(T arg)
		{
			lock (lockObj)
			{
				lastArg = arg;
				cancelToken?.Cancel();
				cancelToken = new CancellationTokenSource();
			}
			CancellationToken token = cancelToken.Token;
			ThreadPool.QueueUserWorkItem(delegate
			{
				Thread.Sleep(milliseconds);
				if (!token.IsCancellationRequested)
				{
					lock (lockObj)
					{
						if (!token.IsCancellationRequested)
						{
							action(lastArg);
						}
					}
				}
			});
		};
	}

	public static Action Create(Action action, int milliseconds)
	{
		CancellationTokenSource cancelToken = null;
		object lockObj = new object();
		return delegate
		{
			lock (lockObj)
			{
				cancelToken?.Cancel();
				cancelToken = new CancellationTokenSource();
			}
			CancellationToken token = cancelToken.Token;
			ThreadPool.QueueUserWorkItem(delegate
			{
				Thread.Sleep(milliseconds);
				if (!token.IsCancellationRequested)
				{
					lock (lockObj)
					{
						if (!token.IsCancellationRequested)
						{
							action();
						}
					}
				}
			});
		};
	}
}
