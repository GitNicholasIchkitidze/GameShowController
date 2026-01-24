namespace GameController.FBService.Services
{

		public interface IAppMetrics
		{
			long IngressCount { get; }
			long EnqueueOk { get; }
			long EnqueueDropped { get; }
			long Dequeued { get; }
			long ProcessedOk { get; }
			long ProcessedFailed { get; }
			long GarbageMessages { get; }
			long NotInTimeUserMessages { get; }
			long RecsSavedInDB { get; }
			long InFlight { get; }

			void IncInFlight();
			void DecInFlight();
			void IncIngress();
			void IncEnqueueOk();
			void IncEnqueueDropped();
			void IncDequeued();
			void IncProcessedOk();
			void IncProcessedFailed();
			void IncGarbageMessages();
			void IncNotInTimeUserMessages();
			void IncRecsSavedInDB();

		object Snapshot();
		object Reset();
	}

}
