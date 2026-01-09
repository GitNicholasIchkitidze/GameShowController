using System.Threading;

namespace GameController.FBService.Services
{
	// ADDED (2025-12): Thread-safe counters (Interlocked).
	public class AppMetrics : IAppMetrics
	{
		private long _ingress;
		private long _enqueueOk;
		private long _enqueueDropped;
		private long _dequeued;
		private long _processedOk;
		private long _processedFailed;
		private long _garbageMessages;
		private long _notInTimeUserMessages;
		private long _recsSavedInDB;
		private long _inFlight;

		public long IngressCount => Interlocked.Read(ref _ingress);
		public long EnqueueOk => Interlocked.Read(ref _enqueueOk);
		public long EnqueueDropped => Interlocked.Read(ref _enqueueDropped);
		public long Dequeued => Interlocked.Read(ref _dequeued);
		public long ProcessedOk => Interlocked.Read(ref _processedOk);
		public long ProcessedFailed => Interlocked.Read(ref _processedFailed);
		public long GarbageMessages => Interlocked.Read(ref _garbageMessages);
		public long NotInTimeUserMessages => Interlocked.Read(ref _notInTimeUserMessages);
		public long RecsSavedInDB => Interlocked.Read(ref _recsSavedInDB);

		public void IncIngress() => Interlocked.Increment(ref _ingress);
		public void IncEnqueueOk() => Interlocked.Increment(ref _enqueueOk);
		public void IncEnqueueDropped() => Interlocked.Increment(ref _enqueueDropped);
		public void IncDequeued() => Interlocked.Increment(ref _dequeued);
		public void IncProcessedOk() => Interlocked.Increment(ref _processedOk);
		public void IncProcessedFailed() => Interlocked.Increment(ref _processedFailed);
		public void IncGarbageMessages() => Interlocked.Increment(ref _garbageMessages);
		public void IncNotInTimeUserMessages() => Interlocked.Increment(ref _notInTimeUserMessages);
		public void IncRecsSavedInDB() => Interlocked.Increment(ref _recsSavedInDB);

		public long InFlight => Interlocked.Read(ref _inFlight);

		public void IncInFlight() => Interlocked.Increment(ref _inFlight);

		public void DecInFlight()
		{
			var v = Interlocked.Decrement(ref _inFlight);
			if (v < 0) Interlocked.Exchange(ref _inFlight, 0); // safety
		}

		public object Snapshot()
		{
			return new
			{
				IngressCount,
				EnqueueOk,
				EnqueueDropped,
				Dequeued,
				ProcessedOk,
				ProcessedFailed,
				GarbageMessages,
				NotInTimeUserMessages,
				RecsSavedInDB,
				InFlight
			};
		}

		public object Reset()
		{

			Interlocked.Exchange(ref _ingress, 0);
			Interlocked.Exchange(ref _enqueueOk, 0);
			Interlocked.Exchange(ref _enqueueDropped, 0);
			Interlocked.Exchange(ref _dequeued, 0);
			Interlocked.Exchange(ref _processedOk, 0);
			Interlocked.Exchange(ref _processedFailed, 0);
			Interlocked.Exchange(ref _garbageMessages, 0);
			Interlocked.Exchange(ref _notInTimeUserMessages, 0);
			Interlocked.Exchange(ref _recsSavedInDB, 0);
			
			return Snapshot();
		}


	}
}
