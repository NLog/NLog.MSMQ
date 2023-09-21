// 
// Copyright (c) 2004-2021 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

namespace NLog.Targets
{
#if NETSTANDARD
    using MSMQ.Messaging;
#else
    using System.Messaging;
#endif
    using System.Text;
    using NLog.Config;
    using NLog.Layouts;

    /// <summary>
    /// Writes log message to the specified message queue handled by MSMQ.
    /// </summary>
    /// <seealso href="https://github.com/nlog/nlog/wiki/MessageQueue-target">Documentation on NLog Wiki</seealso>
    /// <example>
    /// <p>
    /// To set up the target in the <a href="https://github.com/NLog/NLog/wiki/Configuration-file">configuration file</a>, 
    /// use the following syntax:
    /// </p>
    /// <code lang="XML" source="examples/targets/Configuration File/MSMQ/Simple/NLog.config" />
    /// <p>
    /// You can use a single target to write to multiple queues (similar to writing to multiple files with the File target).
    /// </p>
    /// <code lang="XML" source="examples/targets/Configuration File/MSMQ/Multiple/NLog.config" />
    /// <p>
    /// To set up the log target programmatically use code like this:
    /// </p>
    /// <code lang="C#" source="examples/targets/Configuration API/MSMQ/Simple/Example.cs" />
    /// </example>
    [Target("MSMQ")]
    public class MessageQueueTarget : TargetWithLayout
    {
        private const MessagePriority DefaultMessagePriority = MessagePriority.Normal;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueueTarget"/> class.
        /// </summary>
        /// <remarks>
        /// The default value of the layout is: <code>${longdate}|${level:uppercase=true}|${logger}|${message:withexception=true}</code>
        /// </remarks>
        public MessageQueueTarget()
        {
            MessageQueueProxy = new MessageQueueProxy();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueueTarget"/> class.
        /// </summary>
        /// <remarks>
        /// The default value of the layout is: <code>${longdate}|${level:uppercase=true}|${logger}|${message:withexception=true}</code>
        /// </remarks>
        /// <param name="name">Name of the target.</param>
        public MessageQueueTarget(string name) : this()
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the name of the queue to write to.
        /// </summary>
        /// <remarks>
        /// To write to a private queue on a local machine use <c>.\private$\QueueName</c>.
        /// For other available queue names, consult MSMQ documentation.
        /// </remarks>
        /// <docgen category='Queue Options' order='10' />
        [RequiredParameter]
        public Layout Queue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether sending to a transactional queue using single-transaction-style.
        /// </summary>
        /// <docgen category='Queue Options' order='10' />
        public bool SingleTransaction { get; set; }

        /// <summary>
        /// Gets or sets the label to associate with each message.
        /// </summary>
        /// <remarks>
        /// By default no label is associated.
        /// </remarks>
        /// <docgen category='Queue Options' order='10' />
        public Layout Label { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use recoverable messages (with guaranteed delivery).
        /// </summary>
        /// <docgen category='Queue Options' order='10' />
        public bool Recoverable { get; set; }

        /// <summary>
        /// Gets or sets the encoding to be used when writing text to the queue.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Gets or sets a value indicating whether to use the XML format when serializing message.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public bool UseXmlEncoding { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to check if a queue exists before writing to it.
        /// </summary>
        /// <docgen category='Layout Options' order='11' />
        public bool CheckIfQueueExists { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create the queue if it doesn't exists.
        /// </summary>
        /// <docgen category='Queue Options' order='10' />
        public bool CreateQueueIfNotExists { get; set; }

        internal MessageQueueProxy MessageQueueProxy { get; set; }

        /// <summary>
        /// Writes the specified logging event to a queue specified in the Queue 
        /// parameter.
        /// </summary>
        /// <param name="logEvent">The logging event.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            var queue = RenderLogEvent(Queue, logEvent);
            if (string.IsNullOrEmpty(queue))
            {
                Common.InternalLogger.Debug("MessageQueueTarget: Failed sending message, because queue name is empty.");
                return;
            }

            if (CheckIfQueueExists || CreateQueueIfNotExists)
            {
                if (!IsFormatNameSyntax(queue) && !MessageQueueProxy.Exists(queue))
                {
                    if (CreateQueueIfNotExists)
                    {
                        Common.InternalLogger.Debug("MessageQueueTarget: Creating queue: {0}", queue);
                        MessageQueueProxy.Create(queue, SingleTransaction);
                    }
                    else
                    {
                        Common.InternalLogger.Debug("MessageQueueTarget: Skip sending message, because non-existing queue: {0}", queue);
                        return;
                    }
                }
            }

            var msg = PrepareMessage(logEvent);
            MessageQueueProxy.Send(queue, msg, SingleTransaction);
        }

        /// <summary>
        /// Prepares a message to be sent to the message queue.
        /// </summary>
        /// <param name="logEvent">The log event to be used when calculating label and text to be written.</param>
        /// <returns>The message to be sent.</returns>
        /// <remarks>
        /// You may override this method in inheriting classes
        /// to provide services like encryption or message 
        /// authentication.
        /// </remarks>
        protected virtual Message PrepareMessage(LogEventInfo logEvent)
        {
            var msg = new Message();
            var label = RenderLogEvent(Label, logEvent);
            if (!string.IsNullOrEmpty(label))
            {
                msg.Label = label;
            }

            msg.Recoverable = Recoverable;
            msg.Priority = DefaultMessagePriority;

            var body = RenderLogEvent(Layout, logEvent);
            if (UseXmlEncoding)
            {
                msg.Body = body;
            }
            else
            {
                var dataBytes = Encoding.GetBytes(body);
                msg.BodyStream.Write(dataBytes, 0, dataBytes.Length);
            }

            return msg;
        }

        private static bool IsFormatNameSyntax(string queue)
        {
            return queue.IndexOf('=') >= 0;
        }
    }

    internal class MessageQueueProxy
    {
        public virtual bool Exists(string queue)
        {
            return MessageQueue.Exists(queue);
        }

        public virtual void Create(string queue, bool singleTransaction)
        {
            MessageQueue.Create(queue, singleTransaction);
        }

        public virtual void Send(string queue, Message message, bool singleTransaction)
        {
            using (var mq = new MessageQueue(queue, QueueAccessMode.Send))
            {
                if (singleTransaction)
                    mq.Send(message, MessageQueueTransactionType.Single);
                else
                    mq.Send(message);
            }
        }
    }
}
