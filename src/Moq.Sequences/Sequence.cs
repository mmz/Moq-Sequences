using System;

namespace Moq.Sequences
{
    public class Sequence : Loop
    {
        private static readonly Times anyNumberOfTimes = Times.Between(0, Int32.MaxValue, Range.Inclusive);
        private bool invocationExceptionThrown;

        private static Sequence Instance
        {
            get => Sequence.ContextMode.ActiveSequence;
            set => Sequence.ContextMode.ActiveSequence = value;
        }

        private static SequenceContextMode contextMode = SequenceContextMode.Thread;

        /// <summary>
        /// Gets or sets the context mode that determines how sequences behave in the presence of
        /// concurrency or asynchrony. This is a global setting that will affect all sequences.
        /// It is recommended that you set the context mode only once. The default is
        /// <see cref="SequenceContextMode.Thread"/>.
        /// </summary>
        public static SequenceContextMode ContextMode
        {
            get => Sequence.contextMode;
            set => Sequence.contextMode = value ?? throw new ArgumentNullException(nameof(value));
        }

        private Sequence() : base(Times.AtMostOnce())
        {
            Instance = this;
        }

        public static Sequence Create()
        {
            EnsureNoActiveSequence();
            return new Sequence();
        }

        public static Loop Loop()
        {
            EnsureActiveSequence("Creating a loop");
            return Instance.CreateLoop(anyNumberOfTimes);
        }

        public static Loop Loop(Times times)
        {
            EnsureActiveSequence("Creating a loop");
            return Instance.CreateLoop(times);
        }

        internal static Step Step(object setup, Times expectedCalls)
        {
            EnsureActiveSequence("InSequence()");
            return Instance.CreateStep(setup, expectedCalls);
        }

        internal static void Record(Step calledStep)
        {
            EnsureActiveSequence("Mock invocation");
            Instance.RecordCall(calledStep);
        }

        public static void EnsureActiveSequence(string context)
        {
            if (Instance == null)
                throw new SequenceUsageException(context + " can only be called with an active MockSequence created with MockSequence.Create()");
        }

        private static void EnsureNoActiveSequence()
        {
            if (Instance != null)
                throw new SequenceUsageException("Cannot have more than one MockSequence per thread");
        }

        internal override bool RecordCall(Step calledStep)
        {
            try
            {
                return base.RecordCall(calledStep);
            }
            catch (Exception)
            {
                invocationExceptionThrown = true;
                throw;
            }
        }

        protected override void DoDisposalChecks()
        {
            Instance = null;

            if (invocationExceptionThrown)
                return;

            base.DoDisposalChecks();

            EnsureComplete("At end of sequence");
        }
    }
}