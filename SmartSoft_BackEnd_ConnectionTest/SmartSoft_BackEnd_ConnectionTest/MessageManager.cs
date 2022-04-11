using System.Collections.Concurrent;
using UPMessageControlCore;

namespace SmartSoft_BackEnd_ConnectionTest
{
    class DeqMessageEventArgs : EventArgs
    {
        public DeqMessageEventArgs(string Msg, IMessageParameters? messageparams)
            : base()
        {
            Message = Msg;
            MessageParam = messageparams;
        }

        public string Message { get; }
        public IMessageParameters? MessageParam { get; }
    }

    class MessageManager
    {
        private static Lazy<MessageManager> instance = new Lazy<MessageManager>();
        public static MessageManager Instance => instance.Value;

        private class ReceivedMessage
        {
            public IMessageParameters? MessageParam;
            public string Message = "";
        }

        public event EventHandler<DeqMessageEventArgs>? GetMessageParams;

        BlockingCollection<ReceivedMessage> queue;
        CancellationTokenSource source = new CancellationTokenSource();

        public MessageManager()
        {
            queue = new BlockingCollection<ReceivedMessage>(new ConcurrentBag<ReceivedMessage>(), 30);
        }

        public void initialize()
        {
            //Start
            _ = Start();
        }

        public bool bRunFlag { get; set; } = false;

        public void AddMessageParam(string message, IMessageParameters messageparams)
        {

            ReceivedMessage rm = new ReceivedMessage();
            rm.Message = message;
            rm.MessageParam = messageparams;

            queue?.TryAdd(rm);
        }

        public void Stop()
        {
            source.Cancel();

            queue?.CompleteAdding();
        }

        private async Task Start()
        {
            try
            {
                //queue = new BlockingCollection<ReceivedMessage>(new ConcurrentBag<ReceivedMessage>(), 30);

                await Task.Run(() => Dequeue_Async() );
            }
            finally
            {
                queue?.Dispose();
            }
        }

        private void Dequeue_Async()
        {
            while (true)
            {
                if (queue.IsCompleted) break;

                ReceivedMessage? Param;
                try
                {
                    if (queue.TryTake(out Param, -1, source.Token))
                    {
                        DeqMessageEventArgs e = new DeqMessageEventArgs(Param.Message, Param.MessageParam);
                        OnDeqMessage(e);
                    }
                    else
                    {

                    }
                }
                catch (OperationCanceledException e)
                {
                    break;
                }
            }
        }

        private void OnDeqMessage(DeqMessageEventArgs e)
        {
            GetMessageParams?.Invoke(this, e);
        }

    }
}
